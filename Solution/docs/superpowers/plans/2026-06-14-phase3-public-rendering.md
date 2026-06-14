# Phase 3 — Public Rendering, Routing & Clean URLs Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Render every public page server-side from the database with clean, `.html`-free URLs, a fully DB-driven header/megamenu/footer, and 301 redirects from the old `*.html` paths — preserving pixel parity with the existing static clone.

**Architecture:** A thin service layer in `GAC.Core` (interfaces) + `GAC.Infrastructure` (EF implementations) feeds MVC controllers and two layout ViewComponents (Header/Footer). Most page *bodies* are ported verbatim from `HTML/*.html` into Razor partials (decision: "port markup as-is"); the surrounding chrome, hero image, title and listings bind from the DB. A single catch-all `PageController.Show("/{slug}")` resolves a slug to a ContentPage, FormPage or visible Vehicle; dedicated controllers own `/`, `/models`, `/news`, `/offers`. Legacy `*.html` requests 301-redirect to the clean equivalent.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server), Razor views + ViewComponents, xUnit + `WebApplicationFactory` integration tests.

**Phase boundary (do NOT cross):** No form POST handling / Lead creation / SMTP (that is Phase 5 — forms render as static markup only, no submit wiring). No Arabic content values and no `rtl.css` rules (Phase 4 — only keep the existing conditional `rtl.css` link). No admin area (Phase 6). No deep transcription of vehicle specs/trims/colors into the DB — vehicle bodies stay as ported markup; only hero image + name bind from the DB. No caching layer / sitemap (Phase 7).

---

## Conventions (apply in every task)

**Porting a static HTML page body → a Razor partial.** When a task says "port `HTML/<file>.html`", the subagent MUST read that file and transcribe **only the page body** (everything *between* the injected header and footer — i.e. drop `<!doctype>`, `<head>`, `<body>` tag, `<div data-include="header">`, `<div data-include="footer">`, the back-to-top anchor, and the `<script src=".../includes.js">` / `main.js` tags; those live in `_Layout`). Apply these transforms exactly:

1. **Asset paths:** rewrite every `assets/...` (and `./assets/...`) reference to root-absolute `/assets/...`. This applies to `src`, `href`, `<source>`, `poster`, AND inline `style="background-image:url('assets/...')"`. (Learned gotcha: Razor `~/` does NOT resolve inside CSS `url()` — always use `/assets/...` there. For `src`/`href` on plain elements, `/assets/...` is also fine and simplest — use it uniformly.)
2. **Internal links:** rewrite `*.html` hrefs to clean paths: strip the `.html`; `index.html` → `/`; `contact.html` → `/contact-us`; `model-detail.html` → `/models`; anchor-only links (`#id`, `#`) stay as-is. (The redirect middleware is a safety net, but links must be clean.)
3. **Preserve** every CSS class, `id`, `data-*` attribute and inline style exactly — `main.js` keys off them (megamenu `data-mm-tab`/`data-mm-cat`, drawer `data-drawer*`, sliders, `data-lightbox`, etc.). Do not "improve" the markup.
4. **Comments** like `<!--AION-HIDDEN ... -->` are dropped (those vehicles are not visible).

**Culture in views:** read the current language with `GAC.Core.Content.LocalizedTextExtensions.Localize(...)` (added in Task 1) or `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`. The `_Layout` already sets `<html lang dir>`.

**After every task:** run `dotnet build Solution/GAC.sln` (or the solution path) and `dotnet test` from the repo root; both MUST be green before commit. Commit with a `feat(phase3): ...` message. Never commit `appsettings.Development.json` (gitignored — holds the real secret).

**Commands** (run from `C:\Users\anas-\source\repos\GAC`):
- Build: `dotnet build Solution/GAC.sln -c Debug`
- Test: `dotnet test Solution/GAC.sln`

---

## File Structure

**Create:**
- `Solution/GAC.Core/Content/LocalizedTextExtensions.cs` — `Localize()` ambient-culture helper
- `Solution/GAC.Core/Services/ISiteService.cs` — chrome data (SiteSettings + menu tree)
- `Solution/GAC.Core/Services/IVehicleService.cs` — visible list + by-slug
- `Solution/GAC.Core/Services/IContentService.cs` — home, content pages, form pages, news, offers
- `Solution/GAC.Infrastructure/Services/SiteService.cs`
- `Solution/GAC.Infrastructure/Services/VehicleService.cs`
- `Solution/GAC.Infrastructure/Services/ContentService.cs`
- `Solution/GAC.Web/Infrastructure/UrlHelpers.cs` — `NormalizeUrl`, `CategoryCss`, `ThumbPath`
- `Solution/GAC.Web/Infrastructure/LegacyHtmlRedirectMiddleware.cs`
- `Solution/GAC.Web/ViewComponents/HeaderViewComponent.cs` + `Views/Shared/Components/Header/Default.cshtml`
- `Solution/GAC.Web/ViewComponents/FooterViewComponent.cs` + `Views/Shared/Components/Footer/Default.cshtml`
- `Solution/GAC.Web/Controllers/VehiclesController.cs`, `PageController.cs`, `NewsController.cs`, `OffersController.cs`
- `Solution/GAC.Web/Models/` view models as needed (`HomeViewModel.cs`, etc.)
- `Solution/GAC.Web/Views/Vehicles/Index.cshtml`, `Views/Vehicles/Detail.cshtml`, `Views/Vehicles/Models/_<slug>.cshtml` (×9 visible)
- `Solution/GAC.Web/Views/Content/Page.cshtml`, `Views/Content/Pages/_<slug>.cshtml` (×6)
- `Solution/GAC.Web/Views/Forms/Page.cshtml`, `Views/Forms/Forms/_<slug>.cshtml` (×6)
- `Solution/GAC.Web/Views/News/Index.cshtml`, `Views/News/Detail.cshtml`
- `Solution/GAC.Web/Views/Offers/Index.cshtml`
- `Solution/GAC.Web/Views/Shared/NotFound.cshtml`

**Modify:**
- `Solution/GAC.Web/Program.cs` — register services, ViewComponents tag helper, redirect middleware, routes, status-code pages
- `Solution/GAC.Web/Views/_ViewImports.cshtml` — `@addTagHelper *, GAC.Web`, `@using GAC.Core.Content`
- `Solution/GAC.Web/Views/Shared/_Layout.cshtml` — swap header/footer `<partial>` for `<vc:header/>`/`<vc:footer/>`
- `Solution/GAC.Web/Views/Shared/_Header.cshtml`, `_Footer.cshtml` — delete (superseded by ViewComponents) OR leave unused; plan deletes them
- `Solution/GAC.Web/Controllers/HomeController.cs` + `Views/Home/Index.cshtml` — bind from DB
- `Solution/GAC.Infrastructure/Data/ContentSeeder.cs` — clean URLs, vehicle thumbnails, drop news/offers content pages

---

## Task 1: Service layer (Localize + interfaces + EF implementations + DI)

**Files:**
- Create: `Solution/GAC.Core/Content/LocalizedTextExtensions.cs`
- Create: `Solution/GAC.Core/Services/ISiteService.cs`, `IVehicleService.cs`, `IContentService.cs`
- Create: `Solution/GAC.Infrastructure/Services/SiteService.cs`, `VehicleService.cs`, `ContentService.cs`
- Modify: `Solution/GAC.Web/Program.cs`
- Test: `Solution/GAC.Tests/ServiceTests.cs`, `Solution/GAC.Tests/LocalizeTests.cs`

- [ ] **Step 1: Write the failing test for `Localize`**

`Solution/GAC.Tests/LocalizeTests.cs`:
```csharp
using System.Globalization;
using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class LocalizeTests
{
    private static void With(string culture, Action body)
    {
        var prev = CultureInfo.CurrentUICulture;
        try { CultureInfo.CurrentUICulture = new CultureInfo(culture); body(); }
        finally { CultureInfo.CurrentUICulture = prev; }
    }

    [Fact]
    public void Localize_ReturnsArabic_WhenCultureIsAr()
    {
        var t = new LocalizedText { En = "Hello", Ar = "مرحبا" };
        With("ar", () => Assert.Equal("مرحبا", t.Localize()));
    }

    [Fact]
    public void Localize_ReturnsEnglish_WhenCultureIsEn()
    {
        var t = new LocalizedText { En = "Hello", Ar = "مرحبا" };
        With("en", () => Assert.Equal("Hello", t.Localize()));
    }

    [Fact]
    public void Localize_NullSafe_ReturnsEmpty()
    {
        LocalizedText? t = null;
        With("en", () => Assert.Equal(string.Empty, t.Localize()));
    }
}
```

- [ ] **Step 2: Run the test, verify it fails to compile**

Run: `dotnet test Solution/GAC.sln --filter LocalizeTests`
Expected: build error — `Localize` not defined.

- [ ] **Step 3: Implement `LocalizedTextExtensions`**

`Solution/GAC.Core/Content/LocalizedTextExtensions.cs`:
```csharp
using System.Globalization;

namespace GAC.Core.Content;

public static class LocalizedTextExtensions
{
    /// <summary>Value for the ambient UI culture (ar → Arabic, else English), with fallback. Null-safe.</summary>
    public static string Localize(this LocalizedText? text)
    {
        if (text is null) return string.Empty;
        var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return text.Get(culture);
    }
}
```

- [ ] **Step 4: Run the Localize test, verify pass**

Run: `dotnet test Solution/GAC.sln --filter LocalizeTests`
Expected: 3 passed.

- [ ] **Step 5: Define the service interfaces**

`Solution/GAC.Core/Services/ISiteService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface ISiteService
{
    Task<SiteSettings> GetSettingsAsync();
    /// <summary>Top-level menu items (with children + ordered), for the header nav.</summary>
    Task<IReadOnlyList<MenuItem>> GetMenuAsync();
}
```

`Solution/GAC.Core/Services/IVehicleService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IVehicleService
{
    /// <summary>Visible vehicles, ordered by SortOrder, with their images (for grids/megamenu).</summary>
    Task<IReadOnlyList<Vehicle>> GetVisibleAsync();
    /// <summary>A visible vehicle by slug with all child collections; null if missing/hidden.</summary>
    Task<Vehicle?> GetBySlugAsync(string slug);
}
```

`Solution/GAC.Core/Services/IContentService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IContentService
{
    Task<HomePage?> GetHomePageAsync();
    Task<ContentPage?> GetContentPageBySlugAsync(string slug);
    Task<FormPage?> GetFormPageBySlugAsync(string slug);
    Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync();
    Task<NewsArticle?> GetNewsBySlugAsync(string slug);
    Task<IReadOnlyList<Offer>> GetActiveOffersAsync();
}
```

- [ ] **Step 6: Write a failing integration test for the services**

`Solution/GAC.Tests/ServiceTests.cs` (uses the existing `DevWebApplicationFactory` to resolve services against the real dev DB, which the seeder has populated):
```csharp
using GAC.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ServiceTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ServiceTests(DevWebApplicationFactory factory) => _factory = factory;

    private T Resolve<T>(IServiceScope s) where T : notnull => s.ServiceProvider.GetRequiredService<T>();

    [Fact]
    public async Task GetVisibleAsync_ExcludesHiddenVehicles()
    {
        using var scope = _factory.Services.CreateScope();
        var vehicles = await Resolve<IVehicleService>(scope).GetVisibleAsync();
        Assert.DoesNotContain(vehicles, v => v.Slug == "aion-v");   // seeded IsVisible=false
        Assert.Contains(vehicles, v => v.Slug == "gs8");
        Assert.True(vehicles.SequenceEqual(vehicles.OrderBy(v => v.SortOrder)));
    }

    [Fact]
    public async Task GetBySlugAsync_ReturnsNull_ForHiddenVehicle()
    {
        using var scope = _factory.Services.CreateScope();
        Assert.Null(await Resolve<IVehicleService>(scope).GetBySlugAsync("aion-v"));
    }

    [Fact]
    public async Task GetContentPageBySlugAsync_ReturnsAbout()
    {
        using var scope = _factory.Services.CreateScope();
        var page = await Resolve<IContentService>(scope).GetContentPageBySlugAsync("about");
        Assert.NotNull(page);
    }

    [Fact]
    public async Task GetMenuAsync_ReturnsTopLevelOrdered()
    {
        using var scope = _factory.Services.CreateScope();
        var menu = await Resolve<ISiteService>(scope).GetMenuAsync();
        Assert.All(menu, m => Assert.Null(m.ParentId));
        Assert.True(menu.Count >= 5);
    }
}
```

- [ ] **Step 7: Implement the services**

`Solution/GAC.Infrastructure/Services/VehicleService.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class VehicleService : IVehicleService
{
    private readonly ApplicationDbContext _db;
    public VehicleService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Vehicle>> GetVisibleAsync() =>
        await _db.Vehicles.AsNoTracking()
            .Where(v => v.IsVisible)
            .OrderBy(v => v.SortOrder)
            .Include(v => v.Images)
            .ToListAsync();

    public async Task<Vehicle?> GetBySlugAsync(string slug) =>
        await _db.Vehicles.AsNoTracking()
            .Where(v => v.IsVisible && v.Slug == slug)
            .Include(v => v.Images)
            .Include(v => v.Trims)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Features)
            .FirstOrDefaultAsync();
}
```
> NOTE: confirm the child collection navigation on `SpecGroup` is named `Rows`; if the property differs, use the actual name. Read `Solution/GAC.Core/Content/SpecGroup.cs` first.

`Solution/GAC.Infrastructure/Services/SiteService.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class SiteService : ISiteService
{
    private readonly ApplicationDbContext _db;
    public SiteService(ApplicationDbContext db) => _db = db;

    public async Task<SiteSettings> GetSettingsAsync() =>
        await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync() ?? new SiteSettings();

    public async Task<IReadOnlyList<MenuItem>> GetMenuAsync() =>
        await _db.MenuItems.AsNoTracking()
            .Where(m => m.ParentId == null)
            .OrderBy(m => m.SortOrder)
            .Include(m => m.Children.OrderBy(c => c.SortOrder))
            .ToListAsync();
}
```

`Solution/GAC.Infrastructure/Services/ContentService.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class ContentService : IContentService
{
    private readonly ApplicationDbContext _db;
    public ContentService(ApplicationDbContext db) => _db = db;

    public async Task<HomePage?> GetHomePageAsync() =>
        await _db.HomePages.AsNoTracking()
            .Include(h => h.Slides.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync();

    public async Task<ContentPage?> GetContentPageBySlugAsync(string slug) =>
        await _db.ContentPages.AsNoTracking()
            .Where(p => p.IsVisible && p.Slug == slug)
            .Include(p => p.Sections.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync();

    public async Task<FormPage?> GetFormPageBySlugAsync(string slug) =>
        await _db.FormPages.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsVisible && p.Slug == slug);

    public async Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync() =>
        await _db.NewsArticles.AsNoTracking()
            .Where(n => n.IsPublished)
            .OrderBy(n => n.SortOrder)
            .ToListAsync();

    public async Task<NewsArticle?> GetNewsBySlugAsync(string slug) =>
        await _db.NewsArticles.AsNoTracking()
            .FirstOrDefaultAsync(n => n.IsPublished && n.Slug == slug);

    public async Task<IReadOnlyList<Offer>> GetActiveOffersAsync() =>
        await _db.Offers.AsNoTracking()
            .Where(o => o.IsActive)
            .OrderBy(o => o.SortOrder)
            .ToListAsync();
}
```

- [ ] **Step 8: Register services in DI**

In `Solution/GAC.Web/Program.cs`, after `AddControllersWithViews()`:
```csharp
builder.Services.AddScoped<ISiteService, SiteService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IContentService, ContentService>();
```
Add `using GAC.Core.Services;` and `using GAC.Infrastructure.Services;`.

- [ ] **Step 9: Run all tests, verify green**

Run: `dotnet test Solution/GAC.sln`
Expected: existing 15 + new tests all pass.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core Solution/GAC.Infrastructure Solution/GAC.Web/Program.cs Solution/GAC.Tests
git commit -m "feat(phase3): add Localize helper + content/site/vehicle service layer"
```

---

## Task 2: Seeder cleanup — clean URLs, vehicle thumbnails, drop news/offers content pages

**Files:**
- Modify: `Solution/GAC.Infrastructure/Data/ContentSeeder.cs`
- Test: `Solution/GAC.Tests/ContentSeederTests.cs` (extend existing)

**Why:** Menu/HeroSlide URLs are still `.html` stubs; vehicles need a menu/listing thumbnail image distinct from the hero; and `news`/`offers` must NOT be ContentPages (they are owned by NewsController/OffersController). Fresh DBs get clean data; the already-seeded live DB keeps stale rows (harmless — explicit routes win and the render-time `NormalizeUrl` defends against `.html`), so no migration is needed.

- [ ] **Step 1: Write/extend the failing test**

Add to `Solution/GAC.Tests/ContentSeederTests.cs` (these run against the in-memory provider already used there — match the existing fixture pattern in that file):
```csharp
[Fact]
public async Task Seeds_MenuItems_WithCleanUrls()
{
    using var db = NewInMemoryDb();              // use the file's existing helper
    await ContentSeeder.SeedAsync(ServiceProviderFor(db));
    var urls = db.MenuItems.Where(m => m.Url != null).Select(m => m.Url!).ToList();
    Assert.All(urls, u => Assert.DoesNotContain(".html", u));
    Assert.Contains("/models", urls);
}

[Fact]
public async Task Seeds_ThumbnailImage_PerVisibleVehicle()
{
    using var db = NewInMemoryDb();
    await ContentSeeder.SeedAsync(ServiceProviderFor(db));
    var gs8 = db.Vehicles.Include(v => v.Images).Single(v => v.Slug == "gs8");
    Assert.Contains(gs8.Images, i => i.Kind == VehicleImageKind.Gallery);   // thumbnail
    Assert.Contains(gs8.Images, i => i.Kind == VehicleImageKind.Hero);
}

[Fact]
public async Task DoesNotSeed_NewsOrOffers_AsContentPages()
{
    using var db = NewInMemoryDb();
    await ContentSeeder.SeedAsync(ServiceProviderFor(db));
    Assert.DoesNotContain(db.ContentPages, p => p.Slug == "news" || p.Slug == "offers");
}
```
> If the existing test file uses different helper names for building the in-memory context / service provider, reuse those exactly rather than the placeholder names above.

- [ ] **Step 2: Run, verify failing**

Run: `dotnet test Solution/GAC.sln --filter ContentSeederTests`
Expected: 3 new failures.

- [ ] **Step 3: Clean the Menu URLs**

In `SeedMenuAsync`, change every `Url` to its clean form:
`index.html`→`/`, `models.html`→`/models`, `book-a-service.html`→`/book-a-service`, `cost-of-service.html`→`/cost-of-service`, `warranty.html`→`/warranty`, `recall-enquiry.html`→`/recall-enquiry`, `road-assistance.html`→`/road-assistance`, `book-a-test-drive.html`→`/book-a-test-drive`, `request-a-quote.html`→`/request-a-quote`, `contact-us.html`→`/contact-us`, `fleet.html`→`/fleet`, `finance.html`→`/finance`.

- [ ] **Step 4: Clean the HeroSlide CTA links**

In `SeedHomePageAsync`, change each non-null `ctaLink` from `<slug>.html` to `/<slug>` (e.g. `"gs4.html"` → `"/gs4"`). Leave the first slide's null as null.

- [ ] **Step 5: Add a thumbnail image per vehicle**

Extend `MakeVehicle` to accept a `thumbPath` and add a second `VehicleImage { Kind = VehicleImageKind.Gallery, Path = thumbPath, SortOrder = 0 }`. Thumbnails (from `HTML/partials/header.html` megamenu):
`gs8traveller`→`/assets/img/m-gs8-traveller.png`, `gs8`→`/assets/img/m-gs8.jpg`, `gs3emzoom`→`/assets/img/m-gs3-emzoom.png`, `emkoo`→`/assets/img/m-emkoo.png`, `empow`→`/assets/img/m-empow.png`, `m8`→`/assets/img/m-m8.png`, `empow-sport`→`/assets/img/m-empow-sport.png`, `aion-v`→`/assets/img/m-aion-v.png`, `aion-es`→`/assets/img/m-aion-es.png`, `hyptec-ht`→`/assets/img/m-hyptec-ht.png`, `gs4`→`/assets/img/m-gs4.png`.

- [ ] **Step 6: Drop news/offers from content pages**

In `SeedContentPagesAsync`, remove the `news` and `offers` `ContentPage` entries (leaving 6: about, warranty, privacy-policy, finance, cost-of-service, road-assistance).

- [ ] **Step 7: Run tests, verify green**

Run: `dotnet test Solution/GAC.sln`
Expected: all pass.

- [ ] **Step 8: Commit**

```bash
git add Solution/GAC.Infrastructure/Data/ContentSeeder.cs Solution/GAC.Tests/ContentSeederTests.cs
git commit -m "feat(phase3): seed clean URLs, vehicle thumbnails; drop news/offers content pages"
```

---

## Task 3: Dynamic chrome — URL helpers + Header/Footer ViewComponents + layout wiring

**Files:**
- Create: `Solution/GAC.Web/Infrastructure/UrlHelpers.cs`
- Create: `Solution/GAC.Web/ViewComponents/HeaderViewComponent.cs` + `Views/Shared/Components/Header/Default.cshtml`
- Create: `Solution/GAC.Web/ViewComponents/FooterViewComponent.cs` + `Views/Shared/Components/Footer/Default.cshtml`
- Modify: `Solution/GAC.Web/Views/_ViewImports.cshtml`, `Views/Shared/_Layout.cshtml`
- Delete: `Solution/GAC.Web/Views/Shared/_Header.cshtml`, `_Footer.cshtml`
- Test: `Solution/GAC.Tests/ChromeTests.cs`

- [ ] **Step 1: Add the URL/category helpers**

`Solution/GAC.Web/Infrastructure/UrlHelpers.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

public static class UrlHelpers
{
    /// <summary>Defensively normalize a stored link to a clean app path (handles legacy ".html").</summary>
    public static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "#";
        if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith('#')) return url;
        var u = url.Trim();
        if (u.Equals("index.html", StringComparison.OrdinalIgnoreCase) || u == "/") return "/";
        if (u.Equals("contact.html", StringComparison.OrdinalIgnoreCase)) return "/contact-us";
        if (u.Equals("model-detail.html", StringComparison.OrdinalIgnoreCase)) return "/models";
        if (u.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) u = u[..^5];
        if (!u.StartsWith('/')) u = "/" + u;
        return u;
    }

    /// <summary>Megamenu/listing filter classes from category flags, e.g. Suv|Ev → "suv ev".</summary>
    public static string CategoryCss(VehicleCategory c)
    {
        var parts = new List<string>();
        if (c.HasFlag(VehicleCategory.Sedan)) parts.Add("sedan");
        if (c.HasFlag(VehicleCategory.Suv)) parts.Add("suv");
        if (c.HasFlag(VehicleCategory.Ev)) parts.Add("ev");
        return string.Join(' ', parts);
    }

    /// <summary>Listing/menu thumbnail: first Gallery image, else the Hero image, else empty.</summary>
    public static string ThumbPath(Vehicle v)
    {
        var thumb = v.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Gallery)
                    ?? v.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Hero);
        return thumb?.Path ?? "";
    }
}
```

- [ ] **Step 2: Header ViewComponent**

`Solution/GAC.Web/ViewComponents/HeaderViewComponent.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class HeaderViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public IReadOnlyList<MenuItem> Menu { get; set; } = new List<MenuItem>();
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

public class HeaderViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    private readonly IVehicleService _vehicles;
    public HeaderViewComponent(ISiteService site, IVehicleService vehicles)
    { _site = site; _vehicles = vehicles; }

    public async Task<IViewComponentResult> InvokeAsync() => View(new HeaderViewModel
    {
        Settings = await _site.GetSettingsAsync(),
        Menu = await _site.GetMenuAsync(),
        Vehicles = await _vehicles.GetVisibleAsync()
    });
}
```

- [ ] **Step 3: Header view**

`Solution/GAC.Web/Views/Shared/Components/Header/Default.cshtml` — port `HTML/partials/header.html` (already mostly in the current `_Header.cshtml`) but make it data-driven. Model is `HeaderViewModel`. Requirements:
- `@model GAC.Web.ViewComponents.HeaderViewModel` and `@using GAC.Web.Infrastructure`.
- Brandbar: logo links to `/`; WhatsApp `href="https://api.whatsapp.com/send/?phone=@Model.Settings.WhatsApp"`; phone `href="tel:@Model.Settings.Phone"` showing `@Model.Settings.Phone`; keep the language switch block exactly as in the current `_Header.cshtml` (the `asp-controller="Culture"` links).
- Main nav: render `@foreach (var item in Model.Menu)`. The **Home** item (Url `/`) and **Models** item render specially; the **Models** item is followed by the megamenu. For a top-level item with children, render `<li class="has-drop"><a href="#">@item.Label.Localize() <span class="caret">▾</span></a><ul class="drop">` + children `<li><a href="@UrlHelpers.NormalizeUrl(c.Url)">@c.Label.Localize()</a></li>`. For a childless item, `<li><a href="@UrlHelpers.NormalizeUrl(item.Url)">@item.Label.Localize()</a></li>`.
- Megamenu grid: keep the `.megamenu` + `.megamenu__tabs` (All/Sedan/SUV/EV buttons) exactly; replace the hardcoded `.megamenu__item` anchors with:
```cshtml
@foreach (var v in Model.Vehicles)
{
    <a class="megamenu__item" data-mm-cat="@UrlHelpers.CategoryCss(v.Category)" href="/@v.Slug">
        <img src="@UrlHelpers.ThumbPath(v)" alt="@v.Name.Localize()" />
        <span class="mm-name">@v.Name.Localize()</span>
    </a>
}
```
- Mobile drawer: keep structure; in the Models drawer group render `<a href="/models">All Models</a>` then `@foreach (var v in Model.Vehicles) { <a href="/@v.Slug">@v.Name.Localize()</a> }`; for the other groups iterate `Model.Menu` children the same way; WhatsApp/phone from settings.
- Convert the logo/franchise `<img src="~/assets/...">` to `/assets/...`.

- [ ] **Step 4: Footer ViewComponent**

`Solution/GAC.Web/ViewComponents/FooterViewComponent.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class FooterViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    public FooterViewComponent(ISiteService site) => _site = site;

    public async Task<IViewComponentResult> InvokeAsync() => View(await _site.GetSettingsAsync());
}
```

- [ ] **Step 5: Footer view**

`Solution/GAC.Web/Views/Shared/Components/Footer/Default.cshtml` — port the current `_Footer.cshtml` body. `@model GAC.Core.Content.SiteSettings`, `@using GAC.Web.Infrastructure`. Changes:
- Action-dock links → clean: Book a Test Drive `/book-a-test-drive`, Get Online Quote `/request-a-quote`, Find Showroom/Contact `/contact-us`; WhatsApp from `@Model.WhatsApp`; "Download Brochure" stays `href="#"`.
- Footer nav links → `/privacy-policy`, `/about`, `/contact-us`; "Site Map" + "Suggestions" stay `#`.
- Social icons: only render an anchor when the matching URL is non-null, e.g. `@if (Model.InstagramUrl != null) { <a href="@Model.InstagramUrl" ...>…</a> }`. (All are null in seed → none render. Keep the SVGs inside each conditional.)

- [ ] **Step 6: Wire the layout + view imports**

In `Solution/GAC.Web/Views/_ViewImports.cshtml` add:
```cshtml
@using GAC.Core.Content
@using GAC.Web.Infrastructure
@addTagHelper *, GAC.Web
```
In `Solution/GAC.Web/Views/Shared/_Layout.cshtml`, replace `<partial name="_Header" />` with `<vc:header />` and `<partial name="_Footer" />` with `<vc:footer />`.

- [ ] **Step 7: Delete the superseded partials**

Delete `Solution/GAC.Web/Views/Shared/_Header.cshtml` and `_Footer.cshtml`.

- [ ] **Step 8: Write the chrome test**

`Solution/GAC.Tests/ChromeTests.cs`:
```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class ChromeTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ChromeTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_RendersDbDrivenChrome()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("gac-header", html);
        Assert.Contains("megamenu__item", html);
        Assert.Contains("href=\"/gs8\"", html);          // vehicle linked by clean slug
        Assert.DoesNotContain(".html\"", html);           // no legacy links leaked into chrome
    }
}
```

- [ ] **Step 9: Run all tests, verify green**

Run: `dotnet test Solution/GAC.sln`
Expected: all pass. (If `.html"` assertion trips on the still-static home body, scope it: this task only guarantees chrome is clean — temporarily assert on a header-only substring, and the full no-`.html` guarantee lands in Task 4.)

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Web Solution/GAC.Tests/ChromeTests.cs
git commit -m "feat(phase3): DB-driven Header/Footer ViewComponents + clean nav URLs"
```

---

## Task 4: Home page — bind hero, model strip & news from the DB

**Files:**
- Modify: `Solution/GAC.Web/Controllers/HomeController.cs`
- Create: `Solution/GAC.Web/Models/HomeViewModel.cs`
- Modify: `Solution/GAC.Web/Views/Home/Index.cshtml`
- Test: `Solution/GAC.Tests/HomePageSmokeTests.cs` (extend)

- [ ] **Step 1: Home view model**

`Solution/GAC.Web/Models/HomeViewModel.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Web.Models;

public class HomeViewModel
{
    public HomePage? Home { get; set; }
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public IReadOnlyList<NewsArticle> News { get; set; } = new List<NewsArticle>();
}
```

- [ ] **Step 2: Controller**

Update `HomeController` to inject `IContentService` + `IVehicleService` and build the model:
```csharp
public class HomeController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public HomeController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    public async Task<IActionResult> Index() => View(new HomeViewModel
    {
        Home = await _content.GetHomePageAsync(),
        Vehicles = await _vehicles.GetVisibleAsync(),
        News = await _content.GetPublishedNewsAsync()
    });

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
```
Add the needed `using GAC.Core.Services;` and `using GAC.Web.Models;`.

- [ ] **Step 3: Bind the view**

In `Views/Home/Index.cshtml` set `@model HomeViewModel`. Keep all section markup/classes. Replace the three dynamic regions:
- **Hero slider** (`.hero` slides): `@foreach (var s in Model.Home?.Slides ?? new())` render each `.hero__slide` using `s.ImagePath` (as `/assets/...` already) and `s.Heading.Localize()`, CTA `href="@UrlHelpers.NormalizeUrl(s.CtaLink)"`. Preserve slider DOM (`data-hero-*`, viewport, arrows, dots) so `main.js` works; if dots are count-based, emit one per slide.
- **Model strip** (`.model-strip` / `.carousel` cards): `@foreach (var v in Model.Vehicles)` render the model card with `data-cat="@UrlHelpers.CategoryCss(v.Category)"`, image `@UrlHelpers.ThumbPath(v)`, name `@v.Name.Localize()`, link `/@v.Slug`. Keep the All/Sedan/SUV/EV tab buttons static.
- **News cards** (`.news` / `.news-card`): `@foreach (var n in Model.News)` render image `n.ImagePath`, date `@n.PublishedOn.ToString("dd MMM yyyy")`, title `@n.Title.Localize()`, link `/news/@n.Slug`.
- Keep `searchblock`, `promo`, `dual` sections static; rewrite any `.html` links in them to clean paths.

- [ ] **Step 4: Extend the smoke test**

Add to `HomePageSmokeTests`:
```csharp
[Fact]
public async Task Home_RendersVehiclesAndNews_FromDb()
{
    var client = _factory.CreateClient();
    var html = await (await client.GetAsync("/")).Content.ReadAsStringAsync();
    Assert.Contains("GS8", html);                       // a seeded vehicle name
    Assert.Contains("/news/", html);                    // a news card link
}
```

- [ ] **Step 5: Run tests, verify green**

Run: `dotnet test Solution/GAC.sln`

- [ ] **Step 6: Commit**

```bash
git add Solution/GAC.Web Solution/GAC.Tests/HomePageSmokeTests.cs
git commit -m "feat(phase3): bind home hero/model-strip/news from the database"
```

---

## Task 5: Models listing page (`/models`)

**Files:**
- Create: `Solution/GAC.Web/Controllers/VehiclesController.cs`
- Create: `Solution/GAC.Web/Views/Vehicles/Index.cshtml`
- Modify: `Solution/GAC.Web/Program.cs` (route for `/models`)
- Test: `Solution/GAC.Tests/ListingTests.cs`

- [ ] **Step 1: Controller**

`Solution/GAC.Web/Controllers/VehiclesController.cs`:
```csharp
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicles;
    public VehiclesController(IVehicleService vehicles) => _vehicles = vehicles;

    [HttpGet("/models")]
    public async Task<IActionResult> Index() => View(await _vehicles.GetVisibleAsync());
}
```
(Attribute route `/models`; no change to `Program.cs` needed beyond keeping the default conventional route. Confirm attribute routing is active — `AddControllersWithViews` + `MapControllers`/`MapControllerRoute` both honor `[HttpGet("/models")]`. If only `MapControllerRoute` default is present, add `app.MapControllers();` once in Program.cs so attribute routes resolve.)

- [ ] **Step 2: View**

`Solution/GAC.Web/Views/Vehicles/Index.cshtml`: port `HTML/models.html` body. `@model IReadOnlyList<GAC.Core.Content.Vehicle>`, `@using GAC.Web.Infrastructure`. Keep `.page-hero`, `.filters`/`.chip` (All/Sedan/SUV/EV), `.cta-strip`. Replace `.model-grid` cards with:
```cshtml
@foreach (var v in Model)
{
    <article class="lineup-card" data-cat="@UrlHelpers.CategoryCss(v.Category)">
        <img src="@UrlHelpers.ThumbPath(v)" alt="@v.Name.Localize()" />
        <h3>@v.Name.Localize()</h3>
        <div class="lineup-card__actions">
            <a class="btn" href="/@v.Slug">Explore</a>
            <a class="btn btn--ghost" href="/request-a-quote">Request a Quote</a>
        </div>
    </article>
}
```
Match the actual class names/structure found in `models.html` (the snippet above is the shape — use the real classes from the file). Set `ViewData["Title"] = "Models"`.

- [ ] **Step 3: Test**

`Solution/GAC.Tests/ListingTests.cs`:
```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class ListingTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ListingTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Models_ListsVisibleVehicles()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/models");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("GS8", html);
        Assert.Contains("href=\"/gs8\"", html);
        Assert.DoesNotContain("AION V", html);     // hidden
    }
}
```

- [ ] **Step 4: Run tests + commit**

Run: `dotnet test Solution/GAC.sln`
```bash
git add Solution/GAC.Web Solution/GAC.Tests/ListingTests.cs
git commit -m "feat(phase3): /models listing from visible vehicles"
```

---

## Task 6: Routing core — slug resolver, legacy redirects, 404, and three sample pages

This task establishes the catch-all `/{slug}` resolver and proves it end-to-end with one content page (about), one form page (book-a-service) and one vehicle (gs8). Remaining pages are added in Tasks 7–9.

**Files:**
- Create: `Solution/GAC.Web/Controllers/PageController.cs`
- Create: `Solution/GAC.Web/Infrastructure/LegacyHtmlRedirectMiddleware.cs`
- Create: `Solution/GAC.Web/Views/Content/Page.cshtml`, `Views/Content/Pages/_about.cshtml`
- Create: `Solution/GAC.Web/Views/Forms/Page.cshtml`, `Views/Forms/Forms/_book-a-service.cshtml`
- Create: `Solution/GAC.Web/Views/Vehicles/Detail.cshtml`, `Views/Vehicles/Models/_gs8.cshtml`
- Create: `Solution/GAC.Web/Views/Shared/NotFound.cshtml`
- Modify: `Solution/GAC.Web/Program.cs`
- Test: `Solution/GAC.Tests/RoutingTests.cs`

- [ ] **Step 1: Legacy redirect middleware**

`Solution/GAC.Web/Infrastructure/LegacyHtmlRedirectMiddleware.cs`:
```csharp
namespace GAC.Web.Infrastructure;

/// <summary>301-redirects legacy "*.html" paths to their clean equivalents.</summary>
public class LegacyHtmlRedirectMiddleware
{
    private readonly RequestDelegate _next;
    public LegacyHtmlRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var clean = UrlHelpers.NormalizeUrl(path.TrimStart('/'));
            // special dev artifact
            if (path.Equals("/live-empow-sport.html", StringComparison.OrdinalIgnoreCase))
                clean = "/empow-sport";
            ctx.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            ctx.Response.Headers.Location = clean + ctx.Request.QueryString;
            return;
        }
        await _next(ctx);
    }
}
```

- [ ] **Step 2: PageController slug resolver**

`Solution/GAC.Web/Controllers/PageController.cs`:
```csharp
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class PageController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public PageController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    // Catch-all single-segment slug. Registered LAST so dedicated routes win.
    [HttpGet("/{slug}")]
    public async Task<IActionResult> Show(string slug)
    {
        var content = await _content.GetContentPageBySlugAsync(slug);
        if (content != null) { ViewData["Title"] = content.Title.Localize(); return View("~/Views/Content/Page.cshtml", content); }

        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null) { ViewData["Title"] = form.Title.Localize(); return View("~/Views/Forms/Page.cshtml", form); }

        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle != null) { ViewData["Title"] = vehicle.Name.Localize(); return View("~/Views/Vehicles/Detail.cshtml", vehicle); }

        return NotFound();
    }
}
```

- [ ] **Step 3: Container views (render a per-slug body partial)**

`Solution/GAC.Web/Views/Content/Page.cshtml`:
```cshtml
@model GAC.Core.Content.ContentPage
@{ Layout = "_Layout"; }
<partial name="Pages/_@(Model.Slug)" model="Model" />
```
`Solution/GAC.Web/Views/Forms/Page.cshtml`:
```cshtml
@model GAC.Core.Content.FormPage
@{ Layout = "_Layout"; }
<partial name="Forms/_@(Model.Slug)" model="Model" />
```
`Solution/GAC.Web/Views/Vehicles/Detail.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
@{ Layout = "_Layout"; }
<partial name="Models/_@(Model.Slug)" model="Model" />
```
> Partial name resolution: a partial named `Pages/_about` resolves to `Views/Content/Pages/_about.cshtml` because the view is rendered from `Views/Content/`. Verify this path resolution in the first run; if it fails, use an explicit path: `<partial name="~/Views/Content/Pages/_@(Model.Slug).cshtml" model="Model" />`.

- [ ] **Step 4: Three sample body partials**

- `Views/Content/Pages/_about.cshtml`: port `HTML/about.html` body (model `GAC.Core.Content.ContentPage`; you may use `@Model.Title.Localize()` for the page title/`page-hero`). Apply the porting Conventions.
- `Views/Forms/Forms/_book-a-service.cshtml`: port `HTML/book-a-service.html` body (model `GAC.Core.Content.FormPage`). **Render the `<form>` markup statically — do NOT add asp-for/POST action/anti-forgery (Phase 5).** Keep field markup, classes, the model `<select>` may be left static or list `@Model` title via `@Model.Title.Localize()`.
- `Views/Vehicles/Models/_gs8.cshtml`: port `HTML/gs8.html` body (model `GAC.Core.Content.Vehicle`). Bind only the hero: use the hero image `@Model.Images.FirstOrDefault(i => i.Kind == GAC.Core.Content.VehicleImageKind.Hero)?.Path` and `@Model.Name.Localize()` in the `.mp-hero`; everything else stays verbatim. The enquiry `<form>` stays static (Phase 5).

- [ ] **Step 5: 404 view + pipeline wiring**

`Solution/GAC.Web/Views/Shared/NotFound.cshtml`:
```cshtml
@{ Layout = "_Layout"; ViewData["Title"] = "Page not found"; }
<section class="section">
  <div class="container" style="text-align:center;padding:80px 0">
    <h1>404</h1>
    <p>The page you’re looking for isn’t here.</p>
    <a class="btn" href="/">Back to Home</a>
  </div>
</section>
```
In `Program.cs`:
- Add `app.UseStatusCodePagesWithReExecute("/not-found");` (after `UseRouting`), and a route/action for it — simplest: a `HomeController` action `[HttpGet("/not-found")] public IActionResult NotFoundPage() => View("NotFound");` returning 404 body. (Set `Response.StatusCode = 404` is already preserved by re-execute.)
- Register the redirect middleware EARLY (before `UseStaticFiles` so `.html` never hits a file): `app.UseMiddleware<LegacyHtmlRedirectMiddleware>();` as the first `app.Use...` after building.
- Ensure attribute routes resolve: keep the default `MapControllerRoute` AND add `app.MapControllers();` (attribute-routed actions). The catch-all `[HttpGet("/{slug}")]` on `PageController` naturally has lower precedence than literal routes like `/models`, `/news`. Verify `/` still maps to Home (the default conventional route covers it; `Show` requires a non-empty slug so `/` won't match it).

- [ ] **Step 6: Routing tests**

`Solution/GAC.Tests/RoutingTests.cs`:
```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class RoutingTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public RoutingTests(DevWebApplicationFactory factory) => _factory = factory;

    private System.Net.Http.HttpClient NoRedirect() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Theory]
    [InlineData("/about.html", "/about")]
    [InlineData("/book-a-service.html", "/book-a-service")]
    [InlineData("/index.html", "/")]
    [InlineData("/contact.html", "/contact-us")]
    public async Task LegacyHtml_Redirects301(string from, string to)
    {
        var res = await NoRedirect().GetAsync(from);
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode);
        Assert.Equal(to, res.Headers.Location!.ToString());
    }

    [Fact] public async Task About_Renders() => await Ok("/about", "About");
    [Fact] public async Task BookService_Renders() => await Ok("/book-a-service", "");
    [Fact] public async Task Gs8_Renders() => await Ok("/gs8", "GS8");

    [Fact]
    public async Task UnknownSlug_Returns404()
    {
        var res = await _factory.CreateClient().GetAsync("/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private async Task Ok(string url, string contains)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        if (contains.Length > 0)
            Assert.Contains(contains, await res.Content.ReadAsStringAsync());
    }
}
```

- [ ] **Step 7: Run tests, verify green**

Run: `dotnet test Solution/GAC.sln`
Expected: redirects 301, samples 200, unknown 404.

- [ ] **Step 8: Commit**

```bash
git add Solution/GAC.Web Solution/GAC.Tests/RoutingTests.cs
git commit -m "feat(phase3): slug resolver, legacy .html 301 redirects, 404, sample pages"
```

---

## Task 7: Remaining content pages (5 partials)

**Files:**
- Create: `Views/Content/Pages/_warranty.cshtml`, `_privacy-policy.cshtml`, `_finance.cshtml`, `_cost-of-service.cshtml`, `_road-assistance.cshtml`
- Test: extend `Solution/GAC.Tests/RoutingTests.cs`

- [ ] **Step 1: Port each page body**

For each, create `Views/Content/Pages/_<slug>.cshtml` with `@model GAC.Core.Content.ContentPage` and port the matching `HTML/<file>.html` body per the Conventions:
- `_warranty.cshtml` ← `warranty.html` (banner, callout, `.wgrid` warranty cards, `.datatable--matrix` table — all static markup)
- `_privacy-policy.cshtml` ← `privacy-policy.html` (long-form prose)
- `_finance.cshtml` ← `finance.html` (intro, the form renders **static** per Phase boundary, FAQ `details/summary`, product cards)
- `_cost-of-service.cshtml` ← `cost-of-service.html` (large `.datatable` pricing matrix — static)
- `_road-assistance.cshtml` ← `road-assistance.html` (short info + call button)
Use `@Model.Title.Localize()` for the page title where the page has a `page-hero`/`crumb-bar` heading.

- [ ] **Step 2: Tests**

Add to `RoutingTests`:
```csharp
[Theory]
[InlineData("/warranty")]
[InlineData("/privacy-policy")]
[InlineData("/finance")]
[InlineData("/cost-of-service")]
[InlineData("/road-assistance")]
public async Task ContentPages_Render200(string url)
{
    var res = await _factory.CreateClient().GetAsync(url);
    Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
}
```

- [ ] **Step 3: Run tests + commit**

Run: `dotnet test Solution/GAC.sln`
```bash
git add Solution/GAC.Web/Views/Content Solution/GAC.Tests/RoutingTests.cs
git commit -m "feat(phase3): port remaining content pages to Razor"
```

---

## Task 8: Remaining form pages (5 partials)

**Files:**
- Create: `Views/Forms/Forms/_book-a-test-drive.cshtml`, `_request-a-quote.cshtml`, `_contact-us.cshtml`, `_fleet.cshtml`, `_recall-enquiry.cshtml`
- Test: extend `RoutingTests`

- [ ] **Step 1: Port each form body**

For each, create `Views/Forms/Forms/_<slug>.cshtml` with `@model GAC.Core.Content.FormPage`, porting the matching `HTML/<file>.html` body per Conventions. **All `<form>`s render as static markup — no asp-for / POST action / anti-forgery (Phase 5).** Mapping:
- `_book-a-test-drive.cshtml` ← `book-a-test-drive.html`
- `_request-a-quote.cshtml` ← `request-a-quote.html`
- `_contact-us.cshtml` ← `contact-us.html` (directory grid + message form)
- `_fleet.cshtml` ← `fleet.html`
- `_recall-enquiry.cshtml` ← `recall-enquiry.html`

- [ ] **Step 2: Tests**

```csharp
[Theory]
[InlineData("/book-a-test-drive")]
[InlineData("/request-a-quote")]
[InlineData("/contact-us")]
[InlineData("/fleet")]
[InlineData("/recall-enquiry")]
public async Task FormPages_Render200(string url)
{
    var res = await _factory.CreateClient().GetAsync(url);
    Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
}
```

- [ ] **Step 3: Run tests + commit**

```bash
git add Solution/GAC.Web/Views/Forms Solution/GAC.Tests/RoutingTests.cs
git commit -m "feat(phase3): port remaining form pages to Razor (render-only)"
```

---

## Task 9: Remaining vehicle pages (8 partials)

**Files:**
- Create: `Views/Vehicles/Models/_gs8traveller.cshtml`, `_gs3emzoom.cshtml`, `_emkoo.cshtml`, `_empow.cshtml`, `_m8.cshtml`, `_empow-sport.cshtml`, `_hyptec-ht.cshtml`, `_gs4.cshtml`
- Test: extend `RoutingTests`

- [ ] **Step 1: Port each vehicle body**

For each visible vehicle slug, create `Views/Vehicles/Models/_<slug>.cshtml` with `@model GAC.Core.Content.Vehicle`, porting the matching `HTML/<slug>.html` body per Conventions. Bind only the `.mp-hero` image (`@Model.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Hero)?.Path`) and name (`@Model.Name.Localize()`); keep all other sections verbatim. Enquiry forms stay static (Phase 5). (The two hidden vehicles `aion-v`/`aion-es` are intentionally NOT ported — they 404.)

- [ ] **Step 2: Tests**

```csharp
[Theory]
[InlineData("/gs8traveller")]
[InlineData("/gs3emzoom")]
[InlineData("/emkoo")]
[InlineData("/empow")]
[InlineData("/m8")]
[InlineData("/empow-sport")]
[InlineData("/hyptec-ht")]
[InlineData("/gs4")]
public async Task VehiclePages_Render200(string url)
{
    var res = await _factory.CreateClient().GetAsync(url);
    Assert.Equal(System.Net.HttpStatusCode.OK, res.StatusCode);
}

[Theory]
[InlineData("/aion-v")]
[InlineData("/aion-es")]
public async Task HiddenVehicles_Return404(string url)
{
    var res = await _factory.CreateClient().GetAsync(url);
    Assert.Equal(System.Net.HttpStatusCode.NotFound, res.StatusCode);
}
```

- [ ] **Step 3: Run tests + commit**

```bash
git add Solution/GAC.Web/Views/Vehicles Solution/GAC.Tests/RoutingTests.cs
git commit -m "feat(phase3): port remaining vehicle detail pages to Razor"
```

---

## Task 10: News & Offers + final review

**Files:**
- Create: `Solution/GAC.Web/Controllers/NewsController.cs`, `OffersController.cs`
- Create: `Views/News/Index.cshtml`, `Views/News/Detail.cshtml`, `Views/Offers/Index.cshtml`
- Test: `Solution/GAC.Tests/NewsOffersTests.cs`

- [ ] **Step 1: Controllers**

`NewsController.cs`:
```csharp
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class NewsController : Controller
{
    private readonly IContentService _content;
    public NewsController(IContentService content) => _content = content;

    [HttpGet("/news")]
    public async Task<IActionResult> Index() => View(await _content.GetPublishedNewsAsync());

    [HttpGet("/news/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var article = await _content.GetNewsBySlugAsync(slug);
        return article == null ? NotFound() : View(article);
    }
}
```
`OffersController.cs`:
```csharp
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class OffersController : Controller
{
    private readonly IContentService _content;
    public OffersController(IContentService content) => _content = content;

    [HttpGet("/offers")]
    public async Task<IActionResult> Index() => View(await _content.GetActiveOffersAsync());
}
```

- [ ] **Step 2: Views**

- `Views/News/Index.cshtml` (`@model IReadOnlyList<GAC.Core.Content.NewsArticle>`): port `HTML/news.html` `.listing-grid`; render one `.listing-card` per article (image `n.ImagePath`, date `@n.PublishedOn.ToString("dd MMM yyyy")`, title `@n.Title.Localize()`, link `/news/@n.Slug`). Keep `page-hero`, `cta-strip`.
- `Views/News/Detail.cshtml` (`@model GAC.Core.Content.NewsArticle`): minimal — `page-hero` with `@Model.Title.Localize()`, optional image, `@Html.Raw(Model.Body.Localize())` (body is empty in seed → renders nothing). Set `ViewData["Title"]`.
- `Views/Offers/Index.cshtml` (`@model IReadOnlyList<GAC.Core.Content.Offer>`): port `HTML/offers.html`. The seed has marketing offer cards as static copy — render the static `.offer-grid` cards from `offers.html` verbatim (per "port as-is"); the `Model` may be empty/underused. Keep `page-hero`, `cta-strip`, "Enquire Now" → `/request-a-quote`.

- [ ] **Step 3: Tests**

`Solution/GAC.Tests/NewsOffersTests.cs`:
```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class NewsOffersTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public NewsOffersTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task News_ListsArticles()
    {
        var html = await (await _factory.CreateClient().GetAsync("/news")).Content.ReadAsStringAsync();
        Assert.Contains("/news/gac-empow-2026-high-performance-sports-sedan", html);
    }

    [Fact]
    public async Task NewsDetail_Renders200()
    {
        var res = await _factory.CreateClient().GetAsync("/news/gac-empow-2026-high-performance-sports-sedan");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Offers_Renders200()
    {
        var res = await _factory.CreateClient().GetAsync("/offers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
```

- [ ] **Step 4: Run full suite, verify green**

Run: `dotnet test Solution/GAC.sln`
Expected: all phase-3 tests + the prior 15 pass.

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web Solution/GAC.Tests/NewsOffersTests.cs
git commit -m "feat(phase3): /news (list+detail) and /offers pages"
```

- [ ] **Step 6: Final full-implementation review**

Dispatch a final code reviewer over the whole Phase-3 diff. Verify: no `.html` links remain in rendered output (grep views), no form POST wiring leaked in (Phase 5 boundary), no Arabic values or rtl.css rules added (Phase 4 boundary), all assets use `/assets/...`, every visible page returns 200 and hidden vehicles 404.

---

## Self-Review (author check)

- **Spec coverage:** ✅ port page bodies (Tasks 4–10), wire content from DB (Tasks 1,4,5,10 + hero/name in 6,9), dynamic header/megamenu/footer (Task 3), clean routing + old-`.html` redirects (Task 6). Arabic/RTL deliberately deferred to Phase 4; forms render-only deferred to Phase 5 — both noted as boundaries.
- **Type consistency:** services return the Phase-2 entity types; `Localize()` used uniformly in views; `UrlHelpers` centralizes URL/category/thumb logic. `SpecGroup.Rows` navigation name flagged for verification in Task 1.
- **Carry-forward from Phase 2:** singletons fetched via `FirstOrDefault` (✅ in services); Menu/Hero `.html` stubs rewritten (Task 2) + defended at render (`NormalizeUrl`); null social URLs render nothing (Task 3).
- **Known minor deviation:** megamenu/listing thumbnails come from a seeded Gallery image (Task 2); exact per-vehicle thumb crops are editable later via admin (Phase 6).
