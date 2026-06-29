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

// Proves the home page renders the promo block and the dual cards FROM THE
// DATABASE (not the old hardcoded resource strings). Uses an in-memory DB so we
// can inject distinct markers — the seeded values equal the old static text, so
// only an injected value can distinguish DB-driven rendering.
public class HomeSectionsRenderTests : IClassFixture<HomeSectionsRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "home-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public HomeSectionsRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Home_RendersPromoAndCards_FromDatabase()
    {
        // Startup seeding populated the promo + cards into the in-memory DB.
        // Overwrite them with distinct markers, then render the home page.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var promo = await db.PromoSections.FirstAsync();
            promo.Heading = new LocalizedText { En = "ZZZ-PROMO-MARK", Ar = "ZZZ-PROMO-MARK" };
            var card = await db.DualCards.OrderBy(c => c.SortOrder).FirstAsync();
            card.Title = new LocalizedText { En = "ZZZ-CARD-MARK", Ar = "ZZZ-CARD-MARK" };
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-PROMO-MARK", html);   // promo heading came from the DB
        Assert.Contains("ZZZ-CARD-MARK", html);    // a dual card title came from the DB
    }
}
