using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionModelTests
{
    private static ApplicationDbContext Ctx()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=_design;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void SimpleChildren_AreMapped_WithVehicleFk()
    {
        using var ctx = Ctx();
        foreach (var clr in new[]
        {
            typeof(SectionHeading), typeof(StatItem), typeof(CardItem),
            typeof(SafetyToggle), typeof(WarrantyLink)
        })
        {
            var et = ctx.Model.FindEntityType(clr);
            Assert.NotNull(et);
            Assert.NotNull(et!.FindProperty("VehicleId"));
        }
    }

    [Fact]
    public void SectionHeading_LocalizedFields_AreOwned()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(SectionHeading))!;
        foreach (var field in new[] { "Title", "Sub", "Body" })
        {
            var nav = et.FindNavigation(field)!;
            Assert.NotNull(nav);
            Assert.NotNull(nav.TargetEntityType.FindProperty("En"));
            Assert.NotNull(nav.TargetEntityType.FindProperty("Ar"));
        }
        Assert.NotNull(et.FindProperty("Key"));   // enum stored as int
    }

    [Fact]
    public void Vehicle_NewLocalizedFields_AreOwned()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(Vehicle))!;
        foreach (var field in new[] { "StatsNote", "EnquiryTitle", "EnquirySub", "EnquiryLead" })
        {
            var nav = et.FindNavigation(field)!;
            Assert.NotNull(nav);
            Assert.NotNull(nav.TargetEntityType.FindProperty("En"));
        }
        Assert.NotNull(et.FindProperty("TechBannerImage"));
        Assert.NotNull(et.FindProperty("EnquiryBgImage"));
    }

    [Fact]
    public void Grandchildren_AreMapped_WithParentFk()
    {
        using var ctx = Ctx();
        Assert.NotNull(ctx.Model.FindEntityType(typeof(SliderSlide))!.FindProperty("SliderGroupId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(GalleryImage))!.FindProperty("GalleryTabId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(FeatureBullet))!.FindProperty("FeatureSectionId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(TrimPriceRow))!.FindProperty("TrimId"));
    }

    [Fact]
    public void SliderGroup_OwnsLocalized_AndHasSlides()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(SliderGroup))!;
        Assert.NotNull(et.FindNavigation("Eyebrow"));
        Assert.NotNull(et.FindNavigation("Title"));
        Assert.NotNull(et.FindNavigation("Slides"));
    }

    [Fact]
    public void FeatureSection_NewOwnedFields_AndBullets()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(FeatureSection))!;
        Assert.NotNull(et.FindNavigation("TabLabel"));
        Assert.NotNull(et.FindNavigation("Lead"));
        Assert.NotNull(et.FindNavigation("Bullets"));
        Assert.NotNull(et.FindProperty("GroupKey"));
    }

    [Fact]
    public void Trim_NewOwnedFields_AndPriceRows()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(Trim))!;
        Assert.NotNull(et.FindNavigation("ModelLabel"));
        Assert.NotNull(et.FindNavigation("PriceRows"));
        Assert.NotNull(et.FindProperty("ImagePath"));
    }

    [Fact]
    public void QualityBlock_IsMapped_WithVehicleFk()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(QualityBlock))!;
        Assert.NotNull(et.FindProperty("VehicleId"));
        Assert.NotNull(et.FindNavigation("Strapline"));
        Assert.NotNull(et.FindNavigation("Content"));
    }
}
