using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederHomeSectionsTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SeedPromo_IsIdempotent_AndSeedsCampaigns()
    {
        var db = NewDb(nameof(SeedPromo_IsIdempotent_AndSeedsCampaigns));
        db.HomePages.Add(new HomePage());           // singleton exists (as in real seed)
        await db.SaveChangesAsync();

        await ContentSeeder.SeedPromoAsync(db);
        await ContentSeeder.SeedPromoAsync(db);      // second run must not duplicate

        Assert.Equal(1, await db.PromoSections.CountAsync());
        Assert.Equal(2, await db.PromoCampaigns.CountAsync());
    }

    [Fact]
    public async Task SeedDualCards_SeedsThree_Once()
    {
        var db = NewDb(nameof(SeedDualCards_SeedsThree_Once));
        db.HomePages.Add(new HomePage());
        await db.SaveChangesAsync();

        await ContentSeeder.SeedDualCardsAsync(db);
        await ContentSeeder.SeedDualCardsAsync(db);

        Assert.Equal(3, await db.DualCards.CountAsync());
    }

    [Fact]
    public async Task EnsureFormBanners_FillsBlanks_ButNeverOverwrites()
    {
        var db = NewDb(nameof(EnsureFormBanners_FillsBlanks_ButNeverOverwrites));
        db.FormPages.Add(new FormPage { Slug = "fleet", FormType = FormType.Fleet });               // blank
        db.FormPages.Add(new FormPage { Slug = "book-a-service", FormType = FormType.ServiceBooking,
            BannerImagePath = "/custom.jpg", IntroText = new LocalizedText { En = "mine" } });       // user-set
        await db.SaveChangesAsync();

        await ContentSeeder.EnsureFormBannersAsync(db);

        var fleet = await db.FormPages.FirstAsync(f => f.Slug == "fleet");
        var bas = await db.FormPages.FirstAsync(f => f.Slug == "book-a-service");
        Assert.False(string.IsNullOrEmpty(fleet.BannerImagePath));      // blank → filled
        Assert.Equal("/custom.jpg", bas.BannerImagePath);              // user value preserved
        Assert.Equal("mine", bas.IntroText.En);                        // user value preserved
    }
}
