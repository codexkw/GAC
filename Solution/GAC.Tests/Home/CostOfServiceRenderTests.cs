using System;
using System.Threading.Tasks;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

// Proves /cost-of-service renders the structured matrix FROM THE DATABASE (not the
// old raw-HTML blob): title, model columns, price cells, footer — and Arabic chrome.
public class CostOfServiceRenderTests : IClassFixture<CostOfServiceRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "cos-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public CostOfServiceRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task CostOfService_RendersMatrix_FromDatabase()
    {
        var html = await (await _factory.CreateClient().GetAsync("/cost-of-service")).Content.ReadAsStringAsync();

        Assert.Contains("Cost of Service", html);          // seeded title
        Assert.Contains("GS4 Max", html);                  // a model column header
        Assert.Contains("5,000 KM/6 Month", html);         // an interval row label
        Assert.Contains("Kuwaiti Dinars", html);           // footer line
        Assert.Contains(">525<", html);                    // a price cell value
    }

    [Fact]
    public async Task CostOfService_LocalizesToArabic()
    {
        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");

        var raw = await (await client.GetAsync("/cost-of-service")).Content.ReadAsStringAsync();
        // Razor HTML-encodes non-ASCII (.Localize() strings) to numeric entities; decode to compare.
        var html = System.Net.WebUtility.HtmlDecode(raw);

        Assert.Contains("تكلفة", html);          // Arabic title
        Assert.Contains("أشهر", html);           // Arabic interval label
        Assert.Contains("الدينار", html);        // Arabic footer
    }
}
