using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GAC.Tests;

public class HomePageSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomePageSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

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
}
