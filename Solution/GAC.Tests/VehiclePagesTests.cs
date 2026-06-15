using System.Net;
using Xunit;

namespace GAC.Tests;

public class VehiclePagesTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehiclePagesTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/gs8traveller")]
    [InlineData("/gs3emzoom")]
    [InlineData("/emkoo")]
    [InlineData("/empow")]
    [InlineData("/m8")]
    [InlineData("/empow-sport")]
    [InlineData("/hyptec-ht")]
    [InlineData("/gs4")]
    public async Task VehiclePages_Render200(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("mp-hero", html);
    }

    [Theory]
    [InlineData("/aion-v")]
    [InlineData("/aion-es")]
    public async Task HiddenVehicles_Return404(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }
}
