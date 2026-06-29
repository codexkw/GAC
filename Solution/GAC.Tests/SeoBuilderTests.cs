using GAC.Core.Content;
using GAC.Web.Infrastructure;
using Xunit;

namespace GAC.Tests;

public class SeoBuilderTests
{
    [Theory]
    [InlineData("https://x.test", "/gs8", "https://x.test/gs8")]
    [InlineData("https://x.test/", "/gs8", "https://x.test/gs8")]
    [InlineData("https://x.test", "gs8", "https://x.test/gs8")]
    [InlineData("https://x.test", null, "https://x.test/")]
    [InlineData("https://x.test", "", "https://x.test/")]
    [InlineData("https://x.test", "https://cdn.test/a.jpg", "https://cdn.test/a.jpg")]
    public void Abs_ComposesAbsoluteUrls(string baseUrl, string? path, string expected)
        => Assert.Equal(expected, SeoBuilder.Abs(baseUrl, path));

    [Fact]
    public void FirstNonBlank_SkipsNullAndWhitespace()
        => Assert.Equal("hit", SeoBuilder.FirstNonBlank(null, "", "  ", "hit", "next"));

    [Fact]
    public void FirstNonBlank_AllBlank_ReturnsNull()
        => Assert.Null(SeoBuilder.FirstNonBlank(null, "", "   "));

    [Fact]
    public void ForVehicle_UsesMetaTitleWhenSet_ElseName()
    {
        var withMeta = new Vehicle { Slug = "gs8", Name = "GS8", MetaTitle = "GS8 SUV — Best in Class" };
        var noMeta = new Vehicle { Slug = "gs3", Name = "GS3 EMZOOM" };
        Assert.Equal("GS8 SUV — Best in Class", SeoBuilder.ForVehicle(withMeta, "https://x.test").Title);
        Assert.Equal("GS3 EMZOOM", SeoBuilder.ForVehicle(noMeta, "https://x.test").Title);
    }

    [Fact]
    public void ForVehicle_SetsCanonicalTypeAndCarJsonLd()
    {
        var v = new Vehicle { Slug = "gs8", Name = "GS8", Tagline = "Bold." };
        var seo = SeoBuilder.ForVehicle(v, "https://x.test");
        Assert.Equal("/gs8", seo.CanonicalPath);
        Assert.Equal("product", seo.OgType);
        Assert.Equal("Bold.", seo.Description);
        Assert.Single(seo.JsonLd);
        Assert.Contains("\"@type\":\"Car\"", seo.JsonLd[0]);
        Assert.Contains("GS8", seo.JsonLd[0]);
    }

    [Fact]
    public void ForContentPage_FallsBackTitleToTitle_NoIntroText()
    {
        var p = new ContentPage { Slug = "about", Title = "About Us" };
        var seo = SeoBuilder.ForContentPage(p, "https://x.test");
        Assert.Equal("About Us", seo.Title);
        Assert.Equal("/about", seo.CanonicalPath);
        Assert.Equal("website", seo.OgType);
        Assert.Empty(seo.JsonLd);
    }

    [Fact]
    public void ForFormPage_UsesIntroTextForDescriptionFallback()
    {
        var p = new FormPage { Slug = "fleet", Title = "Fleet Sales", IntroText = "Bulk buying made easy." };
        var seo = SeoBuilder.ForFormPage(p, "https://x.test");
        Assert.Equal("Fleet Sales", seo.Title);
        Assert.Equal("Bulk buying made easy.", seo.Description);
    }

    [Fact]
    public void ForNews_SetsArticleTypeAndNewsArticleJsonLd()
    {
        var a = new NewsArticle { Slug = "launch", Title = "Launch Day", Excerpt = "We launched.",
            PublishedOn = new DateOnly(2026, 6, 1), ImagePath = "/assets/img/news.jpg" };
        var seo = SeoBuilder.ForNews(a, "https://x.test");
        Assert.Equal("Launch Day", seo.Title);
        Assert.Equal("We launched.", seo.Description);
        Assert.Equal("/news/launch", seo.CanonicalPath);
        Assert.Equal("article", seo.OgType);
        Assert.Contains("\"@type\":\"NewsArticle\"", seo.JsonLd[0]);
        Assert.Contains("2026-06-01", seo.JsonLd[0]);
    }

    [Fact]
    public void AutoDealerJsonLd_IncludesNameUrlAndNonEmptySameAs()
    {
        var s = new SiteSettings { Phone = "+966 11 000 0000", InstagramUrl = "https://instagram.com/gac",
            FacebookUrl = "", XUrl = "https://x.com/gac" };
        var json = SeoBuilder.AutoDealerJsonLd(s, "https://x.test");
        Assert.Contains("\"@type\":\"AutoDealer\"", json);
        Assert.Contains("GAC MUTAWAALKAZI", json);
        Assert.Contains("https://x.test/", json);
        Assert.Contains("instagram.com/gac", json);
        Assert.Contains("x.com/gac", json);
        Assert.DoesNotContain("\"\"", json); // empty FacebookUrl excluded from sameAs
    }

    [Fact]
    public void JsonLd_EscapesAngleBracketsToPreventScriptBreakout()
    {
        var v = new Vehicle { Slug = "x", Name = "</script><b>x</b>" };
        var seo = SeoBuilder.ForVehicle(v, "https://x.test");
        // A literal </script> must never appear — it would break out of the <script> block.
        Assert.DoesNotContain("</script>", seo.JsonLd[0]);
        // System.Text.Json's default encoder escapes '<' to <.
        Assert.Contains("\\u003C", seo.JsonLd[0]);
    }

    [Fact]
    public void ForVehicle_NoImage_UsesDefaultOgImage()
    {
        var v = new Vehicle { Slug = "gs8", Name = "GS8" }; // no Images
        Assert.Equal("/assets/img/logo.png", SeoBuilder.ForVehicle(v, "https://x.test").OgImage);
    }

    [Fact]
    public void ForListing_SetsTitleCanonicalAndNoJsonLd()
    {
        var seo = SeoBuilder.ForListing("Models", "/models");
        Assert.Equal("Models", seo.Title);
        Assert.Equal("/models", seo.CanonicalPath);
        Assert.Equal("website", seo.OgType);
        Assert.Empty(seo.JsonLd);
    }
}
