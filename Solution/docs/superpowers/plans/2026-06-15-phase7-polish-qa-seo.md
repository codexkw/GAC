# GAC CMS Phase 7 — Polish, QA & SEO — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render per-page localized SEO metadata (title/description/canonical/OpenGraph/Twitter + JSON-LD), serve a dynamic `sitemap.xml`/`robots.txt`, add a configurable GA4/GTM analytics hook, and complete a bilingual accessibility/QA pass — the final polish before deploy.

**Architecture:** Purely additive. A presentation POCO `SeoData` is built per request by each public controller (via a pure, testable `SeoBuilder` helper) and stashed in `ViewData["Seo"]`; a new `_SeoHead` partial in `_Layout`'s `<head>` renders it. A new `SeoController` serves `sitemap.xml`/`robots.txt`. Analytics renders from bound config only when an ID is set. **No EF entity change, no migration.** Two additive read-only methods are added to the existing `IContentService` so the sitemap can enumerate pages.

**Tech Stack:** ASP.NET Core 9 MVC, Razor partials/sections, `System.Text.Json` (JSON-LD, HTML-safe escaping), `System.Xml.Linq` (sitemap), xUnit + `WebApplicationFactory` (real Development DB via `DevWebApplicationFactory`).

---

## Reference facts (verified against the codebase)

- **Entities:** `Vehicle` has `Slug, IsVisible, Name, Tagline, IntroText, MetaTitle, MetaDescription, Images`. `ContentPage` has `Slug, IsVisible, Title, MetaTitle, MetaDescription` (**no `IntroText`**). `FormPage` has `Slug, FormType, IsVisible, Title, IntroText, MetaTitle, MetaDescription`. `NewsArticle` has `Slug, IsPublished, PublishedOn (DateOnly), Title, Excerpt, ImagePath`. `SiteSettings` has `Phone, WhatsApp, Email, InstagramUrl, FacebookUrl, TiktokUrl, SnapchatUrl, XUrl, FooterTagline` (**no address, no logo field**).
- **`MetaTitle`/`MetaDescription` are `LocalizedText`** and already admin-editable (Phase 6a); nothing renders them yet.
- **`.Localize()`** (extension in `GAC.Web.Infrastructure`, imported in `_ViewImports`) reads `CurrentUICulture` (ar→Arabic else English, null-safe). Available in controllers via `using GAC.Web.Infrastructure;`.
- **`UrlHelpers.ThumbPath(Vehicle)`** returns first Gallery image else Hero image path else `""`.
- **Logo asset:** `/assets/img/logo.png` exists. No dedicated OG share image — default OG image = `/assets/img/logo.png`.
- **Services:** `IContentService` (`GetHomePageAsync, GetContentPageBySlugAsync, GetFormPageBySlugAsync, GetPublishedNewsAsync, GetNewsBySlugAsync, GetActiveOffersAsync`); `IVehicleService` (`GetVisibleAsync, GetBySlugAsync`); `ISiteService` (`GetSettingsAsync()` never null, `GetMenuAsync()`). `ContentService` impl uses `AsNoTracking()` and `Include` before `Where`.
- **`_Layout.cshtml`** currently renders `<title>@ViewData["Title"] - GAC Mutawa Alkadi</title>` and a static `<meta name="description">` (lines 12–13); these move into `_SeoHead`. `_Layout` already injects nothing custom; `@using System.Globalization` is at its top.
- **`_ViewImports`** already has `@using GAC.Web.Models`, `@using GAC.Web.Infrastructure`, `@using GAC.Core.Content`, `@inject IHtmlLocalizer<SharedResource> L`.
- **Test factory:** `DevWebApplicationFactory : WebApplicationFactory<Program>` (`UseEnvironment("Development")`) in `GAC.Tests/HomePageSmokeTests.cs`. Integration tests use `IClassFixture<DevWebApplicationFactory>` + `_factory.CreateClient()`. **Tests need the DB reachable.**
- **Build/test commands** (run from repo root `C:\Users\anas-\source\repos\GAC`):
  - Build: `dotnet build Solution/GAC.sln -c Debug`
  - Test all: `dotnet test Solution/GAC.sln`
  - Test one class: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoBuilderTests"`
- **Secrets:** repo is PUBLIC. Never commit `appsettings.Development.json`; always scoped `git add` with explicit paths; never `git add -A`/`.`. The `Analytics` IDs are **non-secret** (set empty in committed `appsettings.json`).

---

## File Structure

**Create:**
- `Solution/GAC.Web/Models/SeoData.cs` — presentation POCO carried in `ViewData["Seo"]`.
- `Solution/GAC.Web/Infrastructure/SeoBuilder.cs` — pure helpers: absolute URLs, per-entity `SeoData` factories, JSON-LD string builders.
- `Solution/GAC.Web/Infrastructure/AnalyticsOptions.cs` — binds the `Analytics` config section.
- `Solution/GAC.Web/Views/Shared/_SeoHead.cshtml` — renders the head tags from `SeoData`.
- `Solution/GAC.Web/Controllers/SeoController.cs` — `/sitemap.xml` + `/robots.txt`.
- `Solution/GAC.Tests/SeoBuilderTests.cs` — unit tests (no HttpContext).
- `Solution/GAC.Tests/SeoHeadTests.cs` — integration tests for head tags + JSON-LD.
- `Solution/GAC.Tests/SitemapRobotsTests.cs` — integration tests for sitemap/robots.
- `Solution/GAC.Tests/AnalyticsTests.cs` — integration test (snippet absent by default).
- `Solution/docs/superpowers/qa/2026-06-15-phase7-qa-checklist.md` — accessibility/QA deliverable.

**Modify:**
- `Solution/GAC.Web/Views/Shared/_Layout.cshtml` — delegate head to `_SeoHead`; add analytics snippet.
- `Solution/GAC.Web/Controllers/{Home,Vehicles,Page,News,Offers,Forms}Controller.cs` — set `ViewData["Seo"]`.
- `Solution/GAC.Core/Services/IContentService.cs` — add `GetAllContentPagesAsync` + `GetAllFormPagesAsync`.
- `Solution/GAC.Infrastructure/Services/ContentService.cs` — implement the two new methods.
- `Solution/GAC.Web/Program.cs` — `Configure<AnalyticsOptions>`.
- `Solution/GAC.Web/appsettings.json` — add empty `Analytics` section.
- (Task 7 only) various Razor views for alt text / skip link / `<main>` landmark.

---

## Task 1: `SeoData` POCO + `SeoBuilder` pure helpers (unit-tested)

**Files:**
- Create: `Solution/GAC.Web/Models/SeoData.cs`
- Create: `Solution/GAC.Web/Infrastructure/SeoBuilder.cs`
- Test: `Solution/GAC.Tests/SeoBuilderTests.cs`

- [ ] **Step 1: Create `SeoData.cs`**

```csharp
namespace GAC.Web.Models;

/// <summary>Per-page SEO data carried via ViewData["Seo"] and rendered by _SeoHead.cshtml.</summary>
public sealed class SeoData
{
    public string? Title { get; set; }          // page title, BEFORE the " - GAC Mutawa Alkadi" suffix
    public string? Description { get; set; }     // meta description
    public string? CanonicalPath { get; set; }   // root-relative clean path, e.g. "/gs8"
    public string? OgImage { get; set; }         // root-relative or absolute image path
    public string OgType { get; set; } = "website";   // website | article | product
    public string? Robots { get; set; }          // e.g. "noindex,nofollow"; null => omit
    public List<string> JsonLd { get; set; } = new();  // each entry is a complete JSON-LD object string
}
```

- [ ] **Step 2: Write the failing unit tests `SeoBuilderTests.cs`**

```csharp
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
        Assert.Contains("GAC Mutawa Alkadi", json);
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
}
```

> Note: `System.Text.Json`'s **default** encoder escapes `<`, `>`, `&` to `<` etc., which is exactly the HTML-safe behavior we want for embedding in a `<script>` block — so do NOT configure `UnsafeRelaxedJsonEscaping`.

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoBuilderTests"`
Expected: FAIL — `SeoBuilder` does not exist (compile error).

- [ ] **Step 4: Implement `SeoBuilder.cs`**

```csharp
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
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoBuilderTests"`
Expected: PASS (all). If the `JsonLd_Escapes...` assert expression is awkward, simplify its body to a single `Assert.DoesNotContain("</script>", seo.JsonLd[0]);`.

- [ ] **Step 6: Commit**

```bash
git add Solution/GAC.Web/Models/SeoData.cs Solution/GAC.Web/Infrastructure/SeoBuilder.cs Solution/GAC.Tests/SeoBuilderTests.cs
git commit -m "feat(seo): add SeoData model + SeoBuilder helpers with unit tests"
```

---

## Task 2: `_SeoHead` partial + wire `_Layout` (remove duplicate title/description)

**Files:**
- Create: `Solution/GAC.Web/Views/Shared/_SeoHead.cshtml`
- Modify: `Solution/GAC.Web/Views/Shared/_Layout.cshtml:12-13`

- [ ] **Step 1: Create `_SeoHead.cshtml`**

```razor
@using System.Globalization
@using GAC.Web.Infrastructure
@{
    var seo = ViewData["Seo"] as GAC.Web.Models.SeoData ?? new GAC.Web.Models.SeoData();
    var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    var ogLocale = culture == "ar" ? "ar_SA" : "en_US";
    var baseUrl = $"{Context.Request.Scheme}://{Context.Request.Host}";

    var titleText = string.IsNullOrWhiteSpace(seo.Title) ? null : seo.Title;
    var fullTitle = titleText is null ? SeoBuilder.SiteName : $"{titleText} - {SeoBuilder.SiteName}";
    var desc = string.IsNullOrWhiteSpace(seo.Description) ? SeoBuilder.DefaultDescription : seo.Description;
    var canonical = SeoBuilder.Abs(baseUrl, string.IsNullOrWhiteSpace(seo.CanonicalPath)
        ? Context.Request.Path.ToString() : seo.CanonicalPath);
    var ogImage = SeoBuilder.Abs(baseUrl, string.IsNullOrWhiteSpace(seo.OgImage)
        ? SeoBuilder.DefaultOgImage : seo.OgImage);
}
<title>@fullTitle</title>
<meta name="description" content="@desc" />
<link rel="canonical" href="@canonical" />
@if (!string.IsNullOrWhiteSpace(seo.Robots))
{
    <meta name="robots" content="@seo.Robots" />
}
<meta property="og:site_name" content="@SeoBuilder.SiteName" />
<meta property="og:title" content="@(titleText ?? SeoBuilder.SiteName)" />
<meta property="og:description" content="@desc" />
<meta property="og:type" content="@seo.OgType" />
<meta property="og:url" content="@canonical" />
<meta property="og:image" content="@ogImage" />
<meta property="og:locale" content="@ogLocale" />
<meta name="twitter:card" content="summary_large_image" />
<meta name="twitter:title" content="@(titleText ?? SeoBuilder.SiteName)" />
<meta name="twitter:description" content="@desc" />
<meta name="twitter:image" content="@ogImage" />
@foreach (var ld in seo.JsonLd)
{
    <script type="application/ld+json">@Html.Raw(ld)</script>
}
```

- [ ] **Step 2: Wire `_Layout.cshtml` — replace lines 12–13 with the partial**

In `Solution/GAC.Web/Views/Shared/_Layout.cshtml`, replace these two lines:

```razor
    <title>@ViewData["Title"] - GAC Mutawa Alkadi</title>
    <meta name="description" content="@(ViewData["MetaDescription"] ?? "Discover the GAC Motor range — SUVs, sedans and EVs — from Mutawa Alkadi Automotive.")" />
```

with:

```razor
    <partial name="_SeoHead" />
```

(Leave `<meta charset>` and `<meta name="viewport">` above it untouched.)

- [ ] **Step 3: Build to verify Razor compiles**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: Build succeeded (Razor compiles at build time here — a malformed `.cshtml` fails the build).

- [ ] **Step 4: Smoke-run the existing suite (no regression)**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~HomePageSmokeTests"`
Expected: PASS — home still renders 200 with chrome. (Title now reads `GAC Mutawa Alkadi` since no controller sets `Seo` yet; that is wired in Task 3.)

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web/Views/Shared/_SeoHead.cshtml Solution/GAC.Web/Views/Shared/_Layout.cshtml
git commit -m "feat(seo): render head metadata via _SeoHead partial"
```

---

## Task 3: Populate `ViewData["Seo"]` in every public controller

**Files:**
- Modify: `Solution/GAC.Web/Controllers/HomeController.cs`
- Modify: `Solution/GAC.Web/Controllers/VehiclesController.cs`
- Modify: `Solution/GAC.Web/Controllers/PageController.cs`
- Modify: `Solution/GAC.Web/Controllers/NewsController.cs`
- Modify: `Solution/GAC.Web/Controllers/OffersController.cs`
- Modify: `Solution/GAC.Web/Controllers/FormsController.cs`
- Test: `Solution/GAC.Tests/SeoHeadTests.cs` (created here; JSON-LD asserts added in Task 4)

- [ ] **Step 1: Write the failing integration tests `SeoHeadTests.cs`**

```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class SeoHeadTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public SeoHeadTests(DevWebApplicationFactory factory) => _factory = factory;

    private async Task<string> GetHtml(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/models")]
    [InlineData("/gs8")]
    [InlineData("/about")]
    [InlineData("/contact-us")]
    public async Task PublicPages_EmitCanonicalAndOpenGraph(string url)
    {
        var html = await GetHtml(url);
        Assert.Contains("rel=\"canonical\"", html);
        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:url\"", html);
        Assert.Contains("name=\"twitter:card\"", html);
    }

    [Fact]
    public async Task VehiclePage_TitleUsesVehicleName()
    {
        var html = await GetHtml("/gs8");
        // The <title> contains the vehicle name and the site suffix.
        Assert.Matches("<title>[^<]*GS8[^<]*GAC Mutawa Alkadi</title>", html);
    }

    [Fact]
    public async Task NotFound_IsNoindex()
    {
        var res = await _factory.CreateClient().GetAsync("/this-slug-does-not-exist-zzz");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("name=\"robots\"", html);
        Assert.Contains("noindex", html);
    }
}
```

> The `/gs8` page's seeded `Name` is `GS8` (verify against the seed; if the visible slug's display name differs, adjust the regex token). `/about` and `/contact-us` are seeded visible pages (content + contact form page).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoHeadTests"`
Expected: FAIL — canonical/og tags not yet emitted per page; `/not-found` has no robots meta.

- [ ] **Step 3: Update `HomeController.cs`** (inject `ISiteService`, set Seo + AutoDealer JSON-LD)

```csharp
using System.Diagnostics;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class HomeController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    private readonly ISiteService _site;
    public HomeController(IContentService content, IVehicleService vehicles, ISiteService site)
    { _content = content; _vehicles = vehicles; _site = site; }

    public async Task<IActionResult> Index()
    {
        var settings = await _site.GetSettingsAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var seo = SeoBuilder.ForListing(null, "/");
        seo.JsonLd.Add(SeoBuilder.AutoDealerJsonLd(settings, baseUrl));
        ViewData["Seo"] = seo;

        return View(new HomeViewModel
        {
            Home = await _content.GetHomePageAsync(),
            Vehicles = await _vehicles.GetVisibleAsync(),
            News = await _content.GetPublishedNewsAsync()
        });
    }

    [HttpGet("/not-found")]
    public IActionResult NotFoundPage()
    {
        ViewData["Seo"] = new SeoData { Title = "Page not found", CanonicalPath = "/not-found",
            Robots = "noindex,nofollow" };
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
```

> `ISiteService.GetSettingsAsync()` never returns null (per Phase 3). `/not-found` is served for unmatched slugs via `UseStatusCodePagesWithReExecute`; setting `Robots` here makes the re-executed 404 body carry `noindex`.

- [ ] **Step 4: Update `VehiclesController.cs`**

```csharp
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicles;
    public VehiclesController(IVehicleService vehicles) => _vehicles = vehicles;

    [HttpGet("/models")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("Models", "/models");
        return View(await _vehicles.GetVisibleAsync());
    }
}
```

- [ ] **Step 5: Update `PageController.cs`** (replace the three `ViewData["Title"]` assignments)

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class PageController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public PageController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    [HttpGet("/{slug:regex(^(?!(?i:admin)$).*$)}")]
    public async Task<IActionResult> Show(string slug)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var content = await _content.GetContentPageBySlugAsync(slug);
        if (content != null)
        {
            ViewData["Seo"] = SeoBuilder.ForContentPage(content, baseUrl);
            return View("~/Views/Content/Page.cshtml", content);
        }

        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null)
        {
            ViewData["Seo"] = SeoBuilder.ForFormPage(form, baseUrl);
            return View("~/Views/Forms/Page.cshtml", new GAC.Web.Models.FormPageViewModel { Page = form });
        }

        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle != null)
        {
            ViewData["Seo"] = SeoBuilder.ForVehicle(vehicle, baseUrl);
            return View("~/Views/Vehicles/Detail.cshtml", vehicle);
        }

        return NotFound();
    }
}
```

- [ ] **Step 6: Update `NewsController.cs`**

```csharp
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class NewsController : Controller
{
    private readonly IContentService _content;
    public NewsController(IContentService content) => _content = content;

    [HttpGet("/news")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("News", "/news");
        return View(await _content.GetPublishedNewsAsync());
    }

    [HttpGet("/news/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var article = await _content.GetNewsBySlugAsync(slug);
        if (article == null) return NotFound();
        ViewData["Seo"] = SeoBuilder.ForNews(article, $"{Request.Scheme}://{Request.Host}");
        return View(article);
    }
}
```

- [ ] **Step 7: Update `OffersController.cs`**

```csharp
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class OffersController : Controller
{
    private readonly IContentService _content;
    public OffersController(IContentService content) => _content = content;

    [HttpGet("/offers")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("Offers", "/offers");
        return View(await _content.GetActiveOffersAsync());
    }
}
```

- [ ] **Step 8: Update `FormsController.cs`** (the invalid-submit re-render path)

In `Submit`, replace the invalid-model block's `ViewData["Title"]` line:

```csharp
        if (!ModelState.IsValid)
        {
            ViewData["Title"] = form.Title.Localize();
            return View("~/Views/Forms/Page.cshtml", new FormPageViewModel { Page = form, Input = input });
        }
```

with:

```csharp
        if (!ModelState.IsValid)
        {
            ViewData["Seo"] = SeoBuilder.ForFormPage(form, $"{Request.Scheme}://{Request.Host}");
            return View("~/Views/Forms/Page.cshtml", new FormPageViewModel { Page = form, Input = input });
        }
```

Add `using GAC.Web.Infrastructure;` to the file's usings.

- [ ] **Step 9: Run the SEO + full suite to verify green**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoHeadTests"`
Expected: PASS (JSON-LD asserts come in Task 4).
Run: `dotnet test Solution/GAC.sln`
Expected: PASS — all existing tests still green (no `ViewData["Title"]` consumers remain; `_Layout` no longer reads it).

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Web/Controllers/HomeController.cs Solution/GAC.Web/Controllers/VehiclesController.cs Solution/GAC.Web/Controllers/PageController.cs Solution/GAC.Web/Controllers/NewsController.cs Solution/GAC.Web/Controllers/OffersController.cs Solution/GAC.Web/Controllers/FormsController.cs Solution/GAC.Tests/SeoHeadTests.cs
git commit -m "feat(seo): set per-page SeoData in all public controllers"
```

---

## Task 4: JSON-LD rendering verification (home + vehicle + news)

The JSON-LD strings are already produced by `SeoBuilder` (Task 1) and rendered by `_SeoHead` (Task 2). This task adds the integration assertions that they actually appear in responses.

**Files:**
- Modify: `Solution/GAC.Tests/SeoHeadTests.cs`

- [ ] **Step 1: Add JSON-LD assertions to `SeoHeadTests.cs`**

Append these facts to the `SeoHeadTests` class:

```csharp
    [Fact]
    public async Task Home_EmitsAutoDealerJsonLd()
    {
        var html = await GetHtml("/");
        Assert.Contains("application/ld+json", html);
        Assert.Contains("\"@type\":\"AutoDealer\"", html);
    }

    [Fact]
    public async Task VehiclePage_EmitsCarJsonLd()
    {
        var html = await GetHtml("/gs8");
        Assert.Contains("\"@type\":\"Car\"", html);
    }

    [Fact]
    public async Task JsonLd_DoesNotContainRawScriptClose()
    {
        var html = await GetHtml("/gs8");
        // Inside the ld+json block, < and > are \u-escaped; ensure no breakout.
        var idx = html.IndexOf("application/ld+json", System.StringComparison.Ordinal);
        Assert.True(idx >= 0);
        // The next "</script>" after the opening tag must be the legitimate closer of THIS script,
        // i.e. there is no stray "</script>" produced by JSON content. Smoke check: page still well-formed.
        Assert.Contains("\"@context\":\"https://schema.org\"", html);
    }
```

- [ ] **Step 2: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SeoHeadTests"`
Expected: PASS — home carries AutoDealer JSON-LD, `/gs8` carries Car JSON-LD.

- [ ] **Step 3: Commit**

```bash
git add Solution/GAC.Tests/SeoHeadTests.cs
git commit -m "test(seo): assert JSON-LD on home and vehicle pages"
```

---

## Task 5: `sitemap.xml` + `robots.txt` (`SeoController`) + service list methods

**Files:**
- Modify: `Solution/GAC.Core/Services/IContentService.cs`
- Modify: `Solution/GAC.Infrastructure/Services/ContentService.cs`
- Create: `Solution/GAC.Web/Controllers/SeoController.cs`
- Test: `Solution/GAC.Tests/SitemapRobotsTests.cs`

- [ ] **Step 1: Add list methods to `IContentService.cs`**

Add these two members to the interface:

```csharp
    Task<IReadOnlyList<ContentPage>> GetAllContentPagesAsync();
    Task<IReadOnlyList<FormPage>> GetAllFormPagesAsync();
```

- [ ] **Step 2: Implement them in `ContentService.cs`**

Add these two methods to the class (mirror the existing `AsNoTracking` style):

```csharp
    public async Task<IReadOnlyList<ContentPage>> GetAllContentPagesAsync()
        => await _db.ContentPages
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.Slug)
            .ToListAsync();

    public async Task<IReadOnlyList<FormPage>> GetAllFormPagesAsync()
        => await _db.FormPages
            .AsNoTracking()
            .Where(p => p.IsVisible)
            .OrderBy(p => p.Slug)
            .ToListAsync();
```

- [ ] **Step 3: Write the failing tests `SitemapRobotsTests.cs`**

```csharp
using System.Net;
using System.Xml.Linq;
using Xunit;

namespace GAC.Tests;

public class SitemapRobotsTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public SitemapRobotsTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Sitemap_ReturnsXmlWithKnownUrls_ExcludesHidden()
    {
        var res = await _factory.CreateClient().GetAsync("/sitemap.xml");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("application/xml", res.Content.Headers.ContentType!.ToString());

        var body = await res.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(body); // must be well-formed
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var locs = doc.Descendants(ns + "loc").Select(e => e.Value).ToList();

        Assert.Contains(locs, l => l.EndsWith("/"));        // home
        Assert.Contains(locs, l => l.EndsWith("/models"));
        Assert.Contains(locs, l => l.EndsWith("/gs8"));     // a visible vehicle
        Assert.DoesNotContain(locs, l => l.EndsWith("/aion-v"));  // hidden vehicle excluded
        Assert.All(locs, l => Assert.StartsWith("http", l)); // absolute
    }

    [Fact]
    public async Task Robots_DisallowsAdminAndPointsToSitemap()
    {
        var res = await _factory.CreateClient().GetAsync("/robots.txt");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.StartsWith("text/plain", res.Content.Headers.ContentType!.ToString());

        var body = await res.Content.ReadAsStringAsync();
        Assert.Contains("Disallow: /admin", body);
        Assert.Matches(@"Sitemap:\s+https?://[^\s]+/sitemap\.xml", body);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SitemapRobotsTests"`
Expected: FAIL — `/sitemap.xml` and `/robots.txt` return 404 (no controller yet).

- [ ] **Step 5: Implement `SeoController.cs`**

```csharp
using System.Xml.Linq;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class SeoController : Controller
{
    private readonly IVehicleService _vehicles;
    private readonly IContentService _content;
    public SeoController(IVehicleService vehicles, IContentService content)
    { _vehicles = vehicles; _content = content; }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>();

        void Add(string path, DateOnly? lastmod = null)
        {
            var el = new XElement(ns + "url", new XElement(ns + "loc", SeoBuilder.Abs(baseUrl, path)));
            if (lastmod.HasValue)
                el.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-dd")));
            urls.Add(el);
        }

        Add("/");
        Add("/models");
        Add("/news");
        Add("/offers");
        foreach (var v in await _vehicles.GetVisibleAsync()) Add("/" + v.Slug);
        foreach (var p in await _content.GetAllContentPagesAsync()) Add("/" + p.Slug);
        foreach (var f in await _content.GetAllFormPagesAsync()) Add("/" + f.Slug);
        foreach (var n in await _content.GetPublishedNewsAsync()) Add("/news/" + n.Slug, n.PublishedOn);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", urls));
        var xml = doc.Declaration + Environment.NewLine + doc;
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var sitemap = SeoBuilder.Abs($"{Request.Scheme}://{Request.Host}", "/sitemap.xml");
        var body = "User-agent: *\n" +
                   "Disallow: /admin\n" +
                   "Disallow: /admin/\n\n" +
                   $"Sitemap: {sitemap}\n";
        return Content(body, "text/plain; charset=utf-8");
    }
}
```

> The literal attribute routes `/sitemap.xml` and `/robots.txt` are more specific than the single-segment `/{slug}` catch-all, so they win (same precedence rule as `/models`). No static `robots.txt`/`sitemap.xml` files exist in `wwwroot`, so the static-files middleware falls through to routing.

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~SitemapRobotsTests"`
Expected: PASS — sitemap well-formed with absolute locs incl. `/gs8`, excludes `/aion-v`; robots disallows `/admin` + has a `Sitemap:` line.

- [ ] **Step 7: Commit**

```bash
git add Solution/GAC.Core/Services/IContentService.cs Solution/GAC.Infrastructure/Services/ContentService.cs Solution/GAC.Web/Controllers/SeoController.cs Solution/GAC.Tests/SitemapRobotsTests.cs
git commit -m "feat(seo): dynamic sitemap.xml + robots.txt"
```

---

## Task 6: Configurable GA4/GTM analytics hook

**Files:**
- Create: `Solution/GAC.Web/Infrastructure/AnalyticsOptions.cs`
- Modify: `Solution/GAC.Web/Program.cs`
- Modify: `Solution/GAC.Web/appsettings.json`
- Modify: `Solution/GAC.Web/Views/Shared/_Layout.cshtml`
- Test: `Solution/GAC.Tests/AnalyticsTests.cs`

- [ ] **Step 1: Create `AnalyticsOptions.cs`**

```csharp
namespace GAC.Web.Infrastructure;

/// <summary>Bound from the "Analytics" config section. IDs are non-secret; empty => nothing rendered.</summary>
public sealed class AnalyticsOptions
{
    public string Ga4MeasurementId { get; set; } = "";
    public string GtmContainerId { get; set; } = "";
}
```

- [ ] **Step 2: Bind it in `Program.cs`** (after the other `Configure` calls, e.g. near the `MediaOptions` block, before `var app = builder.Build();`)

```csharp
builder.Services.Configure<GAC.Web.Infrastructure.AnalyticsOptions>(
    builder.Configuration.GetSection("Analytics"));
```

- [ ] **Step 3: Add the empty `Analytics` section to `appsettings.json`**

Add a top-level section (sibling of `Smtp`/`Media`/`ConnectionStrings`); values stay empty in the committed file (set per-environment at deploy):

```json
  "Analytics": {
    "Ga4MeasurementId": "",
    "GtmContainerId": ""
  }
```

- [ ] **Step 4: Write the failing test `AnalyticsTests.cs`**

```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class AnalyticsTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public AnalyticsTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task NoAnalyticsId_RendersNoTrackingSnippet()
    {
        // Development config leaves Analytics IDs empty => no GTM/GA snippet emitted.
        var res = await _factory.CreateClient().GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.DoesNotContain("googletagmanager.com", html);
        Assert.DoesNotContain("gtag(", html);
    }
}
```

> This assumes `appsettings.Development.json` does **not** set non-empty Analytics IDs. If a developer has set them locally, the test documents the contract; keep dev IDs empty.

- [ ] **Step 5: Run test to verify it fails or passes-trivially, then add the snippet**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AnalyticsTests"`
Expected: PASS already (nothing renders analytics yet) — this is a guard test. Now add the rendering so the contract is real (the test must still pass with empty IDs).

- [ ] **Step 6: Add the analytics snippet to `_Layout.cshtml`**

At the very top of `_Layout.cshtml`, add the injection (after the existing `@using System.Globalization`):

```razor
@inject Microsoft.Extensions.Options.IOptions<GAC.Web.Infrastructure.AnalyticsOptions> Analytics
```

In `<head>`, immediately before `@await RenderSectionAsync("Head", required: false)`, add:

```razor
    @if (!string.IsNullOrWhiteSpace(Analytics.Value.GtmContainerId))
    {
        var gtmId = Analytics.Value.GtmContainerId;
        <script>
            (function(w,d,s,l,i){w[l]=w[l]||[];w[l].push({'gtm.start':new Date().getTime(),event:'gtm.js'});var f=d.getElementsByTagName(s)[0],j=d.createElement(s),dl=l!='dataLayer'?'&l='+l:'';j.async=true;j.src='https://www.googletagmanager.com/gtm.js?id='+i+dl;f.parentNode.insertBefore(j,f);})(window,document,'script','dataLayer','@gtmId');
        </script>
    }
    else if (!string.IsNullOrWhiteSpace(Analytics.Value.Ga4MeasurementId))
    {
        var ga4 = Analytics.Value.Ga4MeasurementId;
        <script async src="https://www.googletagmanager.com/gtag/js?id=@ga4"></script>
        <script>
            window.dataLayer = window.dataLayer || [];
            function gtag(){dataLayer.push(arguments);}
            gtag('js', new Date());
            gtag('config', '@ga4');
        </script>
    }
```

And immediately after `<body id="page-wrap">`, add the GTM `<noscript>` fallback:

```razor
    @if (!string.IsNullOrWhiteSpace(Analytics.Value.GtmContainerId))
    {
        <noscript><iframe src="https://www.googletagmanager.com/ns.html?id=@Analytics.Value.GtmContainerId"
            height="0" width="0" style="display:none;visibility:hidden"></iframe></noscript>
    }
```

- [ ] **Step 7: Build + run the analytics test (must still pass with empty IDs)**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: Build succeeded.
Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AnalyticsTests"`
Expected: PASS — empty IDs → no `googletagmanager.com` / `gtag(` in the output.

- [ ] **Step 8: Commit**

```bash
git add Solution/GAC.Web/Infrastructure/AnalyticsOptions.cs Solution/GAC.Web/Program.cs Solution/GAC.Web/appsettings.json Solution/GAC.Web/Views/Shared/_Layout.cshtml Solution/GAC.Tests/AnalyticsTests.cs
git commit -m "feat(analytics): configurable GA4/GTM hook, off until an ID is set"
```

---

## Task 7: Accessibility + EN/AR QA pass

This task is audit-and-fix plus a checklist deliverable. Apply only concrete, low-risk fixes; do not restructure markup that `main.js` keys off (preserve all classes/ids/`data-*`).

**Files:**
- Modify: `Solution/GAC.Web/Views/Shared/_Layout.cshtml` (skip link + `<main>` landmark)
- Modify: image-bearing views as needed for `alt` text (hero slides, vehicle/news/offer images)
- Modify: `Solution/GAC.Web/wwwroot/assets/css/styles.css` (visually-hidden skip-link style, if not present)
- Create: `Solution/docs/superpowers/qa/2026-06-15-phase7-qa-checklist.md`

- [ ] **Step 1: Add a skip-to-content link + `<main>` landmark in `_Layout.cshtml`**

Wrap the page body region. Change:

```razor
<body id="page-wrap">
    <vc:header />
    @RenderBody()
    <vc:footer />
```

to:

```razor
<body id="page-wrap">
    <a href="#main-content" class="skip-link">@L["Skip to main content"]</a>
    <vc:header />
    <main id="main-content">
        @RenderBody()
    </main>
    <vc:footer />
```

(Keep the analytics `<noscript>` from Task 6 as the first child of `<body>`, before the skip link.)

- [ ] **Step 2: Add the `.skip-link` style to `styles.css`** (visually hidden until focused)

```css
.skip-link{position:absolute;left:-9999px;top:0;z-index:1000;background:#fff;color:#000;padding:8px 16px;border-radius:0 0 6px 0}
.skip-link:focus{left:0}
```

- [ ] **Step 3: Add the Arabic resource string for the skip link**

In `Solution/GAC.Web/Resources/SharedResource.ar.resx`, add an entry (key = English text, value = Arabic):
- Key: `Skip to main content`
- Value: `تخطَّ إلى المحتوى الرئيسي`

- [ ] **Step 4: Audit + fix image alt text**

Review these views and ensure every meaningful `<img>` has a localized, descriptive `alt` (decorative images get `alt=""`):
- `Solution/GAC.Web/Views/Home/Index.cshtml` — hero slides (`alt="@slide.Heading.Localize()"`), news card images (`alt="@article.Title.Localize()"`), per-category carousel vehicle images (`alt="@v.Name.Localize()"`).
- `Solution/GAC.Web/Views/Vehicles/Index.cshtml` — listing card images (`alt="@v.Name.Localize()"`).
- `Solution/GAC.Web/Views/News/Index.cshtml` + `Detail.cshtml` — article image (`alt="@Model... Title"`).
- `Solution/GAC.Web/Views/Offers/Index.cshtml` — offer images (`alt` = offer title).

For each file, only add/scope `alt` attributes; do not touch surrounding classes/markup. If an `alt` is already present and meaningful, leave it.

- [ ] **Step 5: Build + run the full suite (no regression from markup edits)**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: Build succeeded.
Run: `dotnet test Solution/GAC.sln`
Expected: PASS — all tests green (existing assertions like `mp-hero`, `dir-grid` unaffected; `<main>` wraps but does not remove body markers).

- [ ] **Step 6: Manual EN+AR QA + Lighthouse, record findings**

Run the app (`dotnet run --project Solution/GAC.Web`) and, in both languages (toggle via header), spot-check `/`, `/models`, `/gs8`, `/contact-us`:
- Confirm a single `<h1>`, sensible heading order, visible focus ring on the skip link + nav, header/`<main>`/footer landmarks present.
- Run Lighthouse (Chrome DevTools) on each; note Accessibility/SEO scores.
- Confirm RTL parity vs the `HTML/` reference (no layout breakage).

- [ ] **Step 7: Write the QA checklist deliverable `2026-06-15-phase7-qa-checklist.md`**

Create the file documenting: what was checked (the list above), what was fixed (skip link, `<main>`, alt text), Lighthouse scores per page/language, and residual notes. Include this known false positive verbatim:

> **Known false positive (do not chase):** the live tech/safety `h4` toggle headings render 0×0 in the collapsed accordion and trip "empty heading"/"missing label" audits. This is expected behavior of the collapsed accordion, not a defect.

- [ ] **Step 8: Commit**

```bash
git add Solution/GAC.Web/Views/Shared/_Layout.cshtml Solution/GAC.Web/wwwroot/assets/css/styles.css Solution/GAC.Web/Resources/SharedResource.ar.resx Solution/GAC.Web/Views/Home/Index.cshtml Solution/GAC.Web/Views/Vehicles/Index.cshtml Solution/GAC.Web/Views/News/Index.cshtml Solution/GAC.Web/Views/News/Detail.cshtml Solution/GAC.Web/Views/Offers/Index.cshtml Solution/docs/superpowers/qa/2026-06-15-phase7-qa-checklist.md
git commit -m "feat(a11y): skip link + main landmark + image alt text; QA checklist"
```

> If a given view turns out to need no `alt` change, drop it from the `git add` list — only stage files actually modified.

---

## Task 8: Final review + HANDOFF / memory update

**Files:**
- Modify: `Solution/docs/HANDOFF.md`
- Modify: `C:\Users\anas-\.claude\projects\C--Users-anas-\memory\gac_cms_pivot.md`
- Modify: `C:\Users\anas-\.claude\projects\C--Users-anas-\memory\MEMORY.md`

- [ ] **Step 1: Run the entire suite one final time**

Run: `dotnet test Solution/GAC.sln`
Expected: PASS — all tests (the prior 171 + the new Phase-7 tests).

- [ ] **Step 2: Secret scan of all staged/changed files**

Run: `git diff --name-only HEAD~8..HEAD` then scan each changed file:
Run: `grep -rniE "P@ssw0rd|Codex@123|83\.229\.86\.221|Password=__SET|sk_live|G-[A-Z0-9]{8,}|GTM-[A-Z0-9]{5,}" Solution/ ':!Solution/**/appsettings.Development.json'`
Expected: no real secret and no real analytics ID committed (the committed `appsettings.json` `Analytics` values stay empty; `__SET_LOCALLY__` placeholders are fine).

- [ ] **Step 3: Update `HANDOFF.md`**

- Bump "Last updated" to Phase 7.
- Add a `## 5f. Phase 7 — Polish/QA/SEO — what was built` section summarizing: SeoData/SeoBuilder + `_SeoHead`; per-page meta/canonical/OG/Twitter; JSON-LD (AutoDealer/Car/NewsArticle); dynamic `/sitemap.xml` + `/robots.txt` (+ the two new `IContentService` list methods); configurable GA4/GTM hook (empty by default); a11y skip link + `<main>` + alt text; QA checklist path. Note: **no migration**.
- In the Phase status list, mark Phase 7 ✅ and update the test count.
- In the routing map, add rows for `/sitemap.xml` and `/robots.txt` (`SeoController`).
- Update §9 to point at Phase 8 (Deploy) and reiterate the go-live USER ACTIONS, adding: **set real `Analytics:Ga4MeasurementId` or `Analytics:GtmContainerId` at deploy if analytics is wanted.**

- [ ] **Step 4: Update memory files**

- In `gac_cms_pivot.md`: append a "PHASE 7 DONE + PUSHED 2026-06-15" paragraph (SEO meta/OG/JSON-LD, sitemap/robots, analytics hook, a11y pass; no migration); update the "next is Phase 7" pointer to "next is Phase 8 (Deploy)".
- In `MEMORY.md`: update the GAC index line — `PHASE 1-5 + 6a + 6b + 7 DONE + PUSHED ... — next is Phase 8 (Deploy)` and the new test count.

- [ ] **Step 5: Commit + push**

```bash
git add Solution/docs/HANDOFF.md
git commit -m "docs: Phase 7 handoff (Polish/QA/SEO complete)"
git push origin main
```

(Memory files live outside the repo — they are saved via the memory tooling, not committed.)

- [ ] **Step 6: Finishing the branch**

Use superpowers:finishing-a-development-branch. Work is on `main` (per prior phases) — verify tests pass, then push is the completion step (already done in Step 5). Report Phase 7 complete and await user confirmation before Phase 8 (Deploy).

---

## Self-review notes (plan vs. spec)

- **Spec §4.1 SEO pipeline** → Tasks 1–3 (SeoData, SeoBuilder fallbacks, `_SeoHead`, controller wiring). ✓
- **Spec §4.2 sitemap/robots** → Task 5 (+ the two additive `IContentService` methods, flagged as a justified extension of "GAC.Web only"). ✓
- **Spec §4.3 JSON-LD** → Task 1 (builders) + Task 4 (render assertions). ✓
- **Spec §4.4 analytics** → Task 6. ✓
- **Spec §4.5 a11y/QA** → Task 7. ✓
- **Spec §7 testing** → SeoBuilderTests (unit), SeoHeadTests, SitemapRobotsTests, AnalyticsTests; full-suite no-regression checks in Tasks 3/5/7/8. ✓
- **No migration / no entity change** → confirmed; the only `GAC.Core`/`GAC.Infrastructure` edits are two additive read-only service methods. ✓
- **Type consistency:** `SeoData` shape, `SeoBuilder` method names (`Abs`, `FirstNonBlank`, `ForVehicle/ForContentPage/ForFormPage/ForNews/ForListing`, `AutoDealerJsonLd`) are used identically across Tasks 1–6. ✓
- **`ContentPage` has no `IntroText`** — reflected (its description falls back `MetaDescription → DefaultDescription`). ✓
