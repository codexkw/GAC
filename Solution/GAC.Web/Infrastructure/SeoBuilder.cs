using System.Text.Json;
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

/// <summary>Pure, testable helpers that build per-page SeoData (incl. JSON-LD) from entities.</summary>
public static class SeoBuilder
{
    public const string SiteName = "GAC Mutawa Alkadi";
    public const string DefaultDescription =
        "Discover the GAC Motor range — SUVs, sedans and EVs — from Mutawa Alkadi Automotive.";
    public const string DefaultOgImage = "/assets/img/logo.png";

    /// <summary>Compose an absolute URL from a base ("scheme://host") and a path; pass-through absolute inputs.</summary>
    public static string Abs(string baseUrl, string? path)
    {
        if (!string.IsNullOrEmpty(path) &&
            (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
             path.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            return path;
        var b = baseUrl.TrimEnd('/');
        if (string.IsNullOrEmpty(path)) return b + "/";
        return b + (path.StartsWith('/') ? path : "/" + path);
    }

    public static string? FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    public static GAC.Web.Models.SeoData ForVehicle(Vehicle v, string baseUrl)
    {
        var name = v.Name.Localize();
        var title = FirstNonBlank(v.MetaTitle.Localize(), name);
        var desc = FirstNonBlank(v.MetaDescription.Localize(), v.Tagline.Localize(),
            v.IntroText.Localize(), DefaultDescription);
        var image = UrlHelpers.ThumbPath(v);
        var seo = new GAC.Web.Models.SeoData
        {
            Title = title,
            Description = desc,
            CanonicalPath = "/" + v.Slug,
            OgImage = string.IsNullOrWhiteSpace(image) ? DefaultOgImage : image,
            OgType = "product",
        };
        seo.JsonLd.Add(CarJsonLd(v, baseUrl, name, desc));
        return seo;
    }

    public static GAC.Web.Models.SeoData ForContentPage(ContentPage p, string baseUrl)
        => new()
        {
            Title = FirstNonBlank(p.MetaTitle.Localize(), p.Title.Localize()),
            Description = FirstNonBlank(p.MetaDescription.Localize(), DefaultDescription),
            CanonicalPath = "/" + p.Slug,
            OgType = "website",
        };

    public static GAC.Web.Models.SeoData ForFormPage(FormPage p, string baseUrl)
        => new()
        {
            Title = FirstNonBlank(p.MetaTitle.Localize(), p.Title.Localize()),
            Description = FirstNonBlank(p.MetaDescription.Localize(), p.IntroText.Localize(), DefaultDescription),
            CanonicalPath = "/" + p.Slug,
            OgType = "website",
        };

    public static GAC.Web.Models.SeoData ForNews(NewsArticle a, string baseUrl)
    {
        var title = a.Title.Localize();
        var desc = FirstNonBlank(a.Excerpt.Localize(), DefaultDescription);
        var seo = new GAC.Web.Models.SeoData
        {
            Title = title,
            Description = desc,
            CanonicalPath = "/news/" + a.Slug,
            OgImage = string.IsNullOrWhiteSpace(a.ImagePath) ? DefaultOgImage : a.ImagePath,
            OgType = "article",
        };
        seo.JsonLd.Add(NewsArticleJsonLd(a, baseUrl, title, desc));
        return seo;
    }

    /// <summary>Simple listing page (e.g. /models, /news, /offers): title + canonical, no JSON-LD.</summary>
    public static GAC.Web.Models.SeoData ForListing(string? title, string canonicalPath)
        => new() { Title = title, CanonicalPath = canonicalPath, OgType = "website" };

    public static string AutoDealerJsonLd(SiteSettings s, string baseUrl)
    {
        var sameAs = new[] { s.InstagramUrl, s.FacebookUrl, s.TiktokUrl, s.SnapchatUrl, s.XUrl }
            .Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "AutoDealer",
            ["name"] = SiteName,
            ["url"] = Abs(baseUrl, "/"),
            ["logo"] = Abs(baseUrl, DefaultOgImage),
        };
        if (!string.IsNullOrWhiteSpace(s.Phone)) obj["telephone"] = s.Phone;
        if (sameAs.Length > 0) obj["sameAs"] = sameAs;
        return Serialize(obj);
    }

    private static string CarJsonLd(Vehicle v, string baseUrl, string name, string? desc)
    {
        var image = UrlHelpers.ThumbPath(v);
        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Car",
            ["name"] = name,
            ["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = "GAC" },
        };
        if (!string.IsNullOrWhiteSpace(image)) obj["image"] = Abs(baseUrl, image);
        if (!string.IsNullOrWhiteSpace(desc)) obj["description"] = desc;
        return Serialize(obj);
    }

    private static string NewsArticleJsonLd(NewsArticle a, string baseUrl, string headline, string? desc)
    {
        var obj = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "NewsArticle",
            ["headline"] = headline,
            ["datePublished"] = a.PublishedOn.ToString("yyyy-MM-dd"),
        };
        if (!string.IsNullOrWhiteSpace(a.ImagePath)) obj["image"] = Abs(baseUrl, a.ImagePath);
        if (!string.IsNullOrWhiteSpace(desc)) obj["description"] = desc;
        return Serialize(obj);
    }

    // Default encoder escapes <, >, & to \uXXXX — HTML-safe for embedding in a <script> block.
    private static string Serialize(object obj) => JsonSerializer.Serialize(obj);
}
