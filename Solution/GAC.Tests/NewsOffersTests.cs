using System.Net;
using Xunit;

namespace GAC.Tests;

public class NewsOffersTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public NewsOffersTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task News_ListsArticles()
    {
        var html = await (await _factory.CreateClient().GetAsync("/news")).Content.ReadAsStringAsync();
        Assert.Contains("/news/gac-empow-2026-high-performance-sports-sedan", html);
    }

    [Fact]
    public async Task NewsDetail_Renders200()
    {
        var res = await _factory.CreateClient().GetAsync("/news/gac-empow-2026-high-performance-sports-sedan");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task NewsDetail_UnknownSlug_Returns404()
    {
        var res = await _factory.CreateClient().GetAsync("/news/no-such-article");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Offers_Renders200()
    {
        var res = await _factory.CreateClient().GetAsync("/offers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
