# Editable Warranty Page + Dynamic Cars Grid — Design

Date: 2026-06-30
Status: Approved (pending spec review)

## Goal

Make the public `/warranty` page fully editable from the admin panel and replace
its **hardcoded, stale cars grid** with a **live grid driven by the Vehicles**
already managed under car models — **without losing the current content** and
**without changing the per-vehicle warranty tab** (`WarrantyLinks`).

Today `/warranty` is a generic `ContentPage` whose entire page is one raw-HTML
blob (`BodyHtml`), rendered by `Views/Content/Page.cshtml` via
`@Html.Raw(...)`. The cars grid is baked into that HTML and lists old models
(GS8, EMKOO, EMZOOM, EMPOW, GS5, GS4, GS3, GA8, GA6, GA4) with placeholder
`href="#"` booklet links.

## Decisions (locked)

- **Hybrid editing**: structured fields for banner / intro / terms; the
  Extended-Warranty **brand matrix table stays one editable HTML field** (it is a
  6-column matrix — structuring it is not worth the complexity).
- **Cars grid = all VISIBLE vehicles**, in their existing `SortOrder`; auto-updates.
- **Booklet link = new per-vehicle `Vehicle.WarrantyBookletPdf`** field; the grid
  shows a "Warranty Booklet" button only when that vehicle has a PDF set.

## Data preservation (hard requirement — same approach as the home-sections work)

- **Migration is additive only.** Creates `WarrantyPages` + `WarrantyCallouts`
  tables and adds one **nullable** `Vehicles.WarrantyBookletPdf` column. No
  `DROP`, no `ALTER`/`UPDATE` of existing columns/rows. Reviewed before hand-off.
- **Seed/backfill is write-only-when-empty.** `WarrantyPage` seeds only when
  `WarrantyPages` is empty; Arabic is filled only where blank; `WarrantyBookletPdf`
  is left null (no booklets exist yet — current links are `#`).
- **Render fallback.** The public view prefers the DB value and falls back to the
  current hardcoded text/image when a field is empty, so the page looks identical
  before and after seeding.
- **User applies the migration on deploy** (the app does not auto-migrate).
  ⚠️ Coupling: the new warranty view eager-loads the new tables, so `/warranty`
  errors until the migration is applied — deploy code + migration together.

## Data model

New entities in `GAC.Core/Content`, following the `LocalizedText` owned-type
convention.

### WarrantyPage (singleton, like `HomePage`)
```
int Id
string BannerImagePath          // required, max 300  ("/assets/img/warranty/banner.jpg")
LocalizedText BannerLabel        // "GAC Mutawa Alkadi Automotive Warranty"
LocalizedText Heading            // "Warranty"
LocalizedText Intro              // 2 paragraphs (multiline; each non-empty line → <p class="muted">)
string TermsImagePath            // required, max 300  ("/assets/img/warranty/callout.jpg")
LocalizedText TermsNote          // "*terms and conditions apply"
LocalizedText ExtendedHeading    // "Extended Warranty Program"
LocalizedText ExtendedIntro      // 3 paragraphs (multiline)
LocalizedText ExtendedTableHtml  // the brand matrix <table> markup (hybrid HTML field)
List<WarrantyCallout> Callouts
```

(Title / meta / visibility stay on the existing `ContentPage` "warranty" record —
only the page **body** moves to `WarrantyPage`, so SEO is unchanged.)

### WarrantyCallout (the checkmark "terms" lines; add/remove rows)
```
int Id
int WarrantyPageId
LocalizedText Lead   // bold lead  ("5 years extended warranty, unlimited mileage")
LocalizedText Text   // remainder  ("for Mutawa Alkadi showroom customers")
int SortOrder
```

### Vehicle (existing) — add
```
string? WarrantyBookletPdf   // per-vehicle booklet PDF used by the warranty grid
```

## EF mapping & migration

- `ContentConfigurations.cs`: add `WarrantyPageConfig`, `WarrantyCalloutConfig`
  mirroring `HeroSlideConfig` — `OwnsLocalized(...)` for every bilingual field,
  `BannerImagePath`/`TermsImagePath` `HasMaxLength(300).IsRequired()`,
  `HasMany(w => w.Callouts).WithOne().HasForeignKey(c => c.WarrantyPageId).OnDelete(Cascade)`.
  Add `WarrantyBookletPdf` `HasMaxLength(300)` to the existing `VehicleConfig`.
- Register `DbSet`s in `ApplicationDbContext` (`WarrantyPages`, `WarrantyCallouts`).
- One additive EF migration `AddWarrantyPage` (CREATE TABLE ×2 + ADD COLUMN).

## Public rendering

- **Route**: `PageController.Show` — when the resolved `ContentPage.Slug == "warranty"`,
  load the `WarrantyPage` aggregate (incl. ordered `Callouts`) + all **visible**
  vehicles (ordered by `SortOrder`, with their thumbnail images), and render a new
  `~/Views/Content/Warranty.cshtml` with a `WarrantyPageViewModel { Warranty, Vehicles }`.
  Visibility + SEO (title/meta) stay on the `ContentPage` record, unchanged
  (`SeoBuilder.ForContentPage`); only the page body moves to `WarrantyPage`.
- **View** (`Views/Content/Warranty.cshtml`): reproduce the current markup using
  the **exact same CSS classes** (`warr-banner`, `cos-head`, `warr-terms`,
  `callouts`/`callout`, `wgrid`/`wcard`, `warr-ext-title`, `datatable`) so styling
  is unchanged. Sections:
  - Banner: `BannerImagePath` + `BannerLabel`.
  - Intro: `Heading` + `Intro` (each non-empty line → `<p class="muted">`).
  - Terms: `TermsImagePath` + `@foreach` callout (`Lead` bold + `Text`) + `TermsNote`.
  - **Cars grid**: `@foreach` visible vehicle → `wcard` with `UrlHelpers.ThumbPath(v)`,
    `v.Name.Localize()`, and a `btn btn--doc` "Warranty Booklet" link **only when
    `v.WarrantyBookletPdf` is set**.
  - Extended: `ExtendedHeading` + `ExtendedIntro` paragraphs + `@Html.Raw(ExtendedTableHtml.Localize())`.
  - Null/empty guards fall back to current defaults pre-seed.

## Seeding & localization

In `ContentSeeder` (called from `SeedAsync`):
- `SeedWarrantyAsync` — guard on `WarrantyPages` empty; seed all fields from the
  current `SeedContent/content/warranty.html` (EN) + Arabic translations, incl. the
  2 terms callouts and the brand-matrix table HTML verbatim.
- `EnsureArabicAsync` — extend to backfill warranty fields where blank.
- `Vehicle.WarrantyBookletPdf` left null (no booklets yet).

## Admin

- New nav entry **"Warranty page"** (`_AdminNav.cshtml`, Admin/Editor), served by a
  new `WarrantyController` (`[Area("Admin")]`, `ContentEditor` policy,
  `AutoValidateAntiforgeryToken`):
  - `Index` (GET) — loads the singleton aggregate, renders one structured editor.
  - `Save` (POST) — binds `WarrantyPage` incl. `Callouts` list; upserts the singleton
    and **replaces** its callout rows (drops blank rows, re-indexes). Redirects to
    `Index` with `new { area = "Admin" }`.
- Service: `IAdminWarrantyService` / `AdminWarrantyService` (`GetAsync`,
  `SaveAsync`) mirroring `AdminHomeService` (`EnsureWarrantyAsync` singleton bootstrap).
- View (`Areas/Admin/Views/Warranty/Index.cshtml`): reuse `_LocalizedField` for
  bilingual fields, the `data-media-input`/`data-media-pick` + `_PickerModal` image
  picker for the two images, an add/remove rows control for callouts (mirror the
  promo-bullets UI), and a `Code = true` `_LocalizedField` for the brand-table HTML.
- **Per-vehicle booklet**: the existing vehicle admin
  (`Areas/Admin/Views/Vehicles/Edit.cshtml`) gains a `WarrantyBookletPdf` file/media
  field next to Brochure/Spec PDFs; `AdminVehicleService` update copies it.
- **Old `ContentPages` "warranty" entry**: redundant once this editor exists. Its
  admin Edit view shows a note linking to the new Warranty editor and hides the raw
  `BodyHtml` field for that slug (BodyHtml is no longer rendered for warranty).

## Testing (in-memory DB only — no prod contact)

- Mapping round-trip: `WarrantyPage` + `Callouts` + `Vehicle.WarrantyBookletPdf`.
- Service: get/ensure singleton; save upserts + replaces callouts (no duplicate,
  blanks dropped).
- Render (in-memory `WebApplicationFactory` + `InMemoryTestDb.Swap`): seed the
  warranty page + one visible vehicle with a booklet PDF → GET `/warranty` →
  assert a seeded structured marker, the vehicle's name in the grid, the booklet
  link, and the raw brand-table HTML all render; assert a vehicle without a PDF
  shows no booklet button.
- Vehicle service: `WarrantyBookletPdf` persists on update.
- All verification uses in-memory DB so it passes before the real-DB migration.

## Build order

1. Model + EF config + DbSets + migration (additive).
2. `ContentService`/load + seeders (write-only-when-empty) + Arabic.
3. Admin service + controller + view + nav; vehicle booklet field + vehicle admin.
4. Public `Warranty.cshtml` + `PageController` route + viewmodel.
5. Tests + adversarial review.

## Out of scope

- The per-vehicle warranty tab (`WarrantyLinks`) — unchanged.
- Curating/reordering the grid separately from the lineup (auto = all visible).
- Structuring the brand matrix table (kept as one HTML field by decision).
- Booklet PDFs themselves (field added + wired; PDFs uploaded later by the user).
