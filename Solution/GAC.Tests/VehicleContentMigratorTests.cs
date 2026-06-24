using GAC.Core.Content;
using GAC.Infrastructure.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class VehicleContentMigratorTests
{
    private static ApplicationDbContext NewDb(string name)
    {
        var sp = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name))
            .BuildServiceProvider();
        return sp.GetRequiredService<ApplicationDbContext>();
    }

    // Builds an emkoo vehicle whose BodyHtml.En is the real seed body (so parser has real input).
    private static async Task<(ApplicationDbContext Db, int Id)> SeedEmkooAsync(string name)
    {
        var db = NewDb(name);
        var bodyEn = BodyHtmlParserTests.LoadFixture();
        var bodyAr = BodyHtmlParserTests.LoadArFixture();
        var v = new Vehicle
        {
            Slug = "emkoo",
            Name = new LocalizedText { En = "EMKOO" },
            BodyHtml = new LocalizedText { En = bodyEn, Ar = bodyAr },
        };
        db.Vehicles.Add(v);
        await db.SaveChangesAsync();
        return (db, v.Id);
    }

    [Fact]
    public async Task Backfill_PopulatesAllCollections_WithExpectedCounts()
    {
        var (db, id) = await SeedEmkooAsync("mig-counts");
        var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, id);
        Assert.True(ok);

        Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
        Assert.Equal(2, await db.Set<SliderGroup>().CountAsync(g => g.VehicleId == id));
        Assert.Equal(6, await db.Set<FeatureSection>().CountAsync(f => f.VehicleId == id));
        Assert.Equal(3, await db.Set<GalleryTab>().CountAsync(t => t.VehicleId == id));
        Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
        Assert.Equal(3, await db.Set<CardItem>().CountAsync(c => c.VehicleId == id));
        Assert.Equal(3, await db.Set<SafetyToggle>().CountAsync(s => s.VehicleId == id));
        Assert.True(await db.Set<Trim>().CountAsync(t => t.VehicleId == id) >= 1);
        Assert.True(await db.Set<SectionHeading>().CountAsync(h => h.VehicleId == id) >= 6);
    }

    [Fact]
    public async Task Backfill_SetsVehicleScalarFields_AndArabic()
    {
        var (db, id) = await SeedEmkooAsync("mig-scalars");
        await VehicleContentMigrator.BackfillVehicleAsync(db, id);

        var v = await db.Vehicles
            .Include(x => x.Stats)
            .Include(x => x.Features).ThenInclude(f => f.Bullets)
            .FirstAsync(x => x.Id == id);

        Assert.False(string.IsNullOrWhiteSpace(v.TechBannerImage));
        Assert.False(string.IsNullOrWhiteSpace(v.EnquiryBgImage));
        Assert.False(string.IsNullOrWhiteSpace(v.EnquiryTitle.En));
        Assert.False(string.IsNullOrWhiteSpace(v.StatsNote.En));

        // AR merged from the AR body
        var firstStat = v.Stats.OrderBy(s => s.SortOrder).First();
        Assert.Contains("Fuel", firstStat.Label.En);                  // En slot keeps the English label
        Assert.False(string.IsNullOrWhiteSpace(firstStat.Label.Ar));   // Ar slot populated by the merge
        Assert.NotEqual(firstStat.Label.En, firstStat.Label.Ar);       // Ar is the distinct Arabic translation
        var firstBullet = v.Features.OrderBy(f => f.SortOrder).First().Bullets.OrderBy(b => b.SortOrder).First();
        Assert.False(string.IsNullOrWhiteSpace(firstBullet.Label.Ar));
        Assert.NotEqual(firstBullet.Label.En, firstBullet.Label.Ar);
    }

    [Fact]
    public async Task Backfill_IsIdempotent_RunningTwiceDoesNotDuplicate()
    {
        var (db, id) = await SeedEmkooAsync("mig-idem");
        await VehicleContentMigrator.BackfillVehicleAsync(db, id);
        await VehicleContentMigrator.BackfillVehicleAsync(db, id); // second run = skip
        Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
        Assert.Equal(6, await db.Set<FeatureSection>().CountAsync(f => f.VehicleId == id));
        Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
    }

    [Fact]
    public async Task Backfill_PreservesAdminEdits_WhenCollectionsAlreadyPresent()
    {
        var (db, id) = await SeedEmkooAsync("mig-preserve");
        await VehicleContentMigrator.BackfillVehicleAsync(db, id);

        // simulate an admin edit
        var stat = await db.Set<StatItem>().OrderBy(s => s.SortOrder).FirstAsync(s => s.VehicleId == id);
        stat.Value = new LocalizedText { En = "999 HP", Ar = "999 HP (admin AR edit)" };
        await db.SaveChangesAsync();

        await VehicleContentMigrator.BackfillVehicleAsync(db, id); // must skip, not clobber
        var after = await db.Set<StatItem>().OrderBy(s => s.SortOrder).FirstAsync(s => s.VehicleId == id);
        Assert.Equal("999 HP", after.Value.En);
        Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
    }

    [Fact]
    public async Task Backfill_Force_RebuildsCar()
    {
        var (db, id) = await SeedEmkooAsync("mig-force");
        await VehicleContentMigrator.BackfillVehicleAsync(db, id);
        var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, id, force: true);
        Assert.True(ok);
        Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id)); // cleared + rebuilt, no dupes
        Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
    }

    [Fact]
    public async Task BackfillAll_SkipsVehiclesWithEmptyBody()
    {
        var db = NewDb("mig-empty");
        db.Vehicles.Add(new Vehicle { Slug = "aion-v", Name = new LocalizedText { En = "Aion V" }, BodyHtml = new LocalizedText() });
        await db.SaveChangesAsync();
        var report = await VehicleContentMigrator.BackfillAllAsync(db);
        Assert.Equal(1, report.VehiclesScanned);
        Assert.Equal(0, report.VehiclesMigrated);
    }
}
