using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionRoundTripTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Vehicle_WithAllRichCollections_RoundTrips()
    {
        var name = nameof(Vehicle_WithAllRichCollections_RoundTrips);
        int vid;
        using (var db = NewDb(name))
        {
            var v = new Vehicle
            {
                Slug = "round-trip",
                Name = "Round Trip",
                TechBannerImage = "/tb.jpg",
                EnquiryBgImage = "/eb.jpg",
                StatsNote = new LocalizedText { En = "note-en", Ar = "note-ar" },
                EnquiryTitle = "Enquire",
                Quality = new QualityBlock
                {
                    MainImage = "/q-main.jpg",
                    ThumbImage = "/q-thumb.jpg",
                    Strapline = "strap",
                    Content = "content"
                }
            };
            v.Headings.Add(new SectionHeading { Key = SectionKey.Overview, Title = "Overview" });
            v.Stats.Add(new StatItem { Label = "Power", Value = "177 HP", SortOrder = 0 });
            v.Cards.Add(new CardItem { Title = "Card", Text = "Body", ImagePath = "/c.jpg", SortOrder = 0 });
            v.SafetyToggles.Add(new SafetyToggle { Title = "Brakes", Strap = "s", Content = "c", ImagePath = "/s.jpg", SortOrder = 0 });
            v.WarrantyLinks.Add(new WarrantyLink { Label = "Manual", Url = "/m.pdf", SortOrder = 0 });

            var sg = new SliderGroup { Eyebrow = "eye", Title = "Slider", SortOrder = 0 };
            sg.Slides.Add(new SliderSlide { ImagePath = "/sl1.jpg", Alt = "alt1", SortOrder = 0 });
            sg.Slides.Add(new SliderSlide { ImagePath = "/sl2.jpg", Alt = "alt2", SortOrder = 1 });
            v.Sliders.Add(sg);

            var gt = new GalleryTab { Label = "Exterior", SortOrder = 0 };
            gt.Images.Add(new GalleryImage { ImagePath = "/g1.jpg", Alt = "g1", SortOrder = 0 });
            v.GalleryTabs.Add(gt);

            var feat = new FeatureSection { Heading = "Design", GroupKey = FeatureGroup.Design, TabLabel = "Design", Lead = "lead", SortOrder = 0 };
            feat.Bullets.Add(new FeatureBullet { Label = "L", Text = "T", SortOrder = 0 });
            v.Features.Add(feat);

            var trim = new Trim { Name = "GT", ModelLabel = "GS4", ImagePath = "/trim.jpg", SortOrder = 0 };
            trim.PriceRows.Add(new TrimPriceRow { Text = "Total: 10,000", SortOrder = 0 });
            v.Trims.Add(trim);

            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vid = v.Id;
        }

        using (var db = NewDb(name))
        {
            var v = await db.Vehicles
                .Include(x => x.Headings)
                .Include(x => x.Stats)
                .Include(x => x.Cards)
                .Include(x => x.SafetyToggles)
                .Include(x => x.WarrantyLinks)
                .Include(x => x.Sliders).ThenInclude(s => s.Slides)
                .Include(x => x.GalleryTabs).ThenInclude(g => g.Images)
                .Include(x => x.Features).ThenInclude(f => f.Bullets)
                .Include(x => x.Trims).ThenInclude(t => t.PriceRows)
                .Include(x => x.Quality)
                .FirstAsync(x => x.Id == vid);

            Assert.Equal("/tb.jpg", v.TechBannerImage);
            Assert.Equal("note-en", v.StatsNote.En);
            Assert.Single(v.Headings);
            Assert.Equal(SectionKey.Overview, v.Headings[0].Key);
            Assert.Single(v.Stats);
            Assert.Equal("177 HP", v.Stats[0].Value.En);
            Assert.Single(v.Cards);
            Assert.Single(v.SafetyToggles);
            Assert.Single(v.WarrantyLinks);
            Assert.Single(v.Sliders);
            Assert.Equal(2, v.Sliders[0].Slides.Count);
            Assert.Single(v.GalleryTabs);
            Assert.Single(v.GalleryTabs[0].Images);
            Assert.Single(v.Features);
            Assert.Single(v.Features[0].Bullets);
            Assert.Equal(FeatureGroup.Design, v.Features[0].GroupKey);
            Assert.Single(v.Trims);
            Assert.Equal("GS4", v.Trims[0].ModelLabel.En);
            Assert.Single(v.Trims[0].PriceRows);
            Assert.NotNull(v.Quality);
            Assert.Equal("strap", v.Quality!.Strapline.En);
        }
    }

    [Fact]
    public async Task Vehicle_WithoutQuality_RoundTrips_QualityNull()
    {
        var name = nameof(Vehicle_WithoutQuality_RoundTrips_QualityNull);
        int vid;
        using (var db = NewDb(name))
        {
            var v = new Vehicle { Slug = "no-quality", Name = "No Quality" };
            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vid = v.Id;
        }
        using (var db = NewDb(name))
        {
            var v = await db.Vehicles.Include(x => x.Quality).FirstAsync(x => x.Id == vid);
            Assert.Null(v.Quality);
        }
    }
}
