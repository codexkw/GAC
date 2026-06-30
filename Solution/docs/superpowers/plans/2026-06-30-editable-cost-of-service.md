# Editable Cost-of-Service — Implementation Plan

> Implement task-by-task with TDD. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Convert `/cost-of-service` to a structured singleton editor with a price matrix (interval rows × model columns).

**Architecture:** New aggregate `CostOfServicePage` → `CostServiceRow` (intervals) + `CostServiceModel` (columns) → `CostServiceCell` (prices, index-aligned to rows). Full stack mirroring Warranty/Road-Assistance, plus a grid editor.

## Global Constraints

- Additive migration only (4 CreateTable; drops only in `Down()`). Non-breaking for deployed code.
- In-memory tests only. `~GAC.Tests.Content.` / `~GAC.Tests.Home.` (trailing dot) safe; run Admin in-memory classes by explicit name (Admin namespace has prod-DB classes).
- `OwnsLocalized` every `LocalizedText`. Reads `.AsSplitQuery()` (Page has 2 collections). Prices/model names = plain strings. Cells aligned by index to ordered Rows; matrix replaced wholesale on save.

---

### Task 1: models + config + DbSets + migration
**Files:** Create `GAC.Core/Content/CostOfServicePage.cs`, `CostServiceRow.cs`, `CostServiceModel.cs`, `CostServiceCell.cs`; Modify `ContentConfigurations.cs`, `ApplicationDbContext.cs`; Test `GAC.Tests/Content/CostOfServiceMappingTests.cs`.
- [ ] Failing test: save page with 2 rows, 2 models each with 2 cells; reload `Include(Rows).Include(Models).ThenInclude(Cells).AsSplitQuery()`; assert counts + a cell value + ordering.
- [ ] Run → FAIL. Create models; add `CostOfServicePageConfig` (OwnsLocalized Title/ButtonLabel/TableHeadLabel/FooterNote; `Property(ButtonUrl).HasMaxLength(500)`; HasMany Rows/Models cascade), `CostServiceRowConfig` (OwnsLocalized Label), `CostServiceModelConfig` (`Property(Name).HasMaxLength(120)`; HasMany Cells cascade), `CostServiceCellConfig` (`Property(Value).HasMaxLength(100)`); add 4 DbSets.
- [ ] Run → PASS. `dotnet ef migrations add AddCostOfServicePage`; verify Up() = 4 CreateTable, drops only in Down().
- [ ] Commit.

### Task 2: content-service read + seeder
**Files:** Modify `IContentService.cs`, `ContentService.cs`, `ContentSeeder.cs`, `GAC.Tests/FormsControllerTests.cs` (FakeContent); Test `GAC.Tests/Content/SeederCostOfServiceTests.cs`.
- [ ] Failing test: `SeedCostOfServiceAsync` (write-only-when-empty, idempotent) → 1 page, 21 rows, 18 models, each model 21 cells; EN+AR title/labels; a known cell (`GS4 Max`@`5,000 KM` = `525`). `GetCostOfServicePageAsync` returns it.
- [ ] Run → FAIL. Add interface method + impl (`Include` Rows/Models→Cells ordered, `AsSplitQuery().AsNoTracking()`); add `SeedCostOfServiceAsync` (data transcribed from `SeedContent/content/cost-of-service.html` + `ar/`), call after `SeedRoadAssistanceAsync`; add method to `FakeContent`.
- [ ] Run → PASS + build clean.
- [ ] Commit.

### Task 3: admin upsert service (+ NormalizeMatrix)
**Files:** Create `GAC.Core/Services/IAdminCostOfServiceService.cs`, `GAC.Infrastructure/Services/AdminCostOfServiceService.cs`; Modify `Program.cs`; Test `GAC.Tests/Admin/AdminCostOfServiceServiceTests.cs`.
- [ ] Failing tests: `GetAsync` on empty creates singleton; `SaveAsync` upserts (count stays 1), replaces matrix; `NormalizeMatrix` drops a blank row + blank model, re-indexes, pads a short model's cells to Rows.Count (and truncates a long one).
- [ ] Run → FAIL. Implement `EnsureAsync`/`GetAsync` (Include ordered, AsSplitQuery) + `SaveAsync` (upsert fields; RemoveRange existing Rows/Models/Cells; re-add normalized). `NormalizeMatrix`: keep rows with any non-blank label; keep models with non-blank Name OR any non-blank cell; re-index rows/models; each kept model → exactly `rows.Count` cells (pad with blank, truncate extra), re-index cells. Register in `Program.cs`.
- [ ] Run → PASS.
- [ ] Commit.

### Task 4: admin controller + grid view + nav
**Files:** Create `Areas/Admin/Controllers/CostOfServiceController.cs`, `Areas/Admin/Views/CostOfService/Index.cshtml`; Modify `_AdminNav.cshtml`; Test `GAC.Tests/Admin/AdminCostOfServiceRedirectTests.cs` (AdminInMemoryWebApplicationFactory).
- [ ] Failing test: GET `/Admin/CostOfService` 200; POST `/Admin/CostOfService/Save` (token + Title.En + one Rows[0].Label.En + one Models[0].Name + Models[0].Cells[0].Value) → 302 into `/Admin/` and persisted.
- [ ] Run → FAIL. Controller (Index/Save, redirect area Admin). Grid `Index.cshtml`: `_LocalizedField`s (Title, Button label, Table header, Footer multiline) + button-URL media field; a `<table>` grid — first column = interval label EN/AR inputs (`Rows[i].Label.En/.Ar`) + remove; one column per model (header `Models[m].Name` + remove; body `Models[m].Cells[i].Value`). JS: "+ Add car model" (append column: name + a cell input per existing row), "+ Add interval" (append row: label inputs + a cell input per existing model), remove handlers, reindex keeping `Rows[i]`/`Models[m].Cells[i]` contiguous & aligned. Nav link.
- [ ] Run → PASS.
- [ ] Commit.

### Task 5: public render + PageController route + ContentPages note
**Files:** Create `GAC.Web/Views/Content/CostOfService.cshtml`; Modify `PageController.cs`, `Areas/Admin/Views/ContentPages/Edit.cshtml`; Test `GAC.Tests/Home/CostOfServiceRenderTests.cs`.
- [ ] Failing test: GET `/cost-of-service` (seeded) → contains title, a model name (`GS4 Max`), the cell `525`, a footer line; Arabic cookie → AR title + AR interval label. PageController routes the slug.
- [ ] Run → FAIL. `CostOfService.cshtml`: crumb + H1 `Title.Localize()`; button rendered only when `ButtonUrl` set (`@page.ButtonLabel.Localize()`); `datatable` matrix (`<thead>`: TableHeadLabel + model names; `<tbody>`: per row, `Label.Localize()` + `model.Cells.ElementAtOrDefault(i)?.Value`); footer lines split on `\n`. PageController special-case after road-assistance. ContentPages Edit: extend hidden-body branch to `cost-of-service` with a note linking to the editor.
- [ ] Run → PASS.
- [ ] Commit.

### Finalization
- [ ] Full safe in-memory suite (Content. + Home. namespaces + explicit Admin classes) green; `dotnet build` clean.
- [ ] Adversarial workflow review (matrix save/render alignment, migration additivity, localization, edge cases) → fix confirmed findings.
- [ ] Push / apply migration / merge per user.

## Self-review
- Spec coverage: model+config+migration (T1), read+seed (T2), admin upsert+normalize (T3), grid editor+controller (T4), render+route (T5). ✓
- Type consistency: `CostOfServicePage`, `CostServiceRow/Model/Cell`, `GetCostOfServicePageAsync`, `IAdminCostOfServiceService`, `NormalizeMatrix`, `ButtonUrl`, `TableHeadLabel` used identically across tasks. ✓
- Cartesian-explosion guard (`AsSplitQuery`) on every multi-collection read. ✓
