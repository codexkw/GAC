# Vehicle Structured Sections — Design

**Date:** 2026-06-15
**Status:** Approved (pending spec review)
**Phase:** Post-launch enhancement (after Phase 7 / deploy)

## Problem

The public vehicle detail page renders a single field:

```cshtml
@* Views/Vehicles/Detail.cshtml *@
@Html.Raw(Model.BodyHtml.Localize())
```

Every model page (hero, intro, feature blocks, spec tables, colours, gallery, CTA)
lives as one hand-authored HTML blob in `Vehicle.BodyHtml`. A non-technical admin
cannot safely manage it. Meanwhile the `Vehicle` entity **already models** structured
collections that are eager-loaded but never rendered:

- `Trims` (`Trim`: Name, Price, Highlights, SpecPdf)
- `SpecGroups` (`SpecGroup`: Title + `Rows` of Label/Value)
- `Colors` (`ColorOption`: Name, Hex, ImagePath)
- `Features` (`FeatureSection`: Heading, Body, ImagePath, SortOrder)

All four are configured in `ContentConfigurations.cs` with owned `LocalizedText`
and cascade delete. `VehicleService.GetBySlugAsync` already `Include`s them.
The data layer is ready; the rendering and admin layers are not.

## Goal

Let non-technical admins build a model page from typed, form-driven sections instead
of editing raw HTML — while live pages keep working until each is migrated.

## Decisions (locked during brainstorming)

1. **Section model:** Hybrid — fixed typed slots (hero, specs, colours, trims) plus a
   repeatable list of free "feature blocks" (heading + rich text + image + layout).
   Raw HTML kept as an optional hidden escape hatch.
2. **Migration:** HTML fallback — render structured sections if present, else the
   legacy `BodyHtml`. Admins migrate each model at their own pace. Zero downtime.
3. **Text editing:** Simple WYSIWYG toolbar (bold, italic, lists, links) for feature
   block bodies. Stored as sanitised HTML.
4. **Block layout:** Preset layouts per feature block (image-left, image-right,
   full-width banner, text-only).
5. **Scope:** Vehicles only. Content pages (About/Warranty, which have the same
   `BodyHtml` problem and an unused `ContentSection` type) are deferred to a follow-up.

## Data model changes

Only one schema change:

- Add enum `FeatureLayout { ImageLeft, ImageRight, Banner, TextOnly }` (in
  `GAC.Core.Content`).
- Add `public FeatureLayout Layout { get; set; }` to `FeatureSection` (default
  `ImageLeft` = 0).
- EF migration `AddFeatureLayout` — a single `int` column on `FeatureSections`,
  non-null default 0. No data backfill required.

`FeatureSection.Body` (existing owned `LocalizedText`) holds the sanitised WYSIWYG
HTML for En and Ar. All other fields/collections are reused unchanged.

## Public rendering — rewrite `Views/Vehicles/Detail.cshtml`

Render in this fixed order, reusing existing GAC CSS classes so the page matches the
current visual style:

1. **Hero** — first `VehicleImage` with `Kind == Hero`, plus `Name`, `Tagline`,
   `IntroText`, and a CTA row (price-from + brochure PDF link when present).
2. **Feature blocks** — `Features` ordered by `SortOrder`. Each renders per `Layout`:
   - `ImageLeft` — image column left, heading + body right
   - `ImageRight` — image column right, heading + body left
   - `Banner` — full-width image with heading + body overlaid/below
   - `TextOnly` — heading + body, no image
   Body emitted via `@Html.Raw` (already sanitised on save).
3. **Spec tables** — each `SpecGroup` → a titled table of `Label`/`Value` rows.
4. **Colours** — swatch list: `Hex` chip + `Name` (+ optional `ImagePath` thumbnail).
5. **Trims** — cards: `Name`, formatted `Price`, `Highlights`, `SpecPdf` download link.

**Fallback rule (precise):** a vehicle is considered to have structured content when
**any** of `Features`, `SpecGroups`, `Colors`, or `Trims` is non-empty. If **none**
are present, render `@Html.Raw(Model.BodyHtml.Localize())` exactly as today.
Implement as a helper, e.g. `bool HasStructuredContent(Vehicle v)`.

Sub-partials keep the view focused: `_VehicleHero`, `_VehicleFeatures`,
`_VehicleSpecs`, `_VehicleColors`, `_VehicleTrims`.

## Admin editing — mirror the existing `_Images` pattern

The vehicle Edit page (`Areas/Admin/Views/Vehicles/Edit.cshtml`) already manages
`Images` through a partial plus `AddImage`/`RemoveImage`/`MoveImage` actions on the
admin `VehiclesController`, backed by `AdminVehicleService`. Extend the same pattern.
All section management appears only for **saved** vehicles (`!isNew`), like images today.

- **Features** — a list partial (`_Features`) with add/remove/move-up/move-down, and a
  dedicated **sub-page** to edit one block (heading En/Ar, **two Trix editors** for
  body En/Ar, image picker via `_PickerModal`, Layout `<select>`). A dedicated sub-page
  avoids stacking many rich-text editors on one screen.
- **Spec groups / rows** (`_SpecGroups`) — inline: add group (Title En/Ar), add/remove/
  move rows (Label/Value En/Ar) within a group.
- **Colours** (`_Colors`) — inline: Name En/Ar, `<input type="color">` for Hex, optional
  image picker, add/remove/move.
- **Trims** (`_Trims`) — inline: Name En/Ar, Price, Highlights En/Ar, SpecPdf picker,
  add/remove/move.

New methods on `IAdminVehicleService` / `AdminVehicleService` for each collection
(create/update/delete/move), following the existing `*Image*` method shapes. The
service currently persists only scalar/owned fields in `UpdateAsync`, so collection
edits go through these dedicated methods (not the main Save), exactly like images.

The raw `BodyHtml` field stays in the main form but moves into a collapsed
**"Advanced (HTML)"** `<details>` block — available as an escape hatch, out of the way.

## WYSIWYG + sanitisation

- **Client:** self-hosted **Trix** editor — one JS + one CSS file copied into
  `wwwroot/assets/vendor/trix/`. Produces clean semantic HTML; supports RTL via `dir`
  for Arabic. No CDN, so no CSP/network dependency. Each editor binds to a hidden input
  (`Body.En`, `Body.Ar`).
- **Server:** sanitise feature-body HTML with the **`Ganss.Xss` (HtmlSanitizer)** NuGet
  on save, allow-listing only: `p, br, strong, em, b, i, ul, ol, li, a[href], h3, h4`.
  Strip everything else (scripts, styles, event handlers, arbitrary tags). A small
  `IHtmlSanitizerService` wrapper keeps the dependency behind an interface and testable.
  Sanitisation runs in the admin feature-save path before persisting.

## Migration & deploy

- EF migration `AddFeatureLayout` (single column).
- Apply to prod via a **targeted, hand-scoped guarded SQL script** (per the known
  prod `__EFMigrationsHistory` gaps — never `dotnet ef database update` or a full
  `--idempotent` script, which replay old migrations and fail).
- No content migration: the fallback renders existing `BodyHtml` until an admin adds
  sections to a given model.
- New NuGet `Ganss.Xss` pinned to a `9.0.*`-compatible version; ensure `Microsoft.*`
  packages stay pinned (no float).

## Testing

**Rendering (`SeoBuilderTests`-style + view/integration):**
- Vehicle with no collections → page contains the `BodyHtml` content (fallback).
- Vehicle with features/specs/colours/trims → page contains those, not the blob.
- Each `FeatureLayout` value produces its expected wrapper class/structure.
- Spec table, colour swatch, and trim card render expected fields.

**Admin (`AdminVehicleServiceTests`-style):**
- Create/update/delete/move for Features, SpecGroups+Rows, Colours, Trims.
- Bilingual fields (En/Ar) persist and round-trip.
- Sanitiser strips `<script>`, inline handlers, and disallowed tags while keeping
  allowed formatting.

**Existing suite:** all current tests stay green (≈201).

## Out of scope (deferred)

- Content pages (About/Warranty/etc.) using `ContentSection` — same pattern, later spec.
- Drag-and-drop reordering — use up/down arrows like Images.
- Media-library changes — reuse the existing picker as-is.

## Files (anticipated)

**Create:**
- `GAC.Core/Content/FeatureLayout.cs`
- `GAC.Infrastructure/Migrations/<ts>_AddFeatureLayout.cs` (generated)
- `GAC.Core/Services/IHtmlSanitizerService.cs`
- `GAC.Infrastructure/Services/HtmlSanitizerService.cs`
- `GAC.Web/Views/Vehicles/_VehicleHero.cshtml`, `_VehicleFeatures.cshtml`,
  `_VehicleSpecs.cshtml`, `_VehicleColors.cshtml`, `_VehicleTrims.cshtml`
- `GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml`, `FeatureEdit.cshtml`,
  `_SpecGroups.cshtml`, `_Colors.cshtml`, `_Trims.cshtml`
- `GAC.Web/wwwroot/assets/vendor/trix/trix.umd.min.js`, `trix.css`
- Tests under `GAC.Tests/`

**Modify:**
- `GAC.Core/Content/FeatureSection.cs` (+`Layout`)
- `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (map `Layout` if needed)
- `GAC.Web/Views/Vehicles/Detail.cshtml` (structured render + fallback)
- `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml` (section partials + Advanced HTML `<details>`)
- `GAC.Web/Areas/Admin/Controllers/VehiclesController.cs` (collection actions)
- `GAC.Core/Services/IAdminVehicleService.cs` + `GAC.Infrastructure/Services/AdminVehicleService.cs`
- `GAC.Web/Program.cs` (register `IHtmlSanitizerService`)
- `GAC.Web/*.csproj` (add `Ganss.Xss`)
