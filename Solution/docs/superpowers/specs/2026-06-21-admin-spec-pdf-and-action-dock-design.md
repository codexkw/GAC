# Design — Admin-managed Specifications PDF & Action-Dock

**Date:** 2026-06-21
**Status:** Approved (pending spec review)
**Project:** GAC bilingual EN/AR CMS (ASP.NET Core 9 MVC) — `C:\Users\anas-\source\repos\GAC\Solution`

## Summary

Two admin-manageability features for the GAC site:

1. **Per-vehicle Specifications PDF** — an admin can upload a PDF for a car model; the public "Specifications" button links to it. Requires enabling PDF uploads in the (currently image-only) media pipeline.
2. **Action-dock as an admin-managed CRUD list** — the floating quick-actions rail (currently 100% hardcoded) becomes a reorderable, editable list of items in the admin panel, including a working per-vehicle "Download Brochure" link.

Both follow existing CMS patterns (LocalizedText owned types, media picker, the Menu/Settings admin services, guarded data-only prod SQL + a separately-applied EF migration).

## Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| 1 | Specifications PDF level | **Per-vehicle** (one PDF per model). Remove hardcoded `/pdfs/...` from gs4/hyptec bodies; field drives the button. |
| 2 | PDF upload mechanism | **Extend the existing media library** to accept `application/pdf` (unified image+PDF library). |
| 3 | Action-dock scope | **Full CRUD** — add/remove/reorder; edit label (EN/AR), short label, URL, icon (from a fixed set), visibility. |
| 4 | "Download Brochure" behavior | **Per-vehicle** — uses `Vehicle.BrochurePdf` (made uploadable); hidden when the model has none / not on a model page. |
| 5 | Spec button placement | **Fixed CTA under the hero**, rendered by `Detail.cshtml` for both structured and raw-body vehicles. |
| 6 | DockItem special behaviors | Modeled via a `LinkType` enum (Url / WhatsApp / Phone / VehicleBrochure). |

## Current-state facts (from codebase recon)

- **Specifications button today** is driven by the structured `Trim.SpecPdf` field, rendered in `GAC.Web/Views/Vehicles/_VehicleTrims.cshtml` (only when a vehicle has structured trims). gs4 & hyptec-ht have **no** structured trims — they render via raw `BodyHtml`, where the Specifications `<a href="/pdfs/...">` is hardcoded (added 2026-06-21; see `docs/migrations/2026-06-21-content-updates.sql`).
- **`Vehicle.BrochurePdf`** (`string?`) already exists and is admin-editable (plain text input in `Areas/Admin/Views/Vehicles/Edit.cshtml`, persisted in `AdminVehicleService.UpdateAsync`) but is **not rendered** anywhere on the public site.
- **`MediaService`** (`GAC.Infrastructure/Services/MediaService.cs`) restricts uploads to images only (`.jpg/.jpeg/.png/.webp/.gif`, MIME `image/*`), 5 MB cap (`MediaOptions`). Uploads land in `wwwroot/uploads`, recorded in `MediaAssets`. The picker modal `Areas/Admin/Views/Shared/_PickerModal.cshtml` has `accept="image/*"`; picker JS is in `wwwroot/assets/js/admin.js` (`data-media-pick` button → fills nearest `data-media-input`).
- **Action-dock** is 100% hardcoded in `GAC.Web/Views/Shared/Components/Footer/Default.cshtml` (lines ~5–30), rendered on every page via `<vc:footer />` in `_Layout.cshtml`. Only WhatsApp is settings-driven (`Model.WhatsApp`); "Download Brochure" is a dead `href="#"`. Labels use `@L[...]` with full/short variants for responsive layout; CSS in `wwwroot/assets/css/styles.css` (~1871–1977) + `rtl.css`.
- **Admin patterns to mirror:** global singleton = `AdminSettingsService` (`GetAsync`/`UpdateAsync`); ordered list w/ CRUD+reorder = `AdminMenuService` (`ListAllAsync`/`GetAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync`/`MoveAsync`) + `MenuController` + `Menu/Index.cshtml`+`Edit.cshtml` + `_LocalizedField` partial + `_AdminNav.cshtml` sidebar. Policies: `ContentEditor` (Admin or Editor), `AdminOnly`. EF owned localized columns via `OwnsLocalized` → `Field_En`/`Field_Ar`.

---

## Feature 1 — Per-vehicle Specifications PDF

### 1.1 Data model
- Add `public string? SpecPdf { get; set; }` to `GAC.Core/Content/Vehicle.cs`.
- Keep `Vehicle.BrochurePdf` (consumed by the action-dock `VehicleBrochure` item — Feature 2, §2.4).
- EF: optional `HasMaxLength(300)` in `VehicleConfig` (mirrors other path columns), nvarchar(max) is also acceptable.

### 1.2 PDF upload (shared infra for spec + brochure)
- `MediaService`: add `.pdf` to the extension allowlist and `application/pdf` to the MIME allowlist. Introduce a larger size cap for PDFs (e.g. 20 MB) while keeping images at 5 MB — either a second `MediaOptions` value (`PdfMaxBytes`) or branch on content type. Error messages updated to mention PDFs.
- `_PickerModal.cshtml`: change `accept="image/*"` → `accept="image/*,.pdf"`.
- Picker grid: render a **PDF tile** (document icon + original filename) for assets whose path ends in `.pdf`, instead of an `<img>` that would 404. (`MediaController.List` already returns `{Id, Path}`; add `OriginalFileName` if needed, or derive label from path.)
- Picker JS (`admin.js`): no functional change needed (it posts any file and writes the returned path), but verify the grid render branch handles non-images.

### 1.3 Admin editor
- In `Areas/Admin/Views/Vehicles/Edit.cshtml`, render **`SpecPdf`** (new) and **`BrochurePdf`** (existing) as text input + "Choose…" button using the `data-media-input` / `data-media-pick` pattern (copy from `_Trims.cshtml`). Both live in the main vehicle Save form.
- `AdminVehicleService.UpdateAsync`: add `existing.SpecPdf = vehicle.SpecPdf;` (BrochurePdf already mapped). No sanitization needed (plain paths).

### 1.4 Public render
- In `Views/Vehicles/Detail.cshtml`, after the structured-or-raw branch, render a **Specifications CTA** when `Model.SpecPdf` is non-empty:
  ```cshtml
  @if (!string.IsNullOrWhiteSpace(Model.SpecPdf))
  {
      <section class="mp-spec-cta">
        <div class="container">
          <a class="btn btn--primary" href="@Model.SpecPdf" target="_blank" rel="noopener">@L["Specifications"]</a>
        </div>
      </section>
  }
  ```
  Placement: a single, always-visible Specifications button rendered by `Detail.cshtml`. **Structured vehicles:** the band is emitted directly after the `_VehicleHero` partial (under the hero). **Raw-body vehicles:** the body is an opaque blob whose own hero is the first element, so the band is emitted immediately **before** the raw body (top of the model content). The two positions differ slightly by render mode; CSS keeps the button visually consistent. (The alternative — injecting under the raw hero via a body placeholder token — was considered and rejected for added complexity.)
- Remove the hardcoded `<a ... href="/pdfs/gs4-specifications.pdf" ...>Specifications</a>` and the hyptec-ht equivalent from the seed bodies (`SeedContent/vehicles/gs4.html`, `hyptec-ht.html`) and from the prod content-update SQL.
- The per-trim `Trim.SpecPdf` rendering in `_VehicleTrims.cshtml` is left unchanged (still works for structured vehicles; per-vehicle `SpecPdf` is the new primary mechanism).

---

## Feature 2 — Action-dock CRUD admin

### 2.1 Data model
New entity `GAC.Core/Content/DockItem.cs`:
```csharp
public class DockItem
{
    public int Id { get; set; }
    public LocalizedText Label { get; set; } = new();       // full text (desktop)
    public LocalizedText ShortLabel { get; set; } = new();  // mobile text
    public string? Url { get; set; }                        // used when LinkType == Url
    public string Icon { get; set; } = "";                  // icon key from the fixed set
    public DockLinkType LinkType { get; set; } = DockLinkType.Url;
    public bool IsVisible { get; set; } = true;
    public int SortOrder { get; set; }
}

public enum DockLinkType { Url = 0, WhatsApp = 1, Phone = 2, VehicleBrochure = 3 }
```
- EF: `DockItemConfig` with `OwnsLocalized(Label)` + `OwnsLocalized(ShortLabel)`; `DbSet<DockItem> DockItems` on `ApplicationDbContext`.

### 2.2 Icons
- A fixed named set rendered by a helper `DockIcon(string key)` (e.g. in `GAC.Web/Infrastructure`) returning the existing inline SVG markup. Keys: `whatsapp`, `test-drive`, `quote`, `brochure`, `location`, `mail`, `phone` (extensible). Unknown key → a neutral default icon.
- Admin picks the key from a `<select>` in the dock item editor.

### 2.3 Admin section (mirror Menu admin)
- `GAC.Core/Services/IAdminDockService.cs`: `ListAllAsync`, `GetAsync(id)`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `MoveAsync(id, direction)`.
- `GAC.Infrastructure/Services/AdminDockService.cs`: implement like `AdminMenuService` (no parent/child — flat list; reorder by swapping `SortOrder` among all items).
- `GAC.Web/Areas/Admin/Controllers/DockController.cs`: `[Area("Admin")] [Authorize(Policy = AdminPolicies.ContentEditor)] [AutoValidateAntiforgeryToken]`; actions `Index`, `Create`, `Edit`, `Save`, `Delete`, `Move`. Validate `Label.En` non-empty (mirror Menu).
- Views `Areas/Admin/Views/Dock/Index.cshtml` (table: label, icon, link type/target, visibility, order + up/down, Edit/Delete) and `Edit.cshtml` (`_LocalizedField` for Label + ShortLabel; selects for Icon and LinkType; Url input shown for `LinkType==Url`; IsVisible checkbox; SortOrder).
- `Areas/Admin/Views/Shared/_AdminNav.cshtml`: add a "Dock" link (ContentEditor visibility, near Menu).
- Register `IAdminDockService` in `Program.cs`.

### 2.4 Public render
- `FooterViewComponent` loads visible `DockItem`s ordered by `SortOrder`, plus `SiteSettings`, plus the **current vehicle's brochure path** when on a model page.
  - `VehiclesController.Detail` sets `HttpContext.Items["CurrentVehicleBrochure"] = vehicle.BrochurePdf;` (and/or the vehicle). The view component reads it.
- `Footer/Default.cshtml`: replace the hardcoded items with a loop over the dock items. Per `LinkType`:
  - `Url` → `href="@item.Url"`.
  - `WhatsApp` → `href="https://api.whatsapp.com/send/?phone=@settings.WhatsApp"` (`target=_blank rel=noopener`).
  - `Phone` → `href="tel:@settings.Phone"`.
  - `VehicleBrochure` → render only when a current-vehicle brochure path exists; `href` = that path (`target=_blank`). Otherwise skip the item.
  - Icon via `DockIcon(item.Icon)`; full/short labels via `Label`/`ShortLabel` `.Localize()`.
- CSS unchanged (same classes emitted by the loop).

---

## Data / migration / seed / deploy

- **EF migration** `AddSpecPdfAndDock`: adds `Vehicles.SpecPdf` column + `DockItems` table (with `Label_En/Ar`, `ShortLabel_En/Ar`, `Url`, `Icon`, `LinkType`, `IsVisible`, `SortOrder`).
- **Seeder** (`ContentSeeder`): leave `Vehicle.SpecPdf` null; add `SeedDockItemsAsync` seeding the current 6 items so the dock is unchanged after deploy:
  1. WhatsApp (`WhatsApp`, icon `whatsapp`)
  2. Book a Test Drive (`Url` `/book-a-test-drive`, icon `test-drive`)
  3. Get Online Quote (`Url` `/request-a-quote`, icon `quote`)
  4. Download Brochure (`VehicleBrochure`, icon `brochure`)
  5. Find Showroom (`Url` `/contact-us`, icon `location`)
  6. Contact Us (`Url` `/contact-us`, icon `mail`)
  With EN+AR labels/short-labels matching the current `@L[...]` strings; add Arabic in `EnsureArabicAsync` if following that pattern.
- **Guarded data-only prod SQL** (new file under `docs/migrations/`, matching the established workflow): insert the 6 DockItems if the table is empty, and remove the hardcoded Specifications anchors from `Vehicles.BodyHtml_En` for gs4/hyptec-ht (extends/supersedes the relevant section of `2026-06-21-content-updates.sql`). The **schema migration is applied separately before deploy** (guarded script), per the project's "apply before deploy; apps don't auto-migrate" rule.

## Testing

- `MediaService`: accepts a `.pdf`/`application/pdf` upload; still rejects disallowed types; enforces the PDF size cap.
- Vehicle detail: renders the Specifications button when `SpecPdf` set, omits it when null (extend `VehiclePagesTests` / add a focused test).
- Action-dock render: seeded items appear in order; `VehicleBrochure` item shows only on a model page that has a brochure and is hidden otherwise; WhatsApp uses settings phone.
- Admin dock service: CRUD + `MoveAsync` reorder (mirror `AdminMenuServiceTests`).
- Full suite stays green (currently 222 tests).

## Out of scope

- Per-language PDFs (one URL serves both EN/AR).
- Virus/malware scanning of uploads.
- Migrating gs4/hyptec-ht to full structured content.
- Database-driven admin sidebar.

## User actions after build (consistent with project workflow)

- Apply the schema migration (guarded script) to the GAC prod DB, then the data-only content SQL.
- Deploy the Web app (apps don't auto-migrate).
- Upload the actual spec/brochure PDFs per model via the admin panel.
