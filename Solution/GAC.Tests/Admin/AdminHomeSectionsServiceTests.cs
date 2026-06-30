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

    [Fact]
    public async Task DeleteCard_RemovesExisting_ReturnsFalseForMissing()
    {
        var db = NewDb(nameof(DeleteCard_RemovesExisting_ReturnsFalseForMissing));
        var home = new HomePage
        {
            DualCards =
            {
                new DualCard { ImagePath = "/a.jpg", Title = "A", SortOrder = 0 },
                new DualCard { ImagePath = "/b.jpg", Title = "B", SortOrder = 1 },
            }
        };
        db.HomePages.Add(home); await db.SaveChangesAsync();
        var id = home.DualCards[0].Id;
        var svc = new AdminHomeService(db);

        var removed = await svc.DeleteCardAsync(id);
        var missing = await svc.DeleteCardAsync(999999);

        Assert.True(removed);
        Assert.False(missing);                              // nothing to delete → false
        Assert.Equal(1, await db.DualCards.CountAsync());
        Assert.Null(await db.DualCards.FindAsync(id));      // the right row is gone
    }

    [Fact]
    public async Task CreateCard_AppendsToSingletonHome_WithNextSortOrder()
    {
        var db = NewDb(nameof(CreateCard_AppendsToSingletonHome_WithNextSortOrder));
        var home = new HomePage
        {
            DualCards =
            {
                new DualCard { ImagePath = "/a.jpg", Title = "A", SortOrder = 0 },
                new DualCard { ImagePath = "/b.jpg", Title = "B", SortOrder = 1 },
            }
        };
        db.HomePages.Add(home); await db.SaveChangesAsync();
        var svc = new AdminHomeService(db);

        var id = await svc.CreateCardAsync(new DualCard { ImagePath = "/c.jpg", Title = "C", Link = "/x" });

        Assert.True(id > 0);
        Assert.Equal(3, await db.DualCards.CountAsync());
        var created = await db.DualCards.FindAsync(id);
        Assert.Equal(2, created!.SortOrder);          // appended after the existing two
        Assert.Equal(home.Id, created.HomePageId);    // attached to the singleton home
        Assert.Equal("C", created.Title.En);
    }

    [Fact]
    public async Task CreateCard_AfterDeletingMiddleCard_GetsUniqueSortOrder()
    {
        var db = NewDb(nameof(CreateCard_AfterDeletingMiddleCard_GetsUniqueSortOrder));
        var home = new HomePage
        {
            DualCards =
            {
                new DualCard { ImagePath = "/a.jpg", Title = "A", SortOrder = 0 },
                new DualCard { ImagePath = "/b.jpg", Title = "B", SortOrder = 1 },
                new DualCard { ImagePath = "/c.jpg", Title = "C", SortOrder = 2 },
            }
        };
        db.HomePages.Add(home); await db.SaveChangesAsync();
        var midId = home.DualCards[1].Id;     // the card at SortOrder 1
        var svc = new AdminHomeService(db);

        await svc.DeleteCardAsync(midId);     // survivors keep SortOrder { 0, 2 }
        var newId = await svc.CreateCardAsync(new DualCard { ImagePath = "/d.jpg", Title = "D" });

        var sortOrders = await db.DualCards.Select(c => c.SortOrder).ToListAsync();
        Assert.Equal(sortOrders.Count, sortOrders.Distinct().Count());   // no two cards share a SortOrder
        var created = await db.DualCards.FindAsync(newId);
        Assert.DoesNotContain(created!.SortOrder, new[] { 0, 2 });       // distinct from the survivors
    }
}
