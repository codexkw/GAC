using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederOffersTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Seed_EmptyDb_SeedsSixBilingualOffers()
    {
        var db = NewDb(nameof(Seed_EmptyDb_SeedsSixBilingualOffers));
        await ContentSeeder.SeedOffersAsync(db);
        await ContentSeeder.SeedOffersAsync(db);   // idempotent

        var offers = await db.Offers.OrderBy(o => o.SortOrder).ToListAsync();
        Assert.Equal(6, offers.Count);
        Assert.All(offers, o => Assert.True(o.IsActive));
        Assert.All(offers, o => Assert.False(string.IsNullOrWhiteSpace(o.Title.En)));
        Assert.All(offers, o => Assert.False(string.IsNullOrWhiteSpace(o.Title.Ar)));
        Assert.All(offers, o => Assert.False(string.IsNullOrWhiteSpace(o.ButtonLabel.En)));
        Assert.All(offers, o => Assert.False(string.IsNullOrWhiteSpace(o.ButtonLabel.Ar)));
    }

    [Fact]
    public async Task Seed_RetiresLegacyEmptyPlaceholder_ThenSeedsSix()
    {
        var db = NewDb(nameof(Seed_RetiresLegacyEmptyPlaceholder_ThenSeedsSix));
        db.Offers.Add(new Offer { Slug = "current-offers", Title = "Current Offers", IsActive = true, SortOrder = 1 });
        await db.SaveChangesAsync();

        await ContentSeeder.SeedOffersAsync(db);

        Assert.Equal(6, await db.Offers.CountAsync());
        Assert.False(await db.Offers.AnyAsync(o => o.Slug == "current-offers"));
    }

    [Fact]
    public async Task Seed_LeavesRealAdminOffersUntouched()
    {
        var db = NewDb(nameof(Seed_LeavesRealAdminOffersUntouched));
        db.Offers.Add(new Offer { Slug = "my-deal", Title = "My Deal", Body = "real", IsActive = true, SortOrder = 1 });
        await db.SaveChangesAsync();

        await ContentSeeder.SeedOffersAsync(db);

        Assert.Equal(1, await db.Offers.CountAsync());
        Assert.True(await db.Offers.AnyAsync(o => o.Slug == "my-deal"));
    }
}
