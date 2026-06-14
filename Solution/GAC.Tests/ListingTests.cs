using System.Net;
using Xunit;

namespace GAC.Tests;

public class ListingTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ListingTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Models_ListsVisibleVehicles()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/models");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("GS8", html);
        Assert.Contains("href=\"/gs8\"", html);
        Assert.DoesNotContain("AION V", html);   // hidden vehicles excluded
    }
}
