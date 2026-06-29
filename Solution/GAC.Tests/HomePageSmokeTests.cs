using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GAC.Tests;

// Boots the app in the Development environment so it loads the (gitignored)
// appsettings.Development.json that holds the real DB connection string.
// The committed appsettings.json only carries a placeholder password.
public class DevWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Development");
}

public class HomePageSmokeTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;

    public HomePageSmokeTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_Home_ReturnsOk_AndRendersChrome()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("gac-header", html);          // header partial rendered
        Assert.Contains("lang=\"en\"", html);          // default culture
        Assert.Contains("dir=\"ltr\"", html);
    }

    [Fact]
    public async Task Get_Home_WithArabicCookie_RendersRtl()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("lang=\"ar\"", html);
    }

    [Fact]
    public async Task Home_RendersVehiclesAndNews_FromDb()
    {
        var client = _factory.CreateClient();
        var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("GS8", html);            // a seeded vehicle name
        Assert.Contains("data-tab-panel", html); // model-strip per-category panels rendered
        Assert.Contains("/news/", html);         // a news card link
    }
}
