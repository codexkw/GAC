using System.Linq;
using System.Net;
using GAC.Infrastructure.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

// Boots the app against an ISOLATED InMemory DB (seeded by ContentSeeder at startup,
// exactly like the real host), backfills one car's structured sections from its seeded
// BodyHtml, then GETs the page and asserts the NEW master template rendered end-to-end:
// HTTP 200 (no /emzoom-style 500), all ~/Views/Vehicles/ partials resolved, and every
// section's marker is present (proving the Task R1 eager-loading fix flows through HTTP).
public class StructuredRenderFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ApplicationDbContext) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration"))
            ).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<ApplicationDbContext>(o =>
                o.UseInMemoryDatabase("structured-render-e2e"));
        });
    }
}

public class VehicleDetailRenderTests : IClassFixture<StructuredRenderFactory>
{
    private readonly StructuredRenderFactory _factory;
    public VehicleDetailRenderTests(StructuredRenderFactory factory) => _factory = factory;

    [Fact]
    public async Task StructuredVehicle_RendersMasterTemplate_200_WithAllMarkers()
    {
        // Build the host + run startup seeding against InMemory.
        var client = _factory.CreateClient();

        // Backfill emkoo's structured sections from its seeded BodyHtml.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emkoo = await db.Vehicles.FirstAsync(v => v.Slug == "emkoo");
            var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, emkoo.Id);
            Assert.True(ok, "emkoo should backfill from its seeded BodyHtml");
        }

        var res = await client.GetAsync("/emkoo");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);          // no 500: partials resolved
        var html = await res.Content.ReadAsStringAsync();

        // Markers that only appear when the structured branch renders WITH eager-loaded data:
        Assert.Contains("mp-hero", html);                          // hero
        Assert.Contains("mp-subnav", html);                        // subnav partial resolved (static markup)
        Assert.Contains("mp-stat__value", html);                   // stats loaded + rendered
        Assert.Contains("id=\"interior\"", html);                  // 2nd slider wrap (Sliders loaded)
        Assert.Contains("data-tab-panel=\"d1\"", html);            // design feature tab (Features)
        Assert.Contains("mp-gshot", html);                         // gallery images (GalleryTabs.Images)
        Assert.Contains("data-lightbox", html);                    // lightbox singleton
        Assert.Contains("mp-card__title", html);                   // technology cards
        Assert.Contains("mp-stoggle", html);                       // safety toggles
        Assert.Contains("mp-trim__name", html);                    // trims
        Assert.Contains("id=\"warranty\"", html);                  // warranty
        Assert.Contains("id=\"enquiry\"", html);                   // enquiry
    }
}
