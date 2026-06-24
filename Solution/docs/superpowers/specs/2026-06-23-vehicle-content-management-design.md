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

### Real section inventory (revised after reading `emkoo.html` end-to-end during planning)

The master page is **13 section types** — richer than the first-pass model. Section order is taken from the master HTML's `section.mp-section` ids (+ interleaved `.mp-slider-wrap` and `.mp-enquiry`). **All 13 are in scope for this comprehensive build.**

| # | Section (page order) | DOM hooks | Data source | Status |
|---|---|---|---|---|
| 1 | Hero | `.mp-hero` | `VehicleImage`(Hero) + `Vehicle.Name`/`Tagline` | reuse |
| 2 | Section headings (×8) | `.mp-head` keyed by parent `section[id]` | **new `SectionHeading`** | build |
| 3 | Overview stats (×4, value carries units) + note | `.mp-stats > .mp-stat`, `.mp-note` | **new `StatItem`** (+ note field) | build |
| 4 | Sliders (×2: eyebrow+title + N slides) | `.mp-slider-wrap > .mp-slider[data-slider]` | **new `SliderGroup` + `SliderSlide`** | build |
| 5 | Design tabs → 3 feature panels (image, title, lead, bullet list) | `.mp-tabs[data-tabs-wrap]` keys d1/d2/d3 → `.mp-feature` | **extend `FeatureSection`** (+`GroupKey`,+`TabLabel`,+`Lead`,+`FeatureBullet` children) | build |
| 6 | Gallery tabs → 3 galleries (15 zoom images) + lightbox | `.mp-tabs` keys gex/gin/gte → `.mp-gpanel > .mp-gallery > a.mp-gshot` | **new `GalleryTab` + `GalleryImage`** + one `[data-lightbox]` singleton | build |
| 7 | Quality / awards | `.mp-quality` (main+thumb img, strapline, content) | **new `QualityBlock`** (0/1 per car) | build |
| 8 | Technology (banner + 3 cards) | `.mp-tech-banner img`, `.mp-cards > .mp-card` | **new `CardItem`** + `TechBanner` field (cards have NO link) | build |
| 9 | Performance tabs → 3 feature panels | `.mp-tabs` keys p1/p2/p3 → `.mp-feature` | same as #5, `GroupKey=Performance` | build |
| 10 | Safety toggles (×3: title + image + strap + paragraph) | `.mp-stoggles > .mp-stoggle` | **new `SafetyToggle`** | build |
| 11 | Trims (model, name, price text-rows, 2 CTAs) | `.mp-trims > .mp-trim` | **rework `Trim`** (+`ModelLabel`,+`ImagePath`, price as `TrimPriceRow` children; keep `SpecPdf`) | build |
| 12 | Warranty (document/PDF links) | `.mp-warranty__links a.btn--doc` | **new `WarrantyLink`** | build |
| 13 | Enquiry (bg image + title/sub/lead; form stays static) | `.mp-enquiry` (`__title`/`__sub`/`__lead`, inline bg style) | **new fields on `Vehicle`** (form handled by FormsController) | build |

**Corrections to the first-pass model (important):**
- There is **no label/value specifications table** anywhere on the real page. The safety section is **media toggles** (`mp-stoggle`: title + image + strap + paragraph), modelled by the new `SafetyToggle`. The existing `SpecGroup`/`SpecRow` entities (from the simplified structured editor) **do not map to any real section** and are not used by this render path. The "Specifications" the user sees is the per-trim **PDF** CTA, not an on-page table.
- `FeatureSection` is **extended**, not reused as-is: real feature panels are tab-grouped (Design vs Performance) and carry a tab label, an optional lead paragraph, and a bullet list (each bullet = bold label + description) — modelled with `+GroupKey`, `+TabLabel`, `+Lead`, and a `FeatureBullet` child collection (the bullets are discrete fields, not HTML).
- Galleries are **tab-grouped** (3 tabs) and feed a JS **lightbox singleton** that is *not* in `_Layout` today — the render must emit exactly one `<div data-lightbox>` per page.
- `Trim.Price` (single decimal) is replaced by ordered **price text-rows** (`TrimPriceRow`), matching the real `<ul>` of "Price/VAT/Total" lines.

**Three workstreams:** (1) model + EF + admin for the new/changed types; (2) render partials reproducing the real master design *with* the JS hook contract (tabs `data-tabs-wrap`/`data-tab-btn`↔`data-tab-panel`, slider `data-slider`/`data-slider-track`, gallery `a.mp-gshot` + lightbox, safety `mp-stoggle`); (3) the AngleSharp parser + one-time migration of all 11 cars.

### Data model

Conventions (verbatim from codebase): every orderable child is `int Id` + `int VehicleId` (or parent FK), `LocalizedText` owned EN/AR fields, `string? ImagePath`, `int SortOrder`, `: IOrderable` (`{ int Id { get; } int SortOrder { get; set; } }`). No `BaseEntity`/Guid (GAC uses INT IDENTITY). Children have **no** back-reference nav (parent config uses `.WithOne()` with no arg). `LocalizedText` maps as an owned type to `{Field}_En`/`{Field}_Ar` columns via the `OwnsLocalized` helper.

**Enums:** `SectionKey { Overview, Design, Gallery, Technology, Performance, Safety, Trims, Warranty }`; `FeatureGroup { Design, Performance }`.

**Reused unchanged:** `VehicleImage`(Hero) for the hero image; `Vehicle.Name`/`Tagline`/`SpecPdf`/`BrochurePdf`. (`SpecGroup`/`SpecRow`/`ColorOption` and `VehicleImage`(Gallery) are **not** used by this render path — see corrections above. Their existing admin panels remain but are decoupled from the new template.)

**New / changed entities:**

```csharp
// 2. Section headings — one per SectionKey (no reorder; order is fixed by section)
public class SectionHeading { int Id; int VehicleId; SectionKey Key;
  LocalizedText Title=new(); LocalizedText Sub=new(); LocalizedText Body=new(); }

// 3. Overview stats (value text carries units e.g. "177 HP"); note lives on Vehicle
public class StatItem : IOrderable { int Id; int VehicleId;
  LocalizedText Label=new(); LocalizedText Value=new(); int SortOrder; }

// 4. Sliders (group → slides)
public class SliderGroup : IOrderable { int Id; int VehicleId;
  LocalizedText Eyebrow=new(); LocalizedText Title=new(); int SortOrder; List<SliderSlide> Slides=new(); }
public class SliderSlide : IOrderable { int Id; int SliderGroupId;
  string? ImagePath; LocalizedText Alt=new(); int SortOrder; }

// 5/9. Features — EXTEND existing FeatureSection (currently: Id,VehicleId,Heading,Body,ImagePath,Layout,SortOrder)
//      ADD: GroupKey (Design|Performance), TabLabel, Lead, Bullets[]. Heading=panel title; Body retained but render uses Lead+Bullets.
public class FeatureBullet : IOrderable { int Id; int FeatureSectionId;
  LocalizedText Label=new(); LocalizedText Text=new(); int SortOrder; }

// 6. Gallery tabs (tab → images); separate from VehicleImage(Gallery)
public class GalleryTab : IOrderable { int Id; int VehicleId;
  LocalizedText Label=new(); int SortOrder; List<GalleryImage> Images=new(); }
public class GalleryImage : IOrderable { int Id; int GalleryTabId;
  string? ImagePath; LocalizedText Alt=new(); int SortOrder; }   // ImagePath = full-size (a.mp-gshot href == img src)

// 7. Quality / awards — 0 or 1 per vehicle
public class QualityBlock { int Id; int VehicleId;
  string? MainImage; string? ThumbImage; LocalizedText Strapline=new(); LocalizedText Content=new(); }

// 8. Technology cards (no link). Tech banner image = new Vehicle.TechBannerImage field.
public class CardItem : IOrderable { int Id; int VehicleId;
  LocalizedText Title=new(); LocalizedText Text=new(); string? ImagePath; int SortOrder; }

// 10. Safety toggles
public class SafetyToggle : IOrderable { int Id; int VehicleId;
  LocalizedText Title=new(); string? ImagePath; LocalizedText Strap=new(); LocalizedText Content=new(); int SortOrder; }

// 11. Trims — REWORK existing Trim: ADD ModelLabel, ImagePath; price as rows; keep SpecPdf,Name. (Highlights retired from render.)
public class TrimPriceRow : IOrderable { int Id; int TrimId; LocalizedText Text=new(); int SortOrder; }

// 12. Warranty document links
public class WarrantyLink : IOrderable { int Id; int VehicleId;
  LocalizedText Label=new(); string Url=""; int SortOrder; }
```

**New scalar fields on `Vehicle`:** `string? TechBannerImage`; `string? StatsNote` *(or a `LocalizedText StatsNote`)* for the `.mp-note`; `string? EnquiryBgImage`; `LocalizedText EnquiryTitle/EnquirySub/EnquiryLead` (#13). New navs: `List<SectionHeading> Headings`, `List<StatItem> Stats`, `List<SliderGroup> Sliders`, `List<GalleryTab> GalleryTabs`, `List<CardItem> Cards`, `List<SafetyToggle> SafetyToggles`, `List<WarrantyLink> WarrantyLinks`, `QualityBlock? Quality`; plus `FeatureSection.Bullets`, `Trim.PriceRows`. `VehicleService.GetBySlugAsync` + `AdminVehicleService.GetAsync` gain the matching `.Include(...).ThenInclude(...)`.

**Notes:**
- All bilingual text via `LocalizedText` owned columns — EN/AR automatic in admin + renderer.
- Group→child collections (Sliders→Slides, GalleryTabs→Images, FeatureSection→Bullets, Trim→PriceRows) follow the existing `SpecGroup→SpecRow` pattern (parent declares `HasMany(...).WithOne().HasForeignKey(child.ParentId)`).
- Discrete fields over HTML wherever possible (feature bullets = label+text pairs, trim price = text rows) so non-technical editing needs no markup. The only sanitized-HTML field retained is `FeatureSection.Body` (unused by the new render; kept for back-compat) — the new render path uses no admin-entered raw HTML.

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
