using System.Net;
using System.Xml.Linq;
using Xunit;

namespace GAC.Tests;

public class SitemapRobotsTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public SitemapRobotsTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Sitemap_ReturnsXmlWithKnownUrls_ExcludesHidden()
    {
        var res = await _factory.CreateClient().GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("application/xml", res.Content.Headers.ContentType!.ToString());

        var body = await res.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(body); // must be well-formed
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();

        Assert.Contains(locs, l => l.EndsWith("/"));        // home
        Assert.Contains(locs, l => l.EndsWith("/models"));
        Assert.Contains(locs, l => l.EndsWith("/gs8"));     // a visible vehicle
        Assert.DoesNotContain(locs, l => l.EndsWith("/aion-v"));  // hidden vehicle excluded
        Assert.All(locs, l => Assert.StartsWith("http", l)); // absolute
    }

    [Fact]
    public async Task Robots_DisallowsAdminAndPointsToSitemap()
    {
        var res = await _factory.CreateClient().GetAsync("/robots.txt");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("text/plain", res.Content.Headers.ContentType!.ToString());

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Disallow: /admin", body);
        Assert.Matches(@"Sitemap:\s+https?://[^\s]+/sitemap\.xml", body);
    }
}
