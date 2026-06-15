using System.Net;
using Xunit;

namespace GAC.Tests;

public class AnalyticsTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public AnalyticsTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task NoAnalyticsId_RendersNoTrackingSnippet()
    {
        var res = await _factory.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("googletagmanager.com", html);
        Assert.DoesNotContain("gtag(", html);
    }
}
