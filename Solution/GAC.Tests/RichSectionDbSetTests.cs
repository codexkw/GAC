using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionDbSetTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public void AllRichDbSets_AreExposed()
    {
        using var db = NewDb(nameof(AllRichDbSets_AreExposed));
        Assert.NotNull(db.SectionHeadings);
        Assert.NotNull(db.StatItems);
        Assert.NotNull(db.SliderGroups);
        Assert.NotNull(db.SliderSlides);
        Assert.NotNull(db.FeatureBullets);
        Assert.NotNull(db.GalleryTabs);
        Assert.NotNull(db.GalleryImages);
        Assert.NotNull(db.QualityBlocks);
        Assert.NotNull(db.CardItems);
        Assert.NotNull(db.SafetyToggles);
        Assert.NotNull(db.TrimPriceRows);
        Assert.NotNull(db.WarrantyLinks);
    }
}
