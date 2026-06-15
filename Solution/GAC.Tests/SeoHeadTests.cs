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
        Assert.Matches("<title>[^<]*GS8[^<]*GAC Mutawa Alkadi</title>", html);
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
}
