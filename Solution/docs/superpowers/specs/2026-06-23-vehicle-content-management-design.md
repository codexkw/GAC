# Vehicle Content Management (no-HTML admin editing) — Design

- **Date:** 2026-06-23
- **Status:** Approved (brainstorming) — pending spec review, then implementation plan
- **Related:** [2026-06-15-vehicle-structured-sections-design.md](2026-06-15-vehicle-structured-sections-design.md) (the simplified structured editor this supersedes as the render path), [2026-06-21-admin-spec-pdf-and-action-dock-design.md](2026-06-21-admin-spec-pdf-and-action-dock-design.md)

## Problem

Each vehicle detail page (e.g. `/gs4`, `/emkoo`) is rendered from a single hand-crafted `Vehicle.BodyHtml` field — ~31–33 KB of HTML (EN + AR) transcribed from the original static GAC site. Editing that content requires writing HTML, which the **non-technical admin staff cannot and must not do**. A simplified structured editor (Hero/Features/Specs/Colours/Trims) exists but (a) covers only a subset of the real page's sections and (b) is wired all-or-nothing with `BodyHtml` (any single structured row hides the entire body — the footgun that recently blanked `/emzoom`).

We need non-technical staff to manage **all** car-page content through admin forms, with **zero HTML**, while preserving the existing rich, interactive design.

## Goal & scope

**Goal:** Non-technical admins edit the content of the existing ~11 car pages through structured admin forms — no HTML, no layout/structure editing.

**In scope:**
- A fixed, developer-owned master template (one shared layout — all cars already use it).
- Structured, per-car editable content for every section of that template (text EN/AR, images, PDFs, numbers).
- A one-time migration that carries the current page content into the new model.
- Retiring `BodyHtml` as the render path and removing the all-or-nothing footgun.

**Out of scope (for now):**
- Admin self-service creation of brand-new models (stays a developer task).
- Admin control over page structure: adding/removing/reordering whole sections, or inventing new section types (developer/design concern).
- Shared/global "brand" content — all sections are per-car editable (decision below).
- A full free-form page-builder or WYSIWYG over the whole body.

## Locked decisions (from brainstorming)

1. **Edit scope = content only, fixed design.** Admins change text/images/prices/specs/colours/trims/PDFs within the existing layout. Structure stays fixed.
2. **One shared template.** 9 of 10 seed pages are byte-for-byte structurally identical (4 features / 15 gallery shots / 4 stats / 3 tabs / 2 spec-toggles / 2 sliders / 3 cards / 9 sections); only `gn6` differs slightly (a manual clone). One template, one schema.
3. **Goal = edit existing models, no HTML.** New-model creation remains a dev task.
4. **All sections per-car editable.** No shared/global content, even where today's content is identical across cars.
5. **Approach A — field-mapped template** (chosen over token-overlay and WYSIWYG).

## Top-line acceptance criteria

- **AC1 — Data continuity (non-negotiable):** After cutover, opening any car in the admin shows **every field already populated with that car's current live content** (all text EN+AR, all images, gallery, stats, specs, colours, trims, PDFs). Admins never start from a blank form, and nothing currently live is lost.
- **AC2 — No HTML:** A non-technical editor can change any content on any car page using only labelled fields, number inputs, the media picker, and (for tab bodies only) a WYSIWYG. No raw HTML is ever required or exposed for normal editing.
- **AC3 — Design parity:** After migration each of the 11 pages renders visually identical to today (verified by section/marker counts + image `src`s + eyeball on a sample).
- **AC4 — Footgun removed:** No vehicle can be blanked by adding/leaving an empty structured section; `BodyHtml` all-or-nothing branching is gone.

## Approach A — field-mapped template

One developer-owned Razor template renders the fixed master design for every car, reading all content from a structured per-vehicle model. Admins edit that model through a sectioned form. `BodyHtml` is retired as the render path (kept as backup data).

### Architecture & reuse map

Most sections already have models, admin UI, and the media picker:

| Page section | Data source | Status |
|---|---|---|
| Hero image | `VehicleImage` (Kind=Hero) | exists |
| Hero heading / tagline / intro | `Vehicle.Name` / `Tagline` / `IntroText` | exists |
| Gallery (~15 zoom images) | `VehicleImage` (Kind=Gallery) | exists (add/remove/reorder + picker) |
| Feature blocks (×4) | `FeatureSection` | exists |
| Specifications (toggles) | `SpecGroup` / `SpecRow` | exists |
| Colours | `ColorOption` | exists |
| Trims + per-trim PDF | `Trim` | exists |
| Spec / Brochure PDF | `Vehicle.SpecPdf` / `BrochurePdf` | exists |
| Performance stats (×4) | **new `StatItem`** | build |
| Tabbed content (×3 blocks) | **new `TabItem`** | build |
| Sliders / slides | **new `SliderSlide`** | build |
| Cards (×3) | **new `CardItem`** | build |
| Section headings (×9) | **new `SectionHeading`** | build |

**Three workstreams:** (1) model + admin for the new types; (2) render template reproducing the real master design; (3) one-time migration.

### Data model

Reused unchanged: `VehicleImage` (Hero+Gallery), `FeatureSection`, `SpecGroup`/`SpecRow`, `ColorOption`, `Trim`, `Vehicle.SpecPdf`/`BrochurePdf`/`Name`/`Tagline`/`IntroText`.

New child entities of `Vehicle` (same pattern as existing: `int Id`, `int VehicleId`, `LocalizedText` owned EN/AR, `string? ImagePath`, `int SortOrder`, `IOrderable`):

```csharp
public class StatItem : IOrderable        // 4 performance stats
{ int Id; int VehicleId; LocalizedText Value; LocalizedText Label; int SortOrder; }

public class CardItem : IOrderable         // 3 cards
{ int Id; int VehicleId; LocalizedText Title; LocalizedText Text;
  string? ImagePath; string? LinkUrl; int SortOrder; }

public class SliderSlide : IOrderable      // slides; GroupKey = which slider
{ int Id; int VehicleId; int GroupKey;
  LocalizedText Eyebrow; LocalizedText Title; LocalizedText Caption;
  string? ImagePath; int SortOrder; }

public class TabItem : IOrderable          // tabbed content; GroupKey = which tab block
{ int Id; int VehicleId; int GroupKey;
  LocalizedText Label; LocalizedText Heading; LocalizedText Body; // Body = sanitized rich-text-lite
  string? ImagePath; int SortOrder; }

public class SectionHeading                // the 9 mp-section headers
{ int Id; int VehicleId; SectionKey Key;   // enum: Features, Gallery, Stats, Tabs, Specs, Colours, Trims, Sliders, Cards
  LocalizedText Title; LocalizedText Sub; LocalizedText Body; }
```

Hang off `Vehicle` as `List<StatItem> Stats`, `List<CardItem> Cards`, `List<SliderSlide> Slides`, `List<TabItem> Tabs`, `List<SectionHeading> Headings`. `VehicleService.GetBySlugAsync` gains five `.Include(...)`s.

Notes:
- All bilingual text via `LocalizedText` (`_En`/`_Ar` owned columns) — identical to every existing field, so EN/AR works automatically in admin + renderer.
- `GroupKey` lets a flat list belong to the correct one of the 2 sliders / 3 tab blocks without extra parent tables.
- `TabItem.Body` is the only formatted field → existing **Trix** WYSIWYG, sanitized via existing `HtmlSanitizerService`.
- **The exact field list of the 4 new types is finalized by reading the master HTML during planning.** Shapes above are the working model and may gain/lose a field per real binding points.

### Admin editing experience

Extends the existing `Areas/Admin/Views/Vehicles/Edit.cshtml` screen (which already hosts Images/Features/Specs/Colours/Trims panels + shared media `_PickerModal`).

- New partials `_Stats`, `_Cards`, `_Sliders`, `_Tabs`, `_SectionHeadings`, placed **in the same order as the live page**, each a **collapsible panel** with a sticky section-nav (50+ fields per car — keep it navigable).
- New partials reuse the exact `_Colors`/`_Trims` patterns: list of rows with **↑ ↓ reorder**, **✕ remove**, an **"Add …"** form; EN/AR via `_LocalizedField`; every image/PDF field is the **"Choose…" media picker**.
- **Per-section immediate save** through dedicated controller actions (like `AddImage`/`AddColor` today) — no giant all-or-nothing submit; basics (Name/Tagline/PDFs) keep the main **Save**.
- Only **Tabs** carry light formatting → Trix sub-editor like Features. Everything else is plain text / number / image.

Backend (mirrors existing): extend `IAdminVehicleService`/`AdminVehicleService` with CRUD for the new types; extend `Areas/Admin/Controllers/VehiclesController` with parallel `Add*/Remove*/Move*` actions (`[Authorize(ContentEditor)]`); reuse `MediaController` + `_PickerModal` unchanged.

### Migration

**1. Schema migration** — one EF migration `AddVehicleRichSections` creates the five new tables. Applied to the shared prod DB (`83.229.86.221/GAC`) with a **guarded, hand-scoped SQL script** + the `__EFMigrationsHistory` row — never `dotnet ef database update`/full idempotent script (prod history-gap rule). Purely additive (existing structured tables are empty for these 11 cars today).

**2. Content extraction (parser)** — a deliberate one-off utility in `GAC.Infrastructure` (like `ContentSeeder`; run on demand, not on startup):
- For each vehicle, read `BodyHtml_En` **and** `BodyHtml_Ar`, parse with AngleSharp. One set of selectors works for all because the structure is identical (`mp-feature`, `mp-gshot`, `mp-stat`, `mp-tabs`, `mp-stoggle`, `mp-slider`, `mp-card`, `mp-head`). Extract hero, 9 headings, 4 features, ~15 gallery images, 4 stats, tabs, spec toggles, sliders, cards, colours, trims → structured model, EN + AR.
- Re-runnable per vehicle (clear + rebuild that car's rows) so we iterate to perfect parity.
- `gn6` (structural outlier) gets a tweak or a quick manual pass.

**Parity verification** — compare section/marker counts (4 features, 15 gallery, 4 stats, 3 tabs, …) and image `src`s between new render and original `BodyHtml`; eyeball 2–3 cars; an integration test asserts each migrated car renders 200 with expected markers.

**Sequencing & safety:**
- Order: **build full template → extract data → cut render path over.** Don't flip until the template is ready and parity passes (otherwise cars briefly render the old simplified structured view).
- `BodyHtml` columns **kept as backup** (not dropped) — current content never destroyed; can re-parse or fall back.
- Migration is additive against the shared DB (new tables only); no destructive mutation of existing data.

### Render & cutover

- One comprehensive set of Razor partials reproduces the real master design (hero → section heads → features → gallery+zoom → stats → tabs → specs → sliders → cards → colours → trims → enquiry), driven entirely by the model, emitting the same `mp-*` classes the existing site JS/CSS already hooks (tabs, sliders, gallery zoom preserved). **Replaces** the current simplified structured partials.
- Vehicles always render this template; the all-or-nothing `HasStructuredContent` branch is **removed** (footgun retired). Nothing depends on `BodyHtml` anymore, but the column stays as backup.

### Error handling

- Missing optional image/section renders nothing (no broken `<img>`).
- Required basics (name, hero) validated in admin.
- `TabItem.Body` sanitized server-side via existing `HtmlSanitizerService`.
- Arabic falls back to English when an AR field is blank (existing `Localize()` behaviour).

### Testing

- Unit tests for new render helpers and admin CRUD.
- Integration tests: each of the 11 cars renders 200 with correct marker counts (parity check kept as a permanent regression guard, building on `VehiclePagesTests`).
- Migration test: extracted field values non-empty for every car (enforces AC1).

## Open items to finalize during planning

- Exact field shapes of `StatItem`/`TabItem`/`SliderSlide`/`CardItem` and the `SectionKey` set, read from the master HTML.
- `gn6` outlier handling (parser tweak vs manual).
- Whether to keep a hidden dev-only raw-HTML escape hatch after cutover, or remove the body editor entirely from the admin.

## Future (not now)

- Admin self-service new-model creation (clone template + fill form).
- Optionally promoting truly-identical sections to shared/global settings (currently per-car by decision).
