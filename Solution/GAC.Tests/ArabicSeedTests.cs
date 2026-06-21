using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ArabicSeedTests
{
    private static ServiceProvider BuildServices(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeds_ArabicForVehiclesMenuAndPages()
    {
        var sp = BuildServices("seed-arabic");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var gs8 = await db.Vehicles.SingleAsync(v => v.Slug == "gs8");
        Assert.False(string.IsNullOrWhiteSpace(gs8.Name.Ar));
        Assert.Equal("GS8", gs8.Name.En);

        var home = await db.MenuItems.SingleAsync(m => m.Label!.En == "Home");
        Assert.Equal("الرئيسية", home.Label!.Ar);

        var about = await db.ContentPages.SingleAsync(p => p.Slug == "about");
        Assert.False(string.IsNullOrWhiteSpace(about.Title.Ar));

        var news = await db.NewsArticles.FirstAsync();
        Assert.False(string.IsNullOrWhiteSpace(news.Title.Ar));
    }

    [Fact]
    public async Task Backfill_IsIdempotent_AndPreservesArabic()
    {
        var sp = BuildServices("seed-arabic-idem");
        await ContentSeeder.SeedAsync(sp);
        await ContentSeeder.SeedAsync(sp); // run twice
        var db = sp.GetRequiredService<ApplicationDbContext>();

        Assert.Equal(12, await db.Vehicles.CountAsync());

        // A vehicle whose Arabic genuinely differs from English: proves the backfill
        // guard preserved the distinct Arabic and did NOT clobber English on the 2nd pass.
        var emzoom = await db.Vehicles.SingleAsync(v => v.Slug == "gs3emzoom");
        Assert.Equal("إمزوم", emzoom.Name.Ar);
        Assert.Equal("EMZOOM", emzoom.Name.En);
    }
}
