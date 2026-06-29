using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class HomeAggregateMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task HomePage_Promo_Campaigns_And_DualCards_RoundTrip()
    {
        var db = NewDb(nameof(HomePage_Promo_Campaigns_And_DualCards_RoundTrip));
        var home = new HomePage
        {
            Promo = new PromoSection
            {
                ImagePath = "/img/p.jpg",
                Eyebrow = "Promotions", Heading = "Latest Offers",
                Description = "desc", CtaText = "View Offers", CtaLink = "/offers",
                Campaigns =
                {
                    new PromoCampaign { Text = "0% interest", SortOrder = 0 },
                }
            },
            DualCards =
            {
                new DualCard { ImagePath = "/img/c.jpg", Link = "/contact-us",
                    Eyebrow = "Our showrooms", Title = "Locations",
                    Description = "d", ButtonText = "Find Us", SortOrder = 0 },
            }
        };
        db.HomePages.Add(home);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.HomePages
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns)
            .Include(h => h.DualCards)
            .FirstAsync();

        Assert.Equal("Latest Offers", loaded.Promo!.Heading.En);
        Assert.Single(loaded.Promo.Campaigns);
        Assert.Equal("0% interest", loaded.Promo.Campaigns[0].Text.En);
        Assert.Single(loaded.DualCards);
        Assert.Equal("Locations", loaded.DualCards[0].Title.En);
    }

    [Fact]
    public async Task FormPage_BannerImagePath_RoundTrips()
    {
        var db = NewDb(nameof(FormPage_BannerImagePath_RoundTrips));
        db.FormPages.Add(new FormPage { Slug = "fleet", FormType = FormType.Fleet, BannerImagePath = "/img/b.jpg" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var f = await db.FormPages.FirstAsync(x => x.Slug == "fleet");
        Assert.Equal("/img/b.jpg", f.BannerImagePath);
    }
}
