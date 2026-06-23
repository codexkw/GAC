using System.Net;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class VehicleDetailRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehicleDetailRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    // Reproduces the /emzoom 500: a vehicle WITH structured content is served by
    // PageController (controller route value = "Page"), so Detail.cshtml's
    // <partial name="_VehicleHero" /> must resolve even though the structured-section
    // partials live under /Views/Vehicles/. With bare partial names the location
    // expander only probes /Views/Page/ and /Views/Shared/ and the page 500s.
    [Fact]
    public async Task StructuredContentVehicle_Renders_HeroSection()
    {
        var slug = await FindStructuredVehicleSlugAsync();
        Assert.False(string.IsNullOrEmpty(slug),
            "Expected at least one visible vehicle with structured content (e.g. emzoom) in the database.");

        var res = await _factory.CreateClient().GetAsync("/" + slug);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("mp-hero", await res.Content.ReadAsStringAsync());
    }

    private async Task<string?> FindStructuredVehicleSlugAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var vehicles = scope.ServiceProvider.GetRequiredService<IVehicleService>();
        foreach (var v in await vehicles.GetVisibleAsync())
        {
            var full = await vehicles.GetBySlugAsync(v.Slug);
            if (full != null && VehicleContent.HasStructuredContent(full))
                return v.Slug;
        }
        return null;
    }
}
