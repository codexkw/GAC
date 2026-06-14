using System.Net;
using Xunit;

namespace GAC.Tests;

public class ContentPagesTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ContentPagesTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/warranty")]
    [InlineData("/privacy-policy")]
    [InlineData("/finance")]
    [InlineData("/cost-of-service")]
    [InlineData("/road-assistance")]
    public async Task ContentPages_Render200(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
