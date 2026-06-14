using System.Net;
using Xunit;

namespace GAC.Tests;

public class ChromeTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ChromeTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_RendersDbDrivenChrome()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("gac-header", html);
        Assert.Contains("megamenu__item", html);
        Assert.Contains("href=\"/gs8\"", html);     // a visible vehicle linked by clean slug in the megamenu
    }
}
