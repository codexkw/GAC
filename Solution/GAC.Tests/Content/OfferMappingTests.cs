using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class OfferMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Offer_ButtonLabel_RoundTrips()
    {
        var db = NewDb(nameof(Offer_ButtonLabel_RoundTrips));
        db.Offers.Add(new Offer
        {
            Slug = "deal",
            Title = "0% APR",
            Body = "Zero-interest finance",
            ButtonLabel = new LocalizedText { En = "Enquire Now", Ar = "استفسر الآن" },
            IsActive = true,
            SortOrder = 1
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var o = await db.Offers.AsNoTracking().FirstAsync(x => x.Slug == "deal");
        Assert.Equal("Enquire Now", o.ButtonLabel.En);
        Assert.Equal("استفسر الآن", o.ButtonLabel.Ar);
    }
}
