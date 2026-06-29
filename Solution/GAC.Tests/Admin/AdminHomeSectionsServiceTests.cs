using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminHomeSectionsServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SavePromo_Upserts_AndReplacesCampaigns()
    {
        var db = NewDb(nameof(SavePromo_Upserts_AndReplacesCampaigns));
        var svc = new AdminHomeService(db);

        await svc.SavePromoAsync(new PromoSection { ImagePath = "/a.jpg", Heading = "H1",
            Campaigns = { new PromoCampaign { Text = "x", SortOrder = 0 } } });
        await svc.SavePromoAsync(new PromoSection { ImagePath = "/b.jpg", Heading = "H2",
            Campaigns = { new PromoCampaign { Text = "y", SortOrder = 0 }, new PromoCampaign { Text = "z", SortOrder = 1 } } });

        Assert.Equal(1, await db.PromoSections.CountAsync());      // upsert, not insert-twice
        var promo = await db.PromoSections.Include(p => p.Campaigns).FirstAsync();
        Assert.Equal("H2", promo.Heading.En);
        Assert.Equal("/b.jpg", promo.ImagePath);
        Assert.Equal(2, promo.Campaigns.Count);                   // replaced
        Assert.DoesNotContain(promo.Campaigns, c => c.Text.En == "x");
    }

    [Fact]
    public async Task SaveCard_UpdatesExisting()
    {
        var db = NewDb(nameof(SaveCard_UpdatesExisting));
        var home = new HomePage { DualCards = { new DualCard { ImagePath = "/c.jpg", Title = "Old", SortOrder = 0 } } };
        db.HomePages.Add(home); await db.SaveChangesAsync();
        var id = home.DualCards[0].Id;
        var svc = new AdminHomeService(db);

        var ok = await svc.SaveCardAsync(new DualCard { Id = id, ImagePath = "/c2.jpg", Title = "New", Link = "/x" });

        Assert.True(ok);
        var card = await db.DualCards.FindAsync(id);
        Assert.Equal("New", card!.Title.En);
        Assert.Equal("/c2.jpg", card.ImagePath);
    }
}
