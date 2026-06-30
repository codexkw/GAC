using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class WarrantyMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task WarrantyPage_WithCallouts_RoundTrips()
    {
        var db = NewDb(nameof(WarrantyPage_WithCallouts_RoundTrips));
        db.WarrantyPages.Add(new WarrantyPage
        {
            BannerImagePath = "/b.jpg", BannerLabel = "Label", Heading = "Warranty",
            Intro = "i", TermsImagePath = "/t.jpg", TermsNote = "*terms",
            ExtendedHeading = "Ext", ExtendedIntro = "p", ExtendedTableHtml = "<table></table>",
            Callouts = { new WarrantyCallout { Lead = "5y", Text = "rest", SortOrder = 0 } }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var w = await db.WarrantyPages.Include(x => x.Callouts).FirstAsync();
        Assert.Equal("Warranty", w.Heading.En);
        Assert.Single(w.Callouts);
        Assert.Equal("5y", w.Callouts[0].Lead.En);
    }

    [Fact]
    public async Task Vehicle_WarrantyBookletPdf_RoundTrips()
    {
        var db = NewDb(nameof(Vehicle_WarrantyBookletPdf_RoundTrips));
        db.Vehicles.Add(new Vehicle { Slug = "gs8", Name = "GS8", WarrantyBookletPdf = "/w.pdf" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var v = await db.Vehicles.FirstAsync(x => x.Slug == "gs8");
        Assert.Equal("/w.pdf", v.WarrantyBookletPdf);
    }
}
