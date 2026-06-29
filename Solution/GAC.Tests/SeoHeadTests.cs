using System.Net;
using Xunit;

namespace GAC.Tests;

public class SeoHeadTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public SeoHeadTests(DevWebApplicationFactory factory) => _factory = factory;

    private async Task<string> GetHtml(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/models")]
    [InlineData("/gs8")]
    [InlineData("/about")]
    [InlineData("/contact-us")]
    public async Task PublicPages_EmitCanonicalAndOpenGraph(string url)
    {
        var html = await GetHtml(url);
        Assert.Contains("rel=\"canonical\"", html);
        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:url\"", html);
        Assert.Contains("name=\"twitter:card\"", html);
    }

    [Fact]
    public async Task VehiclePage_TitleUsesVehicleName()
    {
        var html = await GetHtml("/gs8");
        Assert.Matches("<title>[^<]*GS8[^<]*GAC MUTAWAALKAZI</title>", html);
    }

    [Fact]
    public async Task NotFound_IsNoindex()
    {
        var res = await _factory.CreateClient().GetAsync("/this-slug-does-not-exist-zzz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("name=\"robots\"", html);
        Assert.Contains("noindex", html);
    }

    [Fact]
    public async Task Home_EmitsAutoDealerJsonLd()
    {
        var html = await GetHtml("/");
        Assert.Contains("application/ld+json", html);
        Assert.Contains("\"@type\":\"AutoDealer\"", html);
    }

    [Fact]
    public async Task VehiclePage_EmitsCarJsonLd()
    {
        var html = await GetHtml("/gs8");
        Assert.Contains("\"@type\":\"Car\"", html);
    }

    [Fact]
    public async Task JsonLd_DoesNotContainRawScriptClose()
    {
        var html = await GetHtml("/gs8");
        var idx = html.IndexOf("application/ld+json", System.StringComparison.Ordinal);
        Assert.True(idx >= 0);
        Assert.Contains("\"@context\":\"https://schema.org\"", html);
    }
}
