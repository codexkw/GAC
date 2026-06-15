using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ContentSeederTests
{
    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeds_ElevenVehicles_WithTwoHidden()
    {
        var sp = BuildServices("seed-vehicles");
        await ContentSeeder.SeedAsync(sp);

        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(11, await db.Vehicles.CountAsync());
        Assert.Equal(2, await db.Vehicles.CountAsync(v => !v.IsVisible));
        Assert.True(await db.Vehicles.AnyAsync(v => v.Slug == "gs8"));
    }

    [Fact]
    public async Task IsIdempotent_RunningTwice_DoesNotDuplicate()
    {
        var sp = BuildServices("seed-idem");
        await ContentSeeder.SeedAsync(sp);
        await ContentSeeder.SeedAsync(sp);

        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(11, await db.Vehicles.CountAsync());
        Assert.Equal(1, await db.SiteSettings.CountAsync());
        Assert.Equal(6, await db.FormPages.CountAsync());
        Assert.Equal(6, await db.ContentPages.CountAsync());
    }

    [Fact]
    public async Task Seeds_NineHeroSlides_AndSixTopLevelMenuItems()
    {
        var sp = BuildServices("seed-home-menu");
        await ContentSeeder.SeedAsync(sp);

        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(9, await db.HeroSlides.CountAsync());
        Assert.Equal(6, await db.MenuItems.CountAsync(m => m.ParentId == null));
    }

    [Fact]
    public async Task Seeds_MenuItems_WithCleanUrls()
    {
        var sp = BuildServices("seed-clean-urls");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var urls = db.MenuItems.Where(m => m.Url != null).Select(m => m.Url!).ToList();
        Assert.All(urls, u => Assert.DoesNotContain(".html", u));
        Assert.Contains("/models", urls);
    }

    [Fact]
    public async Task Seeds_ThumbnailImage_PerVisibleVehicle()
    {
        var sp = BuildServices("seed-thumbs");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var gs8 = db.Vehicles.Include(v => v.Images).Single(v => v.Slug == "gs8");
        Assert.Contains(gs8.Images, i => i.Kind == VehicleImageKind.Gallery);
        Assert.Contains(gs8.Images, i => i.Kind == VehicleImageKind.Hero);
    }

    [Fact]
    public async Task DoesNotSeed_NewsOrOffers_AsContentPages()
    {
        var sp = BuildServices("seed-no-newsoffers");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.DoesNotContain(db.ContentPages, p => p.Slug == "news" || p.Slug == "offers");
    }

    [Fact]
    public async Task Seeds_BodyHtml_ForVehiclesContentAndContactUs()
    {
        var sp = BuildServices("seed-bodies");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        Assert.Contains("mp-hero__title", gs4.BodyHtml.En);
        Assert.Contains("GS4 MAX", gs4.BodyHtml.En);

        var about = await db.ContentPages.SingleAsync(p => p.Slug == "about");
        Assert.False(string.IsNullOrWhiteSpace(about.BodyHtml.En));

        var contact = await db.FormPages.SingleAsync(p => p.Slug == "contact-us");
        Assert.Contains("dir-grid", contact.BodyHtml.En);

        var aion = await db.Vehicles.SingleAsync(v => v.Slug == "aion-v");
        Assert.True(string.IsNullOrEmpty(aion.BodyHtml.En));   // hidden vehicle, no seed body

        Assert.True(string.IsNullOrEmpty(gs4.BodyHtml.Ar));    // Arabic intentionally blank
    }

    [Fact]
    public async Task BodyBackfill_IsIdempotent_AndPreservesEditedBody()
    {
        var sp = BuildServices("seed-bodies-idem");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        gs4.BodyHtml = new LocalizedText { En = "<p>edited</p>", Ar = "<p>محرر</p>" };
        await db.SaveChangesAsync();

        await ContentSeeder.SeedAsync(sp); // re-run
        gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        Assert.Equal("<p>edited</p>", gs4.BodyHtml.En);   // not clobbered
        Assert.Equal("<p>محرر</p>", gs4.BodyHtml.Ar);
    }
}
