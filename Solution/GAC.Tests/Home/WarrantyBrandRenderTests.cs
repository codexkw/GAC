using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

public class WarrantyBrandRenderTests : IClassFixture<WarrantyBrandRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "warr-brand-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public WarrantyBrandRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task RendersBrandTable_FromDatabase()
    {
        var html = await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();
        Assert.Contains("GAC", html);
        Assert.Contains("Chevrolet", html);
        Assert.Contains("Manufacturer Warranty", html);     // seeded header
        Assert.Contains("150,000 KM", html);                // a cell value
    }

    [Fact]
    public async Task PolicyLink_RendersOnlyForSafeSchemes()
    {
        async Task<string> GetWithPolicyUrl(string url)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var row = await db.WarrantyBrandRows.OrderBy(r => r.SortOrder).FirstAsync();
                row.PolicyUrl = url;
                await db.SaveChangesAsync();
            }
            return await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();
        }

        var bad = await GetWithPolicyUrl("javascript:alert(document.cookie)");
        Assert.DoesNotContain("javascript:alert", bad);

        var ok = await GetWithPolicyUrl("https://example.com/policy.pdf");
        Assert.Contains("https://example.com/policy.pdf", ok);
    }

    [Fact]
    public async Task LocalizesHeadersToArabic()
    {
        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");

        var raw = await (await client.GetAsync("/warranty")).Content.ReadAsStringAsync();
        var html = System.Net.WebUtility.HtmlDecode(raw);   // Razor encodes non-ASCII .Localize() output
        Assert.Contains("ضمان المصنّع", html);   // Arabic "Manufacturer Warranty" header
    }
}
