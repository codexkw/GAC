using System.Net;
using Xunit;

namespace GAC.Tests;

public class VehiclePagesTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehiclePagesTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/gs8traveller")]
    [InlineData("/gs8")]
    [InlineData("/emzoom")]
    [InlineData("/emkoo")]
    [InlineData("/empow")]
    [InlineData("/m8")]
    [InlineData("/empow-sport")]
    [InlineData("/hyptec-ht")]
    [InlineData("/gs4")]
    [InlineData("/gn6")]
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

    [Fact]
    public async Task Emkoo_RendersAllSectionMarkers()
    {
        var res = await _factory.CreateClient().GetAsync("/emkoo");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();

        Assert.Contains("mp-hero", html);            // hero
        Assert.Contains("mp-subnav", html);          // jump nav
        Assert.Contains("id=\"exterior\"", html);    // overview/stats
        Assert.Contains("mp-stat__value", html);     // stats values
        Assert.Contains("data-slider", html);        // sliders
        Assert.Contains("id=\"design\"", html);      // design tabs
        Assert.Contains("data-tabs-wrap", html);     // tab contract
        Assert.Contains("data-tab-panel=\"d1\"", html);
        Assert.Contains("id=\"interior\"", html);    // 2nd slider wrap
        Assert.Contains("id=\"gallery\"", html);     // gallery
        Assert.Contains("mp-gshot", html);           // gallery shots
        Assert.Contains("data-lightbox", html);      // single lightbox
        Assert.Contains("id=\"technology\"", html);  // technology
        Assert.Contains("mp-card__title", html);     // tech cards
        Assert.Contains("id=\"performance\"", html); // performance tabs
        Assert.Contains("data-tab-panel=\"p1\"", html);
        Assert.Contains("id=\"safety\"", html);      // safety toggles
        Assert.Contains("mp-stoggle", html);
        Assert.Contains("id=\"trims\"", html);       // trims
        Assert.Contains("mp-trim__name", html);
        Assert.Contains("id=\"warranty\"", html);    // warranty
        Assert.Contains("id=\"enquiry\"", html);     // enquiry
    }
}
