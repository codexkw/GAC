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
        Assert.Equal(8, await db.ContentPages.CountAsync());
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
}
