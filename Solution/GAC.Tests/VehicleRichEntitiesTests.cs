using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class VehicleRichEntitiesTests
{
    [Fact]
    public void SectionKey_HasExpectedMembers()
    {
        Assert.Equal(0, (int)SectionKey.Overview);
        Assert.Equal(7, (int)SectionKey.Warranty);
        Assert.Equal(8, System.Enum.GetValues(typeof(SectionKey)).Length);
    }

    [Fact]
    public void FeatureGroup_HasDesignAndPerformance()
    {
        Assert.Equal(0, (int)FeatureGroup.Design);
        Assert.Equal(1, (int)FeatureGroup.Performance);
    }

    [Fact]
    public void SectionHeading_DefaultsLocalizedTextNonNull()
    {
        var h = new SectionHeading { VehicleId = 1, Key = SectionKey.Overview };
        Assert.NotNull(h.Title);
        Assert.NotNull(h.Sub);
        Assert.NotNull(h.Body);
    }

    [Fact]
    public void StatItem_IsOrderable_WithLocalizedFields()
    {
        IOrderable s = new StatItem { VehicleId = 1, SortOrder = 3 };
        Assert.Equal(3, s.SortOrder);
        var stat = (StatItem)s;
        Assert.NotNull(stat.Label);
        Assert.NotNull(stat.Value);
    }

    [Fact]
    public void SliderGroup_HoldsSlides_AndIsOrderable()
    {
        var g = new SliderGroup { VehicleId = 1, SortOrder = 2 };
        g.Slides.Add(new SliderSlide { ImagePath = "/a.jpg", SortOrder = 0 });
        Assert.Equal(2, ((IOrderable)g).SortOrder);
        Assert.Single(g.Slides);
        Assert.NotNull(g.Eyebrow);
        Assert.NotNull(g.Title);
    }

    [Fact]
    public void SliderSlide_HasParentFk_AndAlt()
    {
        var s = new SliderSlide { SliderGroupId = 5, ImagePath = "/b.jpg", SortOrder = 1 };
        Assert.Equal(5, s.SliderGroupId);
        Assert.Equal("/b.jpg", s.ImagePath);
        Assert.NotNull(s.Alt);
        Assert.Equal(1, ((IOrderable)s).SortOrder);
    }

    [Fact]
    public void FeatureBullet_HasParentFk_LabelAndText()
    {
        var b = new FeatureBullet { FeatureSectionId = 9, SortOrder = 0 };
        Assert.Equal(9, b.FeatureSectionId);
        Assert.NotNull(b.Label);
        Assert.NotNull(b.Text);
        Assert.Equal(0, ((IOrderable)b).SortOrder);
    }

    [Fact]
    public void GalleryTab_HoldsImages_AndIsOrderable()
    {
        var t = new GalleryTab { VehicleId = 1, SortOrder = 1 };
        t.Images.Add(new GalleryImage { ImagePath = "/g.jpg", SortOrder = 0 });
        Assert.Equal(1, ((IOrderable)t).SortOrder);
        Assert.Single(t.Images);
        Assert.NotNull(t.Label);
    }

    [Fact]
    public void GalleryImage_HasParentFk_AndAlt()
    {
        var g = new GalleryImage { GalleryTabId = 7, ImagePath = "/g.jpg", SortOrder = 2 };
        Assert.Equal(7, g.GalleryTabId);
        Assert.Equal("/g.jpg", g.ImagePath);
        Assert.NotNull(g.Alt);
    }

    [Fact]
    public void QualityBlock_HasImagesAndContent()
    {
        var q = new QualityBlock { VehicleId = 1, MainImage = "/m.jpg", ThumbImage = "/t.jpg" };
        Assert.Equal("/m.jpg", q.MainImage);
        Assert.Equal("/t.jpg", q.ThumbImage);
        Assert.NotNull(q.Strapline);
        Assert.NotNull(q.Content);
    }

    [Fact]
    public void CardItem_HasImage_AndIsOrderable()
    {
        var c = new CardItem { VehicleId = 1, ImagePath = "/c.jpg", SortOrder = 2 };
        Assert.Equal("/c.jpg", c.ImagePath);
        Assert.Equal(2, ((IOrderable)c).SortOrder);
        Assert.NotNull(c.Title);
        Assert.NotNull(c.Text);
    }

    [Fact]
    public void SafetyToggle_HasTitleStrapContentImage()
    {
        var s = new SafetyToggle { VehicleId = 1, ImagePath = "/s.jpg", SortOrder = 0 };
        Assert.Equal("/s.jpg", s.ImagePath);
        Assert.NotNull(s.Title);
        Assert.NotNull(s.Strap);
        Assert.NotNull(s.Content);
    }

    [Fact]
    public void WarrantyLink_HasLabelAndUrl()
    {
        var w = new WarrantyLink { VehicleId = 1, Url = "/doc.pdf", SortOrder = 1 };
        Assert.Equal("/doc.pdf", w.Url);
        Assert.NotNull(w.Label);
        Assert.Equal(1, ((IOrderable)w).SortOrder);
    }

    [Fact]
    public void TrimPriceRow_HasParentFkAndText()
    {
        var r = new TrimPriceRow { TrimId = 4, SortOrder = 0 };
        Assert.Equal(4, r.TrimId);
        Assert.NotNull(r.Text);
    }

    [Fact]
    public void FeatureSection_HasNewGroupTabLeadBullets()
    {
        var f = new FeatureSection { GroupKey = FeatureGroup.Performance };
        f.Bullets.Add(new FeatureBullet());
        Assert.Equal(FeatureGroup.Performance, f.GroupKey);
        Assert.NotNull(f.TabLabel);
        Assert.NotNull(f.Lead);
        Assert.Single(f.Bullets);
    }

    [Fact]
    public void Trim_HasModelLabelImageAndPriceRows()
    {
        var t = new Trim { ModelLabel = "GS4", ImagePath = "/trim.jpg" };
        t.PriceRows.Add(new TrimPriceRow { Text = "Total: 10,000" });
        Assert.Equal("GS4", t.ModelLabel.En);
        Assert.Equal("/trim.jpg", t.ImagePath);
        Assert.Single(t.PriceRows);
    }

    [Fact]
    public void Vehicle_HasNewScalarAndLocalizedFields()
    {
        var v = new Vehicle { TechBannerImage = "/tb.jpg", EnquiryBgImage = "/eb.jpg" };
        Assert.Equal("/tb.jpg", v.TechBannerImage);
        Assert.Equal("/eb.jpg", v.EnquiryBgImage);
        Assert.NotNull(v.StatsNote);
        Assert.NotNull(v.EnquiryTitle);
        Assert.NotNull(v.EnquirySub);
        Assert.NotNull(v.EnquiryLead);
    }

    [Fact]
    public void Vehicle_HasNewCollectionsAndQualityNav()
    {
        var v = new Vehicle();
        v.Headings.Add(new SectionHeading());
        v.Stats.Add(new StatItem());
        v.Sliders.Add(new SliderGroup());
        v.GalleryTabs.Add(new GalleryTab());
        v.Cards.Add(new CardItem());
        v.SafetyToggles.Add(new SafetyToggle());
        v.WarrantyLinks.Add(new WarrantyLink());
        v.Quality = new QualityBlock();
        Assert.Single(v.Headings);
        Assert.Single(v.Stats);
        Assert.Single(v.Sliders);
        Assert.Single(v.GalleryTabs);
        Assert.Single(v.Cards);
        Assert.Single(v.SafetyToggles);
        Assert.Single(v.WarrantyLinks);
        Assert.NotNull(v.Quality);
    }
}
