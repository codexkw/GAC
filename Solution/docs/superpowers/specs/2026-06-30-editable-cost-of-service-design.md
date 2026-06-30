# Editable Cost-of-Service Page — Design Spec

**Date:** 2026-06-30
**Repo:** codexkw/GAC · solution at `C:\Users\anas-\source\repos\GAC\Solution`
**Branch:** `feature/editable-cost-of-service` (off `main`)

## Goal

Convert `/cost-of-service` from a raw-HTML `ContentPage` blob into a structured, admin-editable singleton (like Warranty / Road Assistance), with a **structured price matrix** (no HTML): service-interval rows × car-model columns, edited via a grid with "+ Add car model" / "+ Add interval".

## Current state (verified)

`/cost-of-service` is a `ContentPage` (slug `cost-of-service`, visible) rendered as one raw-HTML blob via `Views/Content/Page.cshtml`. Content: title H1, a "Spare Parts Policy" button (`href="#"`), a `<table class="datatable">` matrix (21 interval rows × 18 model columns, cell = price string like `1,005`), and a footer `<p class="note">` of three lines. The Arabic seed translates the title, button, header ("النوع / الماركة") and interval labels; model names + prices are identical across languages.

## Design (decisions locked by user)

- **Table layout: models as columns** (keep current look). Intervals are the editable left-hand rows; "+ Add car model" adds a column.
- **Policy button: PDF upload OR external link** — one URL field with a media picker (pick a PDF → fills the field, or type any URL).
- Title, button label, interval labels, table header, footer = **bilingual EN/AR**. Model names + prices = single values (brand names / numbers; prices stored as **text** to preserve `1,005`, `—`, etc.).

### Data model (new)

`CostOfServicePage` (singleton):
- `Id`, `Title : LocalizedText`, `ButtonLabel : LocalizedText`, `ButtonUrl : string?` (PDF path or external URL), `TableHeadLabel : LocalizedText` (first-column header, e.g. "TYPE/Brand"), `FooterNote : LocalizedText` (multiline → lines)
- `Rows : List<CostServiceRow>` (interval rows), `Models : List<CostServiceModel>` (car-model columns)

`CostServiceRow`: `Id`, `CostOfServicePageId`, `Label : LocalizedText`, `SortOrder`.
`CostServiceModel`: `Id`, `CostOfServicePageId`, `Name : string`, `SortOrder`, `Cells : List<CostServiceCell>`.
`CostServiceCell`: `Id`, `CostServiceModelId`, `SortOrder`, `Value : string?` — **aligned by `SortOrder`/index to the page's ordered `Rows`** (no Row FK; the whole matrix is replaced wholesale on save, so index alignment is stable). Each model is normalized to exactly `Rows.Count` cells.

EF: `Page HasMany Rows` (cascade), `Page HasMany Models` (cascade), `Model HasMany Cells` (cascade). `OwnsLocalized` all `LocalizedText`. `Name`/`Value` plain strings (`Name` `HasMaxLength(120)`). Reads use **`.AsSplitQuery()`** (Page has two collections Rows+Models → avoid the cartesian explosion of `[[ef_multi_include_cartesian_explosion]]`). Migration **`AddCostOfServicePage`** (additive: 4 CreateTable).

### Services

- `IContentService.GetCostOfServicePageAsync()` → Include Rows (ordered), Models (ordered) ThenInclude Cells (ordered), `AsSplitQuery().AsNoTracking().FirstOrDefault()`.
- `IAdminCostOfServiceService` + impl: `GetAsync` (ensure + load singleton incl. ordered Rows/Models/Cells), `SaveAsync` (upsert page fields; RemoveRange existing Rows/Models/Cells; re-add **normalized**: drop blank rows/models, re-index, pad/truncate each model's cells to `Rows.Count`). Register in `Program.cs`.

### Seeder

`SeedCostOfServiceAsync` (write-only-when-empty): seed Title/Button/TableHead/Footer (EN+AR) + the 21 interval rows (EN+AR labels from the live page) + the 18 model columns with their price cells (from the seed HTML). `ButtonUrl` left null (`href="#"` today). Called in `SeedAsync` after `SeedRoadAssistanceAsync`.

### Admin

- `Areas/Admin/Controllers/CostOfServiceController` (Index GET, Save POST → redirect `{ area = "Admin" }`).
- `Areas/Admin/Views/CostOfService/Index.cshtml`: structured `_LocalizedField`s (Title, Button label, Table header, Footer) + button URL media-picker field + a **grid editor**: an HTML table where the first column holds each interval's EN/AR label inputs and a remove button; each subsequent column is a car model (header = name input + remove; body = a price input per interval). "+ Add car model" appends a column (name input + a price input in every row); "+ Add interval" appends a row (label inputs + a price input in every model column). JS reindexes `Rows[i]` / `Models[m].Cells[i]` to stay contiguous and aligned.
- `_AdminNav` link "Cost of Service".

### Public render + routing

- `Views/Content/CostOfService.cshtml` (model `CostOfServicePage`): reproduce the `cos-head` markup — crumb bar, H1 title, the policy button (`@page.ButtonLabel`, `href=@page.ButtonUrl`; render only when a URL is set), the `datatable` matrix (`<thead>` = TableHeadLabel + each model name; `<tbody>` = per interval row, label + each model's cell value by index), footer note (lines split on `\n`).
- `PageController.Show` special-cases `slug == "cost-of-service"` → load `GetCostOfServicePageAsync()` → render `CostOfService.cshtml`.
- `ContentPages/Edit.cshtml`: hide BodyHtml for `cost-of-service` (hidden inputs preserve it) + note linking to the Cost of Service editor.

## Constraints / non-goals

- Additive migration only; non-breaking for currently-deployed code. TDD with the in-memory harness; trailing-dot test namespaces; Admin in-memory classes run by explicit name (the `GAC.Tests.Admin` namespace contains prod-DB classes). See `[[gac_cms_pivot]]`, `[[gac_editable_offers_roadassist_branch]]`.
- Model names are free text (the table has legacy models — GS5/GA8/GN8 — not in the Vehicles list), **not** tied to CMS Vehicles.
- Prices are text, rendered verbatim (no numeric reformatting). The one allowed raw sink stays `@Html.Raw` only if needed for `<br>` in the footer — prefer splitting on `\n` into `<br>`-free paragraphs/lines.

## Testing (in-memory only)

- Mapping: page + rows + models + cells round-trip; cells aligned by SortOrder.
- Seeder: write-only-when-empty seeds 21 rows + 18 models with bilingual labels + price cells; idempotent.
- Admin service: upsert replaces matrix wholesale; `NormalizeMatrix` drops blank rows/models, re-indexes, pads each model to `Rows.Count` cells.
- Admin Save redirect into `/Admin`.
- Public render: GET `/cost-of-service` shows title, a model name, a known price at the right cell, footer line; Arabic cookie → AR title + AR interval label. `PageController` routes the slug to the structured view.
