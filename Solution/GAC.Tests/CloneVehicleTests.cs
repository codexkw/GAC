using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class CloneVehicleTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private sealed class NoopSanitizer : GAC.Core.Services.IHtmlSanitizerService
    {
        public string Sanitize(string? html) => html ?? "";
    }

    // A vehicle with one of every child section (incl. grandchildren) + Quality.
    private static Vehicle FullyLoadedVehicle() => new()
    {
        Slug = "gs4",
        Category = VehicleCategory.Suv,
        IsVisible = true,
        SortOrder = 0,
        PriceFrom = 9999m,
        Name = new LocalizedText { En = "GS4 MAX", Ar = "جي إس 4" },
        Tagline = new LocalizedText { En = "tag", Ar = "تاغ" },
        SpecPdf = "/uploads/gs4.pdf",
        BrochurePdf = "/uploads/gs4-brochure.pdf",
        TechBannerImage = "/img/tech.jpg",
        Images = { new VehicleImage { Path = "/img/a.jpg", Kind = VehicleImageKind.Hero, Alt = new LocalizedText { En = "a" } } },
        Features =
        {
            new FeatureSection
            {
                Heading = new LocalizedText { En = "Design" },
                Bullets = { new FeatureBullet { Label = new LocalizedText { En = "L" }, Text = new LocalizedText { En = "T" } } }
            }
        },
        SpecGroups =
        {
            new SpecGroup
            {
                Title = new LocalizedText { En = "Engine" },
                Rows = { new SpecRow { Label = new LocalizedText { En = "Power" }, Value = new LocalizedText { En = "200hp" } } }
            }
        },
        Colors = { new ColorOption { Name = new LocalizedText { En = "Red" }, Hex = "#ff0000", ImagePath = "/img/red.jpg" } },
        Trims =
        {
            new Trim
            {
                Name = new LocalizedText { En = "GL" },
                SpecPdf = "/uploads/trim.pdf",
                PriceRows = { new TrimPriceRow { Text = new LocalizedText { En = "KD 100/mo" } } }
            }
        },
        Headings = { new SectionHeading { Key = SectionKey.Trims, Title = new LocalizedText { En = "Pick yours" } } },
        Stats = { new StatItem { Label = new LocalizedText { En = "0-100" }, Value = new LocalizedText { En = "7s" } } },
        Sliders =
        {
            new SliderGroup
            {
                Title = new LocalizedText { En = "Gallery" },
                Slides = { new SliderSlide { ImagePath = "/img/s.jpg", Alt = new LocalizedText { En = "s" } } }
            }
        },
        GalleryTabs =
        {
            new GalleryTab
            {
                Label = new LocalizedText { En = "Exterior" },
                Images = { new GalleryImage { ImagePath = "/img/g.jpg", Alt = new LocalizedText { En = "g" } } }
            }
        },
        Cards = { new CardItem { Title = new LocalizedText { En = "Tech" }, Text = new LocalizedText { En = "txt" }, ImagePath = "/img/c.jpg" } },
        SafetyToggles = { new SafetyToggle { Title = new LocalizedText { En = "ABS" }, Content = new LocalizedText { En = "safe" }, ImagePath = "/img/sf.jpg" } },
        WarrantyLinks = { new WarrantyLink { Label = new LocalizedText { En = "Warranty" }, Url = "/warranty" } },
        Quality = new QualityBlock { MainImage = "/img/q.jpg", Strapline = new LocalizedText { En = "Quality" }, Content = new LocalizedText { En = "made well" } }
    };

    [Fact]
    public async Task CloneAsync_DeepCopies_AllSections_RenamesAndHides_LeavingSourceIntact()
    {
        var db = NewDb(nameof(CloneAsync_DeepCopies_AllSections_RenamesAndHides_LeavingSourceIntact));
        var src = FullyLoadedVehicle();
        db.Vehicles.Add(src);
        await db.SaveChangesAsync();
        var sourceId = src.Id;
        var srcImageId = src.Images[0].Id;

        var svc = new AdminVehicleService(db, new NoopSanitizer());
        var newId = await svc.CloneAsync(sourceId);

        Assert.NotEqual(0, newId);
        Assert.NotEqual(sourceId, newId);

        var clone = await svc.GetAsync(newId);
        Assert.NotNull(clone);
        // Renamed + slugged + hidden.
        Assert.Equal("gs4-copy", clone!.Slug);
        Assert.Equal("GS4 MAX -copy", clone.Name.En);
        Assert.Equal("جي إس 4 -copy", clone.Name.Ar);
        Assert.False(clone.IsVisible);
        // Scalars carried over.
        Assert.Equal(VehicleCategory.Suv, clone.Category);
        Assert.Equal(9999m, clone.PriceFrom);
        Assert.Equal("/uploads/gs4.pdf", clone.SpecPdf);
        Assert.Equal("/img/tech.jpg", clone.TechBannerImage);

        // Every child collection copied with the same counts...
        Assert.Single(clone.Images);
        Assert.Single(clone.Features);
        Assert.Single(clone.Features[0].Bullets);
        Assert.Single(clone.SpecGroups);
        Assert.Single(clone.SpecGroups[0].Rows);
        Assert.Single(clone.Colors);
        Assert.Single(clone.Trims);
        Assert.Single(clone.Trims[0].PriceRows);
        Assert.Single(clone.Headings);
        Assert.Single(clone.Stats);
        Assert.Single(clone.Sliders);
        Assert.Single(clone.Sliders[0].Slides);
        Assert.Single(clone.GalleryTabs);
        Assert.Single(clone.GalleryTabs[0].Images);
        Assert.Single(clone.Cards);
        Assert.Single(clone.SafetyToggles);
        Assert.Single(clone.WarrantyLinks);
        Assert.NotNull(clone.Quality);

        // ...as brand-new rows (new ids, re-parented FKs), not shared with the source.
        Assert.NotEqual(srcImageId, clone.Images[0].Id);
        Assert.Equal(newId, clone.Images[0].VehicleId);
        Assert.Equal("/img/a.jpg", clone.Images[0].Path);
        Assert.Equal("GL", clone.Trims[0].Name.En);
        Assert.Equal("KD 100/mo", clone.Trims[0].PriceRows[0].Text.En);

        // Source is untouched.
        var source = await svc.GetAsync(sourceId);
        Assert.Equal("gs4", source!.Slug);
        Assert.Equal("GS4 MAX", source.Name.En);
        Assert.True(source.IsVisible);
        Assert.Single(source.Images);
        Assert.Equal(srcImageId, source.Images[0].Id);
    }

    [Fact]
    public async Task CloneAsync_GivesUniqueSlug_OnRepeatedClones()
    {
        var db = NewDb(nameof(CloneAsync_GivesUniqueSlug_OnRepeatedClones));
        var src = FullyLoadedVehicle();
        db.Vehicles.Add(src);
        await db.SaveChangesAsync();

        var svc = new AdminVehicleService(db, new NoopSanitizer());
        var first = await svc.CloneAsync(src.Id);
        var second = await svc.CloneAsync(src.Id);

        Assert.Equal("gs4-copy", (await svc.GetAsync(first))!.Slug);
        Assert.Equal("gs4-copy-2", (await svc.GetAsync(second))!.Slug);
    }

    [Fact]
    public async Task CloneAsync_Returns0_WhenSourceMissing()
    {
        var db = NewDb(nameof(CloneAsync_Returns0_WhenSourceMissing));
        var svc = new AdminVehicleService(db, new NoopSanitizer());
        Assert.Equal(0, await svc.CloneAsync(404));
    }
}
