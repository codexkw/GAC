using GAC.Core.Services;
using GAC.Infrastructure.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

// Hermetic (InMemory) guard for the PUBLIC render query. ContentSeeder gives emkoo
// its full BodyHtml; the migrator backfills emkoo's structured collections from it;
// GetBySlugAsync must eager-load every collection or the new master template renders
// empty sections. No shared-DB dependency.
public class VehicleRenderLoadingTests
{
    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task GetBySlugAsync_EagerLoads_AllStructuredCollections_AfterBackfill()
    {
        var sp = BuildServices("render-loading-emkoo");
        await ContentSeeder.SeedAsync(sp);   // seeds emkoo with its full BodyHtml

        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var emkoo = await db.Vehicles.FirstAsync(v => v.Slug == "emkoo");
            var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, emkoo.Id);
            Assert.True(ok, "emkoo should backfill from its seeded BodyHtml");
        }

        var svc = new VehicleService(sp.GetRequiredService<ApplicationDbContext>());
        var v = await svc.GetBySlugAsync("emkoo");

        Assert.NotNull(v);
        Assert.NotEmpty(v!.Headings);
        Assert.NotEmpty(v.Stats);
        Assert.NotEmpty(v.Sliders);
        Assert.All(v.Sliders, s => Assert.NotEmpty(s.Slides));
        Assert.NotEmpty(v.GalleryTabs);
        Assert.All(v.GalleryTabs, t => Assert.NotEmpty(t.Images));
        Assert.NotEmpty(v.Cards);
        Assert.NotEmpty(v.SafetyToggles);
        Assert.NotEmpty(v.WarrantyLinks);
        Assert.NotEmpty(v.Features);
        Assert.All(v.Features, f => Assert.NotEmpty(f.Bullets));
        Assert.NotEmpty(v.Trims);
        Assert.NotNull(v.Quality);
    }
}
