# Structured Warranty Brand Table — Design

**Date:** 2026-07-01
**Repo:** codexkw/GAC · Solution at `C:\Users\anas-\source\repos\GAC\Solution`
**Status:** Approved (design), pending implementation plan

## Goal

Replace the raw-HTML **"Brand table (HTML)"** field in `/Admin/Warranty` (the Extended
Warranty section) with a structured, click-+-to-add grid editor — bilingual cells,
editable bilingual column headers, and a per-brand policy link/PDF — and render it as a
real `<table>` on the public `/warranty` page. Mirrors the cost-of-service editor pattern.

## Background — current state

`WarrantyPage.ExtendedTableHtml` is a `LocalizedText` holding a raw HTML `<table>` string
(English only; the Arabic side is empty). The admin edits it as a code textarea; the public
view renders it with `@Html.Raw(...)`. The table is brand-per-row:

| Brand | Manufacturer Warranty | Mfr. Roadside Assistance | Extended Warranty | Extended Roadside Assistance | View Policy |
|---|---|---|---|---|---|
| GAC | 5 Years and/or 150,000 KM | — | +2 Years / +Unlimited Mileage | +2 Years / +Unlimited Mileage | Click Here (`#`) |
| Chevrolet | 3 Years and/or 100,000 KM | 3 Years and/or 100,000 KM | +2 Years / +50,000 KM | +2 Years / +50,000 KM | Click Here (`#`) |
| GMC | 3 Years and/or 100,000 KM | 3 Years and/or 100,000 KM | +2 Years / +50,000 KM | +2 Years / +50,000 KM | Click Here (`#`) |
| Cadillac | 4 Years and/or 100,000 KM | 4 Years and/or 100,000 KM | +1 Year / +50,000 KM | +1 Year / +50,000 KM | Click Here (`#`) |

Cells may contain `<br>` line breaks. The last column is a per-brand link (all `#` today).

## Decisions (locked with the user)

1. **Fixed columns, dynamic brand rows.** The 5 warranty-attribute columns are fixed;
   the admin adds/removes brand ROWS. Chosen over a fully-dynamic matrix because the last
   column is a typed link, not free text.
2. **Policy column = per-brand link OR PDF.** Each brand row has a `PolicyUrl` — paste a
   link or pick an uploaded PDF (same media picker as cost-of-service). Renders only when set.
3. **Editable bilingual column headers.** All 6 header labels are stored on the page and
   editable in both languages (the Arabic side is missing today).
4. **Cells are bilingual** (`LocalizedText`); **brand names are plain text** (proper nouns,
   like cost-of-service model names).

## Architecture

Additive-only. Follows the singleton-page + child-collection pattern already used by
`WarrantyPage`/`WarrantyCallout` and `CostOfServicePage`.

### Data model

New child entity **`WarrantyBrandRow`** (`GAC.Core/Content/WarrantyBrandRow.cs`):

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` | identity |
| `WarrantyPageId` | `int` | FK → `WarrantyPage` |
| `Brand` | `string` (≤120) | plain text, not localized |
| `ManufacturerWarranty` | `LocalizedText` | owned, bilingual, multi-line |
| `ManufacturerRoadside` | `LocalizedText` | owned |
| `ExtendedWarranty` | `LocalizedText` | owned |
| `ExtendedRoadside` | `LocalizedText` | owned |
| `PolicyUrl` | `string?` (≤500) | link or PDF path |
| `SortOrder` | `int` | display order |

**`WarrantyPage`** gains:
- `List<WarrantyBrandRow> BrandRows`
- Six editable bilingual headers (`LocalizedText`): `TableBrandHeader`,
  `TableMfrWarrantyHeader`, `TableMfrRoadsideHeader`, `TableExtWarrantyHeader`,
  `TableExtRoadsideHeader`, `TablePolicyHeader`.

The existing `ExtendedTableHtml` property/column **stays in place, unused** after redeploy.
Keeping it makes the migration purely additive so currently-deployed code (which still reads
it) never breaks. Dropping it is an optional later cleanup migration, out of scope here.

### Persistence & services

- `ContentConfigurations.cs`: add `WarrantyBrandRowConfig` (`OwnsLocalized` ×4;
  `Brand` `HasMaxLength(120)`; `PolicyUrl` `HasMaxLength(500)`). In `WarrantyPageConfig`:
  `HasMany(w => w.BrandRows).WithOne().HasForeignKey(r => r.WarrantyPageId)
  .OnDelete(DeleteBehavior.Cascade)` and `OwnsLocalized` for the 6 headers.
- `ApplicationDbContext`: add `DbSet<WarrantyBrandRow> WarrantyBrandRows`.
- **`WarrantyPage` now has two collections (Callouts + BrandRows)** → both read paths use
  **`.AsSplitQuery()`** to avoid the cartesian-explosion timeout:
  - `ContentService.GetWarrantyPageAsync` — add `Include(BrandRows ordered)` + `AsSplitQuery()`.
  - `AdminWarrantyService.GetAsync` — add `Include(BrandRows ordered)` + `AsSplitQuery()`.
- `AdminWarrantyService.SaveAsync`: map the 6 header `LocalizedText` fields;
  **replace `BrandRows` wholesale** (`_db.WarrantyBrandRows.RemoveRange(existing.BrandRows)`
  then assign the normalized list — owned `LocalizedText` are deleted with their owner).
- `NormalizeBrandRows`: drop rows where `Brand` AND all four cells AND `PolicyUrl` are blank;
  re-index `SortOrder`. Mirrors `NormalizeCallouts` / cost-of-service `Normalize`.

### Seeding (backfill onto the already-seeded prod page)

`SeedWarrantyAsync` is guarded on `WarrantyPages.AnyAsync()`, so on prod (page already exists)
it returns early and will not populate the new structures. A **separate**
`SeedWarrantyBrandRowsAsync`, guarded on **`WarrantyBrandRows.AnyAsync()`**, backfills the
existing page:
- AngleSharp-parses the canonical `ExtendedTableHtml` English string (the same constant the
  current seeder writes) → 4 `WarrantyBrandRow`s. `<br>` → `\n`; English cells filled; Arabic
  cells left empty for the admin to translate. `PolicyUrl` left null.
- Sets the 6 header labels: English from the parsed `<th>` text; Arabic from known
  translations supplied in the seeder.

Called from `SeedAsync` after `SeedWarrantyAsync`. Write-only-when-empty; idempotent.

### Frontend — `Views/Content/Warranty.cshtml`

Replace the `@Html.Raw(w.ExtendedTableHtml.Localize())` block with a structured table:
- `<table class="datatable datatable--matrix">`; `<thead>` from the 6 header labels
  (`.Localize()`).
- One `<tbody>` row per `BrandRows` item: brand name, then the 4 cells. Multi-line cells split
  on `\n` and join with `<br />` (same helper shape as the cost-of-service footer).
- Policy cell: render `<a ... target="_blank" rel="noopener">` with the **link text** a
  localized `"Click Here"` (the `TablePolicyHeader` is the column header, separate from the
  link text) **only when `PolicyUrl` is a safe scheme** — starts with `/`, `#`, `http://`, or
  `https://`. Otherwise render an empty cell. (Same guard as cost-of-service, so an
  admin-supplied `javascript:`/`data:` URL can't become clickable.)

### Admin editor — `Areas/Admin/Views/Warranty/Index.cshtml`

Replace the single `_LocalizedField` (`ExtendedTableHtml`, `Code = true`) with:
1. **Six bilingual header inputs** (En + Ar) for the column labels.
2. A **per-brand card repeater** (`data-brand-row`), each card containing:
   - `BrandRows[i].Brand` text input.
   - A 2-column grid of the four attributes, each with En input + Ar input
     (`dir="rtl"`), multi-line (`<textarea>`), bound to `BrandRows[i].ManufacturerWarranty.En`
     etc.
   - `BrandRows[i].PolicyUrl` input + media picker (`data-media-input` / `data-media-pick`,
     reusing `_PickerModal` already on the page).
   - A remove button.
3. An **"Add brand row"** button cloning a `<template>` card.
4. JS that re-indexes every `BrandRows[i].*` name contiguously on add/remove — same approach
   as the existing callouts repeater in this view (extended to the larger field set).

### Migration

`AddWarrantyBrandTable`:
- `CreateTable WarrantyBrandRows` with FK to `WarrantyPages` and inline owned-`LocalizedText`
  columns (`ManufacturerWarranty_En`, `ManufacturerWarranty_Ar`, …).
- `AddColumn` ×12 on `WarrantyPages` for the 6 headers (`TableBrandHeader_En`, `_Ar`, …).

Additive only — no drops/alters. Safe to apply to prod before the Web redeploy: live code keeps
reading `ExtendedTableHtml`; the new columns/table are simply unused until the new build ships.

## Testing (TDD)

In-memory EF (`InMemoryTestDb` / `UseInMemoryDatabase`); render tests via
`WebApplicationFactory<Program>` with `UseEnvironment("Development")` + `InMemoryTestDb.Swap`.

- **`WarrantyBrandMappingTests`** (`GAC.Tests/Content/`) — config round-trips a page with
  brand rows (owned cells) and the 6 headers through a real save/reload.
- **`SeederWarrantyBrandTests`** (`GAC.Tests/Content/`) — `SeedWarrantyBrandRowsAsync` parses
  the canonical table: 4 rows (GAC, Chevrolet, GMC, Cadillac); GAC `ManufacturerWarranty.En`
  == `"5 Years and/or 150,000 KM"`; Chevrolet `ExtendedWarranty.En` contains `"+2 Years"`;
  multi-line cell preserved as two lines; 6 headers set (EN + AR non-empty).
- **`AdminWarrantyBrandServiceTests`** (`GAC.Tests/Admin/`) — Save upserts and replaces brand
  rows wholesale (old rows not orphaned); `NormalizeBrandRows` drops fully-blank rows and
  re-indexes; `PolicyUrl` and the 6 headers persist.
- **`WarrantyBrandRenderTests`** (`GAC.Tests/Home/`) — public `/warranty` renders the
  structured table from the DB (brand names, header labels, a cell value); the policy link
  renders for `https://`/relative but **not** for `javascript:`; Arabic localization asserted
  after `WebUtility.HtmlDecode` (Razor encodes non-ASCII `.Localize()` output to numeric
  entities).

**Test-filter gotcha:** the `GAC.Tests.Admin` namespace contains classes whose factories boot
the real prod DB. Run the new admin in-memory class by **explicit class name**, never the whole
`GAC.Tests.Admin` namespace.

## Global constraints

- .NET 9 / EF Core 9; SQL Server (prod), EF InMemory (tests). Pin `Microsoft.*` to `9.0.*`.
- Bilingual `LocalizedText{En,Ar}` via the `OwnsLocalized` helper; cookie-driven culture.
- Additive migrations only; apply to prod via a scoped idempotent script
  (`ef migrations script <last-applied> <new> --idempotent`), strip the UTF-8 BOM, prepend
  `SET XACT_ABORT ON;`, run via sqlcmd from inside the script dir with a relative filename.
  Never `dotnet ef database update`.
- No secrets in committed files.

## Out of scope (YAGNI)

- Dynamic add/remove of columns (columns are fixed).
- Dropping the `ExtendedTableHtml` column now (optional later cleanup).
- Per-cell rich text / WYSIWYG (plain multi-line text only).
