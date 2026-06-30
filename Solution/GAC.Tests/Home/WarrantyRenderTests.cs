using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

// Proves /warranty renders its structured fields FROM THE DATABASE, the cars grid
// dynamically FROM the visible Vehicles (with a per-vehicle booklet link), and the
// structured Extended-Warranty brand table (seeded from the canonical table).
// Hermetic in-memory DB.
public class WarrantyRenderTests : IClassFixture<WarrantyRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "warranty-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public WarrantyRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Warranty_RendersStructuredFields_DynamicCars_AndTable()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var w = await db.WarrantyPages.FirstAsync();
            w.Heading = new LocalizedText { En = "ZZZ-WARR-HEAD", Ar = "ZZZ-WARR-HEAD" };
            var v = await db.Vehicles.OrderBy(x => x.SortOrder).FirstAsync();
            v.IsVisible = true;
            v.Name = new LocalizedText { En = "ZZZ-CAR", Ar = "ZZZ-CAR" };
            v.WarrantyBookletPdf = "/zzz-booklet.pdf";
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-WARR-HEAD", html);          // structured heading from DB
        Assert.Contains("ZZZ-CAR", html);                // dynamic car name from Vehicles
        Assert.Contains("/zzz-booklet.pdf", html);       // per-vehicle booklet link
        Assert.Contains("Manufacturer Warranty", html);  // structured brand table header (seeded)
        Assert.Contains("Cadillac", html);               // a seeded brand row
    }
}
