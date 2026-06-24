using System.Reflection;
using AngleSharp.Dom;
using GAC.Infrastructure.Content;
using Xunit;

namespace GAC.Tests;

public class BodyHtmlParserTests
{
    // Loads the embedded emkoo seed-body fixture (resource name = "GAC.Tests.Fixtures.emkoo.html").
    internal static string LoadFixture()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .Single(n => n.EndsWith("Fixtures.emkoo.html", StringComparison.Ordinal));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static IDocument Doc() => BodyHtmlParser.ParseHtml(LoadFixture());

    [Fact]
    public void Fixture_LoadsNonEmptyHtml()
    {
        var html = LoadFixture();
        Assert.Contains("mp-hero__title", html);
        Assert.Contains("data-tab-btn", html);
    }

    [Fact]
    public void ParseHtml_ReturnsDocument_WithExpectedRootSections()
    {
        var doc = Doc();
        var ids = doc.QuerySelectorAll("section.mp-section[id]")
                     .Select(e => e.Id).ToList();
        Assert.Contains("exterior", ids);
        Assert.Contains("design", ids);
        Assert.Contains("gallery", ids);
        Assert.Contains("performance", ids);
        Assert.Contains("trims", ids);
        Assert.Contains("warranty", ids);
    }

    [Fact]
    public void ParseHero_ExtractsImageTitleSub()
    {
        var (img, title, sub) = BodyHtmlParser.ParseHero(Doc());
        Assert.False(string.IsNullOrWhiteSpace(img));
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    [Fact]
    public void ParseHeadings_KeysEightSections()
    {
        var heads = BodyHtmlParser.ParseHeadings(Doc());
        Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Overview);
        Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Design);
        Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Gallery);
        Assert.All(heads, h => Assert.False(string.IsNullOrWhiteSpace(h.Title.En)));
    }

    [Fact]
    public void ParseStats_ReturnsFour_WithLabelAndValue()
    {
        var stats = BodyHtmlParser.ParseStats(Doc());
        Assert.Equal(4, stats.Count);
        Assert.All(stats, s => Assert.False(string.IsNullOrWhiteSpace(s.Value.En)));
        Assert.False(string.IsNullOrWhiteSpace(BodyHtmlParser.ParseStatsNote(Doc())));
    }

    [Fact]
    public void ParseSliders_ReturnsTwoGroups_EachWithSlides()
    {
        var sliders = BodyHtmlParser.ParseSliders(Doc());
        Assert.Equal(2, sliders.Count);
        Assert.All(sliders, g => Assert.True(g.Slides.Count >= 2));
        Assert.All(sliders, g => Assert.False(string.IsNullOrWhiteSpace(g.Title.En)));
    }

    [Fact]
    public void ParseFeatures_ReturnsSix_ThreeDesignThreePerformance_WithBullets()
    {
        var feats = BodyHtmlParser.ParseFeatures(Doc());
        Assert.Equal(6, feats.Count);
        Assert.Equal(3, feats.Count(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design));
        Assert.Equal(3, feats.Count(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Performance));
        Assert.All(feats, f => Assert.False(string.IsNullOrWhiteSpace(f.Heading.En)));
        var first = feats.First(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design);
        Assert.True(first.Bullets.Count >= 4);
        Assert.False(string.IsNullOrWhiteSpace(first.Bullets[0].Label.En));
        Assert.False(string.IsNullOrWhiteSpace(first.Bullets[0].Text.En));
    }

    [Fact]
    public void ParseGalleryTabs_ReturnsThreeTabs_FifteenImagesTotal()
    {
        var tabs = BodyHtmlParser.ParseGalleryTabs(Doc());
        Assert.Equal(3, tabs.Count);
        Assert.Equal(15, tabs.Sum(t => t.Images.Count));
        Assert.All(tabs, t => Assert.False(string.IsNullOrWhiteSpace(t.Label.En)));
    }

    [Fact]
    public void ParseQuality_ExtractsImagesAndText()
    {
        var q = BodyHtmlParser.ParseQuality(Doc());
        Assert.NotNull(q);
        Assert.False(string.IsNullOrWhiteSpace(q!.Content.En));
    }

    [Fact]
    public void ParseTechnology_ReturnsBannerAndThreeCards()
    {
        var (banner, cards) = BodyHtmlParser.ParseTechnology(Doc());
        Assert.False(string.IsNullOrWhiteSpace(banner));
        Assert.Equal(3, cards.Count);
        Assert.All(cards, c => Assert.False(string.IsNullOrWhiteSpace(c.Title.En)));
    }

    [Fact]
    public void ParseSafety_ReturnsThreeToggles()
    {
        var s = BodyHtmlParser.ParseSafety(Doc());
        Assert.Equal(3, s.Count);
        Assert.All(s, t => Assert.False(string.IsNullOrWhiteSpace(t.Title.En)));
    }

    [Fact]
    public void ParseTrims_ReturnsAtLeastOne_WithModelNameAndPriceRows()
    {
        var trims = BodyHtmlParser.ParseTrims(Doc());
        Assert.True(trims.Count >= 1);
        var t = trims[0];
        Assert.False(string.IsNullOrWhiteSpace(t.ModelLabel.En));
        Assert.False(string.IsNullOrWhiteSpace(t.Name.En));
        Assert.True(t.PriceRows.Count >= 2);
        Assert.False(string.IsNullOrWhiteSpace(t.SpecPdf));   // 2nd CTA href
    }

    [Fact]
    public void ParseWarranty_ReturnsLinksWithUrls()
    {
        var links = BodyHtmlParser.ParseWarranty(Doc());
        Assert.True(links.Count >= 1);
        Assert.All(links, l => Assert.False(string.IsNullOrWhiteSpace(l.Url)));
    }

    [Fact]
    public void ParseEnquiry_ExtractsBgTitleSubLead()
    {
        var (bg, title, sub, lead) = BodyHtmlParser.ParseEnquiry(Doc());
        Assert.False(string.IsNullOrWhiteSpace(bg));
        Assert.False(string.IsNullOrWhiteSpace(title));
    }

    // Loads the embedded real-production Arabic emkoo body (resource name ends "Fixtures.emkoo-ar.html").
    internal static string LoadArFixture()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = asm.GetManifestResourceNames()
            .Single(n => n.EndsWith("Fixtures.emkoo-ar.html", StringComparison.Ordinal));
        using var s = asm.GetManifestResourceStream(name)!;
        using var r = new StreamReader(s);
        return r.ReadToEnd();
    }

    private static IDocument ArDoc() => BodyHtmlParser.ParseHtml(LoadArFixture());

    // True when the string contains at least one Arabic-script code point (U+0600-U+06FF).
    // Use \u escapes (not literal Arabic chars) so this is independent of source-file encoding.
    private static bool HasArabic(string? s)
        => !string.IsNullOrWhiteSpace(s) && s.Any(c => c >= '؀' && c <= 'ۿ');

    [Fact]
    public void ParsesArabic_StatsAndFeatures_WithCounts()
    {
        var stats = BodyHtmlParser.ParseStats(ArDoc());
        Assert.Equal(4, stats.Count);
        // Parser is language-blind: AR text lands in the .En slot. Assert it is real Arabic,
        // not a brittle exact-word match (the real first stat is "fuel consumption", not "power").
        Assert.True(HasArabic(stats[0].Label.En), "first stat label should contain Arabic script");
        Assert.True(HasArabic(stats[0].Value.En), "first stat value should contain Arabic script");

        var feats = BodyHtmlParser.ParseFeatures(ArDoc());
        Assert.Equal(6, feats.Count);
        var firstDesign = feats.First(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design);
        Assert.True(HasArabic(firstDesign.Heading.En), "design feature heading should contain Arabic script");
        Assert.False(string.IsNullOrWhiteSpace(feats[0].Bullets[0].Text.En));
        Assert.True(HasArabic(feats[0].Bullets[0].Text.En), "first bullet text should contain Arabic script");
    }

    [Fact]
    public void EnAndAr_ProduceSamePositionalCounts_ForMerge()
    {
        var en = BodyHtmlParser.ParseAll(Doc());
        var ar = BodyHtmlParser.ParseAll(ArDoc());

        // Full-structure positional parity — the real EN/AR bodies come from the same prod row,
        // so every collection must line up for the migrator's positional EN→AR merge to be safe.
        Assert.Equal(en.Stats.Count, ar.Stats.Count);
        Assert.Equal(en.Sliders.Count, ar.Sliders.Count);
        Assert.Equal(en.Features.Count, ar.Features.Count);
        Assert.Equal(en.GalleryTabs.Count, ar.GalleryTabs.Count);
        Assert.Equal(en.GalleryTabs.Sum(t => t.Images.Count), ar.GalleryTabs.Sum(t => t.Images.Count));
        Assert.Equal(en.Cards.Count, ar.Cards.Count);
        Assert.Equal(en.Safety.Count, ar.Safety.Count);
        // bullet counts line up per feature so positional EN/AR bullet merge is safe
        for (var i = 0; i < en.Features.Count; i++)
            Assert.Equal(en.Features[i].Bullets.Count, ar.Features[i].Bullets.Count);
    }
}
