# Admin-managed Specifications PDF & Action-Dock — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let admins (a) upload a per-vehicle Specifications PDF that drives the public "Specifications" button, and (b) fully manage the floating action-dock (add/remove/reorder items, edit label/URL/icon/visibility), including a working per-vehicle "Download Brochure" link.

**Architecture:** Extend the existing image-only media pipeline to accept PDFs (shared by spec + brochure). Add a `Vehicle.SpecPdf` field rendered by `Detail.cshtml`. Replace the hardcoded action-dock markup with a DB-driven `DockItem` list managed through an admin CRUD section that mirrors the existing Menu admin. One additive EF migration; data changes seeded for fresh DBs and applied to prod via a guarded data-only SQL script.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQL Server), bilingual `LocalizedText` owned types, xUnit + EF InMemory + `WebApplicationFactory` integration tests.

## Global Constraints

- **.NET 9 / EF Core 9.0.*** — do not let package versions float to 10.x.
- **Bilingual everywhere:** every user-facing string field is `LocalizedText` (`{Field}_En`/`{Field}_Ar`), edited via the `_LocalizedField` partial. Seed both EN and AR.
- **Shared DB:** the GAC DB at `83.229.86.221,1433` is used by both local dev and the deployed app. The schema migration here is **additive and safe** (new nullable column + new table; the live app ignores them until redeployed). Integration tests (`DevWebApplicationFactory`, Development env) run against this DB, so **the migration MUST be applied to it before running integration tests.**
- **Apps don't auto-migrate** (`Program.cs` runs seeders only). Prod schema is applied via a script before deploy; data via a guarded data-only script.
- **Admin authorization:** all admin controllers use `[Area("Admin")] [Authorize(Policy = AdminPolicies.ContentEditor)] [AutoValidateAntiforgeryToken]`.
- **No secrets in committed files.**
- Spec: `Solution/docs/superpowers/specs/2026-06-21-admin-spec-pdf-and-action-dock-design.md`.

## File Structure

**Create:**
- `GAC.Core/Content/DockItem.cs` — DockItem entity + `DockLinkType` enum.
- `GAC.Core/Services/IAdminDockService.cs` — admin dock CRUD interface.
- `GAC.Infrastructure/Services/AdminDockService.cs` — admin dock CRUD impl.
- `GAC.Web/Infrastructure/DockIcons.cs` — icon-key → inline-SVG helper.
- `GAC.Web/Areas/Admin/Controllers/DockController.cs` — admin dock CRUD controller.
- `GAC.Web/Areas/Admin/Views/Dock/Index.cshtml`, `Edit.cshtml` — admin dock views.
- `GAC.Infrastructure/Migrations/<timestamp>_AddSpecPdfAndDock.cs` — EF migration (generated).
- `GAC.Tests/MediaServiceTests.cs`, `GAC.Tests/Admin/AdminDockServiceTests.cs`, `GAC.Tests/DockIconsTests.cs`, `GAC.Tests/ActionDockTests.cs`, `GAC.Tests/VehicleSpecPdfTests.cs`.
- `Solution/docs/migrations/2026-06-21-spec-and-dock-prod.sql` — guarded data-only prod script.

**Modify:**
- `GAC.Core/Content/Vehicle.cs` — add `SpecPdf`.
- `GAC.Core/Services/MediaOptions.cs` — add `PdfMaxBytes`.
- `GAC.Infrastructure/Services/MediaService.cs` — allow PDFs.
- `GAC.Infrastructure/Services/AdminVehicleService.cs` — persist `SpecPdf`.
- `GAC.Infrastructure/Data/ApplicationDbContext.cs` — `DbSet<DockItem>`.
- `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` — `DockItemConfig`.
- `GAC.Infrastructure/Data/ContentSeeder.cs` — seed 6 dock items.
- `GAC.Core/Services/ISiteService.cs` + `GAC.Infrastructure/Services/SiteService.cs` — `GetDockItemsAsync`.
- `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml` — SpecPdf + Brochure media pickers.
- `GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml` — accept PDFs.
- `GAC.Web/wwwroot/assets/js/admin.js` — PDF tile in picker grid.
- `GAC.Web/Views/Vehicles/Detail.cshtml` — render Specifications button.
- `GAC.Web/wwwroot/assets/css/styles.css` — `.mp-spec-cta` rule.
- `GAC.Web/ViewComponents/FooterViewComponent.cs` — FooterViewModel + dock items + brochure context.
- `GAC.Web/Views/Shared/Components/Footer/Default.cshtml` — DB-driven dock loop.
- `GAC.Web/Controllers/PageController.cs` — set current-vehicle brochure for the footer.
- `GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml` — "Dock" link.
- `GAC.Web/Program.cs` — register `IAdminDockService`.
- `GAC.Infrastructure/SeedContent/vehicles/gs4.html`, `hyptec-ht.html` — remove hardcoded Specifications anchor.
- `Solution/docs/migrations/2026-06-21-content-updates.sql` — change spec-button section to remove the body anchor.

---

## Task 1: Allow PDF uploads in MediaService

**Files:**
- Modify: `GAC.Core/Services/MediaOptions.cs`
- Modify: `GAC.Infrastructure/Services/MediaService.cs:11-31`
- Test: `GAC.Tests/MediaServiceTests.cs`

**Interfaces:**
- Consumes: existing `MediaService(ApplicationDbContext, IOptions<MediaOptions>)`, `MediaUploadResult(bool Ok, string? Path, string? Error)`.
- Produces: `MediaService.UploadAsync` now accepts `application/pdf` + `.pdf` (≤ `MediaOptions.PdfMaxBytes`); images unchanged.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/MediaServiceTests.cs`:
```csharp
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAC.Tests;

public class MediaServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private static MediaService NewSvc(ApplicationDbContext db)
    {
        var root = Path.Combine(Path.GetTempPath(), "gactest-" + Guid.NewGuid().ToString("N"));
        var opt = Options.Create(new MediaOptions { Root = root, PublicPrefix = "/uploads" });
        return new MediaService(db, opt);
    }

    [Fact]
    public async Task Accepts_Pdf_Upload()
    {
        var db = NewDb(nameof(Accepts_Pdf_Upload));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "spec.pdf", "application/pdf", 3);
        Assert.True(res.Ok);
        Assert.NotNull(res.Path);
        Assert.EndsWith(".pdf", res.Path);
    }

    [Fact]
    public async Task Still_Accepts_Png()
    {
        var db = NewDb(nameof(Still_Accepts_Png));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "pic.png", "image/png", 3);
        Assert.True(res.Ok);
    }

    [Fact]
    public async Task Rejects_Disallowed_Type()
    {
        var db = NewDb(nameof(Rejects_Disallowed_Type));
        var svc = NewSvc(db);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var res = await svc.UploadAsync(ms, "evil.exe", "application/octet-stream", 3);
        Assert.False(res.Ok);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~MediaServiceTests`
Expected: `Accepts_Pdf_Upload` FAILS (PDF currently rejected); the other two pass.

- [ ] **Step 3: Add `PdfMaxBytes` to MediaOptions**

In `GAC.Core/Services/MediaOptions.cs`, add after `MaxBytes`:
```csharp
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
    // PDFs (brochures / spec sheets) are allowed a larger ceiling than images.
    public long PdfMaxBytes { get; set; } = 20 * 1024 * 1024;
```

- [ ] **Step 4: Allow PDFs in MediaService**

In `GAC.Infrastructure/Services/MediaService.cs`, replace the allowlists (lines 11-14) with:
```csharp
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf" };
    private static readonly HashSet<string> AllowedCt =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif", "application/pdf" };
```
Then replace the validation block (lines 27-31) with:
```csharp
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext) || !AllowedCt.Contains(contentType))
            return new MediaUploadResult(false, null, "Only image files (jpg, png, webp, gif) and PDF files are allowed.");
        var maxBytes = string.Equals(ext, ".pdf", StringComparison.OrdinalIgnoreCase) ? _opt.PdfMaxBytes : _opt.MaxBytes;
        if (length <= 0 || length > maxBytes)
            return new MediaUploadResult(false, null, $"File must be between 1 byte and {maxBytes / 1024} KB.");
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~MediaServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add GAC.Core/Services/MediaOptions.cs GAC.Infrastructure/Services/MediaService.cs GAC.Tests/MediaServiceTests.cs
git commit -m "feat(media): allow PDF uploads alongside images"
```

---

## Task 2: Schema — Vehicle.SpecPdf + DockItem entity + migration

This task adds all schema changes at once and applies the migration to the shared GAC DB so later integration tests work.

**Files:**
- Modify: `GAC.Core/Content/Vehicle.cs:17`
- Create: `GAC.Core/Content/DockItem.cs`
- Modify: `GAC.Infrastructure/Data/ApplicationDbContext.cs:33`
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (append config)
- Create (generated): `GAC.Infrastructure/Migrations/<timestamp>_AddSpecPdfAndDock.cs`

**Interfaces:**
- Produces: `Vehicle.SpecPdf` (`string?`); `DockItem { Id, Label, ShortLabel (LocalizedText), Url (string?), Icon (string), LinkType (DockLinkType), IsVisible (bool), SortOrder (int) }`; `enum DockLinkType { Url=0, WhatsApp=1, Phone=2, VehicleBrochure=3 }`; `ApplicationDbContext.DockItems`.

- [ ] **Step 1: Add `SpecPdf` to Vehicle**

In `GAC.Core/Content/Vehicle.cs`, change line 17:
```csharp
    public string? BrochurePdf { get; set; }
    public string? SpecPdf { get; set; }
```

- [ ] **Step 2: Create the DockItem entity**

Create `GAC.Core/Content/DockItem.cs`:
```csharp
namespace GAC.Core.Content;

public enum DockLinkType { Url = 0, WhatsApp = 1, Phone = 2, VehicleBrochure = 3 }

/// <summary>A single item in the floating action-dock. Order by SortOrder; render only when IsVisible.</summary>
public class DockItem
{
    public int Id { get; set; }
    public LocalizedText Label { get; set; } = new();       // full text (desktop)
    public LocalizedText ShortLabel { get; set; } = new();  // compact text (mobile)
    public string? Url { get; set; }                        // used when LinkType == Url
    public string Icon { get; set; } = "";                  // icon key (see DockIcons)
    public DockLinkType LinkType { get; set; } = DockLinkType.Url;
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
}
```

- [ ] **Step 3: Register the DbSet**

In `GAC.Infrastructure/Data/ApplicationDbContext.cs`, add after line 33 (`MediaAssets`):
```csharp
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<DockItem> DockItems => Set<DockItem>();
```

- [ ] **Step 4: Add the EF configuration**

In `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`, append a new config class at the end of the file (before the final namespace close if file-scoped; this file uses a namespace statement so just append after `MediaAssetConfig`):
```csharp
public class DockItemConfig : IEntityTypeConfiguration<DockItem>
{
    public void Configure(EntityTypeBuilder<DockItem> b)
    {
        b.Property(d => d.Icon).HasMaxLength(50);
        b.Property(d => d.Url).HasMaxLength(300);
        b.OwnsLocalized(d => d.Label);
        b.OwnsLocalized(d => d.ShortLabel);
    }
}
```

- [ ] **Step 5: Build**

Run: `dotnet build GAC.Web/GAC.Web.csproj -clp:ErrorsOnly`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Generate the migration**

Run: `dotnet ef migrations add AddSpecPdfAndDock --project GAC.Infrastructure --startup-project GAC.Web`
Expected: a new `<timestamp>_AddSpecPdfAndDock.cs` under `GAC.Infrastructure/Migrations`, adding the `Vehicles.SpecPdf` column and the `DockItems` table (`Label_En/Ar`, `ShortLabel_En/Ar`, `Url`, `Icon`, `LinkType` int, `IsVisible` bit, `SortOrder` int).

- [ ] **Step 7: Apply the migration to the shared GAC DB**

Run: `dotnet ef database update --project GAC.Infrastructure --startup-project GAC.Web`
Expected: "Applying migration '<timestamp>_AddSpecPdfAndDock'. Done." (Additive change; safe — the live app ignores the new column/table until redeployed.)

- [ ] **Step 8: Verify existing tests still pass against the migrated DB**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj`
Expected: all currently-passing tests still pass (the new column/table do not break existing queries or seeding).

- [ ] **Step 9: Commit**

```bash
git add GAC.Core/Content/Vehicle.cs GAC.Core/Content/DockItem.cs GAC.Infrastructure/Data/ApplicationDbContext.cs GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs GAC.Infrastructure/Migrations
git commit -m "feat(schema): add Vehicle.SpecPdf and DockItem table (AddSpecPdfAndDock migration)"
```

---

## Task 3: Persist SpecPdf + admin media-picker inputs for SpecPdf/Brochure

**Files:**
- Modify: `GAC.Infrastructure/Services/AdminVehicleService.cs:46`
- Modify: `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml:35-38`
- Test: `GAC.Tests/VehicleSpecPdfTests.cs`

**Interfaces:**
- Consumes: `AdminVehicleService.UpdateAsync(Vehicle)`, `Vehicle.SpecPdf` (Task 2).
- Produces: `UpdateAsync` round-trips `SpecPdf`; admin vehicle form exposes SpecPdf + Brochure with the `data-media-input`/`data-media-pick` picker.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/VehicleSpecPdfTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class VehicleSpecPdfTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private sealed class NoopSanitizer : GAC.Core.Services.IHtmlSanitizerService
    {
        public string Sanitize(string? html) => html ?? "";
    }

    [Fact]
    public async Task UpdateAsync_Persists_SpecPdf()
    {
        var db = NewDb(nameof(UpdateAsync_Persists_SpecPdf));
        db.Vehicles.Add(new Vehicle { Id = 1, Slug = "gs4", Name = "GS4 MAX" });
        await db.SaveChangesAsync();

        var svc = new AdminVehicleService(db, new NoopSanitizer());
        var v = await db.Vehicles.FirstAsync(x => x.Id == 1);
        v.SpecPdf = "/uploads/gs4-spec.pdf";
        Assert.True(await svc.UpdateAsync(v));

        Assert.Equal("/uploads/gs4-spec.pdf", (await db.Vehicles.FindAsync(1))!.SpecPdf);
    }
}
```
> `NoopSanitizer` matches `IHtmlSanitizerService.Sanitize(string?) -> string` (the sanitizer injected into `AdminVehicleService`).

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~VehicleSpecPdfTests`
Expected: FAIL — `UpdateAsync` does not copy `SpecPdf` yet.

- [ ] **Step 3: Persist SpecPdf in the service**

In `GAC.Infrastructure/Services/AdminVehicleService.cs`, add after line 46 (`existing.BrochurePdf = vehicle.BrochurePdf;`):
```csharp
        existing.BrochurePdf = vehicle.BrochurePdf;
        existing.SpecPdf = vehicle.SpecPdf;
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~VehicleSpecPdfTests`
Expected: PASS.

- [ ] **Step 5: Add media-picker inputs to the admin vehicle form**

In `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`, replace the Brochure block (lines 35-38) with:
```html
    <div class="adm-field">
        <label asp-for="SpecPdf">Specifications PDF</label>
        <span style="display:inline-flex;gap:.4rem;align-items:center">
            <input asp-for="SpecPdf" data-media-input />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
    </div>

    <div class="adm-field">
        <label asp-for="BrochurePdf">Brochure PDF</label>
        <span style="display:inline-flex;gap:.4rem;align-items:center">
            <input asp-for="BrochurePdf" data-media-input />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
    </div>
```
> The picker modal (`_PickerModal`) is already included at the bottom of this view for existing vehicles, and `data-media-pick` finds its sibling `data-media-input` inside the `<span>` (matches the `_Trims.cshtml` pattern). The "Choose…" button is inert when creating a brand-new vehicle (modal not yet on page) — save first, then attach PDFs; acceptable and consistent with images/trims.

- [ ] **Step 6: Build**

Run: `dotnet build GAC.Web/GAC.Web.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 7: Commit**

```bash
git add GAC.Infrastructure/Services/AdminVehicleService.cs GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml GAC.Tests/VehicleSpecPdfTests.cs
git commit -m "feat(admin): manage per-vehicle SpecPdf + Brochure via media picker"
```

---

## Task 4: Media picker accepts & displays PDFs

No automated test (browser JS + Razor markup); verified by build + manual check.

**Files:**
- Modify: `GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml:5,10`
- Modify: `GAC.Web/wwwroot/assets/js/admin.js:18-28`

- [ ] **Step 1: Let the picker accept PDFs**

In `GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml`, change line 5 title and line 10 file input:
```html
      <strong>Choose image or PDF</strong>
```
```html
      <input type="file" name="file" accept="image/*,.pdf" />
```

- [ ] **Step 2: Render a PDF tile in the picker grid**

In `GAC.Web/wwwroot/assets/js/admin.js`, replace the `items.forEach` block (lines 19-27) with:
```javascript
        items.forEach(function (it) {
          var isPdf = /\.pdf$/i.test(it.path);
          var el;
          if (isPdf) {
            el = document.createElement("div");
            el.className = "adm-picker-thumb adm-picker-thumb--pdf";
            el.title = it.path;
            el.textContent = "PDF: " + it.path.split("/").pop();
          } else {
            el = document.createElement("img");
            el.src = it.path; el.title = it.path; el.className = "adm-picker-thumb";
          }
          el.addEventListener("click", function () {
            if (activeInput) activeInput.value = it.path;
            close();
          });
          grid.appendChild(el);
        });
```

- [ ] **Step 3: Build**

Run: `dotnet build GAC.Web/GAC.Web.csproj -clp:ErrorsOnly`
Expected: Build succeeded.

- [ ] **Step 4: Manual verification (note for executor)**

On `Admin/Vehicles/Edit/{id}`, click "Choose…" next to Specifications PDF, upload a `.pdf`, confirm it appears as a "PDF: …" tile and clicking it fills the input. (Document the result; no automated assertion.)

- [ ] **Step 5: Commit**

```bash
git add GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml GAC.Web/wwwroot/assets/js/admin.js
git commit -m "feat(admin): media picker accepts and lists PDFs"
```

---

## Task 5: Render the Specifications button + remove hardcoded anchors

**Files:**
- Modify: `GAC.Web/Views/Vehicles/Detail.cshtml`
- Modify: `GAC.Web/wwwroot/assets/css/styles.css` (append)
- Modify: `GAC.Infrastructure/SeedContent/vehicles/gs4.html`, `hyptec-ht.html`
- Modify: `Solution/docs/migrations/2026-06-21-content-updates.sql`
- Test: `GAC.Tests/VehicleSpecPdfTests.cs` (add an integration test)

**Interfaces:**
- Consumes: `Vehicle.SpecPdf` (Task 2), `VehicleContent.HasStructuredContent` (existing).
- Produces: public detail page shows one Specifications button when `SpecPdf` is set, for both render modes.

- [ ] **Step 1: Write the failing integration test**

Append to `GAC.Tests/VehicleSpecPdfTests.cs` a new class (uses the real dev DB via the factory, so it needs a vehicle that has a SpecPdf — we assert the markup is conditional on the field, by checking a vehicle WITHOUT a spec pdf does not show the button, and the CSS hook is wired):
```csharp
public class VehicleSpecPdfRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehicleSpecPdfRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Gs4_Page_Has_No_Hardcoded_Pdf_Link()
    {
        var html = await (await _factory.CreateClient().GetAsync("/gs4")).Content.ReadAsStringAsync();
        // The legacy hardcoded "/pdfs/gs4-specifications.pdf" anchor must be gone; the button is now field-driven.
        Assert.DoesNotContain("/pdfs/gs4-specifications.pdf", html);
    }
}
```
> `DevWebApplicationFactory` is defined in `GAC.Tests/HomePageSmokeTests.cs`.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~VehicleSpecPdfRenderTests`
Expected: FAIL — the seed body still contains `/pdfs/gs4-specifications.pdf`.
> If the dev DB's gs4 body was seeded earlier, this asserts on the seed source; the body backfill only fills blanks, so also ensure the prod/dev DB body is corrected via Step 6 SQL. For the test to pass on a freshly-seeded DB, the seed edit (Step 5) is what matters.

- [ ] **Step 3: Render the Specifications button in Detail.cshtml**

Replace the entire contents of `GAC.Web/Views/Vehicles/Detail.cshtml` with:
```cshtml
@using GAC.Web.Infrastructure
@model GAC.Core.Content.Vehicle
@{ Layout = "_Layout"; }

@{
    var specBtn = !string.IsNullOrWhiteSpace(Model.SpecPdf);
}

@if (VehicleContent.HasStructuredContent(Model))
{
    <partial name="_VehicleHero" model="Model" />
    @if (specBtn)
    {
        <section class="mp-spec-cta"><div class="container"><a class="btn btn--primary" href="@Model.SpecPdf" target="_blank" rel="noopener">@L["Specifications"]</a></div></section>
    }
    <partial name="_VehicleFeatures" model="Model" />
    <partial name="_VehicleSpecs" model="Model" />
    <partial name="_VehicleColors" model="Model" />
    <partial name="_VehicleTrims" model="Model" />
}
else
{
    @if (specBtn)
    {
        <section class="mp-spec-cta"><div class="container"><a class="btn btn--primary" href="@Model.SpecPdf" target="_blank" rel="noopener">@L["Specifications"]</a></div></section>
    }
    @Html.Raw(Model.BodyHtml.Localize())
}
```

- [ ] **Step 4: Add the CSS hook**

Append to `GAC.Web/wwwroot/assets/css/styles.css`:
```css
/* Per-vehicle Specifications download CTA (field-driven; Vehicle.SpecPdf) */
.mp-spec-cta { padding: 1.25rem 0; text-align: center; }
.mp-spec-cta .btn { display: inline-block; }
```

- [ ] **Step 5: Remove the hardcoded Specifications anchor from the seed bodies**

In `GAC.Infrastructure/SeedContent/vehicles/gs4.html`, delete this line (the second trim CTA):
```html
                <a class="btn btn--trim" href="/pdfs/gs4-specifications.pdf" target="_blank" rel="noopener">Specifications</a>
```
In `GAC.Infrastructure/SeedContent/vehicles/hyptec-ht.html`, delete this line:
```html
                <a class="btn btn--trim" href="/pdfs/hyptec-ht-specifications.pdf" target="_blank" rel="noopener">Specifications</a>
```
(Each trims card then has only the "Book A test drive" button.)

- [ ] **Step 6: Update the prod content SQL to remove the body anchor instead of inserting it**

In `Solution/docs/migrations/2026-06-21-content-updates.sql`, replace the two statements under section "3. ... Request a Quote ... -> Specifications" so they REMOVE the Request-a-Quote anchor (the prod DB has not run this script yet, and the Specifications button is now field-driven):
```sql
------------------------------------------------------------------
-- 3. gs4 / hyptec-ht: remove the in-body trim CTA anchor
--    (Specifications is now a field-driven button from Vehicles.SpecPdf)
------------------------------------------------------------------
UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'<a class="btn btn--trim" href="#enquiry">Request a Quote</a>', N'')
 WHERE [Slug] IN ('gs4','hyptec-ht')
   AND [BodyHtml_En] LIKE N'%<a class="btn btn--trim" href="#enquiry">Request a Quote</a>%';
```
> Leave section 4 (the gs4 heading removal) unchanged. Update the file's header comment for section 3 accordingly.

- [ ] **Step 7: Run the test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~VehicleSpecPdf`
Expected: PASS (round-trip + render tests).

- [ ] **Step 8: Commit**

```bash
git add GAC.Web/Views/Vehicles/Detail.cshtml GAC.Web/wwwroot/assets/css/styles.css GAC.Infrastructure/SeedContent/vehicles/gs4.html GAC.Infrastructure/SeedContent/vehicles/hyptec-ht.html "Solution/docs/migrations/2026-06-21-content-updates.sql" GAC.Tests/VehicleSpecPdfTests.cs
git commit -m "feat(vehicles): field-driven Specifications button; drop hardcoded PDF anchors"
```

---

## Task 6: AdminDockService (CRUD + reorder)

**Files:**
- Create: `GAC.Core/Services/IAdminDockService.cs`
- Create: `GAC.Infrastructure/Services/AdminDockService.cs`
- Test: `GAC.Tests/Admin/AdminDockServiceTests.cs`

**Interfaces:**
- Consumes: `DockItem` (Task 2), `ApplicationDbContext.DockItems`.
- Produces: `IAdminDockService { ListAllAsync, GetAsync(id), CreateAsync(item)->int, UpdateAsync(item)->bool, DeleteAsync(id)->bool, MoveAsync(id,direction)->bool }`.

- [ ] **Step 1: Write the failing tests**

Create `GAC.Tests/Admin/AdminDockServiceTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminDockServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Update_RoundTrips()
    {
        var db = NewDb(nameof(Create_Update_RoundTrips));
        var svc = new AdminDockService(db);
        var id = await svc.CreateAsync(new DockItem { Label = "WhatsApp", Icon = "whatsapp", LinkType = DockLinkType.WhatsApp, SortOrder = 0 });
        var item = await svc.GetAsync(id);
        item!.Url = "/x"; item.LinkType = DockLinkType.Url;
        Assert.True(await svc.UpdateAsync(item));
        var saved = await db.DockItems.FindAsync(id);
        Assert.Equal("/x", saved!.Url);
        Assert.Equal(DockLinkType.Url, saved.LinkType);
    }

    [Fact]
    public async Task Delete_Removes_Item()
    {
        var db = NewDb(nameof(Delete_Removes_Item));
        var svc = new AdminDockService(db);
        var id = await svc.CreateAsync(new DockItem { Label = "A", SortOrder = 0 });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Equal(0, await db.DockItems.CountAsync());
    }

    [Fact]
    public async Task Move_Swaps_SortOrder()
    {
        var db = NewDb(nameof(Move_Swaps_SortOrder));
        var svc = new AdminDockService(db);
        var a = await svc.CreateAsync(new DockItem { Label = "A", SortOrder = 0 });
        var b = await svc.CreateAsync(new DockItem { Label = "B", SortOrder = 1 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(0, (await db.DockItems.FindAsync(b))!.SortOrder);
        Assert.Equal(1, (await db.DockItems.FindAsync(a))!.SortOrder);
    }
}
```

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~AdminDockServiceTests`
Expected: FAIL — `IAdminDockService`/`AdminDockService` do not exist.

- [ ] **Step 3: Create the interface**

Create `GAC.Core/Services/IAdminDockService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminDockService
{
    Task<IReadOnlyList<DockItem>> ListAllAsync(CancellationToken ct = default);   // ordered by SortOrder
    Task<DockItem?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(DockItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(DockItem item, CancellationToken ct = default);        // Label, ShortLabel, Url, Icon, LinkType, IsVisible, SortOrder
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default);  // swap with neighbour
}
```

- [ ] **Step 4: Create the implementation**

Create `GAC.Infrastructure/Services/AdminDockService.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminDockService : IAdminDockService
{
    private readonly ApplicationDbContext _db;
    public AdminDockService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<DockItem>> ListAllAsync(CancellationToken ct = default)
        => await _db.DockItems.OrderBy(d => d.SortOrder).ToListAsync(ct);

    public async Task<DockItem?> GetAsync(int id, CancellationToken ct = default)
        => await _db.DockItems.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<int> CreateAsync(DockItem item, CancellationToken ct = default)
    {
        _db.DockItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(DockItem item, CancellationToken ct = default)
    {
        var e = await _db.DockItems.FirstOrDefaultAsync(d => d.Id == item.Id, ct);
        if (e is null) return false;
        e.Label = item.Label;
        e.ShortLabel = item.ShortLabel;
        e.Url = item.Url;
        e.Icon = item.Icon;
        e.LinkType = item.LinkType;
        e.IsVisible = item.IsVisible;
        e.SortOrder = item.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.DockItems.FindAsync([id], ct);
        if (item is null) return false;
        _db.DockItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var list = await _db.DockItems.OrderBy(d => d.SortOrder).ToListAsync(ct);
        var idx = list.FindIndex(d => d.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= list.Count) return false;
        (list[idx].SortOrder, list[swap].SortOrder) = (list[swap].SortOrder, list[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Run the tests to verify they pass**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~AdminDockServiceTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add GAC.Core/Services/IAdminDockService.cs GAC.Infrastructure/Services/AdminDockService.cs GAC.Tests/Admin/AdminDockServiceTests.cs
git commit -m "feat(admin): AdminDockService CRUD + reorder"
```

---

## Task 7: DockIcons helper

**Files:**
- Create: `GAC.Web/Infrastructure/DockIcons.cs`
- Test: `GAC.Tests/DockIconsTests.cs`

**Interfaces:**
- Produces: `DockIcons.Keys` (IReadOnlyList<string>) and `DockIcons.Render(string? key) -> IHtmlContent` (known key → its SVG; unknown → a default SVG).

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/DockIconsTests.cs`:
```csharp
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Html;
using System.IO;
using System.Text.Encodings.Web;
using Xunit;

namespace GAC.Tests;

public class DockIconsTests
{
    private static string Html(IHtmlContent c)
    {
        using var sw = new StringWriter();
        c.WriteTo(sw, HtmlEncoder.Default);
        return sw.ToString();
    }

    [Fact]
    public void Known_Key_Returns_Svg()
    {
        Assert.Contains("whatsapp", DockIcons.Keys);
        Assert.Contains("<svg", Html(DockIcons.Render("whatsapp")));
    }

    [Fact]
    public void Unknown_Key_Returns_Default_Svg()
    {
        Assert.Contains("<svg", Html(DockIcons.Render("nope")));
        Assert.Contains("<svg", Html(DockIcons.Render(null)));
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~DockIconsTests`
Expected: FAIL — `DockIcons` does not exist.

- [ ] **Step 3: Create the helper**

Create `GAC.Web/Infrastructure/DockIcons.cs` (SVGs copied verbatim from the original `Footer/Default.cshtml`; `phone` uses the header phone glyph):
```csharp
using Microsoft.AspNetCore.Html;

namespace GAC.Web.Infrastructure;

public static class DockIcons
{
    public static readonly IReadOnlyList<string> Keys =
        new[] { "whatsapp", "test-drive", "quote", "brochure", "location", "mail", "phone" };

    private static readonly Dictionary<string, string> Svgs = new()
    {
        ["whatsapp"] = "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M12 2a10 10 0 0 0-8.6 15l-1.4 5 5.1-1.3A10 10 0 1 0 12 2zm0 18a8 8 0 0 1-4.1-1.1l-.3-.2-3 .8.8-2.9-.2-.3A8 8 0 1 1 12 20zm4.4-5.6c-.2-.1-1.4-.7-1.6-.8-.2-.1-.4-.1-.6.1-.2.3-.6.8-.8 1-.1.1-.3.2-.5 0-.7-.3-1.4-.6-2-1.2-.5-.5-.9-1.1-1.2-1.7-.1-.2 0-.4.1-.5l.4-.5c.1-.1.1-.3.2-.4 0-.2 0-.3 0-.4l-.7-1.7c-.2-.5-.4-.4-.6-.4h-.5c-.2 0-.4.1-.6.3-.6.6-.9 1.4-.9 2.2.1 1 .5 1.9 1.1 2.7.9 1.3 2 2.3 3.4 2.9.5.2.9.3 1.3.4.5.1 1 .1 1.4 0 .5-.1 1.4-.6 1.6-1.2.2-.5.2-1 .1-1.1z\"/></svg>",
        ["test-drive"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M3 13l2-5a2 2 0 0 1 1.9-1.3h10.2A2 2 0 0 1 19 8l2 5\"/><path d=\"M3 13h18v4a1 1 0 0 1-1 1h-2a1 1 0 0 1-1-1v-1H7v1a1 1 0 0 1-1 1H4a1 1 0 0 1-1-1z\"/><path d=\"M6.5 15.5h.01M17.5 15.5h.01\"/></svg>",
        ["quote"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M14 3H7a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V8z\"/><path d=\"M14 3v5h5\"/><path d=\"M9 13h6M9 17h4\"/></svg>",
        ["brochure"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 3v12\"/><path d=\"m7 10 5 5 5-5\"/><path d=\"M5 21h14\"/></svg>",
        ["location"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><path d=\"M12 21s-7-5.3-7-11a7 7 0 0 1 14 0c0 5.7-7 11-7 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.5\"/></svg>",
        ["mail"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\" stroke-linejoin=\"round\"><rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"m3.5 6.5 8.5 6 8.5-6\"/></svg>",
        ["phone"] = "<svg viewBox=\"0 0 24 24\" fill=\"currentColor\"><path d=\"M6.6 10.8c1.4 2.8 3.8 5.1 6.6 6.6l2.2-2.2c.3-.3.7-.4 1-.2 1.1.4 2.3.6 3.6.6.6 0 1 .4 1 1V20c0 .6-.4 1-1 1C10.6 21 3 13.4 3 4c0-.6.4-1 1-1h3.5c.6 0 1 .4 1 1 0 1.2.2 2.4.6 3.6.1.4 0 .8-.3 1l-2.2 2.2z\"/></svg>",
    };

    public static IHtmlContent Render(string? key)
        => new HtmlString(key is not null && Svgs.TryGetValue(key, out var svg) ? svg : Svgs["quote"]);
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~DockIconsTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add GAC.Web/Infrastructure/DockIcons.cs GAC.Tests/DockIconsTests.cs
git commit -m "feat(dock): icon-key to inline-SVG helper"
```

---

## Task 8: Public dock read + seed the 6 items

**Files:**
- Modify: `GAC.Core/Services/ISiteService.cs`
- Modify: `GAC.Infrastructure/Services/SiteService.cs`
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs`
- Test: `GAC.Tests/ActionDockTests.cs`

**Interfaces:**
- Consumes: `DockItem`, `ApplicationDbContext.DockItems`, `IAdminDockService`/`AdminDockService` (for the seed-read test).
- Produces: `ISiteService.GetDockItemsAsync() -> IReadOnlyList<DockItem>` (visible, ordered); `ContentSeeder.SeedDockItemsAsync` seeds 6 items on an empty table.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/ActionDockTests.cs`:
```csharp
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ActionDockSeedTests
{
    [Fact]
    public async Task GetDockItemsAsync_Returns_Seeded_Visible_Ordered()
    {
        var sp = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(nameof(GetDockItemsAsync_Returns_Seeded_Visible_Ordered)))
            .BuildServiceProvider();

        // Seed via the real seeder so we exercise SeedDockItemsAsync.
        await ContentSeeder.SeedAsync(sp);

        var site = new SiteService(sp.GetRequiredService<ApplicationDbContext>());
        var items = await site.GetDockItemsAsync();

        Assert.Equal(6, items.Count);
        Assert.True(items.Select(i => i.SortOrder).SequenceEqual(items.OrderBy(i => i.SortOrder).Select(i => i.SortOrder)));
        Assert.All(items, i => Assert.True(i.IsVisible));
    }
}
```
> `ContentSeeder.SeedAsync` resolves `ApplicationDbContext` from the provider. Other seed steps are guarded by `AnyAsync` and run harmlessly on the empty InMemory DB.

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~ActionDockSeedTests`
Expected: FAIL — `GetDockItemsAsync` does not exist / no dock seeding.

- [ ] **Step 3: Add the read method to the interface**

In `GAC.Core/Services/ISiteService.cs`, add:
```csharp
    Task<IReadOnlyList<MenuItem>> GetMenuAsync();
    Task<IReadOnlyList<DockItem>> GetDockItemsAsync();
```

- [ ] **Step 4: Implement it**

In `GAC.Infrastructure/Services/SiteService.cs`, add after `GetMenuAsync`:
```csharp
    public async Task<IReadOnlyList<DockItem>> GetDockItemsAsync()
        => await _db.DockItems
            .AsNoTracking()
            .Where(d => d.IsVisible)
            .OrderBy(d => d.SortOrder)
            .ToListAsync();
```

- [ ] **Step 5: Seed the 6 dock items**

In `GAC.Infrastructure/Data/ContentSeeder.cs`, add a call inside `SeedAsync` after `await SeedMenuAsync(db);`:
```csharp
        await SeedMenuAsync(db);
        await SeedDockItemsAsync(db);
```
Then add this method (place near `SeedMenuAsync`):
```csharp
    // ──────────────────────────────────────────────
    //  Action-dock (6 items — matches the previously-hardcoded footer dock)
    // ──────────────────────────────────────────────
    private static async Task SeedDockItemsAsync(ApplicationDbContext db)
    {
        if (await db.DockItems.AnyAsync()) return;

        db.DockItems.AddRange(
            new DockItem
            {
                SortOrder = 1, Icon = "whatsapp", LinkType = DockLinkType.WhatsApp,
                Label = new() { En = "Chat on WhatsApp", Ar = "تواصل عبر واتساب" },
                ShortLabel = new() { En = "WhatsApp", Ar = "واتساب" }
            },
            new DockItem
            {
                SortOrder = 2, Icon = "test-drive", LinkType = DockLinkType.Url, Url = "/book-a-test-drive",
                Label = new() { En = "Book a Test Drive", Ar = "احجز تجربة قيادة" },
                ShortLabel = new() { En = "Test Drive", Ar = "تجربة قيادة" }
            },
            new DockItem
            {
                SortOrder = 3, Icon = "quote", LinkType = DockLinkType.Url, Url = "/request-a-quote",
                Label = new() { En = "Get Online Quote", Ar = "اطلب عرض سعر" },
                ShortLabel = new() { En = "Quote", Ar = "عرض سعر" }
            },
            new DockItem
            {
                SortOrder = 4, Icon = "brochure", LinkType = DockLinkType.VehicleBrochure,
                Label = new() { En = "Download Brochure", Ar = "حمّل الكتيب" },
                ShortLabel = new() { En = "Brochure", Ar = "الكتيب" }
            },
            new DockItem
            {
                SortOrder = 5, Icon = "location", LinkType = DockLinkType.Url, Url = "/contact-us",
                Label = new() { En = "Find Showroom", Ar = "أوجد المعرض" },
                ShortLabel = new() { En = "Showroom", Ar = "المعرض" }
            },
            new DockItem
            {
                SortOrder = 6, Icon = "mail", LinkType = DockLinkType.Url, Url = "/contact-us",
                Label = new() { En = "Contact Us", Ar = "تواصل معنا" },
                ShortLabel = new() { En = "Contact", Ar = "تواصل" }
            }
        );
        await db.SaveChangesAsync();
    }
```
> Add `using GAC.Core.Content;` if not already present (it is — `DockItem`/`DockLinkType` live there).

- [ ] **Step 6: Run the test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~ActionDockSeedTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add GAC.Core/Services/ISiteService.cs GAC.Infrastructure/Services/SiteService.cs GAC.Infrastructure/Data/ContentSeeder.cs GAC.Tests/ActionDockTests.cs
git commit -m "feat(dock): public read + seed the six dock items"
```

---

## Task 9: Admin Dock section (controller + views + nav + DI)

No new unit test (CRUD covered by Task 6); verified by build + the suite.

**Files:**
- Create: `GAC.Web/Areas/Admin/Controllers/DockController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Dock/Index.cshtml`, `Edit.cshtml`
- Modify: `GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml:16`
- Modify: `GAC.Web/Program.cs:65`

**Interfaces:**
- Consumes: `IAdminDockService` (Task 6), `DockIcons.Keys` (Task 7), `DockItem`/`DockLinkType`.

- [ ] **Step 1: Create the controller**

Create `GAC.Web/Areas/Admin/Controllers/DockController.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class DockController : Controller
{
    private readonly IAdminDockService _svc;
    public DockController(IAdminDockService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAllAsync());

    public IActionResult Create() => View("Edit", new DockItem());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _svc.GetAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Save(DockItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Label?.En))
            ModelState.AddModelError("Label.En", "Label (English) is required.");
        if (!ModelState.IsValid) return View("Edit", item);

        if (item.Id == 0)
        {
            await _svc.CreateAsync(item);
            TempData["Flash"] = "Dock item created.";
        }
        else
        {
            await _svc.UpdateAsync(item);
            TempData["Flash"] = "Dock item saved.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        TempData["Flash"] = "Dock item deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        await _svc.MoveAsync(id, direction);
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 2: Create the list view**

Create `GAC.Web/Areas/Admin/Views/Dock/Index.cshtml`:
```cshtml
@model IReadOnlyList<GAC.Core.Content.DockItem>
@{
    ViewData["Title"] = "Action dock";
}
<h1>Action dock</h1>

<p><a asp-action="Create" class="adm-btn">New item</a></p>

<table class="adm-table">
    <thead>
        <tr><th>Label</th><th>Type</th><th>Target</th><th>Icon</th><th>Visible</th><th>Order</th><th></th></tr>
    </thead>
    <tbody>
        @foreach (var d in Model)
        {
            <tr>
                <td>@d.Label.Localize()</td>
                <td>@d.LinkType</td>
                <td>@(d.LinkType == GAC.Core.Content.DockLinkType.Url ? d.Url : "—")</td>
                <td>@d.Icon</td>
                <td>@(d.IsVisible ? "Yes" : "No")</td>
                <td>
                    @d.SortOrder
                    <form asp-action="Move" method="post" style="display:inline">
                        <input type="hidden" name="id" value="@d.Id" />
                        <input type="hidden" name="direction" value="-1" />
                        <button type="submit" class="adm-btn" title="Move up">&uarr;</button>
                    </form>
                    <form asp-action="Move" method="post" style="display:inline">
                        <input type="hidden" name="id" value="@d.Id" />
                        <input type="hidden" name="direction" value="1" />
                        <button type="submit" class="adm-btn" title="Move down">&darr;</button>
                    </form>
                </td>
                <td>
                    <a asp-action="Edit" asp-route-id="@d.Id">Edit</a>
                    <form asp-action="Delete" method="post" style="display:inline"
                          onsubmit="return confirm('Delete this dock item?')">
                        <input type="hidden" name="id" value="@d.Id" />
                        <button type="submit" class="adm-btn adm-btn--danger">Delete</button>
                    </form>
                </td>
            </tr>
        }
    </tbody>
</table>
```

- [ ] **Step 3: Create the edit view**

Create `GAC.Web/Areas/Admin/Views/Dock/Edit.cshtml`:
```cshtml
@model GAC.Core.Content.DockItem
@using GAC.Core.Content
@using GAC.Web.Areas.Admin.Models
@using GAC.Web.Infrastructure
@{
    var isNew = Model.Id == 0;
    ViewData["Title"] = isNew ? "New dock item" : "Edit dock item";
}
<p><a asp-area="Admin" asp-action="Index">&larr; Back to action dock</a></p>

<h1>@(isNew ? "New dock item" : "Edit dock item")</h1>

<form method="post" asp-action="Save">
    <input type="hidden" asp-for="Id" />
    <div asp-validation-summary="All" class="adm-error"></div>

    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Label", NameEn = "Label.En", NameAr = "Label.Ar", ValueEn = Model.Label.En, ValueAr = Model.Label.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Short label (mobile)", NameEn = "ShortLabel.En", NameAr = "ShortLabel.Ar", ValueEn = Model.ShortLabel.En, ValueAr = Model.ShortLabel.Ar }' />

    <div class="adm-field">
        <label asp-for="LinkType">Link type</label>
        <select asp-for="LinkType" asp-items="Html.GetEnumSelectList<DockLinkType>()"></select>
    </div>

    <div class="adm-field">
        <label asp-for="Url">Url (used when link type is Url)</label>
        <input asp-for="Url" />
    </div>

    <div class="adm-field">
        <label asp-for="Icon">Icon</label>
        <select asp-for="Icon">
            @foreach (var key in DockIcons.Keys)
            {
                <option value="@key">@key</option>
            }
        </select>
    </div>

    <div class="adm-field">
        <label><input asp-for="IsVisible" /> Visible</label>
    </div>

    <div class="adm-field">
        <label asp-for="SortOrder">Sort order</label>
        <input asp-for="SortOrder" type="number" />
    </div>

    <button type="submit" class="adm-btn">Save</button>
</form>
```

- [ ] **Step 4: Add the sidebar link**

In `GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml`, add after the Menu link (line 10):
```html
        <a href="/Admin/Menu">Menu</a>
        <a href="/Admin/Dock">Action Dock</a>
```

- [ ] **Step 5: Register the service**

In `GAC.Web/Program.cs`, add after line 65 (`IAdminMenuService`):
```csharp
builder.Services.AddScoped<IAdminMenuService, AdminMenuService>();
builder.Services.AddScoped<IAdminDockService, AdminDockService>();
```

- [ ] **Step 6: Build**

Run: `dotnet build GAC.Web/GAC.Web.csproj -clp:ErrorsOnly`
Expected: Build succeeded (Razor compiles at build time here).

- [ ] **Step 7: Commit**

```bash
git add GAC.Web/Areas/Admin/Controllers/DockController.cs GAC.Web/Areas/Admin/Views/Dock GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml GAC.Web/Program.cs
git commit -m "feat(admin): Action Dock CRUD section"
```

---

## Task 10: Render the dock from the DB + per-vehicle brochure context

**Files:**
- Modify: `GAC.Web/ViewComponents/FooterViewComponent.cs`
- Modify: `GAC.Web/Views/Shared/Components/Footer/Default.cshtml`
- Modify: `GAC.Web/Controllers/PageController.cs:34-39`
- Test: `GAC.Tests/ActionDockTests.cs` (append render tests)

**Interfaces:**
- Consumes: `ISiteService.GetDockItemsAsync` (Task 8), `DockIcons.Render` (Task 7), `HttpContext.Items["CurrentVehicleBrochure"]`.
- Produces: `FooterViewModel { Settings, DockItems, CurrentVehicleBrochure }`; footer renders dock items; brochure item only on a model page with a brochure.

- [ ] **Step 1: Write the failing render tests**

Append to `GAC.Tests/ActionDockTests.cs`:
```csharp
public class ActionDockRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public ActionDockRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Home_Renders_Dock_Items()
    {
        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();
        Assert.Contains("action-dock", html);
        Assert.Contains("action-dock__item--wa", html); // WhatsApp item present
    }

    [Fact]
    public async Task Home_Hides_Brochure_When_Not_On_Vehicle_Page()
    {
        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();
        // The brochure item is VehicleBrochure-typed; off a model page it must not render.
        Assert.DoesNotContain("action-dock__full\">Download Brochure", html.Replace(" ", ""));
    }
}
```
> Uses `DevWebApplicationFactory` from `HomePageSmokeTests.cs`. The second assertion compares against a whitespace-stripped copy to be resilient to formatting; adjust the needle if the markup differs.

- [ ] **Step 2: Run the tests to verify they fail**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~ActionDockRenderTests`
Expected: FAIL — footer still uses hardcoded markup (brochure always present with `href="#"`).

- [ ] **Step 3: Update the FooterViewComponent + model**

Replace the contents of `GAC.Web/ViewComponents/FooterViewComponent.cs` with:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class FooterViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public IReadOnlyList<DockItem> DockItems { get; set; } = new List<DockItem>();
    public string? CurrentVehicleBrochure { get; set; }
}

public class FooterViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    public FooterViewComponent(ISiteService site) => _site = site;

    public async Task<IViewComponentResult> InvokeAsync() => View(new FooterViewModel
    {
        Settings = await _site.GetSettingsAsync(),
        DockItems = await _site.GetDockItemsAsync(),
        CurrentVehicleBrochure = HttpContext.Items["CurrentVehicleBrochure"] as string
    });
}
```

- [ ] **Step 4: Convert the footer view to a dock loop**

Replace the **action-dock `<nav>` block (lines 1-30)** of `GAC.Web/Views/Shared/Components/Footer/Default.cshtml` with the following, and update the `@model` line. Keep the `<footer class="site-footer">` block (lines 32-69) but change every `Model.` social reference to `settings.`:
```cshtml
@using GAC.Web.Infrastructure
@using GAC.Core.Content
@model GAC.Web.ViewComponents.FooterViewModel
@{ var settings = Model.Settings; }

<!-- Fixed quick-action dock (right rail on desktop, bottom bar on mobile) — DB-driven (Admin > Action Dock) -->
<nav class="action-dock" aria-label="Quick actions">
@foreach (var item in Model.DockItems)
{
    if (item.LinkType == DockLinkType.VehicleBrochure && string.IsNullOrWhiteSpace(Model.CurrentVehicleBrochure)) { continue; }
    var href = item.LinkType switch
    {
        DockLinkType.WhatsApp => $"https://api.whatsapp.com/send/?phone={settings.WhatsApp}",
        DockLinkType.Phone => $"tel:{settings.Phone}",
        DockLinkType.VehicleBrochure => Model.CurrentVehicleBrochure,
        _ => item.Url
    };
    var waClass = item.LinkType == DockLinkType.WhatsApp ? " action-dock__item--wa" : "";
    var external = item.LinkType is DockLinkType.WhatsApp or DockLinkType.VehicleBrochure;
    <a class="action-dock__item@(waClass)" href="@href" title="@item.Label.Localize()"
       target="@(external ? "_blank" : null)" rel="noopener">
      <span class="action-dock__icon">@DockIcons.Render(item.Icon)</span>
      <span class="action-dock__text"><span class="action-dock__full">@item.Label.Localize()</span><span class="action-dock__short">@item.ShortLabel.Localize()</span></span>
    </a>
}
</nav>
```
Then in the retained `<footer class="site-footer">` block, change the five social conditionals from `Model.InstagramUrl`, `Model.SnapchatUrl`, `Model.FacebookUrl`, `Model.TiktokUrl`, `Model.XUrl` to `settings.InstagramUrl`, `settings.SnapchatUrl`, `settings.FacebookUrl`, `settings.TiktokUrl`, `settings.XUrl` respectively (the `@model` is now `FooterViewModel`, so `Model.*` no longer points at `SiteSettings`).

- [ ] **Step 5: Set the per-vehicle brochure context**

In `GAC.Web/Controllers/PageController.cs`, set the footer context when serving a vehicle (replace lines 34-39):
```csharp
        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle != null)
        {
            HttpContext.Items["CurrentVehicleBrochure"] = vehicle.BrochurePdf;
            ViewData["Seo"] = SeoBuilder.ForVehicle(vehicle, baseUrl);
            return View("~/Views/Vehicles/Detail.cshtml", vehicle);
        }
```

- [ ] **Step 6: Run the render tests to verify they pass**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter FullyQualifiedName~ActionDockRenderTests`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add GAC.Web/ViewComponents/FooterViewComponent.cs GAC.Web/Views/Shared/Components/Footer/Default.cshtml GAC.Web/Controllers/PageController.cs GAC.Tests/ActionDockTests.cs
git commit -m "feat(dock): render action-dock from DB with per-vehicle brochure"
```

---

## Task 11: Guarded data-only prod SQL

**Files:**
- Create: `Solution/docs/migrations/2026-06-21-spec-and-dock-prod.sql`

**Interfaces:**
- Consumes: the `DockItems` table + `Vehicles.SpecPdf` column (created by the EF migration applied in Task 2 / re-applied to prod separately).

- [ ] **Step 1: Write the script**

Create `Solution/docs/migrations/2026-06-21-spec-and-dock-prod.sql`:
```sql
/*
  GAC — Specifications PDF + Action Dock (prod data) — 2026-06-21
  Run AFTER the EF migration AddSpecPdfAndDock has been applied to prod (adds
  Vehicles.SpecPdf + the DockItems table). Data-only, guarded, idempotent.

  1. Seed the 6 action-dock items if the table is empty (matches the old hardcoded dock).
  2. The Specifications button is now field-driven (Vehicles.SpecPdf); the gs4/hyptec
     in-body trim anchors are removed by 2026-06-21-content-updates.sql (section 3).
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [DockItems])
BEGIN
    INSERT INTO [DockItems] ([Label_En],[Label_Ar],[ShortLabel_En],[ShortLabel_Ar],[Url],[Icon],[LinkType],[IsVisible],[SortOrder])
    VALUES
      (N'Chat on WhatsApp', N'تواصل عبر واتساب', N'WhatsApp',   N'واتساب',       NULL,                 N'whatsapp',  1, 1, 1),
      (N'Book a Test Drive',N'احجز تجربة قيادة', N'Test Drive', N'تجربة قيادة',  N'/book-a-test-drive',N'test-drive',0, 1, 2),
      (N'Get Online Quote', N'اطلب عرض سعر',     N'Quote',      N'عرض سعر',      N'/request-a-quote',  N'quote',     0, 1, 3),
      (N'Download Brochure',N'حمّل الكتيب',       N'Brochure',   N'الكتيب',       NULL,                 N'brochure',  3, 1, 4),
      (N'Find Showroom',    N'أوجد المعرض',      N'Showroom',   N'المعرض',       N'/contact-us',       N'location',  0, 1, 5),
      (N'Contact Us',       N'تواصل معنا',       N'Contact',    N'تواصل',        N'/contact-us',       N'mail',      0, 1, 6);
END

COMMIT;
GO
```
> `LinkType` integer values: Url=0, WhatsApp=1, Phone=2, VehicleBrochure=3 (matches the `DockLinkType` enum).

- [ ] **Step 2: Commit**

```bash
git add "Solution/docs/migrations/2026-06-21-spec-and-dock-prod.sql"
git commit -m "chore(db): guarded prod data script for dock items"
```

---

## Task 12: Full verification

**Files:** none (verification only).

- [ ] **Step 1: Full build**

Run: `dotnet build GAC.Web/GAC.Web.csproj -clp:ErrorsOnly`
Expected: Build succeeded, 0 errors.

- [ ] **Step 2: Full test suite**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj`
Expected: PASS — all prior tests (222) plus the new MediaService, VehicleSpecPdf(+Render), AdminDockService, DockIcons, and ActionDock(Seed+Render) tests; 0 failures.

- [ ] **Step 3: Generate the prod schema script (for the deploy handoff)**

Run: `dotnet ef migrations script --idempotent --project GAC.Infrastructure --startup-project GAC.Web --output docs/migrations/2026-06-21-AddSpecPdfAndDock.schema.sql`
Expected: an idempotent schema script the operator applies to prod before deploy.

- [ ] **Step 4: Commit**

```bash
git add docs/migrations/2026-06-21-AddSpecPdfAndDock.schema.sql
git commit -m "chore(db): idempotent schema script for AddSpecPdfAndDock"
```

---

## Deploy handoff (user actions; consistent with project workflow)

1. Apply `docs/migrations/2026-06-21-AddSpecPdfAndDock.schema.sql` to the prod GAC DB (schema).
2. Apply `docs/migrations/2026-06-21-content-updates.sql` (gs4/hyptec body cleanup) and `docs/migrations/2026-06-21-spec-and-dock-prod.sql` (seed dock items).
3. Deploy the Web app (apps don't auto-migrate).
4. In the admin panel, upload each model's Specifications + Brochure PDFs (Admin → Vehicles → Edit) and adjust dock items (Admin → Action Dock) as desired.

## Self-Review notes

- **Spec coverage:** §Feature 1 (SpecPdf field, PDF upload, admin picker, render, remove hardcoded) → Tasks 1,3,4,5; §Feature 2 (DockItem, LinkType, icons, admin CRUD, public render, brochure) → Tasks 2,6,7,8,9,10; §data/migration/seed/SQL → Tasks 2,8,11,12; §testing → every task + Task 12.
- **Type consistency:** `DockLinkType` values (Url=0/WhatsApp=1/Phone=2/VehicleBrochure=3) are consistent across entity, seeder, footer switch, and prod SQL. `IAdminDockService` signatures match `AdminDockService` and the tests. `GetDockItemsAsync` is on `ISiteService` and used by `FooterViewComponent`.
- **Confirm at execution:** exact whitespace of the dead-brochure assertion needle (Task 10) — adjust the needle to the rendered markup if it differs.
