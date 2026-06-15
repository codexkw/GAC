# Vehicle Structured Sections Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the single raw-HTML `Vehicle.BodyHtml` render with structured, form-driven sections (hero, feature blocks, spec tables, colours, trims) that non-technical admins can manage, while live pages keep rendering their existing HTML until migrated.

**Architecture:** The `Vehicle` entity already models `Features`, `SpecGroups`+`Rows`, `Colors`, `Trims` (EF-mapped, owned `LocalizedText`, cascade delete) and `VehicleService.GetBySlugAsync` already eager-loads them. We add one enum field (`FeatureSection.Layout`), rewrite `Detail.cshtml` to render the collections (falling back to `BodyHtml` when none exist), extend `AdminVehicleService`/`VehiclesController` with per-collection CRUD mirroring the existing `_Images` pattern, and add a self-hosted Trix WYSIWYG for feature bodies with server-side `Ganss.Xss` sanitisation.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server), xUnit + EF InMemory, Trix 2.x (self-hosted), Ganss.Xss (HtmlSanitizer).

**Conventions:** All commands run from `C:\Users\anas-\source\repos\GAC\Solution`. Build: `dotnet build GAC.sln -c Debug --nologo`. Test: `dotnet test GAC.Tests/GAC.Tests.csproj --nologo`. The repo is on `main`; commit after each task. `.cshtml` compiles at build time, so a malformed view fails `dotnet build`.

---

## File Structure

**Create:**
- `GAC.Core/Content/FeatureLayout.cs` — layout enum
- `GAC.Core/Services/IHtmlSanitizerService.cs` — sanitiser interface
- `GAC.Infrastructure/Services/HtmlSanitizerService.cs` — Ganss.Xss wrapper
- `GAC.Web/Infrastructure/VehicleContent.cs` — pure render helpers (fallback + layout→css)
- `GAC.Web/Views/Vehicles/_VehicleHero.cshtml` `_VehicleFeatures.cshtml` `_VehicleSpecs.cshtml` `_VehicleColors.cshtml` `_VehicleTrims.cshtml`
- `GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml` `FeatureEdit.cshtml` `_SpecGroups.cshtml` `_Colors.cshtml` `_Trims.cshtml`
- `GAC.Web/wwwroot/assets/vendor/trix/trix.css` `trix.umd.min.js` (downloaded)
- `GAC.Tests/VehicleContentTests.cs` `HtmlSanitizerServiceTests.cs` `AdminVehicleSectionsTests.cs`
- `GAC.Infrastructure/Migrations/<ts>_AddFeatureLayout.*` (generated)

**Modify:**
- `GAC.Core/Content/FeatureSection.cs` (+`Layout`)
- `GAC.Infrastructure/GAC.Infrastructure.csproj` (+`Ganss.Xss`)
- `GAC.Infrastructure/Services/AdminVehicleService.cs` + `GAC.Core/Services/IAdminVehicleService.cs` (GetAsync includes + collection CRUD)
- `GAC.Web/Areas/Admin/Controllers/VehiclesController.cs` (collection actions)
- `GAC.Web/Views/Vehicles/Detail.cshtml` (structured render + fallback)
- `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml` (section partials + Advanced HTML `<details>` + Trix script)
- `GAC.Web/Program.cs` (register `IHtmlSanitizerService`)
- `GAC.Web/wwwroot/assets/css/styles.css` (feature layout / specs / colours CSS)
- `GAC.Web/wwwroot/assets/css/admin.css` (Trix toolbar trim + editor sizing)
- `GAC.Tests/Admin/AdminVehicleServiceTests.cs` (constructor change for sanitiser)

---

## Task 1: FeatureLayout enum + Layout property + migration

**Files:**
- Create: `GAC.Core/Content/FeatureLayout.cs`
- Modify: `GAC.Core/Content/FeatureSection.cs`
- Create: `GAC.Infrastructure/Migrations/<ts>_AddFeatureLayout.cs` (generated)
- Test: `GAC.Tests/VehicleContentTests.cs` (default-value test only here)

- [ ] **Step 1: Create the enum**

`GAC.Core/Content/FeatureLayout.cs`:
```csharp
namespace GAC.Core.Content;

public enum FeatureLayout
{
    ImageLeft = 0,
    ImageRight = 1,
    Banner = 2,
    TextOnly = 3
}
```

- [ ] **Step 2: Add the property to FeatureSection**

Modify `GAC.Core/Content/FeatureSection.cs` — add the property after `ImagePath`:
```csharp
namespace GAC.Core.Content;

public class FeatureSection
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public FeatureLayout Layout { get; set; } = FeatureLayout.ImageLeft;
    public int SortOrder { get; set; }
}
```

- [ ] **Step 3: Write a default-value test**

Create `GAC.Tests/VehicleContentTests.cs` (more tests added in Task 3):
```csharp
using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class VehicleContentTests
{
    [Fact]
    public void FeatureSection_DefaultLayout_IsImageLeft()
    {
        Assert.Equal(FeatureLayout.ImageLeft, new FeatureSection().Layout);
    }
}
```

- [ ] **Step 4: Run the test**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FeatureSection_DefaultLayout_IsImageLeft --nologo`
Expected: PASS.

- [ ] **Step 5: Generate the migration**

EF maps an enum as `int` by convention, so no `ContentConfigurations.cs` change is needed.
Run: `dotnet ef migrations add AddFeatureLayout --project GAC.Infrastructure --startup-project GAC.Web`
Expected: a new migration adding a non-null `int` column `Layout` to `FeatureSections` with default `0`. Open the generated `Up` method and confirm it is a single `AddColumn<int>(name: "Layout", table: "FeatureSections", ... defaultValue: 0)`. If the generated `defaultValue` is absent, edit the `AddColumn` call to include `defaultValue: 0` so existing rows are valid.

- [ ] **Step 6: Build**

Run: `dotnet build GAC.sln -c Debug --nologo`
Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add GAC.Core/Content/FeatureLayout.cs GAC.Core/Content/FeatureSection.cs GAC.Infrastructure/Migrations GAC.Tests/VehicleContentTests.cs
git commit -m "feat(vehicles): add FeatureLayout enum + AddFeatureLayout migration"
```

---

## Task 2: HTML sanitiser service

**Files:**
- Modify: `GAC.Infrastructure/GAC.Infrastructure.csproj`
- Create: `GAC.Core/Services/IHtmlSanitizerService.cs`
- Create: `GAC.Infrastructure/Services/HtmlSanitizerService.cs`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/HtmlSanitizerServiceTests.cs`

- [ ] **Step 1: Add the NuGet package**

Add to `GAC.Infrastructure/GAC.Infrastructure.csproj` inside the existing package `ItemGroup` (the one with MailKit):
```xml
    <PackageReference Include="HtmlSanitizer" Version="9.0.886" />
```
(The `HtmlSanitizer` package provides the `Ganss.Xss` namespace. Use the latest `9.0.*` if `9.0.886` is unavailable; do not float to a different major.)

Run: `dotnet restore GAC.sln`
Expected: restore succeeds.

- [ ] **Step 2: Write the failing test**

Create `GAC.Tests/HtmlSanitizerServiceTests.cs`:
```csharp
using GAC.Infrastructure.Services;
using Xunit;

namespace GAC.Tests;

public class HtmlSanitizerServiceTests
{
    private readonly HtmlSanitizerService _svc = new();

    [Fact]
    public void Strips_Script_Tags()
    {
        var html = "<p>hi</p><script>alert(1)</script>";
        Assert.DoesNotContain("<script", _svc.Sanitize(html));
    }

    [Fact]
    public void Strips_Event_Handlers()
    {
        var html = "<a href=\"/x\" onclick=\"steal()\">link</a>";
        var result = _svc.Sanitize(html);
        Assert.DoesNotContain("onclick", result);
        Assert.Contains("href", result);
    }

    [Fact]
    public void Keeps_Allowed_Formatting()
    {
        var html = "<div><strong>bold</strong> <em>it</em><ul><li>a</li></ul></div>";
        var result = _svc.Sanitize(html);
        Assert.Contains("<strong>", result);
        Assert.Contains("<em>", result);
        Assert.Contains("<li>", result);
    }

    [Fact]
    public void Null_Or_Empty_Returns_Empty()
    {
        Assert.Equal("", _svc.Sanitize(null));
        Assert.Equal("", _svc.Sanitize(""));
    }
}
```

- [ ] **Step 3: Run to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter HtmlSanitizerServiceTests --nologo`
Expected: FAIL — `HtmlSanitizerService` does not exist.

- [ ] **Step 4: Create the interface**

`GAC.Core/Services/IHtmlSanitizerService.cs`:
```csharp
namespace GAC.Core.Services;

public interface IHtmlSanitizerService
{
    /// <summary>Strip everything except a small formatting allow-list. Null/blank → "".</summary>
    string Sanitize(string? html);
}
```

- [ ] **Step 5: Implement the service**

`GAC.Infrastructure/Services/HtmlSanitizerService.cs`:
```csharp
using GAC.Core.Services;
using Ganss.Xss;

namespace GAC.Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var t in new[] { "p", "div", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a" })
            _sanitizer.AllowedTags.Add(t);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowDataAttributes = false;

        _sanitizer.AllowedSchemes.Clear();
        foreach (var s in new[] { "http", "https", "mailto", "tel" })
            _sanitizer.AllowedSchemes.Add(s);
    }

    public string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? "" : _sanitizer.Sanitize(html);
}
```

- [ ] **Step 6: Register in DI**

In `GAC.Web/Program.cs`, beside the other `AddScoped` registrations (e.g. after `builder.Services.AddScoped<IMediaService, MediaService>();`), add:
```csharp
builder.Services.AddScoped<GAC.Core.Services.IHtmlSanitizerService, GAC.Infrastructure.Services.HtmlSanitizerService>();
```

- [ ] **Step 7: Run tests**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter HtmlSanitizerServiceTests --nologo`
Expected: 4 PASS.

- [ ] **Step 8: Commit**

```bash
git add GAC.Infrastructure/GAC.Infrastructure.csproj GAC.Core/Services/IHtmlSanitizerService.cs GAC.Infrastructure/Services/HtmlSanitizerService.cs GAC.Web/Program.cs GAC.Tests/HtmlSanitizerServiceTests.cs
git commit -m "feat: add HtmlSanitizerService (Ganss.Xss) with formatting allow-list"
```

---

## Task 3: Public render helpers (pure, unit-tested)

**Files:**
- Create: `GAC.Web/Infrastructure/VehicleContent.cs`
- Test: `GAC.Tests/VehicleContentTests.cs` (extend)

- [ ] **Step 1: Write failing tests**

Append to `GAC.Tests/VehicleContentTests.cs` (add `using GAC.Web.Infrastructure;` at top):
```csharp
    [Fact]
    public void HasStructuredContent_FalseWhenAllEmpty()
    {
        Assert.False(VehicleContent.HasStructuredContent(new Vehicle()));
    }

    [Fact]
    public void HasStructuredContent_TrueWhenAnyFeature()
    {
        var v = new Vehicle();
        v.Features.Add(new FeatureSection());
        Assert.True(VehicleContent.HasStructuredContent(v));
    }

    [Fact]
    public void HasStructuredContent_TrueWhenAnyTrimSpecOrColor()
    {
        var withTrim = new Vehicle(); withTrim.Trims.Add(new Trim());
        var withSpec = new Vehicle(); withSpec.SpecGroups.Add(new SpecGroup());
        var withColor = new Vehicle(); withColor.Colors.Add(new ColorOption());
        Assert.True(VehicleContent.HasStructuredContent(withTrim));
        Assert.True(VehicleContent.HasStructuredContent(withSpec));
        Assert.True(VehicleContent.HasStructuredContent(withColor));
    }

    [Theory]
    [InlineData(FeatureLayout.ImageLeft, "mp-feature")]
    [InlineData(FeatureLayout.ImageRight, "mp-feature mp-feature--reverse")]
    [InlineData(FeatureLayout.Banner, "mp-feature mp-feature--banner")]
    [InlineData(FeatureLayout.TextOnly, "mp-feature mp-feature--text")]
    public void FeatureLayoutCss_MapsEachVariant(FeatureLayout layout, string expected)
    {
        Assert.Equal(expected, VehicleContent.FeatureLayoutCss(layout));
    }

    [Fact]
    public void ShowsImage_FalseForTextOnly()
    {
        Assert.False(VehicleContent.ShowsImage(FeatureLayout.TextOnly));
        Assert.True(VehicleContent.ShowsImage(FeatureLayout.ImageLeft));
    }
```

- [ ] **Step 2: Run to verify failure**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter VehicleContentTests --nologo`
Expected: FAIL — `VehicleContent` not found.

- [ ] **Step 3: Implement the helper**

`GAC.Web/Infrastructure/VehicleContent.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

/// <summary>Pure helpers for rendering the vehicle detail page.</summary>
public static class VehicleContent
{
    /// <summary>A vehicle uses structured sections when any typed collection is populated.</summary>
    public static bool HasStructuredContent(Vehicle v)
        => v.Features.Count > 0
        || v.SpecGroups.Count > 0
        || v.Colors.Count > 0
        || v.Trims.Count > 0;

    public static string FeatureLayoutCss(FeatureLayout layout) => layout switch
    {
        FeatureLayout.ImageRight => "mp-feature mp-feature--reverse",
        FeatureLayout.Banner => "mp-feature mp-feature--banner",
        FeatureLayout.TextOnly => "mp-feature mp-feature--text",
        _ => "mp-feature"
    };

    public static bool ShowsImage(FeatureLayout layout) => layout != FeatureLayout.TextOnly;
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter VehicleContentTests --nologo`
Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add GAC.Web/Infrastructure/VehicleContent.cs GAC.Tests/VehicleContentTests.cs
git commit -m "feat(vehicles): add VehicleContent render helpers (fallback + layout css)"
```

---

## Task 4: Public Detail.cshtml structured render + sub-partials + CSS

**Files:**
- Modify: `GAC.Web/Views/Vehicles/Detail.cshtml`
- Create: `GAC.Web/Views/Vehicles/_VehicleHero.cshtml` `_VehicleFeatures.cshtml` `_VehicleSpecs.cshtml` `_VehicleColors.cshtml` `_VehicleTrims.cshtml`
- Modify: `GAC.Web/wwwroot/assets/css/styles.css`

This task is presentational; verification is "build succeeds" + a manual eyeball. No new unit tests (logic is covered by Task 3).

- [ ] **Step 1: Rewrite the detail view with the fallback branch**

`GAC.Web/Views/Vehicles/Detail.cshtml`:
```cshtml
@using GAC.Web.Infrastructure
@model GAC.Core.Content.Vehicle
@{ Layout = "_Layout"; }

@if (VehicleContent.HasStructuredContent(Model))
{
    <partial name="_VehicleHero" model="Model" />
    <partial name="_VehicleFeatures" model="Model" />
    <partial name="_VehicleSpecs" model="Model" />
    <partial name="_VehicleColors" model="Model" />
    <partial name="_VehicleTrims" model="Model" />
}
else
{
    @Html.Raw(Model.BodyHtml.Localize())
}
```

- [ ] **Step 2: Hero partial**

`GAC.Web/Views/Vehicles/_VehicleHero.cshtml`:
```cshtml
@using GAC.Core.Content
@model GAC.Core.Content.Vehicle
@{
    var hero = Model.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Hero)
               ?? Model.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
}
<section class="mp-hero">
  <a class="mp-hero__link" href="#enquiry" aria-label="@L["Book a Test Drive"]">
    @if (hero != null)
    {
        <img class="mp-hero__img" src="@hero.Path" alt="@Model.Name.Localize()" />
    }
    <div class="mp-hero__overlay">
      <div class="container">
        <h1 class="mp-hero__title">@Model.Name.Localize()</h1>
        @if (!string.IsNullOrWhiteSpace(Model.Tagline.Localize()))
        {
            <p class="mp-hero__sub">@Model.Tagline.Localize()</p>
        }
        <span class="btn btn--hero">@L["Book a Test Drive"]</span>
      </div>
    </div>
  </a>
</section>
@if (!string.IsNullOrWhiteSpace(Model.IntroText.Localize()))
{
    <section class="mp-section">
      <div class="container"><p class="mp-head__body">@Model.IntroText.Localize()</p></div>
    </section>
}
```

- [ ] **Step 3: Features partial**

`GAC.Web/Views/Vehicles/_VehicleFeatures.cshtml`:
```cshtml
@using GAC.Web.Infrastructure
@model GAC.Core.Content.Vehicle
@if (Model.Features.Count > 0)
{
    <section class="mp-section">
      <div class="container">
        @foreach (var f in Model.Features.OrderBy(x => x.SortOrder))
        {
            <div class="@VehicleContent.FeatureLayoutCss(f.Layout)">
              @if (VehicleContent.ShowsImage(f.Layout) && !string.IsNullOrWhiteSpace(f.ImagePath))
              {
                  <div class="mp-feature__media"><img src="@f.ImagePath" alt="@f.Heading.Localize()" /></div>
              }
              <div class="mp-feature__body">
                @if (!string.IsNullOrWhiteSpace(f.Heading.Localize()))
                {
                    <h3 class="mp-feature__title">@f.Heading.Localize()</h3>
                }
                @Html.Raw(f.Body.Localize())
              </div>
            </div>
        }
      </div>
    </section>
}
```

- [ ] **Step 4: Specs partial**

`GAC.Web/Views/Vehicles/_VehicleSpecs.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
@if (Model.SpecGroups.Count > 0)
{
    <section class="mp-section mp-section--grey">
      <div class="container">
        @foreach (var g in Model.SpecGroups.OrderBy(x => x.SortOrder))
        {
            <div class="mp-specs">
              @if (!string.IsNullOrWhiteSpace(g.Title.Localize()))
              {
                  <h3 class="mp-specs__title">@g.Title.Localize()</h3>
              }
              <table class="mp-specs__table">
                @foreach (var r in g.Rows.OrderBy(x => x.SortOrder))
                {
                    <tr><th class="mp-specs__label">@r.Label.Localize()</th><td class="mp-specs__value">@r.Value.Localize()</td></tr>
                }
              </table>
            </div>
        }
      </div>
    </section>
}
```

- [ ] **Step 5: Colors partial**

`GAC.Web/Views/Vehicles/_VehicleColors.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
@if (Model.Colors.Count > 0)
{
    <section class="mp-section">
      <div class="container">
        <div class="mp-colors">
          @foreach (var c in Model.Colors.OrderBy(x => x.SortOrder))
          {
              <div class="mp-color">
                @if (!string.IsNullOrWhiteSpace(c.ImagePath))
                {
                    <img class="mp-color__img" src="@c.ImagePath" alt="@c.Name.Localize()" />
                }
                else
                {
                    <span class="mp-color__chip" style="background:@c.Hex"></span>
                }
                <span class="mp-color__name">@c.Name.Localize()</span>
              </div>
          }
        </div>
      </div>
    </section>
}
```

- [ ] **Step 6: Trims partial**

`GAC.Web/Views/Vehicles/_VehicleTrims.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
@if (Model.Trims.Count > 0)
{
    <section class="mp-section" id="trims">
      <div class="container">
        <div class="mp-trims" style="gap:var(--space-7); flex-wrap:wrap;">
          @foreach (var t in Model.Trims.OrderBy(x => x.SortOrder))
          {
              <article class="mp-trim">
                <div class="mp-trim__body">
                  <p class="mp-trim__model">@Model.Name.Localize()</p>
                  <h3 class="mp-trim__name">@t.Name.Localize()</h3>
                  @if (t.Price.HasValue)
                  {
                      <ul class="mp-trim__price"><li>@t.Price.Value.ToString("N0") @L["SAR"]</li></ul>
                  }
                  @if (!string.IsNullOrWhiteSpace(t.Highlights.Localize()))
                  {
                      <div class="mp-trim__highlights">@Html.Raw(t.Highlights.Localize())</div>
                  }
                  <div class="mp-trim__cta">
                    <a class="btn btn--trim" href="#enquiry">@L["Book a Test Drive"]</a>
                    @if (!string.IsNullOrWhiteSpace(t.SpecPdf))
                    {
                        <a class="btn btn--trim" href="@t.SpecPdf" target="_blank" rel="noopener">@L["Specifications"]</a>
                    }
                  </div>
                </div>
              </article>
          }
        </div>
      </div>
    </section>
}
```

> Note: `@L[...]` is the shared `IHtmlLocalizer` injected globally (used across existing views, e.g. the header). If a `_ViewImports.cshtml` does not already expose `L` to `Views/Vehicles`, confirm by building; the header partial uses `@L` so it is available app-wide. Add the four new keys (`Book a Test Drive`, `Specifications`, `SAR`) to `Resources/SharedResource.ar.resx` if Arabic strings are wanted (English falls through by default).

- [ ] **Step 7: Add CSS for the new/modifier classes**

Append to `GAC.Web/wwwroot/assets/css/styles.css`:
```css
/* ---- Structured vehicle sections ---- */
.mp-feature--reverse { flex-direction: row-reverse; }
.mp-feature--banner { display: block; }
.mp-feature--banner .mp-feature__media img { width: 100%; height: auto; display: block; }
.mp-feature--text .mp-feature__media { display: none; }

.mp-specs { margin-bottom: 2rem; }
.mp-specs__title { margin: 0 0 .75rem; }
.mp-specs__table { width: 100%; border-collapse: collapse; }
.mp-specs__table th, .mp-specs__table td { text-align: start; padding: .6rem .75rem; border-bottom: 1px solid rgba(0,0,0,.08); }
.mp-specs__label { width: 45%; font-weight: 600; }

.mp-colors { display: flex; flex-wrap: wrap; gap: 1.25rem; }
.mp-color { display: flex; flex-direction: column; align-items: center; gap: .5rem; width: 120px; }
.mp-color__chip { width: 56px; height: 56px; border-radius: 50%; border: 1px solid rgba(0,0,0,.15); }
.mp-color__img { width: 100%; height: auto; border-radius: 8px; }
.mp-color__name { font-size: .9rem; }
```

- [ ] **Step 8: Build**

Run: `dotnet build GAC.sln -c Debug --nologo`
Expected: Build succeeded (Razor compiles).

- [ ] **Step 9: Commit**

```bash
git add GAC.Web/Views/Vehicles GAC.Web/wwwroot/assets/css/styles.css
git commit -m "feat(vehicles): render structured sections with HTML fallback on detail page"
```

---

## Task 5: AdminVehicleService — GetAsync includes + Features CRUD (sanitised)

**Files:**
- Modify: `GAC.Core/Services/IAdminVehicleService.cs`
- Modify: `GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify: `GAC.Tests/Admin/AdminVehicleServiceTests.cs` (constructor change)
- Test: `GAC.Tests/AdminVehicleSectionsTests.cs`

- [ ] **Step 1: Extend the interface**

Add to `IAdminVehicleService` (after the image methods):
```csharp
    // Feature blocks
    Task<FeatureSection?> GetFeatureAsync(int featureId, CancellationToken ct = default);
    Task<int> AddFeatureAsync(int vehicleId, FeatureSection feature, CancellationToken ct = default);
    Task<bool> UpdateFeatureAsync(FeatureSection feature, CancellationToken ct = default);
    Task<bool> RemoveFeatureAsync(int featureId, CancellationToken ct = default);
    Task<bool> MoveFeatureAsync(int featureId, int direction, CancellationToken ct = default);
```

- [ ] **Step 2: Update the constructor + GetAsync includes + add Feature methods**

In `AdminVehicleService.cs`, change the constructor to inject the sanitiser, broaden `GetAsync`, and add the Feature methods:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminVehicleService : IAdminVehicleService
{
    private readonly ApplicationDbContext _db;
    private readonly IHtmlSanitizerService _sanitizer;
    public AdminVehicleService(ApplicationDbContext db, IHtmlSanitizerService sanitizer)
    { _db = db; _sanitizer = sanitizer; }

    // ... ListAsync unchanged ...

    public async Task<Vehicle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Trims)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
```
Add these methods to the class (anywhere after the image methods):
```csharp
    public async Task<FeatureSection?> GetFeatureAsync(int featureId, CancellationToken ct = default)
        => await _db.Set<FeatureSection>().FirstOrDefaultAsync(f => f.Id == featureId, ct);

    public async Task<int> AddFeatureAsync(int vehicleId, FeatureSection feature, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        feature.VehicleId = vehicleId;
        feature.Body = Sanitize(feature.Body);
        feature.SortOrder = await _db.Set<FeatureSection>().CountAsync(f => f.VehicleId == vehicleId, ct);
        _db.Set<FeatureSection>().Add(feature);
        await _db.SaveChangesAsync(ct);
        return feature.Id;
    }

    public async Task<bool> UpdateFeatureAsync(FeatureSection feature, CancellationToken ct = default)
    {
        var existing = await _db.Set<FeatureSection>().FirstOrDefaultAsync(f => f.Id == feature.Id, ct);
        if (existing is null) return false;
        existing.Heading = feature.Heading;
        existing.Body = Sanitize(feature.Body);
        existing.ImagePath = feature.ImagePath;
        existing.Layout = feature.Layout;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> RemoveFeatureAsync(int featureId, CancellationToken ct = default)
    {
        var f = await _db.Set<FeatureSection>().FindAsync([featureId], ct);
        if (f is null) return false;
        _db.Set<FeatureSection>().Remove(f);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveFeatureAsync(int featureId, int direction, CancellationToken ct = default)
    {
        var f = await _db.Set<FeatureSection>().FindAsync([featureId], ct);
        if (f is null) return false;
        var siblings = await _db.Set<FeatureSection>()
            .Where(x => x.VehicleId == f.VehicleId).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(x => x.Id == featureId);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private LocalizedText Sanitize(LocalizedText body)
        => new() { En = _sanitizer.Sanitize(body.En), Ar = _sanitizer.Sanitize(body.Ar) };
```

- [ ] **Step 3: Fix existing tests for the new constructor**

In `GAC.Tests/Admin/AdminVehicleServiceTests.cs`, add a helper and replace every `new AdminVehicleService(db)` with `NewSvc(db)`:
```csharp
using GAC.Infrastructure.Services;
// ...
    private static AdminVehicleService NewSvc(ApplicationDbContext db)
        => new(db, new HtmlSanitizerService());
```
Use editor find-replace: `new AdminVehicleService(db)` → `NewSvc(db)` (all occurrences).

- [ ] **Step 4: Write failing tests for Feature CRUD**

Create `GAC.Tests/AdminVehicleSectionsTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class AdminVehicleSectionsTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);
    private static AdminVehicleService NewSvc(ApplicationDbContext db) => new(db, new HtmlSanitizerService());

    [Fact]
    public async Task AddFeature_SanitizesBody_AndSetsOrder()
    {
        var db = NewDb(nameof(AddFeature_SanitizesBody_AndSetsOrder));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f", Name = "F" });

        var id1 = await svc.AddFeatureAsync(vid, new FeatureSection
        {
            Heading = "H1",
            Body = new LocalizedText { En = "<p>ok</p><script>bad()</script>" },
            Layout = FeatureLayout.ImageRight
        });
        var id2 = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "H2" });

        var f1 = await svc.GetFeatureAsync(id1);
        Assert.DoesNotContain("<script", f1!.Body.En);
        Assert.Equal(FeatureLayout.ImageRight, f1.Layout);
        Assert.Equal(0, f1.SortOrder);
        Assert.Equal(1, (await svc.GetFeatureAsync(id2))!.SortOrder);
    }

    [Fact]
    public async Task UpdateFeature_ChangesFields_AndSanitizes()
    {
        var db = NewDb(nameof(UpdateFeature_ChangesFields_AndSanitizes));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f2", Name = "F" });
        var id = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "old" });

        var ok = await svc.UpdateFeatureAsync(new FeatureSection
        {
            Id = id, Heading = "new",
            Body = new LocalizedText { En = "<b>x</b><img src=x onerror=y>" },
            Layout = FeatureLayout.Banner
        });

        Assert.True(ok);
        var f = await svc.GetFeatureAsync(id);
        Assert.Equal("new", f!.Heading.En);
        Assert.Equal(FeatureLayout.Banner, f.Layout);
        Assert.DoesNotContain("onerror", f.Body.En);
        Assert.DoesNotContain("<img", f.Body.En);
    }

    [Fact]
    public async Task RemoveFeature_Deletes()
    {
        var db = NewDb(nameof(RemoveFeature_Deletes));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f3", Name = "F" });
        var id = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "x" });
        Assert.True(await svc.RemoveFeatureAsync(id));
        Assert.Null(await svc.GetFeatureAsync(id));
    }

    [Fact]
    public async Task MoveFeature_SwapsOrder()
    {
        var db = NewDb(nameof(MoveFeature_SwapsOrder));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "f4", Name = "F" });
        var a = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "A" }); // order 0
        var b = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "B" }); // order 1
        Assert.True(await svc.MoveFeatureAsync(b, -1));
        Assert.Equal(0, (await svc.GetFeatureAsync(b))!.SortOrder);
        Assert.Equal(1, (await svc.GetFeatureAsync(a))!.SortOrder);
    }
}
```

- [ ] **Step 5: Run to verify failure, then build**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter AdminVehicleSectionsTests --nologo`
Expected: FAIL until Step 2 compiles. After Step 2/3, run again.

- [ ] **Step 6: Run all tests**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --nologo`
Expected: all green (existing + 4 new). If existing `AdminVehicleServiceTests` fail to compile, finish the `NewSvc` replacement from Step 3.

- [ ] **Step 7: Commit**

```bash
git add GAC.Core/Services/IAdminVehicleService.cs GAC.Infrastructure/Services/AdminVehicleService.cs GAC.Tests/Admin/AdminVehicleServiceTests.cs GAC.Tests/AdminVehicleSectionsTests.cs
git commit -m "feat(admin): vehicle Feature CRUD with HTML sanitisation; broaden GetAsync includes"
```

---

## Task 6: Features admin UI (Trix + sub-page + list partial + controller)

**Files:**
- Modify: `GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml` `FeatureEdit.cshtml`
- Modify: `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`
- Create: `GAC.Web/wwwroot/assets/vendor/trix/trix.css` `trix.umd.min.js`
- Modify: `GAC.Web/wwwroot/assets/css/admin.css`

- [ ] **Step 1: Download Trix assets**

Run from repo root:
```bash
mkdir -p Solution/GAC.Web/wwwroot/assets/vendor/trix
curl -L -o Solution/GAC.Web/wwwroot/assets/vendor/trix/trix.css https://unpkg.com/trix@2.1.15/dist/trix.css
curl -L -o Solution/GAC.Web/wwwroot/assets/vendor/trix/trix.umd.min.js https://unpkg.com/trix@2.1.15/dist/trix.umd.min.js
```
Verify both files are non-empty (`trix.umd.min.js` > 100KB, `trix.css` > 5KB). If `unpkg` is unreachable, use `https://cdn.jsdelivr.net/npm/trix@2.1.15/dist/`.

- [ ] **Step 2: Add controller actions**

Add to `VehiclesController.cs` (after `MoveImage`):
```csharp
    public async Task<IActionResult> FeatureEdit(int vehicleId, int? id)
    {
        if (!await _svc.SlugExistsAnyAsync(vehicleId)) { } // no-op guard placeholder removed below
        var feature = id is null
            ? new FeatureSection { VehicleId = vehicleId }
            : await _svc.GetFeatureAsync(id.Value);
        if (feature is null) return NotFound();
        feature.VehicleId = vehicleId;
        return View(feature);
    }

    [HttpPost]
    public async Task<IActionResult> FeatureSave(int vehicleId, FeatureSection feature)
    {
        feature.VehicleId = vehicleId;
        if (feature.Id == 0) await _svc.AddFeatureAsync(vehicleId, feature);
        else await _svc.UpdateFeatureAsync(feature);
        TempData["Flash"] = "Feature saved.";
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }

    [HttpPost] public async Task<IActionResult> RemoveFeature(int featureId, int vehicleId)
    { await _svc.RemoveFeatureAsync(featureId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> MoveFeature(int featureId, int vehicleId, int direction)
    { await _svc.MoveFeatureAsync(featureId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```
Remove the placeholder guard line; the final `FeatureEdit` action body is simply:
```csharp
    public async Task<IActionResult> FeatureEdit(int vehicleId, int? id)
    {
        var feature = id is null
            ? new FeatureSection { VehicleId = vehicleId }
            : await _svc.GetFeatureAsync(id.Value);
        if (feature is null) return NotFound();
        feature.VehicleId = vehicleId;
        return View(feature);
    }
```

- [ ] **Step 3: Features list partial**

`GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
<h2>Feature sections</h2>
<table class="adm-table">
  <thead><tr><th>Order</th><th>Heading</th><th>Layout</th><th></th></tr></thead>
  <tbody>
    @foreach (var f in Model.Features.OrderBy(x => x.SortOrder))
    {
        <tr>
          <td>
            <form asp-action="MoveFeature" method="post" style="display:inline">
              <input type="hidden" name="featureId" value="@f.Id" />
              <input type="hidden" name="vehicleId" value="@Model.Id" />
              <input type="hidden" name="direction" value="-1" />
              <button type="submit" class="adm-btn" title="Move up">&uarr;</button>
            </form>
            <form asp-action="MoveFeature" method="post" style="display:inline">
              <input type="hidden" name="featureId" value="@f.Id" />
              <input type="hidden" name="vehicleId" value="@Model.Id" />
              <input type="hidden" name="direction" value="1" />
              <button type="submit" class="adm-btn" title="Move down">&darr;</button>
            </form>
          </td>
          <td>@f.Heading.En</td>
          <td>@f.Layout</td>
          <td>
            <a asp-area="Admin" asp-action="FeatureEdit" asp-route-vehicleId="@Model.Id" asp-route-id="@f.Id">Edit</a>
            <form asp-action="RemoveFeature" method="post" style="display:inline"
                  onsubmit="return confirm('Remove this feature?')">
              <input type="hidden" name="featureId" value="@f.Id" />
              <input type="hidden" name="vehicleId" value="@Model.Id" />
              <button type="submit" class="adm-btn adm-btn--danger">Remove</button>
            </form>
          </td>
        </tr>
    }
  </tbody>
</table>
<p><a asp-area="Admin" asp-action="FeatureEdit" asp-route-vehicleId="@Model.Id" class="adm-btn">Add feature section</a></p>
```

- [ ] **Step 4: Feature edit sub-page with Trix**

`GAC.Web/Areas/Admin/Views/Vehicles/FeatureEdit.cshtml`:
```cshtml
@using GAC.Core.Content
@using Microsoft.AspNetCore.Mvc.Rendering
@model GAC.Core.Content.FeatureSection
@{
    ViewData["Title"] = Model.Id == 0 ? "New feature" : "Edit feature";
}
@section Head { <link rel="stylesheet" href="/assets/vendor/trix/trix.css" /> }

<p><a asp-area="Admin" asp-action="Edit" asp-route-id="@Model.VehicleId">&larr; Back to vehicle</a></p>
<h1>@ViewData["Title"]</h1>

<form method="post" asp-action="FeatureSave">
  <input type="hidden" name="vehicleId" value="@Model.VehicleId" />
  <input type="hidden" asp-for="Id" />

  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Heading</span>
    <div class="adm-localized__pair">
      <div><label>English</label><input name="Heading.En" value="@Model.Heading.En" /></div>
      <div dir="rtl"><label>Arabic</label><input name="Heading.Ar" value="@Model.Heading.Ar" /></div>
    </div>
  </div>

  <div class="adm-field">
    <label>Layout</label>
    <select asp-for="Layout" asp-items="Html.GetEnumSelectList<FeatureLayout>()"></select>
  </div>

  <div class="adm-field">
    <label>Image</label>
    <span style="display:flex;gap:.5rem;align-items:center">
      <input type="text" name="ImagePath" value="@Model.ImagePath" data-media-input />
      <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
    </span>
  </div>

  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Body</span>
    <div class="adm-localized__pair">
      <div>
        <label>English</label>
        <input type="hidden" id="bodyEn" name="Body.En" value="@Model.Body.En" />
        <trix-editor input="bodyEn" class="adm-trix"></trix-editor>
      </div>
      <div dir="rtl">
        <label>Arabic</label>
        <input type="hidden" id="bodyAr" name="Body.Ar" value="@Model.Body.Ar" />
        <trix-editor input="bodyAr" class="adm-trix" dir="rtl"></trix-editor>
      </div>
    </div>
  </div>

  <button type="submit" class="adm-btn">Save feature</button>
</form>

<partial name="_PickerModal" />

@section Scripts { <script src="/assets/vendor/trix/trix.umd.min.js"></script> }
```

> The admin layout renders `@await RenderSectionAsync("Scripts", ...)` but **not** a `Head` section. Confirm `_AdminLayout.cshtml` has a Head section; if not, move the Trix `<link>` into the existing `<head>` of `_AdminLayout.cshtml` as a permanent stylesheet, or add `@await RenderSectionAsync("Head", required: false)` inside its `<head>`. Pick whichever matches the codebase — adding the optional Head section is the smaller change.

- [ ] **Step 5: Wire the Features partial + ensure picker is present on Edit page**

In `Areas/Admin/Views/Vehicles/Edit.cshtml`, the `@if (!isNew)` block already includes `_Images` and `_PickerModal`. Add `_Features` there:
```cshtml
@if (!isNew)
{
    <partial name="_Images" model="Model" />
    <partial name="_Features" model="Model" />
    <partial name="_PickerModal" />
}
```

- [ ] **Step 6: Trix toolbar trim + editor sizing CSS**

Append to `GAC.Web/wwwroot/assets/css/admin.css`:
```css
/* Trix: limit toolbar to bold/italic/list/link for non-tech editors */
trix-toolbar .trix-button-group--text-tools .trix-button--icon-strike,
trix-toolbar .trix-button-group--block-tools .trix-button--icon-heading-1,
trix-toolbar .trix-button-group--block-tools .trix-button--icon-quote,
trix-toolbar .trix-button-group--block-tools .trix-button--icon-code,
trix-toolbar .trix-button-group--block-tools .trix-button--icon-decrease-nesting-level,
trix-toolbar .trix-button-group--block-tools .trix-button--icon-increase-nesting-level,
trix-toolbar .trix-button-group--file-tools { display: none; }
.adm-trix { min-height: 160px; background: #fff; }
```

- [ ] **Step 7: Build + manual smoke**

Run: `dotnet build GAC.sln -c Debug --nologo`
Expected: Build succeeded.
Manual (optional, run app): edit a vehicle → "Add feature section" → enter heading, pick layout, type formatted body → Save → confirm it appears in the Features list and on the public page.

- [ ] **Step 8: Commit**

```bash
git add GAC.Web/Areas/Admin/Controllers/VehiclesController.cs GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml GAC.Web/Areas/Admin/Views/Vehicles/FeatureEdit.cshtml GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml GAC.Web/wwwroot/assets/vendor/trix GAC.Web/wwwroot/assets/css/admin.css
git commit -m "feat(admin): feature-section editor with Trix WYSIWYG + layout"
```

---

## Task 7: AdminVehicleService — Specs, Colours, Trims CRUD

**Files:**
- Modify: `GAC.Core/Services/IAdminVehicleService.cs`
- Modify: `GAC.Infrastructure/Services/AdminVehicleService.cs`
- Test: `GAC.Tests/AdminVehicleSectionsTests.cs` (extend)

- [ ] **Step 1: Extend the interface**

Add to `IAdminVehicleService`:
```csharp
    // Spec groups + rows
    Task<int> AddSpecGroupAsync(int vehicleId, LocalizedText title, CancellationToken ct = default);
    Task<bool> RemoveSpecGroupAsync(int groupId, CancellationToken ct = default);
    Task<bool> MoveSpecGroupAsync(int groupId, int direction, CancellationToken ct = default);
    Task<int> AddSpecRowAsync(int groupId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
    Task<bool> RemoveSpecRowAsync(int rowId, CancellationToken ct = default);
    // Colours
    Task<int> AddColorAsync(int vehicleId, LocalizedText name, string hex, string? imagePath, CancellationToken ct = default);
    Task<bool> RemoveColorAsync(int colorId, CancellationToken ct = default);
    Task<bool> MoveColorAsync(int colorId, int direction, CancellationToken ct = default);
    // Trims
    Task<int> AddTrimAsync(int vehicleId, Trim trim, CancellationToken ct = default);
    Task<bool> RemoveTrimAsync(int trimId, CancellationToken ct = default);
    Task<bool> MoveTrimAsync(int trimId, int direction, CancellationToken ct = default);
```

- [ ] **Step 2: Implement (add to AdminVehicleService)**

```csharp
    // ---- Spec groups ----
    public async Task<int> AddSpecGroupAsync(int vehicleId, LocalizedText title, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var g = new SpecGroup
        {
            VehicleId = vehicleId, Title = title,
            SortOrder = await _db.Set<SpecGroup>().CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.Set<SpecGroup>().Add(g);
        await _db.SaveChangesAsync(ct);
        return g.Id;
    }

    public async Task<bool> RemoveSpecGroupAsync(int groupId, CancellationToken ct = default)
        => await RemoveByIdAsync<SpecGroup>(groupId, ct);

    public async Task<bool> MoveSpecGroupAsync(int groupId, int direction, CancellationToken ct = default)
    {
        var g = await _db.Set<SpecGroup>().FindAsync([groupId], ct);
        if (g is null) return false;
        return await SwapOrderAsync<SpecGroup>(x => x.VehicleId == g.VehicleId, groupId, direction, ct);
    }

    public async Task<int> AddSpecRowAsync(int groupId, LocalizedText label, LocalizedText value, CancellationToken ct = default)
    {
        if (!await _db.Set<SpecGroup>().AnyAsync(g => g.Id == groupId, ct)) return 0;
        var r = new SpecRow
        {
            SpecGroupId = groupId, Label = label, Value = value,
            SortOrder = await _db.Set<SpecRow>().CountAsync(x => x.SpecGroupId == groupId, ct)
        };
        _db.Set<SpecRow>().Add(r);
        await _db.SaveChangesAsync(ct);
        return r.Id;
    }

    public async Task<bool> RemoveSpecRowAsync(int rowId, CancellationToken ct = default)
        => await RemoveByIdAsync<SpecRow>(rowId, ct);

    // ---- Colours ----
    public async Task<int> AddColorAsync(int vehicleId, LocalizedText name, string hex, string? imagePath, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var c = new ColorOption
        {
            VehicleId = vehicleId, Name = name, Hex = string.IsNullOrWhiteSpace(hex) ? "#000000" : hex,
            ImagePath = imagePath,
            SortOrder = await _db.Set<ColorOption>().CountAsync(x => x.VehicleId == vehicleId, ct)
        };
        _db.Set<ColorOption>().Add(c);
        await _db.SaveChangesAsync(ct);
        return c.Id;
    }

    public async Task<bool> RemoveColorAsync(int colorId, CancellationToken ct = default)
        => await RemoveByIdAsync<ColorOption>(colorId, ct);

    public async Task<bool> MoveColorAsync(int colorId, int direction, CancellationToken ct = default)
    {
        var c = await _db.Set<ColorOption>().FindAsync([colorId], ct);
        if (c is null) return false;
        return await SwapOrderAsync<ColorOption>(x => x.VehicleId == c.VehicleId, colorId, direction, ct);
    }

    // ---- Trims ----
    public async Task<int> AddTrimAsync(int vehicleId, Trim trim, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        trim.VehicleId = vehicleId;
        trim.SortOrder = await _db.Set<Trim>().CountAsync(x => x.VehicleId == vehicleId, ct);
        _db.Set<Trim>().Add(trim);
        await _db.SaveChangesAsync(ct);
        return trim.Id;
    }

    public async Task<bool> RemoveTrimAsync(int trimId, CancellationToken ct = default)
        => await RemoveByIdAsync<Trim>(trimId, ct);

    public async Task<bool> MoveTrimAsync(int trimId, int direction, CancellationToken ct = default)
    {
        var t = await _db.Set<Trim>().FindAsync([trimId], ct);
        if (t is null) return false;
        return await SwapOrderAsync<Trim>(x => x.VehicleId == t.VehicleId, trimId, direction, ct);
    }

    // ---- shared helpers ----
    private async Task<bool> RemoveByIdAsync<T>(int id, CancellationToken ct) where T : class
    {
        var e = await _db.Set<T>().FindAsync([id], ct);
        if (e is null) return false;
        _db.Set<T>().Remove(e);
        await _db.SaveChangesAsync(ct);
        return true;
    }
```

`SwapOrderAsync` requires reflection-free ordering. Implement it concretely per type instead of generically — replace the generic calls above with these typed private methods (generics over a `SortOrder` member are awkward in EF). Add a small interface:

In `GAC.Core/Content`, create `IOrderable.cs`:
```csharp
namespace GAC.Core.Content;
public interface IOrderable { int Id { get; } int SortOrder { get; set; } }
```
Then make `SpecGroup`, `ColorOption`, `Trim`, `FeatureSection` implement it (add `: IOrderable` to each class declaration — `Id`/`SortOrder` already exist, so no body change). Now implement the helper:
```csharp
    private async Task<bool> SwapOrderAsync<T>(
        System.Linq.Expressions.Expression<System.Func<T, bool>> sibling, int id, int direction, CancellationToken ct)
        where T : class, IOrderable
    {
        var list = await _db.Set<T>().Where(sibling).OrderBy(x => x.SortOrder).ToListAsync(ct);
        var idx = list.FindIndex(x => x.Id == id);
        var swap = idx + direction;
        if (idx < 0 || swap < 0 || swap >= list.Count) return false;
        (list[idx].SortOrder, list[swap].SortOrder) = (list[swap].SortOrder, list[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
```
(Add `using GAC.Core.Content;` if not already present — it is.)

- [ ] **Step 3: Write tests**

Append to `GAC.Tests/AdminVehicleSectionsTests.cs`:
```csharp
    [Fact]
    public async Task SpecGroup_And_Row_AddRemove()
    {
        var db = NewDb(nameof(SpecGroup_And_Row_AddRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "s", Name = "S" });
        var gid = await svc.AddSpecGroupAsync(vid, new LocalizedText { En = "Engine" });
        var rid = await svc.AddSpecRowAsync(gid, new LocalizedText { En = "Power" }, new LocalizedText { En = "200hp" });
        Assert.Equal(1, await db.Set<SpecGroup>().CountAsync());
        Assert.Equal(1, await db.Set<SpecRow>().CountAsync());
        Assert.True(await svc.RemoveSpecRowAsync(rid));
        Assert.True(await svc.RemoveSpecGroupAsync(gid));
        Assert.Equal(0, await db.Set<SpecRow>().CountAsync());
    }

    [Fact]
    public async Task Color_AddMoveRemove()
    {
        var db = NewDb(nameof(Color_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "c", Name = "C" });
        var a = await svc.AddColorAsync(vid, new LocalizedText { En = "Red" }, "#ff0000", null);
        var b = await svc.AddColorAsync(vid, new LocalizedText { En = "Blue" }, "#0000ff", null);
        Assert.True(await svc.MoveColorAsync(b, -1));
        Assert.Equal(0, (await db.Set<ColorOption>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveColorAsync(a));
        Assert.Equal(1, await db.Set<ColorOption>().CountAsync());
    }

    [Fact]
    public async Task Trim_AddRemove()
    {
        var db = NewDb(nameof(Trim_AddRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "t", Name = "T" });
        var id = await svc.AddTrimAsync(vid, new Trim { Name = "GT", Price = 100000m });
        Assert.Equal(1, await db.Set<Trim>().CountAsync());
        Assert.True(await svc.RemoveTrimAsync(id));
        Assert.Equal(0, await db.Set<Trim>().CountAsync());
    }
```

- [ ] **Step 4: Run tests**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter AdminVehicleSectionsTests --nologo`
Expected: all PASS (Feature tests from Task 5 + these 3).

- [ ] **Step 5: Commit**

```bash
git add GAC.Core/Services/IAdminVehicleService.cs GAC.Core/Content/IOrderable.cs GAC.Core/Content/SpecGroup.cs GAC.Core/Content/ColorOption.cs GAC.Core/Content/Trim.cs GAC.Core/Content/FeatureSection.cs GAC.Infrastructure/Services/AdminVehicleService.cs GAC.Tests/AdminVehicleSectionsTests.cs
git commit -m "feat(admin): spec/colour/trim CRUD on AdminVehicleService"
```

---

## Task 8: Specs/Colours/Trims admin UI + Advanced HTML escape hatch

**Files:**
- Modify: `GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Vehicles/_SpecGroups.cshtml` `_Colors.cshtml` `_Trims.cshtml`
- Modify: `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`

- [ ] **Step 1: Add controller actions**

Append to `VehiclesController.cs`:
```csharp
    [HttpPost] public async Task<IActionResult> AddSpecGroup(int vehicleId, string? titleEn, string? titleAr)
    { await _svc.AddSpecGroupAsync(vehicleId, new() { En = titleEn, Ar = titleAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSpecGroup(int groupId, int vehicleId)
    { await _svc.RemoveSpecGroupAsync(groupId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSpecGroup(int groupId, int vehicleId, int direction)
    { await _svc.MoveSpecGroupAsync(groupId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> AddSpecRow(int groupId, int vehicleId, string? labelEn, string? labelAr, string? valueEn, string? valueAr)
    { await _svc.AddSpecRowAsync(groupId, new() { En = labelEn, Ar = labelAr }, new() { En = valueEn, Ar = valueAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSpecRow(int rowId, int vehicleId)
    { await _svc.RemoveSpecRowAsync(rowId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddColor(int vehicleId, string? nameEn, string? nameAr, string hex, string? imagePath)
    { await _svc.AddColorAsync(vehicleId, new() { En = nameEn, Ar = nameAr }, hex, imagePath); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveColor(int colorId, int vehicleId)
    { await _svc.RemoveColorAsync(colorId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveColor(int colorId, int vehicleId, int direction)
    { await _svc.MoveColorAsync(colorId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> AddTrim(int vehicleId, string? nameEn, string? nameAr, decimal? price, string? highlightsEn, string? highlightsAr, string? specPdf)
    {
        await _svc.AddTrimAsync(vehicleId, new Trim
        {
            Name = new() { En = nameEn, Ar = nameAr },
            Price = price,
            Highlights = new() { En = highlightsEn, Ar = highlightsAr },
            SpecPdf = specPdf
        });
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }
    [HttpPost] public async Task<IActionResult> RemoveTrim(int trimId, int vehicleId)
    { await _svc.RemoveTrimAsync(trimId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveTrim(int trimId, int vehicleId, int direction)
    { await _svc.MoveTrimAsync(trimId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 2: Spec groups partial**

`GAC.Web/Areas/Admin/Views/Vehicles/_SpecGroups.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
<h2>Specifications</h2>
@foreach (var g in Model.SpecGroups.OrderBy(x => x.SortOrder))
{
    <div class="adm-card">
      <h3>@g.Title.En
        <form asp-action="RemoveSpecGroup" method="post" style="display:inline" onsubmit="return confirm('Remove group and its rows?')">
          <input type="hidden" name="groupId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove group</button>
        </form>
      </h3>
      <table class="adm-table">
        @foreach (var r in g.Rows.OrderBy(x => x.SortOrder))
        {
            <tr><td>@r.Label.En</td><td>@r.Value.En</td>
              <td>
                <form asp-action="RemoveSpecRow" method="post" style="display:inline">
                  <input type="hidden" name="rowId" value="@r.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
                  <button class="adm-btn adm-btn--danger">Remove</button>
                </form>
              </td></tr>
        }
      </table>
      <form asp-action="AddSpecRow" method="post" class="adm-inline">
        <input type="hidden" name="groupId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
        <input name="labelEn" placeholder="Label (EN)" /><input name="labelAr" placeholder="Label (AR)" dir="rtl" />
        <input name="valueEn" placeholder="Value (EN)" /><input name="valueAr" placeholder="Value (AR)" dir="rtl" />
        <button class="adm-btn">Add row</button>
      </form>
    </div>
}
<form asp-action="AddSpecGroup" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="titleEn" placeholder="Group title (EN)" /><input name="titleAr" placeholder="Group title (AR)" dir="rtl" />
  <button class="adm-btn">Add spec group</button>
</form>
```

- [ ] **Step 3: Colours partial**

`GAC.Web/Areas/Admin/Views/Vehicles/_Colors.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
<h2>Colours</h2>
<div class="adm-picker-grid">
  @foreach (var c in Model.Colors.OrderBy(x => x.SortOrder))
  {
      <div class="adm-picker-item">
        @if (!string.IsNullOrWhiteSpace(c.ImagePath))
        { <img src="@c.ImagePath" class="adm-picker-thumb" alt="" /> }
        else
        { <span style="display:inline-block;width:40px;height:40px;border-radius:50%;background:@c.Hex"></span> }
        <div>@c.Name.En</div>
        <form asp-action="MoveColor" method="post" style="display:inline">
          <input type="hidden" name="colorId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveColor" method="post" style="display:inline">
          <input type="hidden" name="colorId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveColor" method="post" style="display:inline" onsubmit="return confirm('Remove colour?')">
          <input type="hidden" name="colorId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove</button>
        </form>
      </div>
  }
</div>
<form asp-action="AddColor" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="nameEn" placeholder="Name (EN)" /><input name="nameAr" placeholder="Name (AR)" dir="rtl" />
  <input type="color" name="hex" value="#000000" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="imagePath" placeholder="Image (optional)" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <button class="adm-btn">Add colour</button>
</form>
```

- [ ] **Step 4: Trims partial**

`GAC.Web/Areas/Admin/Views/Vehicles/_Trims.cshtml`:
```cshtml
@model GAC.Core.Content.Vehicle
<h2>Trims</h2>
<table class="adm-table">
  <thead><tr><th>Order</th><th>Name</th><th>Price</th><th></th></tr></thead>
  <tbody>
    @foreach (var t in Model.Trims.OrderBy(x => x.SortOrder))
    {
        <tr>
          <td>
            <form asp-action="MoveTrim" method="post" style="display:inline">
              <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
              <button class="adm-btn">&uarr;</button>
            </form>
            <form asp-action="MoveTrim" method="post" style="display:inline">
              <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
              <button class="adm-btn">&darr;</button>
            </form>
          </td>
          <td>@t.Name.En</td>
          <td>@(t.Price?.ToString("N0"))</td>
          <td>
            <form asp-action="RemoveTrim" method="post" style="display:inline" onsubmit="return confirm('Remove trim?')">
              <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
              <button class="adm-btn adm-btn--danger">Remove</button>
            </form>
          </td>
        </tr>
    }
  </tbody>
</table>
<form asp-action="AddTrim" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="nameEn" placeholder="Name (EN)" /><input name="nameAr" placeholder="Name (AR)" dir="rtl" />
  <input type="number" step="any" name="price" placeholder="Price" />
  <input name="highlightsEn" placeholder="Highlights (EN)" /><input name="highlightsAr" placeholder="Highlights (AR)" dir="rtl" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="specPdf" placeholder="Spec PDF (optional)" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <button class="adm-btn">Add trim</button>
</form>
```

- [ ] **Step 5: Wire partials + move BodyHtml into Advanced `<details>`**

In `Edit.cshtml`, replace the BodyHtml `_LocalizedField` line (the `Code = true` one) with a collapsed block:
```cshtml
    <details class="adm-field">
      <summary>Advanced — raw HTML body (legacy / escape hatch)</summary>
      <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Page body (HTML)", NameEn = "BodyHtml.En", NameAr = "BodyHtml.Ar", ValueEn = Model.BodyHtml.En, ValueAr = Model.BodyHtml.Ar, Code = true }' />
    </details>
```
And extend the `@if (!isNew)` block to include the three new partials:
```cshtml
@if (!isNew)
{
    <partial name="_Images" model="Model" />
    <partial name="_Features" model="Model" />
    <partial name="_SpecGroups" model="Model" />
    <partial name="_Colors" model="Model" />
    <partial name="_Trims" model="Model" />
    <partial name="_PickerModal" />
}
```

- [ ] **Step 6: Build**

Run: `dotnet build GAC.sln -c Debug --nologo`
Expected: Build succeeded.

> Note: multiple `data-media-pick` buttons now exist on the Edit page (colours, trims) plus the image add form. The picker JS (`admin.js`) binds each button to the `[data-media-input]` within its **own parent element**, so each must keep button + input as siblings under one wrapper (the partials above wrap them in a `<span>`). One shared `_PickerModal` per page is correct.

- [ ] **Step 7: Run full test suite**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --nologo`
Expected: all green.

- [ ] **Step 8: Commit**

```bash
git add GAC.Web/Areas/Admin/Controllers/VehiclesController.cs GAC.Web/Areas/Admin/Views/Vehicles
git commit -m "feat(admin): spec/colour/trim editors + collapse raw HTML to Advanced"
```

---

## Deployment notes (after all tasks pass)

1. **Apply the migration to prod** with a targeted, hand-scoped guarded SQL script (per the known `__EFMigrationsHistory` gaps — do NOT run `dotnet ef database update` or a full `--idempotent` script). The change is a single `ALTER TABLE FeatureSections ADD Layout int NOT NULL DEFAULT 0;` plus the matching `__EFMigrationsHistory` insert row for `AddFeatureLayout`. Generate the SQL with `dotnet ef migrations script <PreviousMigration> AddFeatureLayout` and apply only the new statements.
2. **Re-deploy the Web app** (publish profile). Confirm `Ganss.Xss` ships in the publish output and the Trix files exist under `wwwroot/assets/vendor/trix/`.
3. **No content migration**: existing model pages keep rendering their `BodyHtml` until an admin adds sections. Migrate each model in the admin at will.
4. Smoke-test one model EN + AR after deploy.

---

## Self-Review

**Spec coverage:** ✅ Hybrid model (typed slots + feature blocks) — Tasks 4/6/8; HTML fallback — Task 4; WYSIWYG (Trix) — Task 6; sanitisation — Task 2/5; preset layouts — Task 1/3/6; vehicles only — whole plan; migration via guarded script — Deployment notes.

**Placeholder scan:** Removed the temporary guard line in Task 6 Step 2 (final action body restated). No TBD/TODO.

**Type consistency:** `HasStructuredContent`, `FeatureLayoutCss`, `ShowsImage` (Task 3) match their use in Task 4. `IOrderable` (Task 7) is implemented by `SpecGroup`/`ColorOption`/`Trim`/`FeatureSection` and used by `SwapOrderAsync`. Service method names in the interface (Tasks 5/7) match the controller calls (Tasks 6/8). `AdminVehicleService` constructor change (Task 5) is reflected in test helpers (`NewSvc`).
