using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class ContentServiceHomeTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task GetHomePageAsync_Loads_Promo_And_DualCards()
    {
        var db = NewDb(nameof(GetHomePageAsync_Loads_Promo_And_DualCards));
        db.HomePages.Add(new HomePage
        {
            Promo = new PromoSection { ImagePath = "/p.jpg", Heading = "Latest Offers",
                Campaigns = { new PromoCampaign { Text = "b2", SortOrder = 1 },
                              new PromoCampaign { Text = "b1", SortOrder = 0 } } },
            DualCards = { new DualCard { ImagePath = "/c.jpg", Title = "Locations", SortOrder = 0 } }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var home = await new ContentService(db).GetHomePageAsync();

        Assert.NotNull(home!.Promo);
        Assert.Equal("b1", home.Promo!.Campaigns[0].Text.En);   // ordered by SortOrder
        Assert.Single(home.DualCards);
    }
}
