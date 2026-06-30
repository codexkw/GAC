# Editable Offers + Road-Assistance — Implementation Plan

> **For agentic workers:** implement task-by-task with TDD. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Wire `/offers` to the existing admin Offers data (+ a Button-name field + Arabic chrome), and convert `/road-assistance` into a structured admin editor like the Warranty page.

**Architecture:** Offers = mostly view/seed/resource plumbing on the existing `Offer` entity (one new owned field). Road-assistance = new singleton aggregate (`RoadAssistancePage`) mirroring `WarrantyPage` end-to-end (model → config → DbSet → migration → seeder → content service → admin service/controller/view → public render → PageController route).

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9, xUnit (InMemory harness), Razor, `LocalizedText{En,Ar}` owned types, `IHtmlLocalizer<SharedResource>`.

## Global Constraints

- Additive migrations only; drops only in `Down()`. Two migrations: `AddOfferButtonLabel`, `AddRoadAssistancePage`.
- New columns/table must be non-breaking for the currently-deployed code.
- Tests in-memory only. Use trailing-dot namespaces (`~GAC.Tests.Content.`, `~GAC.Tests.Home.`, `~GAC.Tests.Admin.`) + explicit safe class names; NEVER run the prod-DB smoke classes (`ContentPagesTests`, `HomePageSmokeTests`, `*WebApplicationFactory` redirect tests that boot the real DB).
- `OwnsLocalized(...)` for every `LocalizedText`. Singleton pattern = `EnsureAsync` + upsert.
- Button link fixed `/request-a-quote`; no badge/image on offer cards.

---

## PART A — Offers

### Task A1: `Offer.ButtonLabel` field + config + migration

**Files:** Modify `GAC.Core/Content/Offer.cs`, `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (`OfferConfig`); Test `GAC.Tests/Content/OfferMappingTests.cs` (new).

- [ ] **Step 1 — failing test** `GAC.Tests/Content/OfferMappingTests.cs`: with `InMemoryTestDb.Swap`, add an `Offer` with `ButtonLabel = { En="Enquire Now", Ar="استفسر الآن" }`, save, reload `AsNoTracking`, assert both values round-trip.
- [ ] **Step 2** Run it → FAIL (no `ButtonLabel`).
- [ ] **Step 3** Add `public LocalizedText ButtonLabel { get; set; } = new();` to `Offer`; add `b.OwnsLocalized(o => o.ButtonLabel);` to `OfferConfig`.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** `dotnet ef migrations add AddOfferButtonLabel -p GAC.Infrastructure -s GAC.Web`. Verify the generated `Up()` only does `AddColumn ButtonLabel_En`/`ButtonLabel_Ar` (nullable nvarchar) on `Offers`; drops only in `Down()`.
- [ ] **Step 6** Commit.

### Task A2: admin Edit form + service mapping

**Files:** Modify `GAC.Web/Areas/Admin/Views/Offers/Edit.cshtml`, `GAC.Infrastructure/Services/AdminOfferService.cs`; Test `GAC.Tests/Admin/AdminOfferServiceTests.cs` (new).

- [ ] **Step 1 — failing test**: `AdminOfferService.UpdateAsync` persists a changed `ButtonLabel` (create offer, update with new ButtonLabel, reload, assert).
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** In `UpdateAsync` add `e.ButtonLabel = a.ButtonLabel;`. In `Edit.cshtml` add a `_LocalizedField` for `ButtonLabel` (`NameEn="ButtonLabel.En"`, `NameAr="ButtonLabel.Ar"`) after the Body field.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

### Task A3: seed the 6 real offers + retire the placeholder

**Files:** Modify `GAC.Infrastructure/Data/ContentSeeder.cs` (`SeedOffersAsync`); Test `GAC.Tests/Content/SeederOffersTests.cs` (new).

- [ ] **Step 1 — failing tests** (3): (a) empty DB → `SeedOffersAsync` yields 6 active offers with EN+AR titles and button labels; (b) DB with only the legacy `current-offers` (blank body) → after seed, placeholder gone and 6 present; (c) DB with a real admin offer (slug `my-deal`) → seed is a no-op (still 1, untouched).
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Rewrite `SeedOffersAsync`: load offers; if exactly one row, slug `current-offers`, blank `Body.En` → remove + SaveChanges; `if (await db.Offers.AnyAsync()) return;` then add the 6 cards (slugs `finance-gs8, cashback-empow-r, aion-es-launch, trade-in-emzoom, service-package, business-fleet`, EN+AR Title/Body, `ButtonLabel={En="Enquire Now",Ar="استفسر الآن"}`, SortOrder 1–6, IsActive true).
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

### Task A4: wire the public view + localize chrome

**Files:** Modify `GAC.Web/Views/Offers/Index.cshtml`, `GAC.Web/Resources/SharedResource.ar.resx`; Test `GAC.Tests/Home/OffersRenderTests.cs` (new).

- [ ] **Step 1 — failing tests**: using `WebApplicationFactory<Program>` + `InMemoryTestDb.Swap` (seed 2 offers), GET `/offers` → body contains each offer's title + its button label and NOT the old hardcoded "0% APR on GS8". Arabic request (cookie `c=ar|uic=ar`) → contains `أحدث العروض`.
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Replace the hardcoded grid/hero/CTA in `Index.cshtml` with `@L[...]` chrome + `@foreach (var o in Model)` cards (title, body, button → `/request-a-quote`, label `ButtonLabel.Localize()` || `L["Enquire Now"]`) + empty-state. Add the missing Arabic keys to `SharedResource.ar.resx`.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

---

## PART B — Road-assistance

### Task B1: `RoadAssistancePage` model + config + DbSet + migration

**Files:** Create `GAC.Core/Content/RoadAssistancePage.cs`; Modify `ContentConfigurations.cs`, `ApplicationDbContext.cs`; Test `GAC.Tests/Content/RoadAssistanceMappingTests.cs` (new).

- [ ] **Step 1 — failing test**: save a `RoadAssistancePage` with all fields (incl. `PhoneNumber`, `CallButtonLabel` EN/AR), reload `AsNoTracking`, assert round-trip.
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Create the model (per spec); add `RoadAssistancePageConfig` (`OwnsLocalized` ×4, `Property(PhoneNumber).HasMaxLength(40)`); add `DbSet<RoadAssistancePage> RoadAssistancePages`.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** `dotnet ef migrations add AddRoadAssistancePage`. Verify `Up()` = one `CreateTable RoadAssistancePages` (Id PK identity, 8 nvarchar cols + PhoneNumber); drop only in `Down()`.
- [ ] **Step 6** Commit.

### Task B2: content-service read + seeder

**Files:** Modify `GAC.Core/Services/IContentService.cs`, `GAC.Infrastructure/Services/ContentService.cs`, `GAC.Infrastructure/Data/ContentSeeder.cs`, `GAC.Tests/FormsControllerTests.cs` (FakeContent); Test `GAC.Tests/Content/SeederRoadAssistanceTests.cs` (new).

- [ ] **Step 1 — failing test**: `SeedRoadAssistanceAsync` write-only-when-empty → seeds one row with EN heading "Roadside Assistance", `PhoneNumber="1833334"`, AR heading non-empty; second call is a no-op.
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Add `Task<RoadAssistancePage?> GetRoadAssistancePageAsync()` to interface + impl (`AsNoTracking().FirstOrDefault()`); add `SeedRoadAssistanceAsync` (guarded) with EN+AR content and call it in `SeedAsync` after `SeedWarrantyAsync`; add the method to `FakeContent` in `FormsControllerTests`.
- [ ] **Step 4** Run → PASS (and `dotnet build` clean — interface change compiles).
- [ ] **Step 5** Commit.

### Task B3: admin service

**Files:** Create `GAC.Core/Services/IAdminRoadAssistanceService.cs`, `GAC.Infrastructure/Services/AdminRoadAssistanceService.cs`; Modify `Program.cs`; Test `GAC.Tests/Admin/AdminRoadAssistanceServiceTests.cs` (new).

- [ ] **Step 1 — failing test**: `GetAsync` on empty DB creates+returns the singleton; `SaveAsync` upserts (change Heading + PhoneNumber, reload, assert; second save updates same row, count stays 1).
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Implement interface + `AdminRoadAssistanceService` (`EnsureAsync`, `GetAsync`, `SaveAsync` upsert-all-fields). Register `AddScoped<IAdminRoadAssistanceService, AdminRoadAssistanceService>()` in `Program.cs`.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

### Task B4: admin controller + view + nav

**Files:** Create `GAC.Web/Areas/Admin/Controllers/RoadAssistanceController.cs`, `GAC.Web/Areas/Admin/Views/RoadAssistance/Index.cshtml`; Modify `_AdminNav.cshtml`; Test `GAC.Tests/Admin/AdminRoadAssistanceRedirectTests.cs` (new, `AdminInMemoryWebApplicationFactory`).

- [ ] **Step 1 — failing test**: POST `/Admin/RoadAssistance/Save` (authed test client, antiforgery) → 302 to `/Admin/RoadAssistance` and the row persisted.
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** Controller (Index GET, Save POST → `RedirectToAction(Index, new{area="Admin"})`); structured `Index.cshtml` (`_LocalizedField`s for Heading/Intro(multiline)/ContactLead/ContactText/CallButtonLabel + a plain `<input name="PhoneNumber">`); add `<a href="/Admin/RoadAssistance">Road Assistance</a>` to `_AdminNav`.
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

### Task B5: public render + PageController route + ContentPages note

**Files:** Create `GAC.Web/Views/Content/RoadAssistance.cshtml`; Modify `GAC.Web/Controllers/PageController.cs`, `GAC.Web/Areas/Admin/Views/ContentPages/Edit.cshtml`; Test `GAC.Tests/Home/RoadAssistanceRenderTests.cs` (new).

- [ ] **Step 1 — failing test**: GET `/road-assistance` (InMemory, seeded) → body contains the heading, an intro paragraph, and `href="tel:1833334"`; Arabic cookie → AR heading.
- [ ] **Step 2** Run → FAIL.
- [ ] **Step 3** `RoadAssistance.cshtml` (model `RoadAssistancePage`): `cos-head` section, heading, intro paragraphs split on `\n`, bold `ContactLead`, `ContactText`, `.op-btns` with `<a class="btn btn--hero" href="tel:@digits">@CallButtonLabel.Localize()</a>`. In `PageController.Show`, after the warranty special-case add `if (content.Slug == "road-assistance") return View("~/Views/Content/RoadAssistance.cshtml", await _content.GetRoadAssistancePageAsync() ?? new RoadAssistancePage());`. In `ContentPages/Edit.cshtml` extend the warranty branch to also match `road-assistance` (hidden BodyHtml inputs + note linking to the Road Assistance editor).
- [ ] **Step 4** Run → PASS.
- [ ] **Step 5** Commit.

---

## Finalization

- [ ] Run the full safe in-memory suite (trailing-dot namespaces + explicit classes) → all green.
- [ ] `dotnet build` clean.
- [ ] Push branch; update memory; report (migrations to apply: `AddOfferButtonLabel`, `AddRoadAssistancePage`).

## Self-review notes

- Spec coverage: offers field+view+seed+chrome (A1–A4); road-assistance full stack (B1–B5). ✓
- Type consistency: `ButtonLabel`, `RoadAssistancePage`, `GetRoadAssistancePageAsync`, `IAdminRoadAssistanceService` used identically across tasks. ✓
- Placeholder retire logic guarded narrowly (count==1 && slug==current-offers && blank body). ✓
