# Vehicle Content Management (no-HTML admin editing) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let non-technical staff edit all content of the ~11 existing GAC car pages through admin forms (no HTML), by rendering one fixed developer-owned master template from a structured per-vehicle model and migrating the current `BodyHtml` content into that model so every field opens pre-populated.

**Architecture:** Approach A — field-mapped template. The real master page is **13 section types** (hero, section headings, overview stats, 2 sliders, design feature-tabs, gallery-tabs + lightbox, quality/awards, technology cards, performance feature-tabs, safety toggles, trims, warranty links, enquiry). Each becomes a structured entity (mostly new child collections of `Vehicle`, reusing the existing `VehicleImage`(Hero), `FeatureSection` (extended) and `Trim` (reworked)). New Razor render partials reproduce the design and emit the exact `main.js` hook contract; new admin partials mirror the existing `_Colors`/`_Trims`/`_SpecGroups` patterns; an AngleSharp parser backfills the model from each car's `BodyHtml`. Vehicles then always render the structured template (the all-or-nothing `BodyHtml` gate is removed; `BodyHtml` is kept as backup).

**Tech Stack:** .NET 9 · ASP.NET Core MVC (Razor compiled at build) · EF Core 9.0.6 (SQL Server) · AngleSharp (HTML parsing, new) · Ganss.Xss HtmlSanitizer 9.0.892 · xunit + Microsoft.AspNetCore.Mvc.Testing.

## Global Constraints

*(Every task's requirements implicitly include this section.)*

- **Stack pins:** `net9.0`; all `Microsoft.*` packages `9.0.x` (EF Core SqlServer/Design `9.0.6`). Add **AngleSharp** to `GAC.Infrastructure` pinned to a net9-compatible `1.x` (e.g. `1.1.2`).
- **Entities:** plain `int Id` IDENTITY (NO `BaseEntity`/Guid). Parent FK is a plain `int` (`VehicleId`/`SliderGroupId`/`GalleryTabId`/`FeatureSectionId`/`TrimId`). Children have **no** back-reference nav. Bilingual fields are `LocalizedText` (owned → `{Field}_En`/`{Field}_Ar nvarchar(max)`). Orderable children implement `IOrderable` and have `int SortOrder`.
- **EF config:** in `Data/Configurations/ContentConfigurations.cs`; `b.OwnsLocalized(x => x.Field)` for every `LocalizedText`; `HasMany(...).WithOne().HasForeignKey(...).OnDelete(Cascade)` declared on the **parent** config (Vehicle-side for direct children; the child's own config for grandchildren).
- **No auto-migration:** every schema change ships an EF migration **and** a guarded SQL script in `Solution/docs/migrations/YYYY-MM-DD-<Name>.sql` (history-guarded DDL + `__EFMigrationsHistory` stamp row, `ProductVersion '9.0.6'`). The script is hand-applied to the shared prod DB (`83.229.86.221/GAC`) **before** deploy. Never `dotnet ef database update`.
- **Admin:** `VehiclesController` is `[Area("Admin")] [Authorize(Policy = AdminPolicies.ContentEditor)] [AutoValidateAntiforgeryToken]` — never add `[ValidateAntiForgeryToken]` per action. Each section saves immediately via its own `Add*/Remove*/Move*` POST → `RedirectToAction(nameof(Edit), new { id = vehicleId })`. `LocalizedText` is built in the controller from `…En`/`…Ar` params. Media/PDF fields use the shared `_PickerModal` (`data-media-input` + sibling `data-media-pick` button); include `_PickerModal` once, last, inside Edit.cshtml's `@if (!isNew)` block.
- **Render:** vehicle detail partials live at `GAC.Web/Views/Vehicles/_*.cshtml` and are included from `Detail.cshtml` with **full paths** `~/Views/Vehicles/_X.cshtml` (the page is served by `PageController`, route value `"Page"`, so bare names won't resolve). Use `L["…"]` for static chrome and `@field.Localize()` for content. Emit the exact `mp-*`/`data-*` JS-hook markup (tabs, slider, gallery + **one** `[data-lightbox]` singleton per page, safety toggle). Vehicles **always** render the structured template.
- **Data continuity (AC1):** the parser pre-populates every car's structured collections from its existing `BodyHtml` (EN+AR) **before** cutover; `BodyHtml` columns are retained as backup and never dropped.
- **Bilingual:** every text field is EN+AR via `LocalizedText`; Arabic falls back to English when blank (existing `Localize()` behaviour).

## Canonical naming (single source of truth — use verbatim everywhere)

| Section | Controller actions | row id field | parent id field | Add-form text inputs |
|---|---|---|---|---|
| Section headings | `UpsertSectionHeading` | (hidden `key`) | `vehicleId` | `titleEn/Ar`, `subEn/Ar`, `bodyEn/Ar` |
| Stats | `AddStat`/`RemoveStat`/`MoveStat` | `statId` | `vehicleId` | `labelEn/Ar`, `valueEn/Ar` |
| Sliders | `AddSlider`/`RemoveSlider`/`MoveSlider` | `sliderId` | `vehicleId` | `eyebrowEn/Ar`, `titleEn/Ar` |
| Slider slides | `AddSliderSlide`/`RemoveSliderSlide`/`MoveSliderSlide` | `slideId` | `sliderGroupId` | `imagePath`, `altEn/Ar` |
| Feature bullets | `AddFeatureBullet`/`RemoveFeatureBullet`/`MoveFeatureBullet` | `bulletId` | `featureSectionId` | `labelEn/Ar`, `textEn/Ar` |
| Gallery tabs | `AddGalleryTab`/`RemoveGalleryTab`/`MoveGalleryTab` | `tabId` | `vehicleId` | `labelEn/Ar` |
| Gallery images | `AddGalleryImage`/`RemoveGalleryImage`/`MoveGalleryImage` | `imageId` | `galleryTabId` | `imagePath`, `altEn/Ar` |
| Quality | `UpsertQuality`/`RemoveQuality` | — | `vehicleId` | `mainImage`, `thumbImage`, `straplineEn/Ar`, `contentEn/Ar` |
| Cards | `AddCard`/`RemoveCard`/`MoveCard` | `cardId` | `vehicleId` | `titleEn/Ar`, `textEn/Ar`, `imagePath` |
| Safety toggles | `AddSafetyToggle`/`RemoveSafetyToggle`/`MoveSafetyToggle` | `toggleId` | `vehicleId` | `titleEn/Ar`, `imagePath`, `strapEn/Ar`, `contentEn/Ar` |
| Trims (existing, reworked) | `AddTrim`/`RemoveTrim`/`MoveTrim` | `trimId` | `vehicleId` | + `modelLabelEn/Ar`, `imagePath` |
| Trim price rows | `AddTrimPriceRow`/`RemoveTrimPriceRow`/`MoveTrimPriceRow` | `rowId` | `trimId` | `textEn/Ar` |
| Warranty links | `AddWarrantyLink`/`RemoveWarrantyLink`/`MoveWarrantyLink` | `linkId` | `vehicleId` | `labelEn/Ar`, `url` |

**Vehicle navs (verbatim):** `Headings`, `Stats`, `Sliders`(→`Slides`), `GalleryTabs`(→`Images`), `Cards`, `SafetyToggles`, `WarrantyLinks`, `Quality` (nullable single); `Features`(→`Bullets`, `+GroupKey/TabLabel/Lead`), `Trims`(→`PriceRows`, `+ModelLabel/ImagePath`). **Vehicle scalars:** `TechBannerImage`, `StatsNote`, `EnquiryBgImage`, `EnquiryTitle/Sub/Lead`. **Enums:** `SectionKey { Overview, Design, Gallery, Technology, Performance, Safety, Trims, Warranty }`, `FeatureGroup { Design, Performance }`.

## Phases & execution order

Tasks are numbered within phases (gaps between phases are intentional). Build order:

1. **Phase 1 — Data layer** (Tasks 1–12): entities, enums, `Vehicle`/`FeatureSection`/`Trim` changes, DbSets, EF configs, the `AddVehicleRichSections` migration + guarded SQL.
2. **Phase 2 — Admin backend** (Tasks 20–31): service CRUD + controller actions + `GetAsync` includes. *(Task 20's `GetAsync` test only goes green after the per-collection tasks land — see its note.)*
3. **Phase 3 — Admin views** (Tasks 40–51): one admin partial per section, wired into `Edit.cshtml`.
4. **Phase 4 — Render** (Tasks 60–74): public render partials + lightbox singleton + `Detail.cshtml` rewrite (always-structured).
5. **Phase 5 — Parser & migration** (Tasks 80–88): AngleSharp parser, backfill migrator, fixtures, the one-off run mechanism.
6. **Phase 6 — Cutover & regression** (Tasks 95–101): final cutover, all-cars parity tests, deploy runbook, full-suite gate.

> **Cutover sequencing (critical):** build the template (Phases 1–4) → apply schema SQL → run the backfill migrator against the DB → verify parity → only then flip the render path (Phase 6). Don't populate structured rows before the new template exists or cars briefly render the old simplified view.

---


## Phase 1 — Data layer

### Task 1: Enums + simple per-vehicle entities (SectionHeading, StatItem)

**Files:**
- Create `Solution/GAC.Core/Content/SectionKey.cs`
- Create `Solution/GAC.Core/Content/FeatureGroup.cs`
- Create `Solution/GAC.Core/Content/SectionHeading.cs`
- Create `Solution/GAC.Core/Content/StatItem.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs` (new file in this task)

**Interfaces:**
- Produces: `enum SectionKey { Overview, Design, Gallery, Technology, Performance, Safety, Trims, Warranty }`
- Produces: `enum FeatureGroup { Design, Performance }`
- Produces: `class SectionHeading { int Id; int VehicleId; SectionKey Key; LocalizedText Title,Sub,Body; }`
- Produces: `class StatItem : IOrderable { int Id; int VehicleId; LocalizedText Label,Value; int SortOrder; }`

Steps:

- [ ] **Step 1: Write failing test for the two simple entities and enums.** Create `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class VehicleRichEntitiesTests
{
    [Fact]
    public void SectionKey_HasExpectedMembers()
    {
        Assert.Equal(0, (int)SectionKey.Overview);
        Assert.Equal(7, (int)SectionKey.Warranty);
        Assert.Equal(8, System.Enum.GetValues(typeof(SectionKey)).Length);
    }

    [Fact]
    public void FeatureGroup_HasDesignAndPerformance()
    {
        Assert.Equal(0, (int)FeatureGroup.Design);
        Assert.Equal(1, (int)FeatureGroup.Performance);
    }

    [Fact]
    public void SectionHeading_DefaultsLocalizedTextNonNull()
    {
        var h = new SectionHeading { VehicleId = 1, Key = SectionKey.Overview };
        Assert.NotNull(h.Title);
        Assert.NotNull(h.Sub);
        Assert.NotNull(h.Body);
    }

    [Fact]
    public void StatItem_IsOrderable_WithLocalizedFields()
    {
        IOrderable s = new StatItem { VehicleId = 1, SortOrder = 3 };
        Assert.Equal(3, s.SortOrder);
        var stat = (StatItem)s;
        Assert.NotNull(stat.Label);
        Assert.NotNull(stat.Value);
    }
}
```

- [ ] **Step 2: Run the test and watch it fail to compile.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — expect a compile error: "The type or namespace name 'SectionKey' could not be found".

- [ ] **Step 3: Create the two enums.** `Solution/GAC.Core/Content/SectionKey.cs`:
```csharp
namespace GAC.Core.Content;

public enum SectionKey
{
    Overview = 0,
    Design = 1,
    Gallery = 2,
    Technology = 3,
    Performance = 4,
    Safety = 5,
    Trims = 6,
    Warranty = 7
}
```
`Solution/GAC.Core/Content/FeatureGroup.cs`:
```csharp
namespace GAC.Core.Content;

public enum FeatureGroup
{
    Design = 0,
    Performance = 1
}
```

- [ ] **Step 4: Create `SectionHeading`.** `Solution/GAC.Core/Content/SectionHeading.cs`:
```csharp
namespace GAC.Core.Content;

public class SectionHeading
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public SectionKey Key { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Sub { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
}
```

- [ ] **Step 5: Create `StatItem`.** `Solution/GAC.Core/Content/StatItem.cs`:
```csharp
namespace GAC.Core.Content;

public class StatItem : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Value { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 6: Run the test and watch it pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — all 4 green.

- [ ] **Step 7: Commit.** `git add -A && git commit -m "feat: add SectionKey/FeatureGroup enums + SectionHeading/StatItem entities"`.

---

### Task 2: Slider entities (SliderGroup + SliderSlide)

**Files:**
- Create `Solution/GAC.Core/Content/SliderGroup.cs`
- Create `Solution/GAC.Core/Content/SliderSlide.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`

**Interfaces:**
- Produces: `class SliderGroup : IOrderable { int Id; int VehicleId; LocalizedText Eyebrow,Title; int SortOrder; List<SliderSlide> Slides; }`
- Produces: `class SliderSlide : IOrderable { int Id; int SliderGroupId; string? ImagePath; LocalizedText Alt; int SortOrder; }`

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
    [Fact]
    public void SliderGroup_HoldsSlides_AndIsOrderable()
    {
        var g = new SliderGroup { VehicleId = 1, SortOrder = 2 };
        g.Slides.Add(new SliderSlide { ImagePath = "/a.jpg", SortOrder = 0 });
        Assert.Equal(2, ((IOrderable)g).SortOrder);
        Assert.Single(g.Slides);
        Assert.NotNull(g.Eyebrow);
        Assert.NotNull(g.Title);
    }

    [Fact]
    public void SliderSlide_HasParentFk_AndAlt()
    {
        var s = new SliderSlide { SliderGroupId = 5, ImagePath = "/b.jpg", SortOrder = 1 };
        Assert.Equal(5, s.SliderGroupId);
        Assert.Equal("/b.jpg", s.ImagePath);
        Assert.NotNull(s.Alt);
        Assert.Equal(1, ((IOrderable)s).SortOrder);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — compile error: "'SliderGroup' could not be found".

- [ ] **Step 3: Create `SliderGroup`.** `Solution/GAC.Core/Content/SliderGroup.cs`:
```csharp
namespace GAC.Core.Content;

public class SliderGroup : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Title { get; set; } = new();
    public int SortOrder { get; set; }
    public List<SliderSlide> Slides { get; set; } = new();
}
```

- [ ] **Step 4: Create `SliderSlide`.** `Solution/GAC.Core/Content/SliderSlide.cs`:
```csharp
namespace GAC.Core.Content;

public class SliderSlide : IOrderable
{
    public int Id { get; set; }
    public int SliderGroupId { get; set; }
    public string? ImagePath { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 5: Run & pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — green.

- [ ] **Step 6: Commit.** `git commit -am "feat: add SliderGroup + SliderSlide entities"`.

---

### Task 3: Feature children + Gallery entities (FeatureBullet, GalleryTab, GalleryImage)

**Files:**
- Create `Solution/GAC.Core/Content/FeatureBullet.cs`
- Create `Solution/GAC.Core/Content/GalleryTab.cs`
- Create `Solution/GAC.Core/Content/GalleryImage.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`

**Interfaces:**
- Produces: `class FeatureBullet : IOrderable { int Id; int FeatureSectionId; LocalizedText Label,Text; int SortOrder; }`
- Produces: `class GalleryTab : IOrderable { int Id; int VehicleId; LocalizedText Label; int SortOrder; List<GalleryImage> Images; }`
- Produces: `class GalleryImage : IOrderable { int Id; int GalleryTabId; string? ImagePath; LocalizedText Alt; int SortOrder; }`

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
    [Fact]
    public void FeatureBullet_HasParentFk_LabelAndText()
    {
        var b = new FeatureBullet { FeatureSectionId = 9, SortOrder = 0 };
        Assert.Equal(9, b.FeatureSectionId);
        Assert.NotNull(b.Label);
        Assert.NotNull(b.Text);
        Assert.Equal(0, ((IOrderable)b).SortOrder);
    }

    [Fact]
    public void GalleryTab_HoldsImages_AndIsOrderable()
    {
        var t = new GalleryTab { VehicleId = 1, SortOrder = 1 };
        t.Images.Add(new GalleryImage { ImagePath = "/g.jpg", SortOrder = 0 });
        Assert.Equal(1, ((IOrderable)t).SortOrder);
        Assert.Single(t.Images);
        Assert.NotNull(t.Label);
    }

    [Fact]
    public void GalleryImage_HasParentFk_AndAlt()
    {
        var g = new GalleryImage { GalleryTabId = 7, ImagePath = "/g.jpg", SortOrder = 2 };
        Assert.Equal(7, g.GalleryTabId);
        Assert.Equal("/g.jpg", g.ImagePath);
        Assert.NotNull(g.Alt);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — compile error: "'FeatureBullet' could not be found".

- [ ] **Step 3: Create `FeatureBullet`.** `Solution/GAC.Core/Content/FeatureBullet.cs`:
```csharp
namespace GAC.Core.Content;

public class FeatureBullet : IOrderable
{
    public int Id { get; set; }
    public int FeatureSectionId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 4: Create `GalleryTab`.** `Solution/GAC.Core/Content/GalleryTab.cs`:
```csharp
namespace GAC.Core.Content;

public class GalleryTab : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public int SortOrder { get; set; }
    public List<GalleryImage> Images { get; set; } = new();
}
```

- [ ] **Step 5: Create `GalleryImage`.** `Solution/GAC.Core/Content/GalleryImage.cs`:
```csharp
namespace GAC.Core.Content;

public class GalleryImage : IOrderable
{
    public int Id { get; set; }
    public int GalleryTabId { get; set; }
    public string? ImagePath { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 6: Run & pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — green.

- [ ] **Step 7: Commit.** `git commit -am "feat: add FeatureBullet + GalleryTab + GalleryImage entities"`.

---

### Task 4: Quality, Cards, Safety, Warranty, Trim price-row entities

**Files:**
- Create `Solution/GAC.Core/Content/QualityBlock.cs`
- Create `Solution/GAC.Core/Content/CardItem.cs`
- Create `Solution/GAC.Core/Content/SafetyToggle.cs`
- Create `Solution/GAC.Core/Content/WarrantyLink.cs`
- Create `Solution/GAC.Core/Content/TrimPriceRow.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`

**Interfaces:**
- Produces: `class QualityBlock { int Id; int VehicleId; string? MainImage,ThumbImage; LocalizedText Strapline,Content; }`
- Produces: `class CardItem : IOrderable { int Id; int VehicleId; LocalizedText Title,Text; string? ImagePath; int SortOrder; }`
- Produces: `class SafetyToggle : IOrderable { int Id; int VehicleId; LocalizedText Title; string? ImagePath; LocalizedText Strap,Content; int SortOrder; }`
- Produces: `class WarrantyLink : IOrderable { int Id; int VehicleId; LocalizedText Label; string Url; int SortOrder; }`
- Produces: `class TrimPriceRow : IOrderable { int Id; int TrimId; LocalizedText Text; int SortOrder; }`

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
    [Fact]
    public void QualityBlock_HasImagesAndContent()
    {
        var q = new QualityBlock { VehicleId = 1, MainImage = "/m.jpg", ThumbImage = "/t.jpg" };
        Assert.Equal("/m.jpg", q.MainImage);
        Assert.Equal("/t.jpg", q.ThumbImage);
        Assert.NotNull(q.Strapline);
        Assert.NotNull(q.Content);
    }

    [Fact]
    public void CardItem_HasImage_AndIsOrderable()
    {
        var c = new CardItem { VehicleId = 1, ImagePath = "/c.jpg", SortOrder = 2 };
        Assert.Equal("/c.jpg", c.ImagePath);
        Assert.Equal(2, ((IOrderable)c).SortOrder);
        Assert.NotNull(c.Title);
        Assert.NotNull(c.Text);
    }

    [Fact]
    public void SafetyToggle_HasTitleStrapContentImage()
    {
        var s = new SafetyToggle { VehicleId = 1, ImagePath = "/s.jpg", SortOrder = 0 };
        Assert.Equal("/s.jpg", s.ImagePath);
        Assert.NotNull(s.Title);
        Assert.NotNull(s.Strap);
        Assert.NotNull(s.Content);
    }

    [Fact]
    public void WarrantyLink_HasLabelAndUrl()
    {
        var w = new WarrantyLink { VehicleId = 1, Url = "/doc.pdf", SortOrder = 1 };
        Assert.Equal("/doc.pdf", w.Url);
        Assert.NotNull(w.Label);
        Assert.Equal(1, ((IOrderable)w).SortOrder);
    }

    [Fact]
    public void TrimPriceRow_HasParentFkAndText()
    {
        var r = new TrimPriceRow { TrimId = 4, SortOrder = 0 };
        Assert.Equal(4, r.TrimId);
        Assert.NotNull(r.Text);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — compile error: "'QualityBlock' could not be found".

- [ ] **Step 3: Create `QualityBlock`.** `Solution/GAC.Core/Content/QualityBlock.cs`:
```csharp
namespace GAC.Core.Content;

public class QualityBlock
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string? MainImage { get; set; }
    public string? ThumbImage { get; set; }
    public LocalizedText Strapline { get; set; } = new();
    public LocalizedText Content { get; set; } = new();
}
```

- [ ] **Step 4: Create `CardItem`.** `Solution/GAC.Core/Content/CardItem.cs`:
```csharp
namespace GAC.Core.Content;

public class CardItem : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 5: Create `SafetyToggle`.** `Solution/GAC.Core/Content/SafetyToggle.cs`:
```csharp
namespace GAC.Core.Content;

public class SafetyToggle : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public string? ImagePath { get; set; }
    public LocalizedText Strap { get; set; } = new();
    public LocalizedText Content { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 6: Create `WarrantyLink`.** `Solution/GAC.Core/Content/WarrantyLink.cs`:
```csharp
namespace GAC.Core.Content;

public class WarrantyLink : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}
```

- [ ] **Step 7: Create `TrimPriceRow`.** `Solution/GAC.Core/Content/TrimPriceRow.cs`:
```csharp
namespace GAC.Core.Content;

public class TrimPriceRow : IOrderable
{
    public int Id { get; set; }
    public int TrimId { get; set; }
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
```

- [ ] **Step 8: Run & pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — green.

- [ ] **Step 9: Commit.** `git commit -am "feat: add QualityBlock/CardItem/SafetyToggle/WarrantyLink/TrimPriceRow entities"`.

---

### Task 5: Extend FeatureSection and Trim

**Files:**
- Modify `Solution/GAC.Core/Content/FeatureSection.cs`
- Modify `Solution/GAC.Core/Content/Trim.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`

**Interfaces:**
- Produces (FeatureSection ADD): `FeatureGroup GroupKey; LocalizedText TabLabel=new(); LocalizedText Lead=new(); List<FeatureBullet> Bullets=new();`
- Produces (Trim ADD): `LocalizedText ModelLabel=new(); string? ImagePath; List<TrimPriceRow> PriceRows=new();`

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
    [Fact]
    public void FeatureSection_HasNewGroupTabLeadBullets()
    {
        var f = new FeatureSection { GroupKey = FeatureGroup.Performance };
        f.Bullets.Add(new FeatureBullet());
        Assert.Equal(FeatureGroup.Performance, f.GroupKey);
        Assert.NotNull(f.TabLabel);
        Assert.NotNull(f.Lead);
        Assert.Single(f.Bullets);
    }

    [Fact]
    public void Trim_HasModelLabelImageAndPriceRows()
    {
        var t = new Trim { ModelLabel = "GS4", ImagePath = "/trim.jpg" };
        t.PriceRows.Add(new TrimPriceRow { Text = "Total: 10,000" });
        Assert.Equal("GS4", t.ModelLabel.En);
        Assert.Equal("/trim.jpg", t.ImagePath);
        Assert.Single(t.PriceRows);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — compile error: "'FeatureSection' does not contain a definition for 'GroupKey'".

- [ ] **Step 3: Extend `FeatureSection`.** Edit `Solution/GAC.Core/Content/FeatureSection.cs` so the class body reads exactly:
```csharp
namespace GAC.Core.Content;

public class FeatureSection : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public FeatureLayout Layout { get; set; } = FeatureLayout.ImageLeft;
    public int SortOrder { get; set; }

    public FeatureGroup GroupKey { get; set; } = FeatureGroup.Design;
    public LocalizedText TabLabel { get; set; } = new();
    public LocalizedText Lead { get; set; } = new();
    public List<FeatureBullet> Bullets { get; set; } = new();
}
```

- [ ] **Step 4: Extend `Trim`.** Edit `Solution/GAC.Core/Content/Trim.cs` so the class body reads exactly:
```csharp
namespace GAC.Core.Content;

public class Trim : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Name { get; set; } = new();
    public decimal? Price { get; set; }
    public LocalizedText Highlights { get; set; } = new();
    public string? SpecPdf { get; set; }
    public int SortOrder { get; set; }

    public LocalizedText ModelLabel { get; set; } = new();
    public string? ImagePath { get; set; }
    public List<TrimPriceRow> PriceRows { get; set; } = new();
}
```

- [ ] **Step 5: Run & pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — green.

- [ ] **Step 6: Commit.** `git commit -am "feat: extend FeatureSection (group/tab/lead/bullets) and Trim (modelLabel/image/priceRows)"`.

---

### Task 6: Extend Vehicle with new scalar/localized fields and navigations

**Files:**
- Modify `Solution/GAC.Core/Content/Vehicle.cs`
- Modify `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`

**Interfaces:**
- Produces (Vehicle ADD scalars/localized): `string? TechBannerImage; LocalizedText StatsNote=new(); string? EnquiryBgImage; LocalizedText EnquiryTitle=new(),EnquirySub=new(),EnquiryLead=new();`
- Produces (Vehicle ADD navs): `List<SectionHeading> Headings; List<StatItem> Stats; List<SliderGroup> Sliders; List<GalleryTab> GalleryTabs; List<CardItem> Cards; List<SafetyToggle> SafetyToggles; List<WarrantyLink> WarrantyLinks; QualityBlock? Quality;`

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/VehicleRichEntitiesTests.cs`:
```csharp
    [Fact]
    public void Vehicle_HasNewScalarAndLocalizedFields()
    {
        var v = new Vehicle { TechBannerImage = "/tb.jpg", EnquiryBgImage = "/eb.jpg" };
        Assert.Equal("/tb.jpg", v.TechBannerImage);
        Assert.Equal("/eb.jpg", v.EnquiryBgImage);
        Assert.NotNull(v.StatsNote);
        Assert.NotNull(v.EnquiryTitle);
        Assert.NotNull(v.EnquirySub);
        Assert.NotNull(v.EnquiryLead);
    }

    [Fact]
    public void Vehicle_HasNewCollectionsAndQualityNav()
    {
        var v = new Vehicle();
        v.Headings.Add(new SectionHeading());
        v.Stats.Add(new StatItem());
        v.Sliders.Add(new SliderGroup());
        v.GalleryTabs.Add(new GalleryTab());
        v.Cards.Add(new CardItem());
        v.SafetyToggles.Add(new SafetyToggle());
        v.WarrantyLinks.Add(new WarrantyLink());
        v.Quality = new QualityBlock();
        Assert.Single(v.Headings);
        Assert.Single(v.Stats);
        Assert.Single(v.Sliders);
        Assert.Single(v.GalleryTabs);
        Assert.Single(v.Cards);
        Assert.Single(v.SafetyToggles);
        Assert.Single(v.WarrantyLinks);
        Assert.NotNull(v.Quality);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — compile error: "'Vehicle' does not contain a definition for 'TechBannerImage'".

- [ ] **Step 3: Extend `Vehicle`.** Edit `Solution/GAC.Core/Content/Vehicle.cs` so the class body reads exactly:
```csharp
namespace GAC.Core.Content;

public class Vehicle
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public VehicleCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public decimal? PriceFrom { get; set; }

    public LocalizedText Name { get; set; } = new();
    public LocalizedText Tagline { get; set; } = new();
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();

    public string? BrochurePdf { get; set; }
    public string? SpecPdf { get; set; }

    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();

    // Rich-section scalar/localized fields
    public string? TechBannerImage { get; set; }
    public LocalizedText StatsNote { get; set; } = new();
    public string? EnquiryBgImage { get; set; }
    public LocalizedText EnquiryTitle { get; set; } = new();
    public LocalizedText EnquirySub { get; set; } = new();
    public LocalizedText EnquiryLead { get; set; } = new();

    public List<VehicleImage> Images { get; set; } = new();
    public List<Trim> Trims { get; set; } = new();
    public List<SpecGroup> SpecGroups { get; set; } = new();
    public List<ColorOption> Colors { get; set; } = new();
    public List<FeatureSection> Features { get; set; } = new();

    // Rich-section collections
    public List<SectionHeading> Headings { get; set; } = new();
    public List<StatItem> Stats { get; set; } = new();
    public List<SliderGroup> Sliders { get; set; } = new();
    public List<GalleryTab> GalleryTabs { get; set; } = new();
    public List<CardItem> Cards { get; set; } = new();
    public List<SafetyToggle> SafetyToggles { get; set; } = new();
    public List<WarrantyLink> WarrantyLinks { get; set; } = new();
    public QualityBlock? Quality { get; set; }
}
```

- [ ] **Step 4: Run & pass.** `dotnet test Solution/GAC.Tests --filter VehicleRichEntitiesTests` — green.

- [ ] **Step 5: Commit.** `git commit -am "feat: extend Vehicle with rich-section fields and navigations"`.

---

### Task 7: DbSet properties on ApplicationDbContext

**Files:**
- Modify `Solution/GAC.Infrastructure/Data/ApplicationDbContext.cs`
- Create `Solution/GAC.Tests/RichSectionDbSetTests.cs`

**Interfaces:**
- Produces DbSets: `SectionHeadings, StatItems, SliderGroups, SliderSlides, FeatureBullets, GalleryTabs, GalleryImages, QualityBlocks, CardItems, SafetyToggles, TrimPriceRows, WarrantyLinks`

Steps:

- [ ] **Step 1: Write failing test.** Create `Solution/GAC.Tests/RichSectionDbSetTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionDbSetTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public void AllRichDbSets_AreExposed()
    {
        using var db = NewDb(nameof(AllRichDbSets_AreExposed));
        Assert.NotNull(db.SectionHeadings);
        Assert.NotNull(db.StatItems);
        Assert.NotNull(db.SliderGroups);
        Assert.NotNull(db.SliderSlides);
        Assert.NotNull(db.FeatureBullets);
        Assert.NotNull(db.GalleryTabs);
        Assert.NotNull(db.GalleryImages);
        Assert.NotNull(db.QualityBlocks);
        Assert.NotNull(db.CardItems);
        Assert.NotNull(db.SafetyToggles);
        Assert.NotNull(db.TrimPriceRows);
        Assert.NotNull(db.WarrantyLinks);
    }
}
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter RichSectionDbSetTests` — compile error: "'ApplicationDbContext' does not contain a definition for 'SectionHeadings'".

- [ ] **Step 3: Add the DbSet properties.** In `Solution/GAC.Infrastructure/Data/ApplicationDbContext.cs`, after the existing `public DbSet<DockItem> DockItems => Set<DockItem>();` line, insert:
```csharp
    public DbSet<SectionHeading> SectionHeadings => Set<SectionHeading>();
    public DbSet<StatItem> StatItems => Set<StatItem>();
    public DbSet<SliderGroup> SliderGroups => Set<SliderGroup>();
    public DbSet<SliderSlide> SliderSlides => Set<SliderSlide>();
    public DbSet<FeatureBullet> FeatureBullets => Set<FeatureBullet>();
    public DbSet<GalleryTab> GalleryTabs => Set<GalleryTab>();
    public DbSet<GalleryImage> GalleryImages => Set<GalleryImage>();
    public DbSet<QualityBlock> QualityBlocks => Set<QualityBlock>();
    public DbSet<CardItem> CardItems => Set<CardItem>();
    public DbSet<SafetyToggle> SafetyToggles => Set<SafetyToggle>();
    public DbSet<TrimPriceRow> TrimPriceRows => Set<TrimPriceRow>();
    public DbSet<WarrantyLink> WarrantyLinks => Set<WarrantyLink>();
```

- [ ] **Step 4: Run & pass.** `dotnet test Solution/GAC.Tests --filter RichSectionDbSetTests` — green.

- [ ] **Step 5: Commit.** `git commit -am "feat: expose rich-section DbSets on ApplicationDbContext"`.

---

### Task 8: EF configurations for simple per-vehicle children (SectionHeading, StatItem, CardItem, SafetyToggle, WarrantyLink) + Vehicle relationships

**Files:**
- Modify `Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`
- Create `Solution/GAC.Tests/RichSectionModelTests.cs`

**Interfaces:**
- Consumes: `OwnedExtensions.OwnsLocalized<TEntity>(...)` (existing helper, top of `ContentConfigurations.cs`).
- Produces: `SectionHeadingConfig`, `StatItemConfig`, `CardItemConfig`, `SafetyToggleConfig`, `WarrantyLinkConfig` (each `IEntityTypeConfiguration<X>`); `VehicleConfig` gains `HasMany` for `Headings/Stats/Cards/SafetyToggles/WarrantyLinks` + `OwnsLocalized` for the new Vehicle localized fields + scalar lengths.

Steps:

- [ ] **Step 1: Write failing test.** Create `Solution/GAC.Tests/RichSectionModelTests.cs`. This boots the real SqlServer model (mirrors `DbContextModelTests`, validates owned columns exist) — it asserts the EF model maps the new owned localized fields and FK relationships:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionModelTests
{
    private static ApplicationDbContext Ctx()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=_design;TrustServerCertificate=True")
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public void SimpleChildren_AreMapped_WithVehicleFk()
    {
        using var ctx = Ctx();
        foreach (var clr in new[]
        {
            typeof(SectionHeading), typeof(StatItem), typeof(CardItem),
            typeof(SafetyToggle), typeof(WarrantyLink)
        })
        {
            var et = ctx.Model.FindEntityType(clr);
            Assert.NotNull(et);
            Assert.NotNull(et!.FindProperty("VehicleId"));
        }
    }

    [Fact]
    public void SectionHeading_LocalizedFields_AreOwned()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(SectionHeading))!;
        foreach (var field in new[] { "Title", "Sub", "Body" })
        {
            var nav = et.FindNavigation(field)!;
            Assert.NotNull(nav);
            Assert.NotNull(nav.TargetEntityType.FindProperty("En"));
            Assert.NotNull(nav.TargetEntityType.FindProperty("Ar"));
        }
        Assert.NotNull(et.FindProperty("Key"));   // enum stored as int
    }

    [Fact]
    public void Vehicle_NewLocalizedFields_AreOwned()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(Vehicle))!;
        foreach (var field in new[] { "StatsNote", "EnquiryTitle", "EnquirySub", "EnquiryLead" })
        {
            var nav = et.FindNavigation(field)!;
            Assert.NotNull(nav);
            Assert.NotNull(nav.TargetEntityType.FindProperty("En"));
        }
        Assert.NotNull(et.FindProperty("TechBannerImage"));
        Assert.NotNull(et.FindProperty("EnquiryBgImage"));
    }
}
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter RichSectionModelTests` — expect failure: `SimpleChildren_AreMapped_WithVehicleFk` throws because `FindEntityType(typeof(SectionHeading))` returns null (no config wires it as an entity with a relationship; without a config the owned `LocalizedText` props are unmapped). Note this test class hits SqlServer only to build the model graph (no DB connection needed for `ctx.Model`).

- [ ] **Step 3: Add the five child configs + extend `VehicleConfig`.** In `Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`:

First, extend `VehicleConfig.Configure` by adding, immediately after the existing `b.HasMany(v => v.Features)...` line:
```csharp
        b.OwnsLocalized(v => v.StatsNote);
        b.OwnsLocalized(v => v.EnquiryTitle);
        b.OwnsLocalized(v => v.EnquirySub);
        b.OwnsLocalized(v => v.EnquiryLead);
        b.Property(v => v.TechBannerImage).HasMaxLength(300);
        b.Property(v => v.EnquiryBgImage).HasMaxLength(300);
        b.HasMany(v => v.Headings).WithOne().HasForeignKey(h => h.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Stats).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Sliders).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.GalleryTabs).WithOne().HasForeignKey(g => g.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Cards).WithOne().HasForeignKey(c => c.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.SafetyToggles).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.WarrantyLinks).WithOne().HasForeignKey(w => w.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(v => v.Quality).WithOne().HasForeignKey<QualityBlock>(q => q.VehicleId).OnDelete(DeleteBehavior.Cascade);
```
(Sliders/GalleryTabs/Quality HasMany/HasOne are declared here; their own configs + grandchild relations land in Task 9. `Quality` 0/1 uses `HasOne...WithOne...HasForeignKey<QualityBlock>`.)

Then append these config classes at the end of the file (before the final closing — they are top-level classes in the namespace):
```csharp
public class SectionHeadingConfig : IEntityTypeConfiguration<SectionHeading>
{
    public void Configure(EntityTypeBuilder<SectionHeading> b)
    {
        b.OwnsLocalized(s => s.Title);
        b.OwnsLocalized(s => s.Sub);
        b.OwnsLocalized(s => s.Body);
    }
}

public class StatItemConfig : IEntityTypeConfiguration<StatItem>
{
    public void Configure(EntityTypeBuilder<StatItem> b)
    {
        b.OwnsLocalized(s => s.Label);
        b.OwnsLocalized(s => s.Value);
    }
}

public class CardItemConfig : IEntityTypeConfiguration<CardItem>
{
    public void Configure(EntityTypeBuilder<CardItem> b)
    {
        b.Property(c => c.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(c => c.Title);
        b.OwnsLocalized(c => c.Text);
    }
}

public class SafetyToggleConfig : IEntityTypeConfiguration<SafetyToggle>
{
    public void Configure(EntityTypeBuilder<SafetyToggle> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(s => s.Title);
        b.OwnsLocalized(s => s.Strap);
        b.OwnsLocalized(s => s.Content);
    }
}

public class WarrantyLinkConfig : IEntityTypeConfiguration<WarrantyLink>
{
    public void Configure(EntityTypeBuilder<WarrantyLink> b)
    {
        b.Property(w => w.Url).HasMaxLength(500).IsRequired();
        b.OwnsLocalized(w => w.Label);
    }
}
```

- [ ] **Step 4: Run & pass.** `dotnet test Solution/GAC.Tests --filter RichSectionModelTests` — green (all three facts). Note `Sliders`/`GalleryTabs`/`Quality` navs are now declared on `VehicleConfig`; their child configs follow in Task 9, but the model still builds because the owned LocalizedText on the *parent* sides is configured here.

- [ ] **Step 5: Commit.** `git commit -am "feat: EF configs for SectionHeading/StatItem/CardItem/SafetyToggle/WarrantyLink + Vehicle relationships"`.

---

### Task 9: EF configurations for group→child entities (SliderGroup/SliderSlide, GalleryTab/GalleryImage, FeatureBullet, TrimPriceRow, QualityBlock) + extend FeatureSection/Trim configs

**Files:**
- Modify `Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`
- Modify `Solution/GAC.Tests/RichSectionModelTests.cs`

**Interfaces:**
- Produces: `SliderGroupConfig` (+`HasMany(Slides)`), `SliderSlideConfig`, `GalleryTabConfig` (+`HasMany(Images)`), `GalleryImageConfig`, `FeatureBulletConfig`, `TrimPriceRowConfig`, `QualityBlockConfig`; extended `FeatureSectionConfig` (+`OwnsLocalized(TabLabel/Lead)` + `HasMany(Bullets)`); extended `TrimConfig` (+`OwnsLocalized(ModelLabel)` + `HasMany(PriceRows)` + ImagePath length).

Steps:

- [ ] **Step 1: Write failing test.** Append to `Solution/GAC.Tests/RichSectionModelTests.cs`:
```csharp
    [Fact]
    public void Grandchildren_AreMapped_WithParentFk()
    {
        using var ctx = Ctx();
        Assert.NotNull(ctx.Model.FindEntityType(typeof(SliderSlide))!.FindProperty("SliderGroupId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(GalleryImage))!.FindProperty("GalleryTabId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(FeatureBullet))!.FindProperty("FeatureSectionId"));
        Assert.NotNull(ctx.Model.FindEntityType(typeof(TrimPriceRow))!.FindProperty("TrimId"));
    }

    [Fact]
    public void SliderGroup_OwnsLocalized_AndHasSlides()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(SliderGroup))!;
        Assert.NotNull(et.FindNavigation("Eyebrow"));
        Assert.NotNull(et.FindNavigation("Title"));
        Assert.NotNull(et.FindNavigation("Slides"));
    }

    [Fact]
    public void FeatureSection_NewOwnedFields_AndBullets()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(FeatureSection))!;
        Assert.NotNull(et.FindNavigation("TabLabel"));
        Assert.NotNull(et.FindNavigation("Lead"));
        Assert.NotNull(et.FindNavigation("Bullets"));
        Assert.NotNull(et.FindProperty("GroupKey"));
    }

    [Fact]
    public void Trim_NewOwnedFields_AndPriceRows()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(Trim))!;
        Assert.NotNull(et.FindNavigation("ModelLabel"));
        Assert.NotNull(et.FindNavigation("PriceRows"));
        Assert.NotNull(et.FindProperty("ImagePath"));
    }

    [Fact]
    public void QualityBlock_IsMapped_WithVehicleFk()
    {
        using var ctx = Ctx();
        var et = ctx.Model.FindEntityType(typeof(QualityBlock))!;
        Assert.NotNull(et.FindProperty("VehicleId"));
        Assert.NotNull(et.FindNavigation("Strapline"));
        Assert.NotNull(et.FindNavigation("Content"));
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test Solution/GAC.Tests --filter RichSectionModelTests` — `Grandchildren_AreMapped_WithParentFk` fails: `FindEntityType(typeof(SliderSlide))` is null (no config; not reachable as a mapped grandchild yet).

- [ ] **Step 3: Extend `FeatureSectionConfig` and `TrimConfig`.** In `Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`, replace the existing `FeatureSectionConfig` body so it reads:
```csharp
public class FeatureSectionConfig : IEntityTypeConfiguration<FeatureSection>
{
    public void Configure(EntityTypeBuilder<FeatureSection> b)
    {
        b.OwnsLocalized(f => f.Heading);
        b.OwnsLocalized(f => f.Body);
        b.OwnsLocalized(f => f.TabLabel);
        b.OwnsLocalized(f => f.Lead);
        b.HasMany(f => f.Bullets).WithOne().HasForeignKey(x => x.FeatureSectionId).OnDelete(DeleteBehavior.Cascade);
    }
}
```
Replace the existing `TrimConfig` body so it reads:
```csharp
public class TrimConfig : IEntityTypeConfiguration<Trim>
{
    public void Configure(EntityTypeBuilder<Trim> b)
    {
        b.Property(t => t.Price).HasColumnType("decimal(18,2)");
        b.Property(t => t.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(t => t.Name);
        b.OwnsLocalized(t => t.Highlights);
        b.OwnsLocalized(t => t.ModelLabel);
        b.HasMany(t => t.PriceRows).WithOne().HasForeignKey(x => x.TrimId).OnDelete(DeleteBehavior.Cascade);
    }
}
```

- [ ] **Step 4: Append the remaining configs.** At the end of `ContentConfigurations.cs`, append:
```csharp
public class SliderGroupConfig : IEntityTypeConfiguration<SliderGroup>
{
    public void Configure(EntityTypeBuilder<SliderGroup> b)
    {
        b.OwnsLocalized(s => s.Eyebrow);
        b.OwnsLocalized(s => s.Title);
        b.HasMany(s => s.Slides).WithOne().HasForeignKey(x => x.SliderGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SliderSlideConfig : IEntityTypeConfiguration<SliderSlide>
{
    public void Configure(EntityTypeBuilder<SliderSlide> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(s => s.Alt);
    }
}

public class GalleryTabConfig : IEntityTypeConfiguration<GalleryTab>
{
    public void Configure(EntityTypeBuilder<GalleryTab> b)
    {
        b.OwnsLocalized(g => g.Label);
        b.HasMany(g => g.Images).WithOne().HasForeignKey(x => x.GalleryTabId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GalleryImageConfig : IEntityTypeConfiguration<GalleryImage>
{
    public void Configure(EntityTypeBuilder<GalleryImage> b)
    {
        b.Property(g => g.ImagePath).HasMaxLength(300);
        b.OwnsLocalized(g => g.Alt);
    }
}

public class FeatureBulletConfig : IEntityTypeConfiguration<FeatureBullet>
{
    public void Configure(EntityTypeBuilder<FeatureBullet> b)
    {
        b.OwnsLocalized(x => x.Label);
        b.OwnsLocalized(x => x.Text);
    }
}

public class TrimPriceRowConfig : IEntityTypeConfiguration<TrimPriceRow>
{
    public void Configure(EntityTypeBuilder<TrimPriceRow> b)
    {
        b.OwnsLocalized(x => x.Text);
    }
}

public class QualityBlockConfig : IEntityTypeConfiguration<QualityBlock>
{
    public void Configure(EntityTypeBuilder<QualityBlock> b)
    {
        b.Property(q => q.MainImage).HasMaxLength(300);
        b.Property(q => q.ThumbImage).HasMaxLength(300);
        b.OwnsLocalized(q => q.Strapline);
        b.OwnsLocalized(q => q.Content);
    }
}
```
(Note: `QualityBlock`'s VehicleId FK and 0/1 relationship were declared on `VehicleConfig` in Task 8 via `HasOne(v => v.Quality).WithOne()...HasForeignKey<QualityBlock>(...)`; this config only owns its localized fields + image lengths. Likewise `SliderGroup`/`GalleryTab` VehicleId FK relationships were declared on `VehicleConfig` in Task 8.)

- [ ] **Step 5: Run & pass.** `dotnet test Solution/GAC.Tests --filter RichSectionModelTests` — all green.

- [ ] **Step 6: Commit.** `git commit -am "feat: EF configs for slider/gallery/bullet/priceRow/quality + extend FeatureSection/Trim configs"`.

---

### Task 10: In-memory round-trip test for a fully-populated Vehicle graph

**Files:**
- Create `Solution/GAC.Tests/RichSectionRoundTripTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext` (in-memory provider), all entities + DbSets + configs from Tasks 1-9.

Steps:

- [ ] **Step 1: Write failing test.** Create `Solution/GAC.Tests/RichSectionRoundTripTests.cs`. This persists a Vehicle with every new collection (incl. grandchildren) and the optional QualityBlock, then reloads with Includes and asserts counts/values survive a save:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class RichSectionRoundTripTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Vehicle_WithAllRichCollections_RoundTrips()
    {
        var name = nameof(Vehicle_WithAllRichCollections_RoundTrips);
        int vid;
        using (var db = NewDb(name))
        {
            var v = new Vehicle
            {
                Slug = "round-trip",
                Name = "Round Trip",
                TechBannerImage = "/tb.jpg",
                EnquiryBgImage = "/eb.jpg",
                StatsNote = new LocalizedText { En = "note-en", Ar = "note-ar" },
                EnquiryTitle = "Enquire",
                Quality = new QualityBlock
                {
                    MainImage = "/q-main.jpg",
                    ThumbImage = "/q-thumb.jpg",
                    Strapline = "strap",
                    Content = "content"
                }
            };
            v.Headings.Add(new SectionHeading { Key = SectionKey.Overview, Title = "Overview" });
            v.Stats.Add(new StatItem { Label = "Power", Value = "177 HP", SortOrder = 0 });
            v.Cards.Add(new CardItem { Title = "Card", Text = "Body", ImagePath = "/c.jpg", SortOrder = 0 });
            v.SafetyToggles.Add(new SafetyToggle { Title = "Brakes", Strap = "s", Content = "c", ImagePath = "/s.jpg", SortOrder = 0 });
            v.WarrantyLinks.Add(new WarrantyLink { Label = "Manual", Url = "/m.pdf", SortOrder = 0 });

            var sg = new SliderGroup { Eyebrow = "eye", Title = "Slider", SortOrder = 0 };
            sg.Slides.Add(new SliderSlide { ImagePath = "/sl1.jpg", Alt = "alt1", SortOrder = 0 });
            sg.Slides.Add(new SliderSlide { ImagePath = "/sl2.jpg", Alt = "alt2", SortOrder = 1 });
            v.Sliders.Add(sg);

            var gt = new GalleryTab { Label = "Exterior", SortOrder = 0 };
            gt.Images.Add(new GalleryImage { ImagePath = "/g1.jpg", Alt = "g1", SortOrder = 0 });
            v.GalleryTabs.Add(gt);

            var feat = new FeatureSection { Heading = "Design", GroupKey = FeatureGroup.Design, TabLabel = "Design", Lead = "lead", SortOrder = 0 };
            feat.Bullets.Add(new FeatureBullet { Label = "L", Text = "T", SortOrder = 0 });
            v.Features.Add(feat);

            var trim = new Trim { Name = "GT", ModelLabel = "GS4", ImagePath = "/trim.jpg", SortOrder = 0 };
            trim.PriceRows.Add(new TrimPriceRow { Text = "Total: 10,000", SortOrder = 0 });
            v.Trims.Add(trim);

            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vid = v.Id;
        }

        using (var db = NewDb(name))
        {
            var v = await db.Vehicles
                .Include(x => x.Headings)
                .Include(x => x.Stats)
                .Include(x => x.Cards)
                .Include(x => x.SafetyToggles)
                .Include(x => x.WarrantyLinks)
                .Include(x => x.Sliders).ThenInclude(s => s.Slides)
                .Include(x => x.GalleryTabs).ThenInclude(g => g.Images)
                .Include(x => x.Features).ThenInclude(f => f.Bullets)
                .Include(x => x.Trims).ThenInclude(t => t.PriceRows)
                .Include(x => x.Quality)
                .FirstAsync(x => x.Id == vid);

            Assert.Equal("/tb.jpg", v.TechBannerImage);
            Assert.Equal("note-en", v.StatsNote.En);
            Assert.Single(v.Headings);
            Assert.Equal(SectionKey.Overview, v.Headings[0].Key);
            Assert.Single(v.Stats);
            Assert.Equal("177 HP", v.Stats[0].Value.En);
            Assert.Single(v.Cards);
            Assert.Single(v.SafetyToggles);
            Assert.Single(v.WarrantyLinks);
            Assert.Single(v.Sliders);
            Assert.Equal(2, v.Sliders[0].Slides.Count);
            Assert.Single(v.GalleryTabs);
            Assert.Single(v.GalleryTabs[0].Images);
            Assert.Single(v.Features);
            Assert.Single(v.Features[0].Bullets);
            Assert.Equal(FeatureGroup.Design, v.Features[0].GroupKey);
            Assert.Single(v.Trims);
            Assert.Equal("GS4", v.Trims[0].ModelLabel.En);
            Assert.Single(v.Trims[0].PriceRows);
            Assert.NotNull(v.Quality);
            Assert.Equal("strap", v.Quality!.Strapline.En);
        }
    }

    [Fact]
    public async Task Vehicle_WithoutQuality_RoundTrips_QualityNull()
    {
        var name = nameof(Vehicle_WithoutQuality_RoundTrips_QualityNull);
        int vid;
        using (var db = NewDb(name))
        {
            var v = new Vehicle { Slug = "no-quality", Name = "No Quality" };
            db.Vehicles.Add(v);
            await db.SaveChangesAsync();
            vid = v.Id;
        }
        using (var db = NewDb(name))
        {
            var v = await db.Vehicles.Include(x => x.Quality).FirstAsync(x => x.Id == vid);
            Assert.Null(v.Quality);
        }
    }
}
```

- [ ] **Step 2: Run & fail (or fail at compile).** `dotnet test Solution/GAC.Tests --filter RichSectionRoundTripTests` — this should pass straight away if Tasks 1-9 are correct; run it first to confirm the graph saves. If any Include throws "navigation not found" or a count mismatch occurs, fix the offending config in `ContentConfigurations.cs` before proceeding (TDD safety net for the wiring).

- [ ] **Step 3: Confirm green.** `dotnet test Solution/GAC.Tests --filter RichSectionRoundTripTests` — both facts green.

- [ ] **Step 4: Commit.** `git commit -am "test: in-memory round-trip for full Vehicle rich-section graph"`.

---

### Task 11: Generate the EF migration AddVehicleRichSections

**Files:**
- Create `Solution/GAC.Infrastructure/Migrations/<timestamp>_AddVehicleRichSections.cs` (generated)
- Create `Solution/GAC.Infrastructure/Migrations/<timestamp>_AddVehicleRichSections.Designer.cs` (generated)
- Modify `Solution/GAC.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` (regenerated by the tool)

**Interfaces:**
- Consumes: all entities, DbSets, and configs from Tasks 1-9. Migration target project `GAC.Infrastructure`, startup project `GAC.Web`.

Steps:

- [ ] **Step 1: Build first to ensure the model compiles.** `dotnet build Solution/GAC.Infrastructure` — must succeed (the design-time model is read from this assembly).

- [ ] **Step 2: Generate the migration.** From the repo root run:
```
dotnet ef migrations add AddVehicleRichSections --project Solution/GAC.Infrastructure --startup-project Solution/GAC.Web
```
Expect output: `Done. To undo this action, use 'ef migrations remove'.` and three changed files (the migration `.cs`, its `.Designer.cs`, and the regenerated `ApplicationDbContextModelSnapshot.cs`).

- [ ] **Step 3: Inspect the generated Up().** Open the new `<timestamp>_AddVehicleRichSections.cs` and verify it:
  - `AddColumn` on `Vehicles`: `TechBannerImage` (nvarchar(300) null), `EnquiryBgImage` (nvarchar(300) null), `StatsNote_En/_Ar`, `EnquiryTitle_En/_Ar`, `EnquirySub_En/_Ar`, `EnquiryLead_En/_Ar` (all nvarchar(max) null).
  - `AddColumn` on `FeatureSections`: `GroupKey` (int not null default 0), `TabLabel_En/_Ar`, `Lead_En/_Ar`.
  - `AddColumn` on `Trims`: `ImagePath` (nvarchar(300) null), `ModelLabel_En/_Ar`.
  - `CreateTable` for: `SectionHeadings`, `StatItems`, `SliderGroups`, `SliderSlides`, `GalleryTabs`, `GalleryImages`, `QualityBlocks`, `CardItems`, `SafetyToggles`, `WarrantyLinks`, `FeatureBullets`, `TrimPriceRows`, each with its FK to the parent (Vehicle/SliderGroup/GalleryTab/FeatureSection/Trim) `ON DELETE CASCADE` and the matching `IX_...` index. `QualityBlocks` must have a UNIQUE index on `VehicleId` (the 0/1 relationship). It must NOT drop or rename any existing column/table.
  If anything is wrong, `dotnet ef migrations remove --project Solution/GAC.Infrastructure --startup-project Solution/GAC.Web`, fix the config, and regenerate.

- [ ] **Step 4: Confirm the test suite still builds against the new snapshot.** `dotnet test Solution/GAC.Tests --filter "RichSectionModelTests|RichSectionRoundTripTests|DbContextModelTests"` — green (the model snapshot and live model agree).

- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat: add AddVehicleRichSections EF migration"`.

---

### Task 12: Guarded SQL script for AddVehicleRichSections

**Files:**
- Create `Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql`

**Interfaces:**
- Consumes: the `MigrationId` of the migration generated in Task 11 (the `<timestamp>_AddVehicleRichSections` folder/file name). Produces a history-guarded additive script for the shared prod DB.

Steps:

- [ ] **Step 1: Read the real MigrationId.** Run `ls Solution/GAC.Infrastructure/Migrations` (via Glob) and note the exact `<timestamp>_AddVehicleRichSections` id (e.g. `20260623HHMMSS_AddVehicleRichSections`). Use it verbatim everywhere `@MIGRATION_ID@` appears below.

- [ ] **Step 2: Write the guarded SQL.** Create `Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql`. The script: ensures `[__EFMigrationsHistory]` exists, guards every DDL statement on `WHERE [MigrationId] = N'@MIGRATION_ID@'` NOT EXISTS, then inserts the history row last. Replace `@MIGRATION_ID@` with the value from Step 1. Mirror the column types EF produced (verify against Task 11's `Up()`):
```sql
-- AddVehicleRichSections — guarded, additive. Apply to shared GAC DB. Do NOT run dotnet ef database update.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;

-- Vehicle new scalar/localized columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    ALTER TABLE [Vehicles] ADD
        [TechBannerImage] nvarchar(300) NULL,
        [EnquiryBgImage] nvarchar(300) NULL,
        [StatsNote_En] nvarchar(max) NULL,
        [StatsNote_Ar] nvarchar(max) NULL,
        [EnquiryTitle_En] nvarchar(max) NULL,
        [EnquiryTitle_Ar] nvarchar(max) NULL,
        [EnquirySub_En] nvarchar(max) NULL,
        [EnquirySub_Ar] nvarchar(max) NULL,
        [EnquiryLead_En] nvarchar(max) NULL,
        [EnquiryLead_Ar] nvarchar(max) NULL;
END;

-- FeatureSection new columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    ALTER TABLE [FeatureSections] ADD
        [GroupKey] int NOT NULL DEFAULT 0,
        [TabLabel_En] nvarchar(max) NULL,
        [TabLabel_Ar] nvarchar(max) NULL,
        [Lead_En] nvarchar(max) NULL,
        [Lead_Ar] nvarchar(max) NULL;
END;

-- Trim new columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    ALTER TABLE [Trims] ADD
        [ImagePath] nvarchar(300) NULL,
        [ModelLabel_En] nvarchar(max) NULL,
        [ModelLabel_Ar] nvarchar(max) NULL;
END;

-- SectionHeadings
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [SectionHeadings] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Key] int NOT NULL,
        [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
        [Sub_En] nvarchar(max) NULL, [Sub_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL, [Body_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_SectionHeadings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SectionHeadings_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SectionHeadings_VehicleId] ON [SectionHeadings] ([VehicleId]);
END;

-- StatItems
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [StatItems] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
        [Value_En] nvarchar(max) NULL, [Value_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_StatItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StatItems_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_StatItems_VehicleId] ON [StatItems] ([VehicleId]);
END;

-- SliderGroups
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [SliderGroups] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Eyebrow_En] nvarchar(max) NULL, [Eyebrow_Ar] nvarchar(max) NULL,
        [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SliderGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SliderGroups_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SliderGroups_VehicleId] ON [SliderGroups] ([VehicleId]);
END;

-- SliderSlides
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [SliderSlides] (
        [Id] int NOT NULL IDENTITY,
        [SliderGroupId] int NOT NULL,
        [ImagePath] nvarchar(300) NULL,
        [Alt_En] nvarchar(max) NULL, [Alt_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SliderSlides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SliderSlides_SliderGroups_SliderGroupId] FOREIGN KEY ([SliderGroupId]) REFERENCES [SliderGroups] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SliderSlides_SliderGroupId] ON [SliderSlides] ([SliderGroupId]);
END;

-- GalleryTabs
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [GalleryTabs] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GalleryTabs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GalleryTabs_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_GalleryTabs_VehicleId] ON [GalleryTabs] ([VehicleId]);
END;

-- GalleryImages
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [GalleryImages] (
        [Id] int NOT NULL IDENTITY,
        [GalleryTabId] int NOT NULL,
        [ImagePath] nvarchar(300) NULL,
        [Alt_En] nvarchar(max) NULL, [Alt_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GalleryImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GalleryImages_GalleryTabs_GalleryTabId] FOREIGN KEY ([GalleryTabId]) REFERENCES [GalleryTabs] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_GalleryImages_GalleryTabId] ON [GalleryImages] ([GalleryTabId]);
END;

-- QualityBlocks (0/1 per vehicle -> UNIQUE VehicleId)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [QualityBlocks] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [MainImage] nvarchar(300) NULL,
        [ThumbImage] nvarchar(300) NULL,
        [Strapline_En] nvarchar(max) NULL, [Strapline_Ar] nvarchar(max) NULL,
        [Content_En] nvarchar(max) NULL, [Content_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_QualityBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QualityBlocks_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_QualityBlocks_VehicleId] ON [QualityBlocks] ([VehicleId]);
END;

-- CardItems
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [CardItems] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
        [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CardItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CardItems_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CardItems_VehicleId] ON [CardItems] ([VehicleId]);
END;

-- SafetyToggles
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [SafetyToggles] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(300) NULL,
        [Strap_En] nvarchar(max) NULL, [Strap_Ar] nvarchar(max) NULL,
        [Content_En] nvarchar(max) NULL, [Content_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SafetyToggles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SafetyToggles_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SafetyToggles_VehicleId] ON [SafetyToggles] ([VehicleId]);
END;

-- WarrantyLinks
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [WarrantyLinks] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
        [Url] nvarchar(500) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_WarrantyLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarrantyLinks_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_WarrantyLinks_VehicleId] ON [WarrantyLinks] ([VehicleId]);
END;

-- FeatureBullets
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [FeatureBullets] (
        [Id] int NOT NULL IDENTITY,
        [FeatureSectionId] int NOT NULL,
        [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
        [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_FeatureBullets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeatureBullets_FeatureSections_FeatureSectionId] FOREIGN KEY ([FeatureSectionId]) REFERENCES [FeatureSections] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FeatureBullets_FeatureSectionId] ON [FeatureBullets] ([FeatureSectionId]);
END;

-- TrimPriceRows
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    CREATE TABLE [TrimPriceRows] (
        [Id] int NOT NULL IDENTITY,
        [TrimId] int NOT NULL,
        [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_TrimPriceRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrimPriceRows_Trims_TrimId] FOREIGN KEY ([TrimId]) REFERENCES [Trims] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_TrimPriceRows_TrimId] ON [TrimPriceRows] ([TrimId]);
END;

-- Stamp history
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'@MIGRATION_ID@')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'@MIGRATION_ID@', N'9.0.6');
END;

COMMIT;
GO
```

- [ ] **Step 3: Cross-check column names/types against the generated migration.** Open Task 11's `Up()` and confirm every column name in this SQL matches (especially `[Key]`, the bracket-escaped reserved word on `SectionHeadings`, and the `_En`/`_Ar` owned suffixes). Fix any divergence so the script is byte-faithful to what EF would emit. Confirm `@MIGRATION_ID@` was replaced everywhere with the real id from Step 1.

- [ ] **Step 4: Commit.** `git add Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql && git commit -m "feat: guarded SQL for AddVehicleRichSections (prod, additive, history-stamped)"`.


---

## Phase 2 — Admin backend

### Task 20: Admin service — GetAsync eager-load + Vehicle scalar/localized save path for new fields

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes (already defined by the entity/EF tasks): `Vehicle` navs `Headings`, `Stats`, `Sliders` (→`Slides`), `GalleryTabs` (→`Images`), `Cards`, `SafetyToggles`, `WarrantyLinks`, `Quality`, plus `Features`→`Bullets`, `Trims`→`PriceRows`; `Vehicle` scalars `TechBannerImage`, `EnquiryBgImage` and localized `StatsNote`, `EnquiryTitle`, `EnquirySub`, `EnquiryLead`; `DbSet`s `SectionHeadings`, `StatItems`, `SliderGroups`, `SliderSlides`, `FeatureBullets`, `GalleryTabs`, `GalleryImages`, `QualityBlocks`, `CardItems`, `SafetyToggles`, `TrimPriceRows`, `WarrantyLinks`.
- Produces: updated `AdminVehicleService.GetAsync` (all new collections eager-loaded) and `AdminVehicleService.UpdateAsync` (persists the 6 new Vehicle fields). No interface signature change in this task.

- [ ] **Step 1: Write failing test for GetAsync include + Update of new Vehicle fields.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task GetAsync_Includes_NewCollections_AndQuality()
    {
        var db = NewDb(nameof(GetAsync_Includes_NewCollections_AndQuality));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "inc", Name = "Inc" });
        await svc.AddStatAsync(id, new LocalizedText { En = "Power" }, new LocalizedText { En = "177 HP" });
        var sg = await svc.AddSliderAsync(id, new LocalizedText { En = "Eye" }, new LocalizedText { En = "Title" });
        await svc.AddSliderSlideAsync(sg, "/uploads/s1.png", new LocalizedText { En = "alt" });
        var gt = await svc.AddGalleryTabAsync(id, new LocalizedText { En = "Exterior" });
        await svc.AddGalleryImageAsync(gt, "/uploads/g1.png", new LocalizedText { En = "alt" });
        await svc.AddCardAsync(id, new LocalizedText { En = "Card" }, new LocalizedText { En = "txt" }, "/uploads/c1.png");
        await svc.AddSafetyToggleAsync(id, new LocalizedText { En = "Brakes" }, "/uploads/b.png", new LocalizedText { En = "strap" }, new LocalizedText { En = "body" });
        await svc.AddWarrantyLinkAsync(id, new LocalizedText { En = "PDF" }, "/docs/w.pdf");
        await svc.UpsertSectionHeadingAsync(id, SectionKey.Overview, new LocalizedText { En = "Overview" }, new LocalizedText { En = "sub" }, new LocalizedText { En = "body" });
        await svc.UpsertQualityAsync(id, "/uploads/q-main.png", "/uploads/q-thumb.png", new LocalizedText { En = "strap" }, new LocalizedText { En = "content" });

        var v = await svc.GetAsync(id);
        Assert.NotNull(v);
        Assert.Single(v!.Stats);
        Assert.Single(v.Sliders);
        Assert.Single(v.Sliders[0].Slides);
        Assert.Single(v.GalleryTabs);
        Assert.Single(v.GalleryTabs[0].Images);
        Assert.Single(v.Cards);
        Assert.Single(v.SafetyToggles);
        Assert.Single(v.WarrantyLinks);
        Assert.Single(v.Headings);
        Assert.NotNull(v.Quality);
    }

    [Fact]
    public async Task UpdateAsync_PersistsEnquiryAndTechFields()
    {
        var db = NewDb(nameof(UpdateAsync_PersistsEnquiryAndTechFields));
        var svc = NewSvc(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "enq", Name = "Enq" });

        var ok = await svc.UpdateAsync(new Vehicle
        {
            Id = id, Slug = "enq", Name = "Enq",
            TechBannerImage = "/uploads/tech.png",
            EnquiryBgImage = "/uploads/bg.png",
            StatsNote = new LocalizedText { En = "note en", Ar = "note ar" },
            EnquiryTitle = new LocalizedText { En = "Get a quote" },
            EnquirySub = new LocalizedText { En = "sub" },
            EnquiryLead = new LocalizedText { En = "lead" }
        });

        Assert.True(ok);
        var v = await svc.GetAsync(id);
        Assert.Equal("/uploads/tech.png", v!.TechBannerImage);
        Assert.Equal("/uploads/bg.png", v.EnquiryBgImage);
        Assert.Equal("note en", v.StatsNote.En);
        Assert.Equal("Get a quote", v.EnquiryTitle.En);
        Assert.Equal("sub", v.EnquirySub.En);
        Assert.Equal("lead", v.EnquiryLead.En);
    }
```

- [ ] **Step 2: Run the tests & watch them fail to compile / fail.** Run from `C:/Users/anas-/source/repos/GAC/Solution`:
```
dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.GetAsync_Includes_NewCollections_AndQuality|FullyQualifiedName~AdminVehicleServiceTests.UpdateAsync_PersistsEnquiryAndTechFields"
```
Expect a build error (methods `AddStatAsync`, `AddSliderAsync`, `UpsertSectionHeadingAsync`, etc. do not exist yet, and `Vehicle.TechBannerImage` etc. assigned in test). These methods are added in later tasks; for THIS task confirm the compile error names the new service members, then proceed to implement only the GetAsync includes + UpdateAsync field copies (the Add*/Upsert* method bodies arrive in Tasks 21-31). To keep this task independently runnable, comment out the lines in `GetAsync_Includes_NewCollections_AndQuality` that call Add*/Upsert* not yet implemented is NOT allowed — instead implement Task 20 LAST among the service edits, OR run this test only after Tasks 21-31 land. Document this ordering in the commit message.

- [ ] **Step 3: Implement GetAsync eager-loading.** Replace the body of `GetAsync` in `AdminVehicleService.cs`:
```csharp
    public async Task<Vehicle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Vehicles
            .Include(v => v.Images)
            .Include(v => v.Features).ThenInclude(f => f.Bullets)
            .Include(v => v.SpecGroups).ThenInclude(g => g.Rows)
            .Include(v => v.Colors)
            .Include(v => v.Trims).ThenInclude(t => t.PriceRows)
            .Include(v => v.Headings)
            .Include(v => v.Stats)
            .Include(v => v.Sliders).ThenInclude(s => s.Slides)
            .Include(v => v.GalleryTabs).ThenInclude(g => g.Images)
            .Include(v => v.Cards)
            .Include(v => v.SafetyToggles)
            .Include(v => v.WarrantyLinks)
            .Include(v => v.Quality)
            .FirstOrDefaultAsync(v => v.Id == id, ct);
```

- [ ] **Step 4: Implement UpdateAsync field copies.** In `UpdateAsync`, after the existing `existing.MetaDescription = vehicle.MetaDescription;` line and before `await _db.SaveChangesAsync(ct);`, add:
```csharp
        existing.TechBannerImage = vehicle.TechBannerImage;
        existing.EnquiryBgImage = vehicle.EnquiryBgImage;
        existing.StatsNote = vehicle.StatsNote;
        existing.EnquiryTitle = vehicle.EnquiryTitle;
        existing.EnquirySub = vehicle.EnquirySub;
        existing.EnquiryLead = vehicle.EnquiryLead;
```

- [ ] **Step 5: Run the two tests & confirm pass** (after Tasks 21-31 are merged the GetAsync test passes; the UpdateAsync test passes immediately).
```
dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.UpdateAsync_PersistsEnquiryAndTechFields"
```

- [ ] **Step 6: Commit.** `git commit -am "feat: eager-load rich vehicle sections in GetAsync + persist tech/enquiry/stats-note fields"`

---

### Task 21: SectionHeading upsert (set-by-key) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `SectionHeading { int Id; int VehicleId; SectionKey Key; LocalizedText Title,Sub,Body; }`, enum `SectionKey`, `DbSet<SectionHeading> SectionHeadings`.
- Produces: `Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default)`; controller `UpsertSectionHeading`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task UpsertSectionHeading_InsertsThenUpdatesInPlace()
    {
        var db = NewDb(nameof(UpsertSectionHeading_InsertsThenUpdatesInPlace));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "sh", Name = "S" });

        var first = await svc.UpsertSectionHeadingAsync(vid, SectionKey.Overview,
            new LocalizedText { En = "Overview" }, new LocalizedText { En = "sub" }, new LocalizedText { En = "body" });
        Assert.Equal(1, await db.Set<SectionHeading>().CountAsync());

        var second = await svc.UpsertSectionHeadingAsync(vid, SectionKey.Overview,
            new LocalizedText { En = "Overview 2" }, new LocalizedText { En = "sub 2" }, new LocalizedText { En = "body 2" });
        Assert.Equal(first, second); // same row, not a new one
        Assert.Equal(1, await db.Set<SectionHeading>().CountAsync());
        var row = await db.Set<SectionHeading>().FindAsync(first);
        Assert.Equal("Overview 2", row!.Title.En);
        Assert.Equal("body 2", row.Body.En);

        // a different key creates a second row
        await svc.UpsertSectionHeadingAsync(vid, SectionKey.Design,
            new LocalizedText { En = "Design" }, new LocalizedText(), new LocalizedText());
        Assert.Equal(2, await db.Set<SectionHeading>().CountAsync());
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.UpsertSectionHeading_InsertsThenUpdatesInPlace"` — expect build failure (`UpsertSectionHeadingAsync` missing).

- [ ] **Step 3: Add interface member.** In `IAdminVehicleService.cs`, after the Trims block, add a region and method:
```csharp
    // Section headings (set-by-key, no reorder)
    Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** In `AdminVehicleService.cs`, add before the `// ---- shared helpers ----` region:
```csharp
    // ---- Section headings ----
    public async Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var existing = await _db.Set<SectionHeading>().FirstOrDefaultAsync(h => h.VehicleId == vehicleId && h.Key == key, ct);
        if (existing is null)
        {
            existing = new SectionHeading { VehicleId = vehicleId, Key = key };
            _db.Set<SectionHeading>().Add(existing);
        }
        existing.Title = title;
        existing.Sub = sub;
        existing.Body = body;
        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }
```

- [ ] **Step 5: Add controller action.** In `VehiclesController.cs`, add (after the Trim actions). Note `SectionKey` is bound from a hidden `<input name="key">` int/enum value:
```csharp
    [HttpPost] public async Task<IActionResult> UpsertSectionHeading(int vehicleId, SectionKey key, string? titleEn, string? titleAr, string? subEn, string? subAr, string? bodyEn, string? bodyAr)
    { await _svc.UpsertSectionHeadingAsync(vehicleId, key, new() { En = titleEn, Ar = titleAr }, new() { En = subEn, Ar = subAr }, new() { En = bodyEn, Ar = bodyAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```
Add `using GAC.Core.Content;` is already present (top of file).

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.UpsertSectionHeading_InsertsThenUpdatesInPlace"`

- [ ] **Step 7: Commit.** `git commit -am "feat: UpsertSectionHeadingAsync + controller (set-by-key section heads)"`

---

### Task 22: StatItem add/remove/move — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `StatItem : IOrderable { Id; VehicleId; LocalizedText Label,Value; SortOrder; }`, `DbSet<StatItem> StatItems`. Reuses `RemoveByIdAsync<T>`, `SwapOrderAsync<T>`.
- Produces: `Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default)`, `Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default)`, `Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default)`; controller `AddStat`/`RemoveStat`/`MoveStat`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Stat_AddMoveRemove()
    {
        var db = NewDb(nameof(Stat_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "st", Name = "S" });
        var a = await svc.AddStatAsync(vid, new LocalizedText { En = "Power" }, new LocalizedText { En = "177 HP" });
        var b = await svc.AddStatAsync(vid, new LocalizedText { En = "Torque" }, new LocalizedText { En = "270 Nm" });
        Assert.Equal(0, (await db.Set<StatItem>().FindAsync(a))!.SortOrder);
        Assert.Equal(1, (await db.Set<StatItem>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.MoveStatAsync(b, -1));
        Assert.Equal(0, (await db.Set<StatItem>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveStatAsync(a));
        Assert.Equal(1, await db.Set<StatItem>().CountAsync());
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Stat_AddMoveRemove"` — expect missing `AddStatAsync` build error.

- [ ] **Step 3: Add interface members.** In `IAdminVehicleService.cs` add:
```csharp
    // Overview stats
    Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
    Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default);
    Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** In `AdminVehicleService.cs` add before `// ---- shared helpers ----`:
```csharp
    // ---- Overview stats ----
    public async Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new StatItem { VehicleId = vehicleId, Label = label, Value = value, SortOrder = await _db.Set<StatItem>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<StatItem>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default)
        => await RemoveByIdAsync<StatItem>(statId, ct);

    public async Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<StatItem>().FindAsync([statId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<StatItem>(x => x.VehicleId == e.VehicleId, statId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.** In `VehiclesController.cs` add:
```csharp
    [HttpPost] public async Task<IActionResult> AddStat(int vehicleId, string? labelEn, string? labelAr, string? valueEn, string? valueAr)
    { await _svc.AddStatAsync(vehicleId, new() { En = labelEn, Ar = labelAr }, new() { En = valueEn, Ar = valueAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveStat(int statId, int vehicleId)
    { await _svc.RemoveStatAsync(statId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveStat(int statId, int vehicleId, int direction)
    { await _svc.MoveStatAsync(statId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Stat_AddMoveRemove"`

- [ ] **Step 7: Commit.** `git commit -am "feat: StatItem add/remove/move service + controller"`

---

### Task 23: SliderGroup + SliderSlide (grandchild) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `SliderGroup : IOrderable { Id; VehicleId; LocalizedText Eyebrow,Title; SortOrder; List<SliderSlide> Slides; }`, `SliderSlide : IOrderable { Id; int SliderGroupId; string? ImagePath; LocalizedText Alt; SortOrder; }`, `DbSet<SliderGroup> SliderGroups`, `DbSet<SliderSlide> SliderSlides`. Reuses `RemoveByIdAsync`, `SwapOrderAsync`.
- Produces: `AddSliderAsync(int vehicleId, LocalizedText eyebrow, LocalizedText title, CancellationToken ct=default)`, `RemoveSliderAsync(int sliderId, …)`, `MoveSliderAsync(int sliderId, int direction, …)`, `AddSliderSlideAsync(int sliderGroupId, string? imagePath, LocalizedText alt, …)`, `RemoveSliderSlideAsync(int slideId, …)`, `MoveSliderSlideAsync(int slideId, int direction, …)`; matching controller actions.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Slider_And_Slide_AddMoveRemove()
    {
        var db = NewDb(nameof(Slider_And_Slide_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "sl", Name = "S" });
        var g1 = await svc.AddSliderAsync(vid, new LocalizedText { En = "Eye1" }, new LocalizedText { En = "T1" });
        var g2 = await svc.AddSliderAsync(vid, new LocalizedText { En = "Eye2" }, new LocalizedText { En = "T2" });
        Assert.Equal(0, (await db.Set<SliderGroup>().FindAsync(g1))!.SortOrder);
        Assert.True(await svc.MoveSliderAsync(g2, -1));
        Assert.Equal(0, (await db.Set<SliderGroup>().FindAsync(g2))!.SortOrder);

        var s1 = await svc.AddSliderSlideAsync(g1, "/uploads/a.png", new LocalizedText { En = "a" });
        var s2 = await svc.AddSliderSlideAsync(g1, "/uploads/b.png", new LocalizedText { En = "b" });
        Assert.Equal(0, (await db.Set<SliderSlide>().FindAsync(s1))!.SortOrder);
        Assert.Equal(1, (await db.Set<SliderSlide>().FindAsync(s2))!.SortOrder);
        Assert.True(await svc.MoveSliderSlideAsync(s2, -1));
        Assert.Equal(0, (await db.Set<SliderSlide>().FindAsync(s2))!.SortOrder);
        Assert.True(await svc.RemoveSliderSlideAsync(s1));
        Assert.Equal(1, await db.Set<SliderSlide>().CountAsync());
        Assert.True(await svc.RemoveSliderAsync(g1));
    }

    [Fact]
    public async Task AddSliderSlide_OnMissingGroup_ReturnsZero()
    {
        var db = NewDb(nameof(AddSliderSlide_OnMissingGroup_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddSliderSlideAsync(999999, "/x.png", new LocalizedText { En = "x" }));
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Slider_And_Slide_AddMoveRemove|FullyQualifiedName~AdminVehicleServiceTests.AddSliderSlide_OnMissingGroup_ReturnsZero"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Sliders (group -> slides)
    Task<int> AddSliderAsync(int vehicleId, LocalizedText eyebrow, LocalizedText title, CancellationToken ct = default);
    Task<bool> RemoveSliderAsync(int sliderId, CancellationToken ct = default);
    Task<bool> MoveSliderAsync(int sliderId, int direction, CancellationToken ct = default);
    Task<int> AddSliderSlideAsync(int sliderGroupId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> RemoveSliderSlideAsync(int slideId, CancellationToken ct = default);
    Task<bool> MoveSliderSlideAsync(int slideId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Sliders ----
    public async Task<int> AddSliderAsync(int vehicleId, LocalizedText eyebrow, LocalizedText title, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new SliderGroup { VehicleId = vehicleId, Eyebrow = eyebrow, Title = title, SortOrder = await _db.Set<SliderGroup>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<SliderGroup>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSliderAsync(int sliderId, CancellationToken ct = default)
        => await RemoveByIdAsync<SliderGroup>(sliderId, ct);

    public async Task<bool> MoveSliderAsync(int sliderId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<SliderGroup>().FindAsync([sliderId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SliderGroup>(x => x.VehicleId == e.VehicleId, sliderId, direction, ct);
    }

    public async Task<int> AddSliderSlideAsync(int sliderGroupId, string? imagePath, LocalizedText alt, CancellationToken ct = default)
    {
        if (!await _db.Set<SliderGroup>().AnyAsync(g => g.Id == sliderGroupId, ct)) return 0;
        var e = new SliderSlide { SliderGroupId = sliderGroupId, ImagePath = imagePath, Alt = alt, SortOrder = await _db.Set<SliderSlide>().CountAsync(x => x.SliderGroupId == sliderGroupId, ct) };
        _db.Set<SliderSlide>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSliderSlideAsync(int slideId, CancellationToken ct = default)
        => await RemoveByIdAsync<SliderSlide>(slideId, ct);

    public async Task<bool> MoveSliderSlideAsync(int slideId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<SliderSlide>().FindAsync([slideId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SliderSlide>(x => x.SliderGroupId == e.SliderGroupId, slideId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> AddSlider(int vehicleId, string? eyebrowEn, string? eyebrowAr, string? titleEn, string? titleAr)
    { await _svc.AddSliderAsync(vehicleId, new() { En = eyebrowEn, Ar = eyebrowAr }, new() { En = titleEn, Ar = titleAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSlider(int sliderId, int vehicleId)
    { await _svc.RemoveSliderAsync(sliderId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSlider(int sliderId, int vehicleId, int direction)
    { await _svc.MoveSliderAsync(sliderId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> AddSliderSlide(int sliderGroupId, int vehicleId, string? imagePath, string? altEn, string? altAr)
    { await _svc.AddSliderSlideAsync(sliderGroupId, imagePath, new() { En = altEn, Ar = altAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSliderSlide(int slideId, int vehicleId)
    { await _svc.RemoveSliderSlideAsync(slideId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSliderSlide(int slideId, int vehicleId, int direction)
    { await _svc.MoveSliderSlideAsync(slideId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Slider"`

- [ ] **Step 7: Commit.** `git commit -am "feat: SliderGroup + SliderSlide add/remove/move service + controller"`

---

### Task 24: FeatureSection extension + FeatureBullet (grandchild) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: extended `FeatureSection { …; FeatureGroup GroupKey; LocalizedText TabLabel; LocalizedText Lead; List<FeatureBullet> Bullets; }`, `FeatureBullet : IOrderable { Id; int FeatureSectionId; LocalizedText Label,Text; SortOrder; }`, enum `FeatureGroup`, `DbSet<FeatureBullet> FeatureBullets`. Existing `AddFeatureAsync(int, FeatureSection)` / `UpdateFeatureAsync(FeatureSection)` are reused; UpdateFeature must copy the NEW fields.
- Produces: `AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct=default)`, `RemoveFeatureBulletAsync(int bulletId, …)`, `MoveFeatureBulletAsync(int bulletId, int direction, …)`; controller `AddFeatureBullet`/`RemoveFeatureBullet`/`MoveFeatureBullet`. Updated `UpdateFeatureAsync` persisting `GroupKey`, `TabLabel`, `Lead`. Updated `AddFeature`/`FeatureSave` controller binding new fields.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Feature_NewFields_Persist_AndBullets_AddMoveRemove()
    {
        var db = NewDb(nameof(Feature_NewFields_Persist_AndBullets_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "fb", Name = "F" });
        var fid = await svc.AddFeatureAsync(vid, new FeatureSection
        {
            Heading = "Panel",
            GroupKey = FeatureGroup.Design,
            TabLabel = new LocalizedText { En = "Design" },
            Lead = new LocalizedText { En = "lead text" }
        });
        var f = await svc.GetFeatureAsync(fid);
        Assert.Equal(FeatureGroup.Design, f!.GroupKey);
        Assert.Equal("Design", f.TabLabel.En);
        Assert.Equal("lead text", f.Lead.En);

        var b1 = await svc.AddFeatureBulletAsync(fid, new LocalizedText { En = "L1" }, new LocalizedText { En = "T1" });
        var b2 = await svc.AddFeatureBulletAsync(fid, new LocalizedText { En = "L2" }, new LocalizedText { En = "T2" });
        Assert.Equal(0, (await db.Set<FeatureBullet>().FindAsync(b1))!.SortOrder);
        Assert.Equal(1, (await db.Set<FeatureBullet>().FindAsync(b2))!.SortOrder);
        Assert.True(await svc.MoveFeatureBulletAsync(b2, -1));
        Assert.Equal(0, (await db.Set<FeatureBullet>().FindAsync(b2))!.SortOrder);
        Assert.True(await svc.RemoveFeatureBulletAsync(b1));
        Assert.Equal(1, await db.Set<FeatureBullet>().CountAsync());

        var ok = await svc.UpdateFeatureAsync(new FeatureSection
        {
            Id = fid, Heading = "Panel2", GroupKey = FeatureGroup.Performance,
            TabLabel = new LocalizedText { En = "Perf" }, Lead = new LocalizedText { En = "lead2" }
        });
        Assert.True(ok);
        var f2 = await svc.GetFeatureAsync(fid);
        Assert.Equal(FeatureGroup.Performance, f2!.GroupKey);
        Assert.Equal("Perf", f2.TabLabel.En);
        Assert.Equal("lead2", f2.Lead.En);
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Feature_NewFields_Persist_AndBullets_AddMoveRemove"`

- [ ] **Step 3: Update `UpdateFeatureAsync` to copy new fields.** In `AdminVehicleService.cs`, in `UpdateFeatureAsync`, after `existing.Layout = feature.Layout;` add:
```csharp
        existing.GroupKey = feature.GroupKey;
        existing.TabLabel = feature.TabLabel;
        existing.Lead = feature.Lead;
```

- [ ] **Step 4: Add interface members.**
```csharp
    // Feature bullets (feature -> bullets)
    Task<int> AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct = default);
    Task<bool> RemoveFeatureBulletAsync(int bulletId, CancellationToken ct = default);
    Task<bool> MoveFeatureBulletAsync(int bulletId, int direction, CancellationToken ct = default);
```

- [ ] **Step 5: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Feature bullets ----
    public async Task<int> AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct = default)
    {
        if (!await _db.Set<FeatureSection>().AnyAsync(f => f.Id == featureSectionId, ct)) return 0;
        var e = new FeatureBullet { FeatureSectionId = featureSectionId, Label = label, Text = text, SortOrder = await _db.Set<FeatureBullet>().CountAsync(x => x.FeatureSectionId == featureSectionId, ct) };
        _db.Set<FeatureBullet>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveFeatureBulletAsync(int bulletId, CancellationToken ct = default)
        => await RemoveByIdAsync<FeatureBullet>(bulletId, ct);

    public async Task<bool> MoveFeatureBulletAsync(int bulletId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<FeatureBullet>().FindAsync([bulletId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<FeatureBullet>(x => x.FeatureSectionId == e.FeatureSectionId, bulletId, direction, ct);
    }
```

- [ ] **Step 6: Update + add controller actions.** In `VehiclesController.cs`, replace the existing `FeatureSave` action so it binds the new fields (the partial/form passes `groupKey`, `tabLabelEn/Ar`, `leadEn/Ar`):
```csharp
    [HttpPost]
    public async Task<IActionResult> FeatureSave(int vehicleId, FeatureSection feature)
    {
        feature.VehicleId = vehicleId;
        if (feature.Id == 0) await _svc.AddFeatureAsync(vehicleId, feature);
        else await _svc.UpdateFeatureAsync(feature);
        TempData["Flash"] = "Feature saved.";
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }
```
(MVC model-binds `GroupKey`, `TabLabel.En/Ar`, `Lead.En/Ar` directly onto the `FeatureSection` parameter from form field names `GroupKey`, `TabLabel.En`, `TabLabel.Ar`, `Lead.En`, `Lead.Ar` — the feature editor view supplies them.) Then add the bullet actions:
```csharp
    [HttpPost] public async Task<IActionResult> AddFeatureBullet(int featureSectionId, int vehicleId, string? labelEn, string? labelAr, string? textEn, string? textAr)
    { await _svc.AddFeatureBulletAsync(featureSectionId, new() { En = labelEn, Ar = labelAr }, new() { En = textEn, Ar = textAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveFeatureBullet(int bulletId, int vehicleId)
    { await _svc.RemoveFeatureBulletAsync(bulletId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveFeatureBullet(int bulletId, int vehicleId, int direction)
    { await _svc.MoveFeatureBulletAsync(bulletId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 7: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Feature_NewFields_Persist_AndBullets_AddMoveRemove"`

- [ ] **Step 8: Commit.** `git commit -am "feat: FeatureSection new fields persist + FeatureBullet add/remove/move service + controller"`

---

### Task 25: GalleryTab + GalleryImage (grandchild) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `GalleryTab : IOrderable { Id; VehicleId; LocalizedText Label; SortOrder; List<GalleryImage> Images; }`, `GalleryImage : IOrderable { Id; int GalleryTabId; string? ImagePath; LocalizedText Alt; SortOrder; }`, `DbSet<GalleryTab> GalleryTabs`, `DbSet<GalleryImage> GalleryImages`. Reuses `RemoveByIdAsync`, `SwapOrderAsync`.
- Produces: `AddGalleryTabAsync(int vehicleId, LocalizedText label, …)`, `RemoveGalleryTabAsync(int tabId, …)`, `MoveGalleryTabAsync(int tabId, int direction, …)`, `AddGalleryImageAsync(int galleryTabId, string? imagePath, LocalizedText alt, …)`, `RemoveGalleryImageAsync(int imageId, …)`, `MoveGalleryImageAsync(int imageId, int direction, …)`; matching controller actions.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task GalleryTab_And_Image_AddMoveRemove()
    {
        var db = NewDb(nameof(GalleryTab_And_Image_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "gt", Name = "G" });
        var t1 = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "Exterior" });
        var t2 = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "Interior" });
        Assert.Equal(0, (await db.Set<GalleryTab>().FindAsync(t1))!.SortOrder);
        Assert.True(await svc.MoveGalleryTabAsync(t2, -1));
        Assert.Equal(0, (await db.Set<GalleryTab>().FindAsync(t2))!.SortOrder);

        var i1 = await svc.AddGalleryImageAsync(t1, "/uploads/a.png", new LocalizedText { En = "a" });
        var i2 = await svc.AddGalleryImageAsync(t1, "/uploads/b.png", new LocalizedText { En = "b" });
        Assert.Equal(0, (await db.Set<GalleryImage>().FindAsync(i1))!.SortOrder);
        Assert.Equal(1, (await db.Set<GalleryImage>().FindAsync(i2))!.SortOrder);
        Assert.True(await svc.MoveGalleryImageAsync(i2, -1));
        Assert.Equal(0, (await db.Set<GalleryImage>().FindAsync(i2))!.SortOrder);
        Assert.True(await svc.RemoveGalleryImageAsync(i1));
        Assert.Equal(1, await db.Set<GalleryImage>().CountAsync());
        Assert.True(await svc.RemoveGalleryTabAsync(t1));
    }

    [Fact]
    public async Task AddGalleryImage_OnMissingTab_ReturnsZero()
    {
        var db = NewDb(nameof(AddGalleryImage_OnMissingTab_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddGalleryImageAsync(999999, "/x.png", new LocalizedText { En = "x" }));
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.GalleryTab_And_Image_AddMoveRemove|FullyQualifiedName~AdminVehicleServiceTests.AddGalleryImage_OnMissingTab_ReturnsZero"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Gallery tabs (tab -> images)
    Task<int> AddGalleryTabAsync(int vehicleId, LocalizedText label, CancellationToken ct = default);
    Task<bool> RemoveGalleryTabAsync(int tabId, CancellationToken ct = default);
    Task<bool> MoveGalleryTabAsync(int tabId, int direction, CancellationToken ct = default);
    Task<int> AddGalleryImageAsync(int galleryTabId, string? imagePath, LocalizedText alt, CancellationToken ct = default);
    Task<bool> RemoveGalleryImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> MoveGalleryImageAsync(int imageId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Gallery tabs ----
    public async Task<int> AddGalleryTabAsync(int vehicleId, LocalizedText label, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new GalleryTab { VehicleId = vehicleId, Label = label, SortOrder = await _db.Set<GalleryTab>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<GalleryTab>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveGalleryTabAsync(int tabId, CancellationToken ct = default)
        => await RemoveByIdAsync<GalleryTab>(tabId, ct);

    public async Task<bool> MoveGalleryTabAsync(int tabId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<GalleryTab>().FindAsync([tabId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<GalleryTab>(x => x.VehicleId == e.VehicleId, tabId, direction, ct);
    }

    public async Task<int> AddGalleryImageAsync(int galleryTabId, string? imagePath, LocalizedText alt, CancellationToken ct = default)
    {
        if (!await _db.Set<GalleryTab>().AnyAsync(g => g.Id == galleryTabId, ct)) return 0;
        var e = new GalleryImage { GalleryTabId = galleryTabId, ImagePath = imagePath, Alt = alt, SortOrder = await _db.Set<GalleryImage>().CountAsync(x => x.GalleryTabId == galleryTabId, ct) };
        _db.Set<GalleryImage>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveGalleryImageAsync(int imageId, CancellationToken ct = default)
        => await RemoveByIdAsync<GalleryImage>(imageId, ct);

    public async Task<bool> MoveGalleryImageAsync(int imageId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<GalleryImage>().FindAsync([imageId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<GalleryImage>(x => x.GalleryTabId == e.GalleryTabId, imageId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> AddGalleryTab(int vehicleId, string? labelEn, string? labelAr)
    { await _svc.AddGalleryTabAsync(vehicleId, new() { En = labelEn, Ar = labelAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveGalleryTab(int tabId, int vehicleId)
    { await _svc.RemoveGalleryTabAsync(tabId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveGalleryTab(int tabId, int vehicleId, int direction)
    { await _svc.MoveGalleryTabAsync(tabId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> AddGalleryImage(int galleryTabId, int vehicleId, string? imagePath, string? altEn, string? altAr)
    { await _svc.AddGalleryImageAsync(galleryTabId, imagePath, new() { En = altEn, Ar = altAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveGalleryImage(int imageId, int vehicleId)
    { await _svc.RemoveGalleryImageAsync(imageId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveGalleryImage(int imageId, int vehicleId, int direction)
    { await _svc.MoveGalleryImageAsync(imageId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```
NOTE: `RemoveGalleryImage`/`MoveGalleryImage` action names DO NOT collide with the existing `RemoveImage`/`MoveImage` (those are `VehicleImage`). Keep both.

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.GalleryTab"`

- [ ] **Step 7: Commit.** `git commit -am "feat: GalleryTab + GalleryImage add/remove/move service + controller"`

---

### Task 26: QualityBlock upsert (0/1 per vehicle) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `QualityBlock { Id; VehicleId; string? MainImage,ThumbImage; LocalizedText Strapline,Content; }`, `DbSet<QualityBlock> QualityBlocks`.
- Produces: `Task<int> UpsertQualityAsync(int vehicleId, string? mainImage, string? thumbImage, LocalizedText strapline, LocalizedText content, CancellationToken ct = default)`, `Task<bool> RemoveQualityAsync(int vehicleId, CancellationToken ct = default)`; controller `UpsertQuality`/`RemoveQuality`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Quality_Upsert_IsSingleton_AndRemove()
    {
        var db = NewDb(nameof(Quality_Upsert_IsSingleton_AndRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "q", Name = "Q" });

        var first = await svc.UpsertQualityAsync(vid, "/uploads/m.png", "/uploads/t.png", new LocalizedText { En = "strap" }, new LocalizedText { En = "content" });
        Assert.Equal(1, await db.Set<QualityBlock>().CountAsync());

        var second = await svc.UpsertQualityAsync(vid, "/uploads/m2.png", "/uploads/t2.png", new LocalizedText { En = "strap2" }, new LocalizedText { En = "content2" });
        Assert.Equal(first, second);
        Assert.Equal(1, await db.Set<QualityBlock>().CountAsync());
        var row = await db.Set<QualityBlock>().FindAsync(first);
        Assert.Equal("/uploads/m2.png", row!.MainImage);
        Assert.Equal("content2", row.Content.En);

        Assert.True(await svc.RemoveQualityAsync(vid));
        Assert.Equal(0, await db.Set<QualityBlock>().CountAsync());
        Assert.False(await svc.RemoveQualityAsync(vid)); // already gone
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Quality_Upsert_IsSingleton_AndRemove"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Quality block (0/1 per vehicle)
    Task<int> UpsertQualityAsync(int vehicleId, string? mainImage, string? thumbImage, LocalizedText strapline, LocalizedText content, CancellationToken ct = default);
    Task<bool> RemoveQualityAsync(int vehicleId, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Quality block ----
    public async Task<int> UpsertQualityAsync(int vehicleId, string? mainImage, string? thumbImage, LocalizedText strapline, LocalizedText content, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var existing = await _db.Set<QualityBlock>().FirstOrDefaultAsync(q => q.VehicleId == vehicleId, ct);
        if (existing is null)
        {
            existing = new QualityBlock { VehicleId = vehicleId };
            _db.Set<QualityBlock>().Add(existing);
        }
        existing.MainImage = mainImage;
        existing.ThumbImage = thumbImage;
        existing.Strapline = strapline;
        existing.Content = content;
        await _db.SaveChangesAsync(ct);
        return existing.Id;
    }

    public async Task<bool> RemoveQualityAsync(int vehicleId, CancellationToken ct = default)
    {
        var existing = await _db.Set<QualityBlock>().FirstOrDefaultAsync(q => q.VehicleId == vehicleId, ct);
        if (existing is null) return false;
        _db.Set<QualityBlock>().Remove(existing);
        await _db.SaveChangesAsync(ct);
        return true;
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> UpsertQuality(int vehicleId, string? mainImage, string? thumbImage, string? straplineEn, string? straplineAr, string? contentEn, string? contentAr)
    { await _svc.UpsertQualityAsync(vehicleId, mainImage, thumbImage, new() { En = straplineEn, Ar = straplineAr }, new() { En = contentEn, Ar = contentAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveQuality(int vehicleId)
    { await _svc.RemoveQualityAsync(vehicleId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Quality_Upsert_IsSingleton_AndRemove"`

- [ ] **Step 7: Commit.** `git commit -am "feat: QualityBlock upsert/remove service + controller"`

---

### Task 27: CardItem (technology cards) add/remove/move — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `CardItem : IOrderable { Id; VehicleId; LocalizedText Title,Text; string? ImagePath; SortOrder; }`, `DbSet<CardItem> CardItems`. Reuses `RemoveByIdAsync`, `SwapOrderAsync`.
- Produces: `AddCardAsync(int vehicleId, LocalizedText title, LocalizedText text, string? imagePath, …)`, `RemoveCardAsync(int cardId, …)`, `MoveCardAsync(int cardId, int direction, …)`; controller `AddCard`/`RemoveCard`/`MoveCard`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Card_AddMoveRemove()
    {
        var db = NewDb(nameof(Card_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "cd", Name = "C" });
        var a = await svc.AddCardAsync(vid, new LocalizedText { En = "C1" }, new LocalizedText { En = "T1" }, "/uploads/c1.png");
        var b = await svc.AddCardAsync(vid, new LocalizedText { En = "C2" }, new LocalizedText { En = "T2" }, "/uploads/c2.png");
        Assert.Equal(0, (await db.Set<CardItem>().FindAsync(a))!.SortOrder);
        Assert.Equal("/uploads/c1.png", (await db.Set<CardItem>().FindAsync(a))!.ImagePath);
        Assert.True(await svc.MoveCardAsync(b, -1));
        Assert.Equal(0, (await db.Set<CardItem>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveCardAsync(a));
        Assert.Equal(1, await db.Set<CardItem>().CountAsync());
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Card_AddMoveRemove"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Technology cards
    Task<int> AddCardAsync(int vehicleId, LocalizedText title, LocalizedText text, string? imagePath, CancellationToken ct = default);
    Task<bool> RemoveCardAsync(int cardId, CancellationToken ct = default);
    Task<bool> MoveCardAsync(int cardId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Technology cards ----
    public async Task<int> AddCardAsync(int vehicleId, LocalizedText title, LocalizedText text, string? imagePath, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new CardItem { VehicleId = vehicleId, Title = title, Text = text, ImagePath = imagePath, SortOrder = await _db.Set<CardItem>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<CardItem>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveCardAsync(int cardId, CancellationToken ct = default)
        => await RemoveByIdAsync<CardItem>(cardId, ct);

    public async Task<bool> MoveCardAsync(int cardId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<CardItem>().FindAsync([cardId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<CardItem>(x => x.VehicleId == e.VehicleId, cardId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> AddCard(int vehicleId, string? titleEn, string? titleAr, string? textEn, string? textAr, string? imagePath)
    { await _svc.AddCardAsync(vehicleId, new() { En = titleEn, Ar = titleAr }, new() { En = textEn, Ar = textAr }, imagePath); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveCard(int cardId, int vehicleId)
    { await _svc.RemoveCardAsync(cardId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveCard(int cardId, int vehicleId, int direction)
    { await _svc.MoveCardAsync(cardId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Card_AddMoveRemove"`

- [ ] **Step 7: Commit.** `git commit -am "feat: CardItem add/remove/move service + controller"`

---

### Task 28: SafetyToggle add/remove/move — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `SafetyToggle : IOrderable { Id; VehicleId; LocalizedText Title; string? ImagePath; LocalizedText Strap,Content; SortOrder; }`, `DbSet<SafetyToggle> SafetyToggles`. Reuses `RemoveByIdAsync`, `SwapOrderAsync`.
- Produces: `AddSafetyToggleAsync(int vehicleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, …)`, `RemoveSafetyToggleAsync(int toggleId, …)`, `MoveSafetyToggleAsync(int toggleId, int direction, …)`; controller `AddSafetyToggle`/`RemoveSafetyToggle`/`MoveSafetyToggle`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task SafetyToggle_AddMoveRemove()
    {
        var db = NewDb(nameof(SafetyToggle_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "sf", Name = "S" });
        var a = await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "ADAS" }, "/uploads/a.png", new LocalizedText { En = "strap1" }, new LocalizedText { En = "body1" });
        var b = await svc.AddSafetyToggleAsync(vid, new LocalizedText { En = "Airbags" }, "/uploads/b.png", new LocalizedText { En = "strap2" }, new LocalizedText { En = "body2" });
        Assert.Equal(0, (await db.Set<SafetyToggle>().FindAsync(a))!.SortOrder);
        Assert.Equal("strap1", (await db.Set<SafetyToggle>().FindAsync(a))!.Strap.En);
        Assert.True(await svc.MoveSafetyToggleAsync(b, -1));
        Assert.Equal(0, (await db.Set<SafetyToggle>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveSafetyToggleAsync(a));
        Assert.Equal(1, await db.Set<SafetyToggle>().CountAsync());
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.SafetyToggle_AddMoveRemove"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Safety toggles
    Task<int> AddSafetyToggleAsync(int vehicleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, CancellationToken ct = default);
    Task<bool> RemoveSafetyToggleAsync(int toggleId, CancellationToken ct = default);
    Task<bool> MoveSafetyToggleAsync(int toggleId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Safety toggles ----
    public async Task<int> AddSafetyToggleAsync(int vehicleId, LocalizedText title, string? imagePath, LocalizedText strap, LocalizedText content, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new SafetyToggle { VehicleId = vehicleId, Title = title, ImagePath = imagePath, Strap = strap, Content = content, SortOrder = await _db.Set<SafetyToggle>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<SafetyToggle>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveSafetyToggleAsync(int toggleId, CancellationToken ct = default)
        => await RemoveByIdAsync<SafetyToggle>(toggleId, ct);

    public async Task<bool> MoveSafetyToggleAsync(int toggleId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<SafetyToggle>().FindAsync([toggleId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<SafetyToggle>(x => x.VehicleId == e.VehicleId, toggleId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> AddSafetyToggle(int vehicleId, string? titleEn, string? titleAr, string? imagePath, string? strapEn, string? strapAr, string? contentEn, string? contentAr)
    { await _svc.AddSafetyToggleAsync(vehicleId, new() { En = titleEn, Ar = titleAr }, imagePath, new() { En = strapEn, Ar = strapAr }, new() { En = contentEn, Ar = contentAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveSafetyToggle(int toggleId, int vehicleId)
    { await _svc.RemoveSafetyToggleAsync(toggleId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveSafetyToggle(int toggleId, int vehicleId, int direction)
    { await _svc.MoveSafetyToggleAsync(toggleId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.SafetyToggle_AddMoveRemove"`

- [ ] **Step 7: Commit.** `git commit -am "feat: SafetyToggle add/remove/move service + controller"`

---

### Task 29: Trim extension + TrimPriceRow (grandchild) — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: extended `Trim { …; LocalizedText ModelLabel; string? ImagePath; List<TrimPriceRow> PriceRows; }` (Price/Highlights retained but unused by new render), `TrimPriceRow : IOrderable { Id; int TrimId; LocalizedText Text; SortOrder; }`, `DbSet<TrimPriceRow> TrimPriceRows`. Existing `AddTrimAsync(int, Trim)` reused (already sets ModelLabel/ImagePath because they are on the Trim object passed in).
- Produces: `AddTrimPriceRowAsync(int trimId, LocalizedText text, …)`, `RemoveTrimPriceRowAsync(int rowId, …)`, `MoveTrimPriceRowAsync(int rowId, int direction, …)`; updated `AddTrim` controller action binding `modelLabelEn/Ar`, `imagePath`; new `TrimPriceRow` controller actions.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task Trim_NewFields_Persist_AndPriceRows_AddMoveRemove()
    {
        var db = NewDb(nameof(Trim_NewFields_Persist_AndPriceRows_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "tr", Name = "T" });
        var tid = await svc.AddTrimAsync(vid, new Trim
        {
            Name = "GT",
            ModelLabel = new LocalizedText { En = "2024 Model" },
            ImagePath = "/uploads/trim.png",
            SpecPdf = "/docs/spec.pdf"
        });
        var t = await db.Set<Trim>().FindAsync(tid);
        Assert.Equal("2024 Model", t!.ModelLabel.En);
        Assert.Equal("/uploads/trim.png", t.ImagePath);

        var r1 = await svc.AddTrimPriceRowAsync(tid, new LocalizedText { En = "Price: 10,000" });
        var r2 = await svc.AddTrimPriceRowAsync(tid, new LocalizedText { En = "VAT: 500" });
        Assert.Equal(0, (await db.Set<TrimPriceRow>().FindAsync(r1))!.SortOrder);
        Assert.Equal(1, (await db.Set<TrimPriceRow>().FindAsync(r2))!.SortOrder);
        Assert.True(await svc.MoveTrimPriceRowAsync(r2, -1));
        Assert.Equal(0, (await db.Set<TrimPriceRow>().FindAsync(r2))!.SortOrder);
        Assert.True(await svc.RemoveTrimPriceRowAsync(r1));
        Assert.Equal(1, await db.Set<TrimPriceRow>().CountAsync());
    }

    [Fact]
    public async Task AddTrimPriceRow_OnMissingTrim_ReturnsZero()
    {
        var db = NewDb(nameof(AddTrimPriceRow_OnMissingTrim_ReturnsZero));
        var svc = NewSvc(db);
        Assert.Equal(0, await svc.AddTrimPriceRowAsync(999999, new LocalizedText { En = "x" }));
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Trim_NewFields_Persist_AndPriceRows_AddMoveRemove|FullyQualifiedName~AdminVehicleServiceTests.AddTrimPriceRow_OnMissingTrim_ReturnsZero"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Trim price rows (trim -> rows)
    Task<int> AddTrimPriceRowAsync(int trimId, LocalizedText text, CancellationToken ct = default);
    Task<bool> RemoveTrimPriceRowAsync(int rowId, CancellationToken ct = default);
    Task<bool> MoveTrimPriceRowAsync(int rowId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Trim price rows ----
    public async Task<int> AddTrimPriceRowAsync(int trimId, LocalizedText text, CancellationToken ct = default)
    {
        if (!await _db.Set<Trim>().AnyAsync(t => t.Id == trimId, ct)) return 0;
        var e = new TrimPriceRow { TrimId = trimId, Text = text, SortOrder = await _db.Set<TrimPriceRow>().CountAsync(x => x.TrimId == trimId, ct) };
        _db.Set<TrimPriceRow>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveTrimPriceRowAsync(int rowId, CancellationToken ct = default)
        => await RemoveByIdAsync<TrimPriceRow>(rowId, ct);

    public async Task<bool> MoveTrimPriceRowAsync(int rowId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<TrimPriceRow>().FindAsync([rowId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<TrimPriceRow>(x => x.TrimId == e.TrimId, rowId, direction, ct);
    }
```

- [ ] **Step 5: Update `AddTrim` controller + add TrimPriceRow actions.** In `VehiclesController.cs`, replace the existing `AddTrim` action so it binds the new Trim fields:
```csharp
    [HttpPost] public async Task<IActionResult> AddTrim(int vehicleId, string? nameEn, string? nameAr, decimal? price, string? highlightsEn, string? highlightsAr, string? specPdf, string? modelLabelEn, string? modelLabelAr, string? imagePath)
    {
        await _svc.AddTrimAsync(vehicleId, new Trim
        {
            Name = new() { En = nameEn, Ar = nameAr },
            Price = price,
            Highlights = new() { En = highlightsEn, Ar = highlightsAr },
            SpecPdf = specPdf,
            ModelLabel = new() { En = modelLabelEn, Ar = modelLabelAr },
            ImagePath = imagePath
        });
        return RedirectToAction(nameof(Edit), new { id = vehicleId });
    }
```
Add the price-row actions:
```csharp
    [HttpPost] public async Task<IActionResult> AddTrimPriceRow(int trimId, int vehicleId, string? textEn, string? textAr)
    { await _svc.AddTrimPriceRowAsync(trimId, new() { En = textEn, Ar = textAr }); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveTrimPriceRow(int rowId, int vehicleId)
    { await _svc.RemoveTrimPriceRowAsync(rowId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveTrimPriceRow(int rowId, int vehicleId, int direction)
    { await _svc.MoveTrimPriceRowAsync(rowId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.Trim_NewFields_Persist_AndPriceRows_AddMoveRemove|FullyQualifiedName~AdminVehicleServiceTests.AddTrimPriceRow_OnMissingTrim_ReturnsZero"`

- [ ] **Step 7: Commit.** `git commit -am "feat: Trim new fields + TrimPriceRow add/remove/move service + controller"`

---

### Task 30: WarrantyLink add/remove/move — service + controller

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Core/Services/IAdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Services/AdminVehicleService.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: `WarrantyLink : IOrderable { Id; VehicleId; LocalizedText Label; string Url; SortOrder; }`, `DbSet<WarrantyLink> WarrantyLinks`. Reuses `RemoveByIdAsync`, `SwapOrderAsync`.
- Produces: `AddWarrantyLinkAsync(int vehicleId, LocalizedText label, string url, …)`, `RemoveWarrantyLinkAsync(int linkId, …)`, `MoveWarrantyLinkAsync(int linkId, int direction, …)`; controller `AddWarrantyLink`/`RemoveWarrantyLink`/`MoveWarrantyLink`.

- [ ] **Step 1: Write failing test.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task WarrantyLink_AddMoveRemove()
    {
        var db = NewDb(nameof(WarrantyLink_AddMoveRemove));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "w", Name = "W" });
        var a = await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "Warranty PDF" }, "/docs/w1.pdf");
        var b = await svc.AddWarrantyLinkAsync(vid, new LocalizedText { En = "Owner Manual" }, "/docs/w2.pdf");
        Assert.Equal(0, (await db.Set<WarrantyLink>().FindAsync(a))!.SortOrder);
        Assert.Equal("/docs/w1.pdf", (await db.Set<WarrantyLink>().FindAsync(a))!.Url);
        Assert.True(await svc.MoveWarrantyLinkAsync(b, -1));
        Assert.Equal(0, (await db.Set<WarrantyLink>().FindAsync(b))!.SortOrder);
        Assert.True(await svc.RemoveWarrantyLinkAsync(a));
        Assert.Equal(1, await db.Set<WarrantyLink>().CountAsync());
    }
```

- [ ] **Step 2: Run & fail.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.WarrantyLink_AddMoveRemove"`

- [ ] **Step 3: Add interface members.**
```csharp
    // Warranty document links
    Task<int> AddWarrantyLinkAsync(int vehicleId, LocalizedText label, string url, CancellationToken ct = default);
    Task<bool> RemoveWarrantyLinkAsync(int linkId, CancellationToken ct = default);
    Task<bool> MoveWarrantyLinkAsync(int linkId, int direction, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in service.** Add before `// ---- shared helpers ----`:
```csharp
    // ---- Warranty links ----
    public async Task<int> AddWarrantyLinkAsync(int vehicleId, LocalizedText label, string url, CancellationToken ct = default)
    {
        if (!await _db.Vehicles.AnyAsync(v => v.Id == vehicleId, ct)) return 0;
        var e = new WarrantyLink { VehicleId = vehicleId, Label = label, Url = url ?? "", SortOrder = await _db.Set<WarrantyLink>().CountAsync(x => x.VehicleId == vehicleId, ct) };
        _db.Set<WarrantyLink>().Add(e);
        await _db.SaveChangesAsync(ct);
        return e.Id;
    }

    public async Task<bool> RemoveWarrantyLinkAsync(int linkId, CancellationToken ct = default)
        => await RemoveByIdAsync<WarrantyLink>(linkId, ct);

    public async Task<bool> MoveWarrantyLinkAsync(int linkId, int direction, CancellationToken ct = default)
    {
        var e = await _db.Set<WarrantyLink>().FindAsync([linkId], ct);
        if (e is null) return false;
        return await SwapOrderAsync<WarrantyLink>(x => x.VehicleId == e.VehicleId, linkId, direction, ct);
    }
```

- [ ] **Step 5: Add controller actions.**
```csharp
    [HttpPost] public async Task<IActionResult> AddWarrantyLink(int vehicleId, string? labelEn, string? labelAr, string url)
    { await _svc.AddWarrantyLinkAsync(vehicleId, new() { En = labelEn, Ar = labelAr }, url); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> RemoveWarrantyLink(int linkId, int vehicleId)
    { await _svc.RemoveWarrantyLinkAsync(linkId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
    [HttpPost] public async Task<IActionResult> MoveWarrantyLink(int linkId, int vehicleId, int direction)
    { await _svc.MoveWarrantyLinkAsync(linkId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
```

- [ ] **Step 6: Run & pass.** `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests.WarrantyLink_AddMoveRemove"`

- [ ] **Step 7: Commit.** `git commit -am "feat: WarrantyLink add/remove/move service + controller"`

---

### Task 31: Full admin-backend regression sweep + guard tests

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs`

**Interfaces:**
- Consumes: every service method added in Tasks 21-30 plus the `GetAsync` eager-loading from Task 20.
- Produces: cross-cutting guard tests (missing-parent → 0, out-of-bounds move → false, cascade behaviour) plus the green build that unblocks the render + parser parts.

- [ ] **Step 1: Write guard/edge tests.** Append to `AdminVehicleServiceTests.cs`:
```csharp
    [Fact]
    public async Task AddMethods_OnMissingVehicle_ReturnZero()
    {
        var db = NewDb(nameof(AddMethods_OnMissingVehicle_ReturnZero));
        var svc = NewSvc(db);
        const int missing = 987654;
        Assert.Equal(0, await svc.AddStatAsync(missing, new LocalizedText { En = "x" }, new LocalizedText { En = "y" }));
        Assert.Equal(0, await svc.AddSliderAsync(missing, new LocalizedText(), new LocalizedText()));
        Assert.Equal(0, await svc.AddGalleryTabAsync(missing, new LocalizedText()));
        Assert.Equal(0, await svc.AddCardAsync(missing, new LocalizedText(), new LocalizedText(), null));
        Assert.Equal(0, await svc.AddSafetyToggleAsync(missing, new LocalizedText(), null, new LocalizedText(), new LocalizedText()));
        Assert.Equal(0, await svc.AddWarrantyLinkAsync(missing, new LocalizedText(), "/x.pdf"));
        Assert.Equal(0, await svc.UpsertSectionHeadingAsync(missing, SectionKey.Overview, new LocalizedText(), new LocalizedText(), new LocalizedText()));
        Assert.Equal(0, await svc.UpsertQualityAsync(missing, null, null, new LocalizedText(), new LocalizedText()));
    }

    [Fact]
    public async Task MoveMethods_OutOfBounds_ReturnFalse()
    {
        var db = NewDb(nameof(MoveMethods_OutOfBounds_ReturnFalse));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "ob", Name = "O" });
        var stat = await svc.AddStatAsync(vid, new LocalizedText { En = "Only" }, new LocalizedText { En = "1" });
        Assert.False(await svc.MoveStatAsync(stat, -1)); // already top
        Assert.False(await svc.MoveStatAsync(stat, 1));  // already bottom
        Assert.False(await svc.MoveStatAsync(999999, -1)); // not found
        Assert.False(await svc.RemoveStatAsync(999999));
    }

    [Fact]
    public async Task GetAsync_EagerLoads_Grandchildren()
    {
        var db = NewDb(nameof(GetAsync_EagerLoads_Grandchildren));
        var svc = NewSvc(db);
        var vid = await svc.CreateAsync(new Vehicle { Slug = "eg", Name = "E" });
        var sg = await svc.AddSliderAsync(vid, new LocalizedText { En = "e" }, new LocalizedText { En = "t" });
        await svc.AddSliderSlideAsync(sg, "/uploads/x.png", new LocalizedText { En = "x" });
        var gt = await svc.AddGalleryTabAsync(vid, new LocalizedText { En = "ext" });
        await svc.AddGalleryImageAsync(gt, "/uploads/g.png", new LocalizedText { En = "g" });
        var fid = await svc.AddFeatureAsync(vid, new FeatureSection { Heading = "P", GroupKey = FeatureGroup.Design, TabLabel = new LocalizedText { En = "D" }, Lead = new LocalizedText { En = "l" } });
        await svc.AddFeatureBulletAsync(fid, new LocalizedText { En = "L" }, new LocalizedText { En = "T" });
        var tid = await svc.AddTrimAsync(vid, new Trim { Name = "GT" });
        await svc.AddTrimPriceRowAsync(tid, new LocalizedText { En = "Price: 1" });

        var v = await svc.GetAsync(vid);
        Assert.Single(v!.Sliders[0].Slides);
        Assert.Single(v.GalleryTabs[0].Images);
        Assert.Single(v.Features.First(f => f.Id == fid).Bullets);
        Assert.Single(v.Trims.First(t => t.Id == tid).PriceRows);
    }
```

- [ ] **Step 2: Run the full admin service test class & confirm green.**
```
dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminVehicleServiceTests"
```
All tests from Tasks 20-31 (including `GetAsync_Includes_NewCollections_AndQuality` from Task 20, which now compiles because every Add*/Upsert* exists) must pass.

- [ ] **Step 3: Build the whole solution to catch Razor/controller compile errors.**
```
dotnet build GAC.sln -c Debug
```
Confirm 0 errors (the new controller actions in `VehiclesController.cs` compile; the Razor admin views are wired in the admin-views part).

- [ ] **Step 4: Commit.** `git commit -am "test: admin vehicle rich-section guard + grandchild eager-load regression sweep"`


---

## Phase 3 — Admin views

### Task 40: Vehicle scalar fields + section-nav + partial wiring in Edit.cshtml

**Files:**
- Modify `Solution/GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle` (new scalar fields `TechBannerImage`, `EnquiryBgImage` (string?) + `StatsNote`, `EnquiryTitle`, `EnquirySub`, `EnquiryLead` (LocalizedText), added by the model/EF plan) and the new render/admin partials authored in Tasks 41–50.
- Consumes `_LocalizedField` partial (`GAC.Web.Areas.Admin.Models.LocalizedFieldModel`) and `_PickerModal`.
- Produces the wired admin Edit screen: scalar fields inside the main Save `<form>`; every new partial rendered inside the existing `@if (!isNew)` block with `_PickerModal` kept LAST and ONCE; a sticky section-nav.

This task wires the shell. Tasks 41–50 create the individual partials it references; if a referenced partial does not yet exist when this view is built, the Razor BUILD will fail — so commit this task LAST in your local sequence (after 41–50) OR temporarily comment the not-yet-created `<partial>` lines. The steps below assume Tasks 41–50 partials already exist in the working tree.

- [ ] **Step 1: Write a failing integration smoke test for the new scalar fields.** Create `Solution/GAC.Tests/Admin/AdminVehicleEditViewTests.cs` with the harness all panel assertions reuse (this file is shared across Tasks 40–50; add to it in later tasks):

```csharp
using System.Net;
using System.Text.RegularExpressions;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminVehicleEditViewTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminVehicleEditViewTests(AdminWebApplicationFactory factory) => _factory = factory;

    // Resolve a real vehicle id from the shared dev DB via the admin list, then GET its Edit page as an Editor.
    private async Task<string> GetFirstVehicleEditHtmlAsync()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success, "Expected at least one vehicle Edit link in the admin list.");
        var res = await client.GetAsync($"/Admin/Vehicles/Edit/{m.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Edit_RendersScalarFields_TechBanner_StatsNote_Enquiry()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Technology banner image", html);
        Assert.Contains("Stats note", html);
        Assert.Contains("Enquiry background image", html);
        Assert.Contains("Enquiry title", html);
        Assert.Contains("Enquiry sub", html);
        Assert.Contains("Enquiry lead", html);
    }

    [Fact]
    public async Task Edit_RendersSectionNav()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("adm-section-nav", html);
    }
}
```

Run `dotnet test --filter AdminVehicleEditViewTests` from `Solution`. Expect FAIL: the assertions for the scalar-field labels and `adm-section-nav` are not present in the current Edit.cshtml.

- [ ] **Step 2: Add the scalar fields to the main Save form and the section-nav, then wire the partials.** Replace the whole closing region of `Edit.cshtml`. First, insert the scalar fields INTO the main Save `<form method="post" asp-action="Save">` block, just before the final `<button type="submit" class="adm-btn">Save</button>` line:

```cshtml
    <hr />
    <h2>Page-level images &amp; enquiry</h2>

    <div class="adm-field">
        <label asp-for="TechBannerImage">Technology banner image</label>
        <span style="display:inline-flex;gap:.4rem;align-items:center">
            <input asp-for="TechBannerImage" data-media-input />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
    </div>

    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Stats note", NameEn = "StatsNote.En", NameAr = "StatsNote.Ar", ValueEn = Model.StatsNote.En, ValueAr = Model.StatsNote.Ar }' />

    <div class="adm-field">
        <label asp-for="EnquiryBgImage">Enquiry background image</label>
        <span style="display:inline-flex;gap:.4rem;align-items:center">
            <input asp-for="EnquiryBgImage" data-media-input />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
    </div>

    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Enquiry title", NameEn = "EnquiryTitle.En", NameAr = "EnquiryTitle.Ar", ValueEn = Model.EnquiryTitle.En, ValueAr = Model.EnquiryTitle.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Enquiry sub", NameEn = "EnquirySub.En", NameAr = "EnquirySub.Ar", ValueEn = Model.EnquirySub.En, ValueAr = Model.EnquirySub.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Enquiry lead", NameEn = "EnquiryLead.En", NameAr = "EnquiryLead.Ar", ValueEn = Model.EnquiryLead.En, ValueAr = Model.EnquiryLead.Ar, Multiline = true }' />
```

Then REPLACE the existing trailing `@if (!isNew)` block with the section-nav + full partial wiring (PickerModal LAST/ONCE):

```cshtml
@if (!isNew)
{
    <nav class="adm-section-nav" aria-label="Vehicle content sections">
        <a href="#sec-images">Images</a>
        <a href="#sec-headings">Section headings</a>
        <a href="#sec-stats">Stats</a>
        <a href="#sec-sliders">Sliders</a>
        <a href="#sec-features">Features</a>
        <a href="#sec-gallery">Gallery</a>
        <a href="#sec-quality">Quality</a>
        <a href="#sec-cards">Technology cards</a>
        <a href="#sec-safety">Safety</a>
        <a href="#sec-trims">Trims</a>
        <a href="#sec-warranty">Warranty</a>
    </nav>

    <section id="sec-images"><partial name="_Images" model="Model" /></section>
    <section id="sec-headings"><partial name="_SectionHeadings" model="Model" /></section>
    <section id="sec-stats"><partial name="_Stats" model="Model" /></section>
    <section id="sec-sliders"><partial name="_Sliders" model="Model" /></section>
    <section id="sec-features"><partial name="_Features" model="Model" /></section>
    <section id="sec-gallery"><partial name="_GalleryTabs" model="Model" /></section>
    <section id="sec-quality"><partial name="_Quality" model="Model" /></section>
    <section id="sec-cards"><partial name="_Cards" model="Model" /></section>
    <section id="sec-safety"><partial name="_Safety" model="Model" /></section>
    <section id="sec-trims"><partial name="_Trims" model="Model" /></section>
    <section id="sec-warranty"><partial name="_Warranty" model="Model" /></section>

    <partial name="_SpecGroups" model="Model" />
    <partial name="_Colors" model="Model" />
    <partial name="_PickerModal" />
}
```

Note: `_SpecGroups` and `_Colors` are retained (their data is decoupled from the new render path but the panels stay per the spec) and are NOT in the section-nav. `@using GAC.Web.Areas.Admin.Models` is already at the top of Edit.cshtml.

- [ ] **Step 3: Build and re-run the test.** `dotnet build` then `dotnet test --filter AdminVehicleEditViewTests` from `Solution`. Both new facts PASS (the scalar-field labels and `adm-section-nav` now render). Razor compiles (all referenced partials exist from Tasks 41–50).

- [ ] **Step 4: Add minimal sticky-nav CSS.** Append to `Solution/GAC.Web/wwwroot/css/admin.css` (verify the file path with a quick search; if the admin stylesheet lives elsewhere, append there instead — do not invent a new file):

```css
.adm-section-nav{position:sticky;top:0;z-index:5;display:flex;flex-wrap:wrap;gap:.5rem;padding:.6rem .8rem;margin:1rem 0;background:#fff;border:1px solid #e2e2e2;border-radius:6px}
.adm-section-nav a{font-size:.85rem;text-decoration:none;color:#0a3d62;padding:.15rem .4rem;border-radius:4px}
.adm-section-nav a:hover{background:#eef3f8}
```

- [ ] **Step 5: Commit.** `git add -A && git commit -m "feat: wire vehicle scalar fields, section-nav and rich-section admin partials into Edit.cshtml"`

---

### Task 41: _SectionHeadings admin partial (8 fixed-key upsert panels)

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_SectionHeadings.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle` with `List<SectionHeading> Headings` (each `SectionHeading { int Id; int VehicleId; SectionKey Key; LocalizedText Title,Sub,Body; }`) and the enum `GAC.Core.Content.SectionKey { Overview, Design, Gallery, Technology, Performance, Safety, Trims, Warranty }`.
- Produces 8 forms that POST to `UpsertSectionHeading(int vehicleId, GAC.Core.Content.SectionKey key, string? titleEn, string? titleAr, string? subEn, string? subAr, string? bodyEn, string? bodyAr)` (controller action authored in the admin-controller plan). There is NO reorder/remove — headings are a fixed set, one per `SectionKey`.

`SectionHeading` is NOT IOrderable. Each of the 8 keys gets exactly one upsert panel (no Add/Remove). The partial looks up the existing heading per key (or null) to pre-fill.

- [ ] **Step 1: Write a failing assertion in the shared test.** Add to `Solution/GAC.Tests/Admin/AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersSectionHeadingsPanel_AllEightKeys()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Section headings", html);
        foreach (var key in new[] { "Overview", "Design", "Gallery", "Technology", "Performance", "Safety", "Trims", "Warranty" })
            Assert.Contains($"UpsertSectionHeading", html);
        Assert.Contains("name=\"key\" value=\"Overview\"", html);
        Assert.Contains("name=\"key\" value=\"Warranty\"", html);
    }
```

Run `dotnet test --filter Edit_RendersSectionHeadingsPanel_AllEightKeys`. Expect FAIL (build error: `_SectionHeadings` partial does not exist / not wired). Note: wiring is Task 40 — if running 41 before 40, the assertion fails on missing markup once the partial is referenced.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_SectionHeadings.cshtml`:

```cshtml
@using GAC.Core.Content
@model GAC.Core.Content.Vehicle
<h2>Section headings</h2>
<p class="adm-hint">One title/subtitle/body per page section. These are fixed sections — fill the text only.</p>
@foreach (var key in (SectionKey[])Enum.GetValues(typeof(SectionKey)))
{
    var h = Model.Headings.FirstOrDefault(x => x.Key == key);
    <div class="adm-card">
      <h3>@key</h3>
      <form asp-action="UpsertSectionHeading" method="post">
        <input type="hidden" name="vehicleId" value="@Model.Id" />
        <input type="hidden" name="key" value="@key" />
        <div class="adm-field adm-localized">
          <span class="adm-localized__label">Title</span>
          <div class="adm-localized__pair">
            <div><label>English</label><input name="titleEn" value="@(h?.Title.En)" /></div>
            <div dir="rtl"><label>Arabic</label><input name="titleAr" value="@(h?.Title.Ar)" dir="rtl" /></div>
          </div>
        </div>
        <div class="adm-field adm-localized">
          <span class="adm-localized__label">Subtitle</span>
          <div class="adm-localized__pair">
            <div><label>English</label><input name="subEn" value="@(h?.Sub.En)" /></div>
            <div dir="rtl"><label>Arabic</label><input name="subAr" value="@(h?.Sub.Ar)" dir="rtl" /></div>
          </div>
        </div>
        <div class="adm-field adm-localized">
          <span class="adm-localized__label">Body</span>
          <div class="adm-localized__pair">
            <div><label>English</label><textarea name="bodyEn" rows="3">@(h?.Body.En)</textarea></div>
            <div dir="rtl"><label>Arabic</label><textarea name="bodyAr" rows="3" dir="rtl">@(h?.Body.Ar)</textarea></div>
          </div>
        </div>
        <button class="adm-btn">Save heading</button>
      </form>
    </div>
}
```

- [ ] **Step 3: Build, ensure Task 40 wiring references this partial, re-run the test.** `dotnet build` then `dotnet test --filter Edit_RendersSectionHeadingsPanel_AllEightKeys`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _SectionHeadings admin partial with 8 fixed-key upsert forms"`

---

### Task 42: _Stats admin partial

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Stats.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.Stats` (`List<StatItem>`; `StatItem : IOrderable { int Id; int VehicleId; LocalizedText Label,Value; int SortOrder; }`).
- Produces forms posting to `AddStat(int vehicleId, string? labelEn, string? labelAr, string? valueEn, string? valueAr)`, `RemoveStat(int statId, int vehicleId)`, `MoveStat(int statId, int vehicleId, int direction)`.

Note: the `.mp-note` text is the Vehicle-level `StatsNote` field and is edited in the main Save form (Task 40), NOT here.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersStatsPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Overview stats", html);
        Assert.Contains("AddStat", html);
    }
```

Run `dotnet test --filter Edit_RendersStatsPanel`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Stats.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Overview stats</h2>
<table class="adm-table">
  <thead><tr><th>Order</th><th>Label</th><th>Value</th><th></th></tr></thead>
  <tbody>
    @foreach (var s in Model.Stats.OrderBy(x => x.SortOrder))
    {
        <tr>
          <td>
            <form asp-action="MoveStat" method="post" style="display:inline">
              <input type="hidden" name="statId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
              <button class="adm-btn">&uarr;</button>
            </form>
            <form asp-action="MoveStat" method="post" style="display:inline">
              <input type="hidden" name="statId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
              <button class="adm-btn">&darr;</button>
            </form>
          </td>
          <td>@s.Label.En</td>
          <td>@s.Value.En</td>
          <td>
            <form asp-action="RemoveStat" method="post" style="display:inline" onsubmit="return confirm('Remove stat?')">
              <input type="hidden" name="statId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
              <button class="adm-btn adm-btn--danger">Remove</button>
            </form>
          </td>
        </tr>
    }
  </tbody>
</table>
<form asp-action="AddStat" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="labelEn" placeholder="Label (EN)" /><input name="labelAr" placeholder="Label (AR)" dir="rtl" />
  <input name="valueEn" placeholder="Value e.g. 177 HP (EN)" /><input name="valueAr" placeholder="Value (AR)" dir="rtl" />
  <button class="adm-btn">Add stat</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersStatsPanel`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Stats admin partial (add/move/remove stat rows)"`

---

### Task 43: _Sliders admin partial with nested slides

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Sliders.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.Sliders` (`List<SliderGroup>`; `SliderGroup : IOrderable { int Id; int VehicleId; LocalizedText Eyebrow,Title; int SortOrder; List<SliderSlide> Slides; }`; `SliderSlide : IOrderable { int Id; int SliderGroupId; string? ImagePath; LocalizedText Alt; int SortOrder; }`).
- Produces forms posting to `AddSlider(int vehicleId, string? eyebrowEn, string? eyebrowAr, string? titleEn, string? titleAr)`, `RemoveSlider(int sliderId, int vehicleId)`, `MoveSlider(int sliderId, int vehicleId, int direction)`, and nested-slide actions `AddSliderSlide(int sliderGroupId, int vehicleId, string? imagePath, string? altEn, string? altAr)`, `RemoveSliderSlide(int slideId, int vehicleId)`, `MoveSliderSlide(int slideId, int vehicleId, int direction)`.

Uses the `_SpecGroups` nested pattern: `adm-card` per group, inner table of slide rows, inner Add-slide form carrying the parent `sliderGroupId`.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersSlidersPanel_WithSlideForm()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Sliders", html);
        Assert.Contains("AddSlider", html);
        Assert.Contains("AddSliderSlide", html);
    }
```

Run `dotnet test --filter Edit_RendersSlidersPanel_WithSlideForm`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Sliders.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Sliders</h2>
@foreach (var g in Model.Sliders.OrderBy(x => x.SortOrder))
{
    <div class="adm-card">
      <h3>@g.Title.En
        <form asp-action="MoveSlider" method="post" style="display:inline">
          <input type="hidden" name="sliderId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveSlider" method="post" style="display:inline">
          <input type="hidden" name="sliderId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveSlider" method="post" style="display:inline" onsubmit="return confirm('Remove slider and its slides?')">
          <input type="hidden" name="sliderId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove slider</button>
        </form>
      </h3>
      <p class="adm-hint">Eyebrow: @g.Eyebrow.En &mdash; Title: @g.Title.En</p>
      <table class="adm-table">
        <thead><tr><th>Order</th><th>Image</th><th>Alt</th><th></th></tr></thead>
        <tbody>
          @foreach (var sl in g.Slides.OrderBy(x => x.SortOrder))
          {
              <tr>
                <td>
                  <form asp-action="MoveSliderSlide" method="post" style="display:inline">
                    <input type="hidden" name="slideId" value="@sl.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
                    <button class="adm-btn">&uarr;</button>
                  </form>
                  <form asp-action="MoveSliderSlide" method="post" style="display:inline">
                    <input type="hidden" name="slideId" value="@sl.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
                    <button class="adm-btn">&darr;</button>
                  </form>
                </td>
                <td>@if (!string.IsNullOrWhiteSpace(sl.ImagePath)) { <img src="@sl.ImagePath" class="adm-picker-thumb" alt="" /> }</td>
                <td>@sl.Alt.En</td>
                <td>
                  <form asp-action="RemoveSliderSlide" method="post" style="display:inline" onsubmit="return confirm('Remove slide?')">
                    <input type="hidden" name="slideId" value="@sl.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
                    <button class="adm-btn adm-btn--danger">Remove</button>
                  </form>
                </td>
              </tr>
          }
        </tbody>
      </table>
      <form asp-action="AddSliderSlide" method="post" class="adm-inline">
        <input type="hidden" name="sliderGroupId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
        <span style="display:inline-flex;gap:.4rem;align-items:center">
          <input type="text" name="imagePath" placeholder="Image" data-media-input />
          <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
        <input name="altEn" placeholder="Alt (EN)" /><input name="altAr" placeholder="Alt (AR)" dir="rtl" />
        <button class="adm-btn">Add slide</button>
      </form>
    </div>
}
<form asp-action="AddSlider" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="eyebrowEn" placeholder="Eyebrow (EN)" /><input name="eyebrowAr" placeholder="Eyebrow (AR)" dir="rtl" />
  <input name="titleEn" placeholder="Title (EN)" /><input name="titleAr" placeholder="Title (AR)" dir="rtl" />
  <button class="adm-btn">Add slider</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersSlidersPanel_WithSlideForm`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Sliders admin partial with nested slide add/move/remove"`

---

### Task 44: Extend _Features admin list + extend FeatureEdit sub-page + add _FeatureBullets

**Files:**
- Modify `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Features.cshtml`
- Modify `Solution/GAC.Web/Areas/Admin/Views/Vehicles/FeatureEdit.cshtml`

**Interfaces:**
- Consumes the EXTENDED `GAC.Core.Content.FeatureSection` (now with `FeatureGroup GroupKey`, `LocalizedText TabLabel`, `LocalizedText Lead`, `List<FeatureBullet> Bullets`) and the enum `GAC.Core.Content.FeatureGroup { Design, Performance }`. `FeatureBullet : IOrderable { int Id; int FeatureSectionId; LocalizedText Label,Text; int SortOrder; }`.
- Consumes existing controller actions `FeatureEdit(int vehicleId, int? id)` / `FeatureSave(int vehicleId, FeatureSection feature)` (already accept the extended entity via model binding once the FeatureEdit form carries the new fields).
- Produces nested-bullet actions `AddFeatureBullet(int featureSectionId, int vehicleId, string? labelEn, string? labelAr, string? textEn, string? textAr)`, `RemoveFeatureBullet(int bulletId, int vehicleId)`, `MoveFeatureBullet(int bulletId, int vehicleId, int direction)` wired in `FeatureEdit.cshtml`.

The existing `_Features.cshtml` is a list with an "Edit" link to the `FeatureEdit` sub-page (kept). We (a) add a "Group" column showing `GroupKey` to the list, and (b) extend the `FeatureEdit` sub-page with `GroupKey` (select), `TabLabel`, `Lead`, and a bullet sub-list. Bullets are managed on the FeatureEdit page (it already knows the feature id) — they only appear once the feature is saved (Id != 0).

- [ ] **Step 1: Write a failing assertion in the shared test.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_FeaturesList_ShowsGroupColumn()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Feature sections", html);
        Assert.Contains(">Group<", html); // new column header
    }
```

Run `dotnet test --filter Edit_FeaturesList_ShowsGroupColumn`. Expect FAIL (no Group column yet).

- [ ] **Step 2: Add the Group column to _Features.cshtml.** Edit the `<thead>` row and the body row. Change the header:

```cshtml
  <thead><tr><th>Order</th><th>Heading</th><th>Group</th><th>Layout</th><th></th></tr></thead>
```

And insert a `<td>@f.GroupKey</td>` cell immediately after the `<td>@f.Heading.En</td>` cell:

```cshtml
          <td>@f.Heading.En</td>
          <td>@f.GroupKey</td>
          <td>@f.Layout</td>
```

- [ ] **Step 3: Run the list assertion.** `dotnet build` then `dotnet test --filter Edit_FeaturesList_ShowsGroupColumn`. PASS.

- [ ] **Step 4: Write a failing assertion for the FeatureEdit sub-page.** Add a separate test class to `AdminVehicleEditViewTests.cs` (the FeatureEdit page needs a feature id; derive it from the rendered Edit page's `FeatureEdit?...&id=` link, or open the "Add feature section" page which has no bullets yet). Append:

```csharp
public class AdminFeatureEditViewTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminFeatureEditViewTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task FeatureEdit_New_RendersGroupTabLabelLead()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success);
        var vid = m.Groups[1].Value;
        var res = await client.GetAsync($"/Admin/Vehicles/FeatureEdit?vehicleId={vid}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("GroupKey", html);     // group select bound to FeatureGroup
        Assert.Contains("TabLabel.En", html);  // tab label field
        Assert.Contains("Lead.En", html);      // lead paragraph field
    }
}
```

Run `dotnet test --filter FeatureEdit_New_RendersGroupTabLabelLead`. Expect FAIL.

- [ ] **Step 5: Extend FeatureEdit.cshtml.** Add, immediately after the existing `Layout` `<select>` `<div class="adm-field">` block, the GroupKey select, TabLabel and Lead fields:

```cshtml
  <div class="adm-field">
    <label>Group (which tab strip)</label>
    <select asp-for="GroupKey" asp-items="Html.GetEnumSelectList<FeatureGroup>()"></select>
  </div>

  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Tab label</span>
    <div class="adm-localized__pair">
      <div><label>English</label><input name="TabLabel.En" value="@Model.TabLabel.En" /></div>
      <div dir="rtl"><label>Arabic</label><input name="TabLabel.Ar" value="@Model.TabLabel.Ar" dir="rtl" /></div>
    </div>
  </div>

  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Lead paragraph</span>
    <div class="adm-localized__pair">
      <div><label>English</label><textarea name="Lead.En" rows="3">@Model.Lead.En</textarea></div>
      <div dir="rtl"><label>Arabic</label><textarea name="Lead.Ar" rows="3" dir="rtl">@Model.Lead.Ar</textarea></div>
    </div>
  </div>
```

`FeatureEdit.cshtml` already has `@using GAC.Core.Content` so `FeatureGroup` resolves. Then add the bullet sub-list AFTER the closing `</form>` of the main feature form and BEFORE `<partial name="_PickerModal" />`:

```cshtml
@if (Model.Id != 0)
{
    <h2>Feature bullets</h2>
    <table class="adm-table">
      <thead><tr><th>Order</th><th>Label</th><th>Text</th><th></th></tr></thead>
      <tbody>
        @foreach (var b in Model.Bullets.OrderBy(x => x.SortOrder))
        {
            <tr>
              <td>
                <form asp-action="MoveFeatureBullet" method="post" style="display:inline">
                  <input type="hidden" name="bulletId" value="@b.Id" /><input type="hidden" name="vehicleId" value="@Model.VehicleId" /><input type="hidden" name="direction" value="-1" />
                  <button class="adm-btn">&uarr;</button>
                </form>
                <form asp-action="MoveFeatureBullet" method="post" style="display:inline">
                  <input type="hidden" name="bulletId" value="@b.Id" /><input type="hidden" name="vehicleId" value="@Model.VehicleId" /><input type="hidden" name="direction" value="1" />
                  <button class="adm-btn">&darr;</button>
                </form>
              </td>
              <td>@b.Label.En</td>
              <td>@b.Text.En</td>
              <td>
                <form asp-action="RemoveFeatureBullet" method="post" style="display:inline" onsubmit="return confirm('Remove bullet?')">
                  <input type="hidden" name="bulletId" value="@b.Id" /><input type="hidden" name="vehicleId" value="@Model.VehicleId" />
                  <button class="adm-btn adm-btn--danger">Remove</button>
                </form>
              </td>
            </tr>
        }
      </tbody>
    </table>
    <form asp-action="AddFeatureBullet" method="post" class="adm-inline">
      <input type="hidden" name="featureSectionId" value="@Model.Id" /><input type="hidden" name="vehicleId" value="@Model.VehicleId" />
      <input name="labelEn" placeholder="Label (EN)" /><input name="labelAr" placeholder="Label (AR)" dir="rtl" />
      <input name="textEn" placeholder="Text (EN)" /><input name="textAr" placeholder="Text (AR)" dir="rtl" />
      <button class="adm-btn">Add bullet</button>
    </form>
}
```

- [ ] **Step 6: Build + re-run all feature assertions.** `dotnet build` then `dotnet test --filter "Edit_FeaturesList_ShowsGroupColumn|FeatureEdit_New_RendersGroupTabLabelLead"`. PASS.

- [ ] **Step 7: Commit.** `git add -A && git commit -m "feat: extend _Features list + FeatureEdit with group/tablabel/lead/bullets"`

---

### Task 45: _GalleryTabs admin partial with nested images

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_GalleryTabs.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.GalleryTabs` (`List<GalleryTab>`; `GalleryTab : IOrderable { int Id; int VehicleId; LocalizedText Label; int SortOrder; List<GalleryImage> Images; }`; `GalleryImage : IOrderable { int Id; int GalleryTabId; string? ImagePath; LocalizedText Alt; int SortOrder; }`).
- Produces `AddGalleryTab(int vehicleId, string? labelEn, string? labelAr)`, `RemoveGalleryTab(int tabId, int vehicleId)`, `MoveGalleryTab(int tabId, int vehicleId, int direction)`, plus nested `AddGalleryImage(int galleryTabId, int vehicleId, string? imagePath, string? altEn, string? altAr)`, `RemoveGalleryImage(int imageId, int vehicleId)`, `MoveGalleryImage(int imageId, int vehicleId, int direction)`.

Same `_SpecGroups` nested pattern as `_Sliders` (Task 43). Grandchild Add carries `galleryTabId`.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersGalleryTabsPanel_WithImageForm()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Gallery tabs", html);
        Assert.Contains("AddGalleryTab", html);
        Assert.Contains("AddGalleryImage", html);
    }
```

Run `dotnet test --filter Edit_RendersGalleryTabsPanel_WithImageForm`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_GalleryTabs.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Gallery tabs</h2>
@foreach (var g in Model.GalleryTabs.OrderBy(x => x.SortOrder))
{
    <div class="adm-card">
      <h3>@g.Label.En
        <form asp-action="MoveGalleryTab" method="post" style="display:inline">
          <input type="hidden" name="tabId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveGalleryTab" method="post" style="display:inline">
          <input type="hidden" name="tabId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveGalleryTab" method="post" style="display:inline" onsubmit="return confirm('Remove tab and its images?')">
          <input type="hidden" name="tabId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove tab</button>
        </form>
      </h3>
      <div class="adm-picker-grid">
        @foreach (var im in g.Images.OrderBy(x => x.SortOrder))
        {
            <div class="adm-picker-item">
              @if (!string.IsNullOrWhiteSpace(im.ImagePath)) { <img src="@im.ImagePath" class="adm-picker-thumb" alt="" /> }
              <div>@im.Alt.En</div>
              <form asp-action="MoveGalleryImage" method="post" style="display:inline">
                <input type="hidden" name="imageId" value="@im.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
                <button class="adm-btn">&uarr;</button>
              </form>
              <form asp-action="MoveGalleryImage" method="post" style="display:inline">
                <input type="hidden" name="imageId" value="@im.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
                <button class="adm-btn">&darr;</button>
              </form>
              <form asp-action="RemoveGalleryImage" method="post" style="display:inline" onsubmit="return confirm('Remove image?')">
                <input type="hidden" name="imageId" value="@im.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
                <button class="adm-btn adm-btn--danger">Remove</button>
              </form>
            </div>
        }
      </div>
      <form asp-action="AddGalleryImage" method="post" class="adm-inline">
        <input type="hidden" name="galleryTabId" value="@g.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
        <span style="display:inline-flex;gap:.4rem;align-items:center">
          <input type="text" name="imagePath" placeholder="Image" data-media-input />
          <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </span>
        <input name="altEn" placeholder="Alt (EN)" /><input name="altAr" placeholder="Alt (AR)" dir="rtl" />
        <button class="adm-btn">Add image</button>
      </form>
    </div>
}
<form asp-action="AddGalleryTab" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="labelEn" placeholder="Tab label (EN)" /><input name="labelAr" placeholder="Tab label (AR)" dir="rtl" />
  <button class="adm-btn">Add gallery tab</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersGalleryTabsPanel_WithImageForm`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _GalleryTabs admin partial with nested image add/move/remove"`

---

### Task 46: _Quality admin partial (single upsert form)

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Quality.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.Quality` (`QualityBlock? Quality`; `QualityBlock { int Id; int VehicleId; string? MainImage; string? ThumbImage; LocalizedText Strapline,Content; }`) — 0 or 1 per vehicle, NOT IOrderable.
- Produces a single form posting to `UpsertQuality(int vehicleId, string? mainImage, string? thumbImage, string? straplineEn, string? straplineAr, string? contentEn, string? contentAr)`.

One upsert form, prefilled from `Model.Quality` (may be null). No add/remove/move.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersQualityPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Quality / awards", html);
        Assert.Contains("UpsertQuality", html);
    }
```

Run `dotnet test --filter Edit_RendersQualityPanel`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Quality.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
@{ var q = Model.Quality; }
<h2>Quality / awards</h2>
<form asp-action="UpsertQuality" method="post">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <div class="adm-field">
    <label>Main image</label>
    <span style="display:inline-flex;gap:.4rem;align-items:center">
      <input type="text" name="mainImage" value="@(q?.MainImage)" data-media-input />
      <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
    </span>
  </div>
  <div class="adm-field">
    <label>Thumbnail image</label>
    <span style="display:inline-flex;gap:.4rem;align-items:center">
      <input type="text" name="thumbImage" value="@(q?.ThumbImage)" data-media-input />
      <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
    </span>
  </div>
  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Strapline</span>
    <div class="adm-localized__pair">
      <div><label>English</label><input name="straplineEn" value="@(q?.Strapline.En)" /></div>
      <div dir="rtl"><label>Arabic</label><input name="straplineAr" value="@(q?.Strapline.Ar)" dir="rtl" /></div>
    </div>
  </div>
  <div class="adm-field adm-localized">
    <span class="adm-localized__label">Content</span>
    <div class="adm-localized__pair">
      <div><label>English</label><textarea name="contentEn" rows="4">@(q?.Content.En)</textarea></div>
      <div dir="rtl"><label>Arabic</label><textarea name="contentAr" rows="4" dir="rtl">@(q?.Content.Ar)</textarea></div>
    </div>
  </div>
  <button class="adm-btn">Save quality block</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersQualityPanel`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Quality admin partial (single upsert form)"`

---

### Task 47: _Cards admin partial (technology cards)

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Cards.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.Cards` (`List<CardItem>`; `CardItem : IOrderable { int Id; int VehicleId; LocalizedText Title,Text; string? ImagePath; int SortOrder; }` — NO link).
- Produces `AddCard(int vehicleId, string? titleEn, string? titleAr, string? textEn, string? textAr, string? imagePath)`, `RemoveCard(int cardId, int vehicleId)`, `MoveCard(int cardId, int vehicleId, int direction)`.

Note: the technology BANNER image is the Vehicle-level `TechBannerImage` scalar field, edited in the main Save form (Task 40). This partial only manages the 3 cards. Add a one-line hint pointing at the banner field.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersCardsPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Technology cards", html);
        Assert.Contains("AddCard", html);
    }
```

Run `dotnet test --filter Edit_RendersCardsPanel`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Cards.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Technology cards</h2>
<p class="adm-hint">The technology banner image is set in the main form above (&ldquo;Technology banner image&rdquo;). These cards appear below it.</p>
<div class="adm-picker-grid">
  @foreach (var c in Model.Cards.OrderBy(x => x.SortOrder))
  {
      <div class="adm-picker-item">
        @if (!string.IsNullOrWhiteSpace(c.ImagePath)) { <img src="@c.ImagePath" class="adm-picker-thumb" alt="" /> }
        <div>@c.Title.En</div>
        <form asp-action="MoveCard" method="post" style="display:inline">
          <input type="hidden" name="cardId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveCard" method="post" style="display:inline">
          <input type="hidden" name="cardId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveCard" method="post" style="display:inline" onsubmit="return confirm('Remove card?')">
          <input type="hidden" name="cardId" value="@c.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove</button>
        </form>
      </div>
  }
</div>
<form asp-action="AddCard" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="titleEn" placeholder="Title (EN)" /><input name="titleAr" placeholder="Title (AR)" dir="rtl" />
  <input name="textEn" placeholder="Text (EN)" /><input name="textAr" placeholder="Text (AR)" dir="rtl" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="imagePath" placeholder="Image" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <button class="adm-btn">Add card</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersCardsPanel`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Cards admin partial (technology cards)"`

---

### Task 48: _Safety admin partial (safety toggles)

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Safety.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.SafetyToggles` (`List<SafetyToggle>`; `SafetyToggle : IOrderable { int Id; int VehicleId; LocalizedText Title; string? ImagePath; LocalizedText Strap,Content; int SortOrder; }`).
- Produces `AddSafetyToggle(int vehicleId, string? titleEn, string? titleAr, string? imagePath, string? strapEn, string? strapAr, string? contentEn, string? contentAr)`, `RemoveSafetyToggle(int toggleId, int vehicleId)`, `MoveSafetyToggle(int toggleId, int vehicleId, int direction)`.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersSafetyPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Safety toggles", html);
        Assert.Contains("AddSafetyToggle", html);
    }
```

Run `dotnet test --filter Edit_RendersSafetyPanel`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Safety.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Safety toggles</h2>
@foreach (var s in Model.SafetyToggles.OrderBy(x => x.SortOrder))
{
    <div class="adm-card">
      <h3>@s.Title.En
        <form asp-action="MoveSafetyToggle" method="post" style="display:inline">
          <input type="hidden" name="toggleId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveSafetyToggle" method="post" style="display:inline">
          <input type="hidden" name="toggleId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveSafetyToggle" method="post" style="display:inline" onsubmit="return confirm('Remove safety toggle?')">
          <input type="hidden" name="toggleId" value="@s.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove</button>
        </form>
      </h3>
      @if (!string.IsNullOrWhiteSpace(s.ImagePath)) { <img src="@s.ImagePath" class="adm-picker-thumb" alt="" /> }
      <p><strong>Strap:</strong> @s.Strap.En</p>
      <p>@s.Content.En</p>
    </div>
}
<form asp-action="AddSafetyToggle" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="titleEn" placeholder="Title (EN)" /><input name="titleAr" placeholder="Title (AR)" dir="rtl" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="imagePath" placeholder="Image" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <input name="strapEn" placeholder="Strap (EN)" /><input name="strapAr" placeholder="Strap (AR)" dir="rtl" />
  <input name="contentEn" placeholder="Content (EN)" /><input name="contentAr" placeholder="Content (AR)" dir="rtl" />
  <button class="adm-btn">Add safety toggle</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersSafetyPanel`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Safety admin partial (safety toggles)"`

---

### Task 49: Rework _Trims admin partial (ModelLabel + image + nested price rows)

**Files:**
- Modify `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Trims.cshtml`

**Interfaces:**
- Consumes the REWORKED `GAC.Core.Content.Trim` (now with `LocalizedText ModelLabel`, `string? ImagePath`, `List<TrimPriceRow> PriceRows`; existing `Name`, `SpecPdf`, `Price`, `Highlights` retained but Price/Highlights unused by the new render). `TrimPriceRow : IOrderable { int Id; int TrimId; LocalizedText Text; int SortOrder; }`.
- Consumes existing `AddTrim(...)` / `RemoveTrim(...)` / `MoveTrim(...)` actions — `AddTrim` is extended with `modelLabelEn`/`modelLabelAr`/`imagePath` params (controller-plan change). Produces nested-price actions `AddTrimPriceRow(int trimId, int vehicleId, string? textEn, string? textAr)`, `RemoveTrimPriceRow(int rowId, int vehicleId)`, `MoveTrimPriceRow(int rowId, int vehicleId, int direction)`.

Convert the existing single-table `_Trims` into the `_SpecGroups` nested card pattern (one `adm-card` per trim with an inner price-row table + inner Add-price-row form), plus reorder/remove on the trim card.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersTrimsPanel_WithModelLabelAndPriceRows()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Trims", html);
        Assert.Contains("modelLabelEn", html);   // new ModelLabel field on Add form
        Assert.Contains("AddTrimPriceRow", html); // nested price-row form
    }
```

Run `dotnet test --filter Edit_RendersTrimsPanel_WithModelLabelAndPriceRows`. Expect FAIL.

- [ ] **Step 2: Replace _Trims.cshtml entirely.** Overwrite `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Trims.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Trims</h2>
@foreach (var t in Model.Trims.OrderBy(x => x.SortOrder))
{
    <div class="adm-card">
      <h3>@t.ModelLabel.En @t.Name.En
        <form asp-action="MoveTrim" method="post" style="display:inline">
          <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
          <button class="adm-btn">&uarr;</button>
        </form>
        <form asp-action="MoveTrim" method="post" style="display:inline">
          <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
          <button class="adm-btn">&darr;</button>
        </form>
        <form asp-action="RemoveTrim" method="post" style="display:inline" onsubmit="return confirm('Remove trim and its price rows?')">
          <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
          <button class="adm-btn adm-btn--danger">Remove trim</button>
        </form>
      </h3>
      @if (!string.IsNullOrWhiteSpace(t.ImagePath)) { <img src="@t.ImagePath" class="adm-picker-thumb" alt="" /> }
      <table class="adm-table">
        <thead><tr><th>Order</th><th>Price line</th><th></th></tr></thead>
        <tbody>
          @foreach (var r in t.PriceRows.OrderBy(x => x.SortOrder))
          {
              <tr>
                <td>
                  <form asp-action="MoveTrimPriceRow" method="post" style="display:inline">
                    <input type="hidden" name="rowId" value="@r.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
                    <button class="adm-btn">&uarr;</button>
                  </form>
                  <form asp-action="MoveTrimPriceRow" method="post" style="display:inline">
                    <input type="hidden" name="rowId" value="@r.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
                    <button class="adm-btn">&darr;</button>
                  </form>
                </td>
                <td>@r.Text.En</td>
                <td>
                  <form asp-action="RemoveTrimPriceRow" method="post" style="display:inline" onsubmit="return confirm('Remove price line?')">
                    <input type="hidden" name="rowId" value="@r.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
                    <button class="adm-btn adm-btn--danger">Remove</button>
                  </form>
                </td>
              </tr>
          }
        </tbody>
      </table>
      <form asp-action="AddTrimPriceRow" method="post" class="adm-inline">
        <input type="hidden" name="trimId" value="@t.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
        <input name="textEn" placeholder="e.g. Total: 95,000 SAR (EN)" /><input name="textAr" placeholder="Text (AR)" dir="rtl" />
        <button class="adm-btn">Add price line</button>
      </form>
    </div>
}
<form asp-action="AddTrim" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="modelLabelEn" placeholder="Model label (EN)" /><input name="modelLabelAr" placeholder="Model label (AR)" dir="rtl" />
  <input name="nameEn" placeholder="Name (EN)" /><input name="nameAr" placeholder="Name (AR)" dir="rtl" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="imagePath" placeholder="Image" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="specPdf" placeholder="Spec PDF (optional)" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <button class="adm-btn">Add trim</button>
</form>
```

Note: the previous `_Trims` Add form passed `price`/`highlightsEn`/`highlightsAr`; those are dropped from the form here (Price/Highlights are unused by the new render). The `AddTrim` controller action must keep those params OPTIONAL (nullable) so this form binds — flagged in crossNotes for the controller plan.

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersTrimsPanel_WithModelLabelAndPriceRows`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: rework _Trims admin partial with model label, image and nested price rows"`

---

### Task 50: _Warranty admin partial (document/PDF links)

**Files:**
- Create `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Warranty.cshtml`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle.WarrantyLinks` (`List<WarrantyLink>`; `WarrantyLink : IOrderable { int Id; int VehicleId; LocalizedText Label; string Url; int SortOrder; }`).
- Produces `AddWarrantyLink(int vehicleId, string? labelEn, string? labelAr, string url)`, `RemoveWarrantyLink(int linkId, int vehicleId)`, `MoveWarrantyLink(int linkId, int vehicleId, int direction)`. The `Url` is a document/PDF path → use the media picker.

- [ ] **Step 1: Write a failing assertion.** Add to `AdminVehicleEditViewTests.cs`:

```csharp
    [Fact]
    public async Task Edit_RendersWarrantyPanel()
    {
        var html = await GetFirstVehicleEditHtmlAsync();
        Assert.Contains("Warranty links", html);
        Assert.Contains("AddWarrantyLink", html);
    }
```

Run `dotnet test --filter Edit_RendersWarrantyPanel`. Expect FAIL.

- [ ] **Step 2: Create the partial.** Write `Solution/GAC.Web/Areas/Admin/Views/Vehicles/_Warranty.cshtml`:

```cshtml
@model GAC.Core.Content.Vehicle
<h2>Warranty links</h2>
<table class="adm-table">
  <thead><tr><th>Order</th><th>Label</th><th>URL / document</th><th></th></tr></thead>
  <tbody>
    @foreach (var w in Model.WarrantyLinks.OrderBy(x => x.SortOrder))
    {
        <tr>
          <td>
            <form asp-action="MoveWarrantyLink" method="post" style="display:inline">
              <input type="hidden" name="linkId" value="@w.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="-1" />
              <button class="adm-btn">&uarr;</button>
            </form>
            <form asp-action="MoveWarrantyLink" method="post" style="display:inline">
              <input type="hidden" name="linkId" value="@w.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" /><input type="hidden" name="direction" value="1" />
              <button class="adm-btn">&darr;</button>
            </form>
          </td>
          <td>@w.Label.En</td>
          <td>@w.Url</td>
          <td>
            <form asp-action="RemoveWarrantyLink" method="post" style="display:inline" onsubmit="return confirm('Remove warranty link?')">
              <input type="hidden" name="linkId" value="@w.Id" /><input type="hidden" name="vehicleId" value="@Model.Id" />
              <button class="adm-btn adm-btn--danger">Remove</button>
            </form>
          </td>
        </tr>
    }
  </tbody>
</table>
<form asp-action="AddWarrantyLink" method="post" class="adm-inline">
  <input type="hidden" name="vehicleId" value="@Model.Id" />
  <input name="labelEn" placeholder="Label (EN)" /><input name="labelAr" placeholder="Label (AR)" dir="rtl" />
  <span style="display:inline-flex;gap:.4rem;align-items:center">
    <input type="text" name="url" placeholder="Document / PDF" data-media-input />
    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
  </span>
  <button class="adm-btn">Add warranty link</button>
</form>
```

- [ ] **Step 3: Build + re-run.** `dotnet build` then `dotnet test --filter Edit_RendersWarrantyPanel`. PASS.

- [ ] **Step 4: Commit.** `git add -A && git commit -m "feat: add _Warranty admin partial (document/PDF links)"`

---

### Task 51: Full admin Edit-view integration smoke test (all panels present in one render)

**Files:**
- Modify `Solution/GAC.Tests/Admin/AdminVehicleEditViewTests.cs`

**Interfaces:**
- Consumes the fully wired `/Admin/Vehicles/Edit/{id}` page (Tasks 40–50) via `AdminWebApplicationFactory.ClientForRole(Roles.Editor)` + `TestAuthHandler` (`X-Test-Role: Editor`).
- Produces a single regression test asserting every new panel heading + the section-nav are present in ONE GET, plus a role-gating assertion.

This consolidates the per-panel assertions into one durable guard and confirms auth. Mirrors the existing admin-view auth pattern (`AdminAuthTests` / `AdminWebApplicationFactory`) exactly.

- [ ] **Step 1: Add the consolidated failing test.** Append to `AdminVehicleEditViewTests.cs`:

```csharp
public class AdminVehicleEditViewSmokeTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminVehicleEditViewSmokeTests(AdminWebApplicationFactory factory) => _factory = factory;

    private async Task<string> EditHtmlAsync(string role)
    {
        var client = _factory.ClientForRole(role);
        var list = await client.GetStringAsync("/Admin/Vehicles");
        var m = Regex.Match(list, @"/Admin/Vehicles/Edit/(\d+)");
        Assert.True(m.Success);
        var res = await client.GetAsync($"/Admin/Vehicles/Edit/{m.Groups[1].Value}");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadAsStringAsync();
    }

    [Fact]
    public async Task Edit_AsEditor_RendersEveryRichSectionPanel()
    {
        var html = await EditHtmlAsync(Roles.Editor);
        foreach (var heading in new[]
        {
            "adm-section-nav",
            "Section headings",
            "Overview stats",
            "Sliders",
            "Feature sections",
            "Gallery tabs",
            "Quality / awards",
            "Technology cards",
            "Safety toggles",
            "Trims",
            "Warranty links",
            "Technology banner image",
            "Enquiry title",
        })
            Assert.Contains(heading, html);
        // PickerModal rendered exactly once.
        Assert.Equal(1, Regex.Matches(html, "id=\"mediaPicker\"").Count);
    }

    [Fact]
    public async Task Edit_AsSales_IsForbidden()
    {
        var client = _factory.ClientForRole(Roles.Sales);
        var list = await client.GetAsync("/Admin/Vehicles");
        // Sales lacks ContentEditor → redirected away from the Vehicles controller.
        Assert.Equal(HttpStatusCode.Found, list.StatusCode);
    }
}
```

Run `dotnet test --filter AdminVehicleEditViewSmokeTests` from `Solution`. If Tasks 40–50 are complete it PASSES immediately; if any panel/heading text differs from the partials, this surfaces the mismatch (fix the partial text, not the test, to match the canonical headings above).

- [ ] **Step 2: Run the full admin suite to confirm no regressions.** `dotnet test --filter "FullyQualifiedName~GAC.Tests.Admin"` from `Solution`. All green.

- [ ] **Step 3: Commit.** `git add -A && git commit -m "test: add consolidated admin Edit-view smoke + role-gating regression guard"`


---

## Phase 4 — Render

### Task 60: VehicleContent render helpers (tab keys + feature grouping + safety/slider helpers)

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Infrastructure/VehicleContent.cs`
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleContentTests.cs`

**Interfaces:**
- Consumes `GAC.Core.Content.Vehicle`, `FeatureSection`, `FeatureGroup`, `SliderGroup`, `SafetyToggle` (added by the model tasks 01-09).
- Produces (new static methods on `VehicleContent`):
  - `IEnumerable<FeatureSection> DesignFeatures(Vehicle v)` — features where `GroupKey == FeatureGroup.Design`, ordered by `SortOrder`.
  - `IEnumerable<FeatureSection> PerformanceFeatures(Vehicle v)` — features where `GroupKey == FeatureGroup.Performance`, ordered by `SortOrder`.
  - `string TabKey(string prefix, int index)` — e.g. `TabKey("d", 0) == "d1"`, `TabKey("p", 2) == "p3"`, `TabKey("g", 1) == "g2"` (1-based suffix).
  - `string StateActive(bool isFirst)` — returns `" is-active"` when first else `""` (leading space; appended to a class list).
  - `string StateOpen(bool isFirst)` — returns `" is-open"` when first else `""`.
  - `string AriaExpanded(bool isFirst)` — returns `"true"` when first else `"false"`.

Note: `HasStructuredContent` is intentionally **left in place** (still referenced by `VehicleDetailRenderTests`), but Detail.cshtml will no longer branch on it (Task 73). Do not delete it.

Steps:

- [ ] **Step 1: Write failing tests for the new helpers.** Append to `VehicleContentTests.cs`:
  ```csharp
    [Fact]
    public void TabKey_BuildsOneBasedSuffix()
    {
        Assert.Equal("d1", VehicleContent.TabKey("d", 0));
        Assert.Equal("p3", VehicleContent.TabKey("p", 2));
        Assert.Equal("g2", VehicleContent.TabKey("g", 1));
    }

    [Fact]
    public void DesignFeatures_FiltersAndOrders()
    {
        var v = new Vehicle();
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Performance, SortOrder = 0 });
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Design, SortOrder = 2 });
        v.Features.Add(new FeatureSection { GroupKey = FeatureGroup.Design, SortOrder = 1 });

        var design = VehicleContent.DesignFeatures(v).ToList();
        var perf = VehicleContent.PerformanceFeatures(v).ToList();

        Assert.Equal(2, design.Count);
        Assert.Equal(1, design[0].SortOrder);
        Assert.Equal(2, design[1].SortOrder);
        Assert.Single(perf);
    }

    [Fact]
    public void StateHelpers_OnlyFirstIsActiveOrOpen()
    {
        Assert.Equal(" is-active", VehicleContent.StateActive(true));
        Assert.Equal("", VehicleContent.StateActive(false));
        Assert.Equal(" is-open", VehicleContent.StateOpen(true));
        Assert.Equal("", VehicleContent.StateOpen(false));
        Assert.Equal("true", VehicleContent.AriaExpanded(true));
        Assert.Equal("false", VehicleContent.AriaExpanded(false));
    }
  ```
  Add `using System.Linq;` at the top of the test file if not already present.
- [ ] **Step 2: Run the tests; confirm compile failure** (`'VehicleContent' does not contain a definition for 'TabKey'`): `dotnet test C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests --filter "FullyQualifiedName~VehicleContentTests"`.
- [ ] **Step 3: Implement the helpers.** Append inside the `VehicleContent` class in `VehicleContent.cs` (and add `using System.Collections.Generic;` + `using System.Linq;` at the top if missing):
  ```csharp
    public static IEnumerable<FeatureSection> DesignFeatures(Vehicle v)
        => v.Features.Where(f => f.GroupKey == FeatureGroup.Design).OrderBy(f => f.SortOrder);

    public static IEnumerable<FeatureSection> PerformanceFeatures(Vehicle v)
        => v.Features.Where(f => f.GroupKey == FeatureGroup.Performance).OrderBy(f => f.SortOrder);

    public static string TabKey(string prefix, int index) => $"{prefix}{index + 1}";

    public static string StateActive(bool isFirst) => isFirst ? " is-active" : "";

    public static string StateOpen(bool isFirst) => isFirst ? " is-open" : "";

    public static string AriaExpanded(bool isFirst) => isFirst ? "true" : "false";
  ```
- [ ] **Step 4: Run tests; confirm green:** `dotnet test C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests --filter "FullyQualifiedName~VehicleContentTests"`.
- [ ] **Step 5: Commit:** `git add -A && git commit -m "feat: VehicleContent render helpers for tabs, feature groups, and toggle state"`.

---

### Task 61: Hero + subnav + section-heading helper partial

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleHero.cshtml`
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleSubnav.cshtml`
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_SectionHead.cshtml`

**Interfaces:**
- `_VehicleHero` consumes `Vehicle.Images` (Hero), `Vehicle.Name`, `Vehicle.Tagline`. Produces `<section class="mp-hero">` with `.mp-hero__img/.mp-hero__title/.mp-hero__sub`.
- `_VehicleSubnav` consumes nothing dynamic (static jump nav anchored to fixed section ids). Produces `<nav class="mp-subnav">` for the scroll-spy JS (`main.js` queries `.mp-subnav a`).
- `_SectionHead` is a reusable partial: `@model GAC.Core.Content.SectionHeading?`; renders the `<header class="mp-head mp-head--left">` block (`.mp-head__title/.mp-head__sub/.mp-head__body`) for a given `SectionHeading`, rendering nothing when the model is null.

Steps:

- [ ] **Step 1: Drop IntroText section from `_VehicleHero`.** The master page has no intro paragraph between hero and overview; that `IntroText` block was part of the simplified view and would render an orphan section. Replace the entire content of `_VehicleHero.cshtml` with (keeps the hero contract identical, removes the trailing `IntroText` section):
  ```cshtml
  @using GAC.Core.Content
  @model GAC.Core.Content.Vehicle
  @{
      var hero = Model.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Hero)
                 ?? Model.Images.OrderBy(i => i.SortOrder).FirstOrDefault();
  }
  <section class="mp-hero">
    <a class="mp-hero__link" href="#enquiry" aria-label="@L["Book a Test Drive"]">
      @if (hero != null)
      {
          <img class="mp-hero__img" src="@hero.Path" alt="@Model.Name.Localize()" />
      }
      <div class="mp-hero__overlay">
        <div class="container">
          <h1 class="mp-hero__title">@Model.Name.Localize()</h1>
          @if (!string.IsNullOrWhiteSpace(Model.Tagline.Localize()))
          {
              <p class="mp-hero__sub">@Model.Tagline.Localize()</p>
          }
          <span class="btn btn--hero">@L["Book a Test Drive"]</span>
        </div>
      </div>
    </a>
  </section>
  ```
- [ ] **Step 2: Create `_VehicleSubnav.cshtml`** — the jump nav from the master HTML (lines 39-55 of `emkoo.html`), with localized labels. `is-active` on the first anchor matches the master:
  ```cshtml
  @model GAC.Core.Content.Vehicle
  <nav class="mp-subnav" aria-label="@L["Model sections"]">
    <div class="container mp-subnav__inner">
      <a class="btn btn--subnav" href="#trims">@L["Price and Trims"]</a>
      <div class="mp-subnav__links">
        <a href="#exterior" class="is-active">@L["Exterior"]</a>
        <a href="#design">@L["Design"]</a>
        <a href="#interior">@L["Interior"]</a>
        <a href="#gallery">@L["Gallery"]</a>
        <a href="#quality">@L["Quality"]</a>
        <a href="#technology">@L["Technology"]</a>
        <a href="#performance">@L["Performance"]</a>
        <a href="#safety">@L["Safety"]</a>
        <a href="#warranty">@L["Warranty"]</a>
      </div>
      <a class="btn btn--subnav" href="#enquiry">@L["Order Online"]</a>
    </div>
  </nav>
  ```
- [ ] **Step 3: Create `_SectionHead.cshtml`** — reusable heading block used by every section partial. Renders nothing when null; emits `__body` only when present (master uses `.mp-head__body` in safety/trims/warranty):
  ```cshtml
  @model GAC.Core.Content.SectionHeading
  @if (Model != null)
  {
      <header class="mp-head mp-head--left">
        @if (!string.IsNullOrWhiteSpace(Model.Title.Localize()))
        {
            <h2 class="mp-head__title">@Model.Title.Localize()</h2>
        }
        @if (!string.IsNullOrWhiteSpace(Model.Sub.Localize()))
        {
            <p class="mp-head__sub">@Html.Raw(Model.Sub.Localize())</p>
        }
        @if (!string.IsNullOrWhiteSpace(Model.Body.Localize()))
        {
            <p class="mp-head__body">@Html.Raw(Model.Body.Localize())</p>
        }
      </header>
  }
  ```
  Sub/Body use `@Html.Raw` because the parser preserves `<br>` in those fields (see SHARED CONTRACTS parser cheatsheet: head sub/body innerHTML).
- [ ] **Step 4: Build to confirm Razor compiles** (Razor compiles at build): `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 5: Commit:** `git add -A && git commit -m "feat: hero (drop intro), subnav, and reusable section-head render partials"`.

---

### Task 62: Overview stats partial (#exterior)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleStats.cshtml`

**Interfaces:**
- Consumes `Vehicle.Headings` (key `SectionKey.Overview`), `Vehicle.Stats` (`StatItem.Label/Value/SortOrder`), `Vehicle.StatsNote` (LocalizedText).
- Produces `<section class="mp-section" id="exterior">` containing `.mp-head`, `.mp-stats > .mp-stat` (`.mp-stat__label`/`.mp-stat__value`), and `.mp-note`.

Steps:

- [ ] **Step 1: Create `_VehicleStats.cshtml`:**
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Overview);
      var stats = Model.Stats.OrderBy(s => s.SortOrder).ToList();
      var note = Model.StatsNote.Localize();
  }
  @if (head != null || stats.Count > 0)
  {
      <section class="mp-section" id="exterior">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          @if (stats.Count > 0)
          {
              <div class="mp-stats">
                @foreach (var s in stats)
                {
                    <div class="mp-stat"><span class="mp-stat__label">@s.Label.Localize()</span><span class="mp-stat__value">@s.Value.Localize()</span></div>
                }
              </div>
          }
          @if (!string.IsNullOrWhiteSpace(note))
          {
              <p class="mp-note">@note</p>
          }
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: overview stats render partial (#exterior)"`.

---

### Task 63: Slider partial (reusable, used twice — exterior + interior)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleSlider.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.SliderGroup` — a SINGLE slider group (caller passes one group at a time, so the same partial renders the exterior slider and the interior slider, the latter wrapped with `id="interior"`).
- Consumes `SliderGroup.Eyebrow/Title/Slides` and each `SliderSlide.ImagePath/Alt/SortOrder`.
- Produces the EXACT slider JS contract: `.mp-slider-wrap > .mp-slider[data-slider]` with `[data-slider-track]`, `[data-slider-prev]`, `[data-slider-next]` and `.mp-slider__caption`. JS builds the pager dots — do NOT hand-author them.

Steps:

- [ ] **Step 1: Create `_VehicleSlider.cshtml`** (the `mp-slider-wrap` and optional `id` are emitted by the CALLER, see Task 73 — this partial emits the `.mp-slider` and inner contract):
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.SliderGroup
  @{
      var slides = Model.Slides.OrderBy(s => s.SortOrder).Where(s => !string.IsNullOrWhiteSpace(s.ImagePath)).ToList();
  }
  @if (slides.Count > 0)
  {
      <div class="mp-slider" data-slider>
        <div class="mp-slider__viewport">
          <div class="mp-slider__track" data-slider-track>
            @foreach (var s in slides)
            {
                <figure class="mp-slide"><img src="@s.ImagePath" alt="@s.Alt.Localize()" /></figure>
            }
          </div>
        </div>
        <div class="mp-slider__caption">
          <span class="mp-slider__eyebrow">@Model.Eyebrow.Localize()</span>
          <span class="mp-slider__title">@Model.Title.Localize()</span>
        </div>
        <button class="mp-slider__arrow mp-slider__arrow--prev" data-slider-prev aria-label="@L["Previous"]">&lsaquo;</button>
        <button class="mp-slider__arrow mp-slider__arrow--next" data-slider-next aria-label="@L["Next"]">&rsaquo;</button>
      </div>
  }
  ```
  Note: caption order differs from `_VehicleStats`; this matches the master HTML where caption precedes the arrows. JS only needs the hooks present, not the order.
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: reusable slider render partial (data-slider contract)"`.

---

### Task 64: Design tabs partial (#design — feature panels, GroupKey=Design)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleDesign.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Design`) and `VehicleContent.DesignFeatures(Model)` (each `FeatureSection.TabLabel/Heading/Lead/ImagePath/Bullets`, each `FeatureBullet.Label/Text`).
- Produces `<section class="mp-section" id="design">` with the EXACT tabs contract: `.mp-tabs[data-tabs-wrap]` → `.mp-tabs__nav[data-tabs]` (buttons `data-tab-btn="d1"...`, first `is-active`) sharing a parent with `.mp-tabs__root[data-tab-root]` (panels `.mp-feature[data-tab-panel="d1"]`, first `is-active`).

Steps:

- [ ] **Step 1: Create `_VehicleDesign.cshtml`:**
  ```cshtml
  @using System.Linq
  @using GAC.Web.Infrastructure
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Design);
      var features = VehicleContent.DesignFeatures(Model).ToList();
  }
  @if (features.Count > 0)
  {
      <section class="mp-section" id="design">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          <div class="mp-tabs" data-tabs-wrap>
            <div class="mp-tabs__nav" data-tabs>
              @for (var i = 0; i < features.Count; i++)
              {
                  <button class="mp-tabs__btn@(VehicleContent.StateActive(i == 0))" data-tab-btn="@VehicleContent.TabKey("d", i)">@features[i].TabLabel.Localize()</button>
              }
            </div>
            <div class="mp-tabs__root" data-tab-root>
              @for (var i = 0; i < features.Count; i++)
              {
                  var f = features[i];
                  var bullets = f.Bullets.OrderBy(b => b.SortOrder).ToList();
                  <div class="mp-feature@(VehicleContent.StateActive(i == 0))" data-tab-panel="@VehicleContent.TabKey("d", i)">
                    @if (!string.IsNullOrWhiteSpace(f.ImagePath))
                    {
                        <div class="mp-feature__media"><img src="@f.ImagePath" alt="@f.Heading.Localize()" /></div>
                    }
                    <div class="mp-feature__body">
                      @if (!string.IsNullOrWhiteSpace(f.Heading.Localize()))
                      {
                          <h3 class="mp-feature__title">@f.Heading.Localize()</h3>
                      }
                      @if (!string.IsNullOrWhiteSpace(f.Lead.Localize()))
                      {
                          <p>@f.Lead.Localize()</p>
                      }
                      @if (bullets.Count > 0)
                      {
                          <ul class="mp-feature__list">
                            @foreach (var b in bullets)
                            {
                                <li><strong>@b.Label.Localize():</strong> @Html.Raw(b.Text.Localize())</li>
                            }
                          </ul>
                      }
                    </div>
                  </div>
              }
            </div>
          </div>
        </div>
      </section>
  }
  ```
  `@Html.Raw(b.Text.Localize())` because bullet text may contain inline `<br>`/`<a>` preserved by the parser (cheatsheet: feature bullets innerHTML). `<strong>@b.Label:</strong>` reproduces the master `<strong>X:</strong> Y` shape.
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: design tabs render partial (#design feature panels)"`.

---

### Task 65: Performance tabs partial (#performance — feature panels, GroupKey=Performance)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehiclePerformance.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Performance`) and `VehicleContent.PerformanceFeatures(Model)`.
- Produces `<section class="mp-section" id="performance">` with the tabs contract, panel keys `p1/p2/p3`.

Steps:

- [ ] **Step 1: Create `_VehiclePerformance.cshtml`** (identical shape to Design with `p` tab keys and the Performance heading/features):
  ```cshtml
  @using System.Linq
  @using GAC.Web.Infrastructure
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Performance);
      var features = VehicleContent.PerformanceFeatures(Model).ToList();
  }
  @if (features.Count > 0)
  {
      <section class="mp-section" id="performance">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          <div class="mp-tabs" data-tabs-wrap>
            <div class="mp-tabs__nav" data-tabs>
              @for (var i = 0; i < features.Count; i++)
              {
                  <button class="mp-tabs__btn@(VehicleContent.StateActive(i == 0))" data-tab-btn="@VehicleContent.TabKey("p", i)">@features[i].TabLabel.Localize()</button>
              }
            </div>
            <div class="mp-tabs__root" data-tab-root>
              @for (var i = 0; i < features.Count; i++)
              {
                  var f = features[i];
                  var bullets = f.Bullets.OrderBy(b => b.SortOrder).ToList();
                  <div class="mp-feature@(VehicleContent.StateActive(i == 0))" data-tab-panel="@VehicleContent.TabKey("p", i)">
                    @if (!string.IsNullOrWhiteSpace(f.ImagePath))
                    {
                        <div class="mp-feature__media"><img src="@f.ImagePath" alt="@f.Heading.Localize()" /></div>
                    }
                    <div class="mp-feature__body">
                      @if (!string.IsNullOrWhiteSpace(f.Heading.Localize()))
                      {
                          <h3 class="mp-feature__title">@f.Heading.Localize()</h3>
                      }
                      @if (!string.IsNullOrWhiteSpace(f.Lead.Localize()))
                      {
                          <p>@f.Lead.Localize()</p>
                      }
                      @if (bullets.Count > 0)
                      {
                          <ul class="mp-feature__list">
                            @foreach (var b in bullets)
                            {
                                <li><strong>@b.Label.Localize():</strong> @Html.Raw(b.Text.Localize())</li>
                            }
                          </ul>
                      }
                    </div>
                  </div>
              }
            </div>
          </div>
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: performance tabs render partial (#performance feature panels)"`.

---

### Task 66: Gallery tabs partial (#gallery) + lightbox singleton helper

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleGallery.cshtml`
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_Lightbox.cshtml`

**Interfaces:**
- `_VehicleGallery` `@model GAC.Core.Content.Vehicle`. Consumes `Vehicle.Headings` (key `SectionKey.Gallery`), `Vehicle.GalleryTabs` (`GalleryTab.Label/SortOrder/Images`), each `GalleryImage.ImagePath/Alt/SortOrder`. Produces `<section class="mp-section" id="gallery">` with tabs (`.mp-gpanel[data-tab-panel="g1"...]`, first `is-active`) and `.mp-gallery > a.mp-gshot[href=full] > img + .mp-gshot__zoom svg`.
- `_Lightbox` `@model object` (no data) — emits the EXACT single `[data-lightbox]` viewer. Rendered EXACTLY ONCE per page by Detail.cshtml (NOT in `_Layout`).

Steps:

- [ ] **Step 1: Create `_VehicleGallery.cshtml`** (`a.mp-gshot href` == `img src` == `GalleryImage.ImagePath`, full-size):
  ```cshtml
  @using System.Linq
  @using GAC.Web.Infrastructure
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Gallery);
      var tabs = Model.GalleryTabs.OrderBy(t => t.SortOrder).ToList();
  }
  @if (tabs.Count > 0)
  {
      <section class="mp-section" id="gallery">
        <div class="container">
          @if (head != null)
          {
              <header class="mp-head mp-head--center">
                @if (!string.IsNullOrWhiteSpace(head.Title.Localize()))
                {
                    <h2 class="mp-head__title">@head.Title.Localize()</h2>
                }
              </header>
          }
          <div class="mp-tabs" data-tabs-wrap>
            <div class="mp-tabs__nav" data-tabs>
              @for (var i = 0; i < tabs.Count; i++)
              {
                  <button class="mp-tabs__btn@(VehicleContent.StateActive(i == 0))" data-tab-btn="@VehicleContent.TabKey("g", i)">@tabs[i].Label.Localize()</button>
              }
            </div>
            <div class="mp-tabs__root" data-tab-root>
              @for (var i = 0; i < tabs.Count; i++)
              {
                  var images = tabs[i].Images.OrderBy(im => im.SortOrder).Where(im => !string.IsNullOrWhiteSpace(im.ImagePath)).ToList();
                  <div class="mp-gpanel@(VehicleContent.StateActive(i == 0))" data-tab-panel="@VehicleContent.TabKey("g", i)">
                    <div class="mp-gallery">
                      @foreach (var im in images)
                      {
                          <a class="mp-gshot" href="@im.ImagePath"><img src="@im.ImagePath" alt="@im.Alt.Localize()" loading="lazy" /><span class="mp-gshot__zoom" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="M10 4a6 6 0 104.47 10.03l4.25 4.25 1.41-1.41-4.25-4.25A6 6 0 0010 4zm0 2a4 4 0 110 8 4 4 0 010-8z"/></svg></span></a>
                      }
                    </div>
                  </div>
              }
            </div>
          </div>
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Create `_Lightbox.cshtml`** (the single page-level viewer, verbatim from the contract):
  ```cshtml
  @model object
  <div class="mp-lightbox" data-lightbox aria-hidden="true" role="dialog" aria-label="@L["Image viewer"]">
    <button class="mp-lightbox__close" data-lb-close aria-label="@L["Close"]">&times;</button>
    <button class="mp-lightbox__nav mp-lightbox__nav--prev" data-lb-prev aria-label="@L["Previous image"]">&lsaquo;</button>
    <img class="mp-lightbox__img" data-lb-img src="" alt="" />
    <button class="mp-lightbox__nav mp-lightbox__nav--next" data-lb-next aria-label="@L["Next image"]">&rsaquo;</button>
    <div class="mp-lightbox__count" data-lb-count></div>
  </div>
  ```
- [ ] **Step 3: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 4: Commit:** `git add -A && git commit -m "feat: gallery tabs render partial + lightbox singleton (#gallery)"`.

---

### Task 67: Quality / awards partial (#quality)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleQuality.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Quality` (`QualityBlock?`: `MainImage/ThumbImage/Strapline/Content`).
- Produces `<section class="mp-section" id="quality">` with `.mp-quality__main img`, `.mp-quality__card > .mp-quality__thumb img`, `.mp-quality__strapline`, `.mp-quality__content`.

Steps:

- [ ] **Step 1: Create `_VehicleQuality.cshtml`:**
  ```cshtml
  @model GAC.Core.Content.Vehicle
  @{
      var q = Model.Quality;
  }
  @if (q != null && (!string.IsNullOrWhiteSpace(q.MainImage) || !string.IsNullOrWhiteSpace(q.Strapline.Localize()) || !string.IsNullOrWhiteSpace(q.Content.Localize())))
  {
      <section class="mp-section" id="quality">
        <div class="container">
          <div class="mp-quality">
            @if (!string.IsNullOrWhiteSpace(q.MainImage))
            {
                <div class="mp-quality__main">
                  <img src="@q.MainImage" alt="@Model.Name.Localize()" />
                </div>
            }
            <aside class="mp-quality__card">
              @if (!string.IsNullOrWhiteSpace(q.ThumbImage))
              {
                  <div class="mp-quality__thumb"><img src="@q.ThumbImage" alt="@Model.Name.Localize()" loading="lazy" /></div>
              }
              <div class="mp-quality__text">
                @if (!string.IsNullOrWhiteSpace(q.Strapline.Localize()))
                {
                    <span class="mp-quality__strapline">@q.Strapline.Localize()</span>
                }
                @if (!string.IsNullOrWhiteSpace(q.Content.Localize()))
                {
                    <p class="mp-quality__content">@q.Content.Localize()</p>
                }
              </div>
            </aside>
          </div>
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: quality/awards render partial (#quality)"`.

---

### Task 68: Technology partial (#technology — banner + cards)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleTechnology.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Technology`), `Vehicle.TechBannerImage` (string?), `Vehicle.Cards` (`CardItem.Title/Text/ImagePath/SortOrder`, NO link).
- Produces `<section class="mp-section mp-section--grey" id="technology">` with `.mp-tech-banner img`, `.mp-cards > .mp-card` (`.mp-card__media img`/`.mp-card__title`/`.mp-card__text`).

Steps:

- [ ] **Step 1: Create `_VehicleTechnology.cshtml`** (note `mp-section--grey` matches the master technology section):
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Technology);
      var cards = Model.Cards.OrderBy(c => c.SortOrder).ToList();
  }
  @if (cards.Count > 0 || !string.IsNullOrWhiteSpace(Model.TechBannerImage) || head != null)
  {
      <section class="mp-section mp-section--grey" id="technology">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          @if (!string.IsNullOrWhiteSpace(Model.TechBannerImage))
          {
              <div class="mp-tech-banner"><img src="@Model.TechBannerImage" alt="@Model.Name.Localize()" /></div>
          }
          @if (cards.Count > 0)
          {
              <div class="mp-cards">
                @foreach (var c in cards)
                {
                    <article class="mp-card">
                      @if (!string.IsNullOrWhiteSpace(c.ImagePath))
                      {
                          <div class="mp-card__media"><img src="@c.ImagePath" alt="@c.Title.Localize()" /></div>
                      }
                      @if (!string.IsNullOrWhiteSpace(c.Title.Localize()))
                      {
                          <h3 class="mp-card__title">@c.Title.Localize()</h3>
                      }
                      @if (!string.IsNullOrWhiteSpace(c.Text.Localize()))
                      {
                          <p class="mp-card__text">@c.Text.Localize()</p>
                      }
                    </article>
                }
              </div>
          }
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: technology render partial (#technology banner + cards)"`.

---

### Task 69: Safety toggles partial (#safety)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleSafety.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Safety`), `Vehicle.SafetyToggles` (`SafetyToggle.Title/ImagePath/Strap/Content/SortOrder`).
- Produces `<section class="mp-section mp-section--grey" id="safety">` with `.mp-stoggles > article.mp-stoggle` (first `is-open` + `aria-expanded="true"`, rest closed/`false`), `.mp-stoggle__head button > span`, `.mp-stoggle__body > .mp-stoggle__media img`, `.mp-stoggle__strap`, `.mp-stoggle__content`.

Steps:

- [ ] **Step 1: Create `_VehicleSafety.cshtml`:**
  ```cshtml
  @using System.Linq
  @using GAC.Web.Infrastructure
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Safety);
      var toggles = Model.SafetyToggles.OrderBy(t => t.SortOrder).ToList();
  }
  @if (toggles.Count > 0)
  {
      <section class="mp-section mp-section--grey" id="safety">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          <div class="mp-stoggles">
            @for (var i = 0; i < toggles.Count; i++)
            {
                var t = toggles[i];
                <article class="mp-stoggle@(VehicleContent.StateOpen(i == 0))">
                  <button class="mp-stoggle__head" type="button" aria-expanded="@VehicleContent.AriaExpanded(i == 0)"><span>@t.Title.Localize()</span><i class="mp-stoggle__icon"></i></button>
                  <div class="mp-stoggle__body">
                    @if (!string.IsNullOrWhiteSpace(t.ImagePath))
                    {
                        <div class="mp-stoggle__media"><img src="@t.ImagePath" alt="@t.Title.Localize()" loading="lazy" /></div>
                    }
                    @if (!string.IsNullOrWhiteSpace(t.Strap.Localize()))
                    {
                        <h3 class="mp-stoggle__strap">@t.Strap.Localize()</h3>
                    }
                    @if (!string.IsNullOrWhiteSpace(t.Content.Localize()))
                    {
                        <p class="mp-stoggle__content">@t.Content.Localize()</p>
                    }
                  </div>
                </article>
            }
          </div>
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: safety toggles render partial (#safety)"`.

---

### Task 70: Trims partial rewrite (#trims — model/name/price rows/CTAs)

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleTrims.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Trims`), `Vehicle.Trims` (each `Trim.ModelLabel/Name/ImagePath/SpecPdf/PriceRows`), each `TrimPriceRow.Text/SortOrder`.
- Produces `<section class="mp-section" id="trims">` with `.mp-head`, `.mp-trims > article.mp-trim` (`.mp-trim__media img`, `.mp-trim__body > .mp-trim__model`, `.mp-trim__name`, `ul.mp-trim__price > li`, `.mp-trim__cta` with the static "#enquiry" CTA first then the Specifications PDF CTA second).

Steps:

- [ ] **Step 1: Replace the entire content of `_VehicleTrims.cshtml`** (the old version used `Trim.Price`/`Highlights`; the new render uses `ModelLabel`, `ImagePath`, `PriceRows`):
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Trims);
      var trims = Model.Trims.OrderBy(t => t.SortOrder).ToList();
  }
  @if (trims.Count > 0)
  {
      <section class="mp-section" id="trims">
        <div class="container">
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          <div class="mp-trims">
            @foreach (var t in trims)
            {
                var rows = t.PriceRows.OrderBy(p => p.SortOrder).ToList();
                <article class="mp-trim">
                  @if (!string.IsNullOrWhiteSpace(t.ImagePath))
                  {
                      <div class="mp-trim__media"><img src="@t.ImagePath" alt="@t.Name.Localize()" /></div>
                  }
                  <div class="mp-trim__body">
                    @if (!string.IsNullOrWhiteSpace(t.ModelLabel.Localize()))
                    {
                        <p class="mp-trim__model">@t.ModelLabel.Localize()</p>
                    }
                    <h3 class="mp-trim__name">@t.Name.Localize()</h3>
                    @if (rows.Count > 0)
                    {
                        <ul class="mp-trim__price">
                          @foreach (var r in rows)
                          {
                              <li>@Html.Raw(r.Text.Localize())</li>
                          }
                        </ul>
                    }
                    <div class="mp-trim__cta">
                      <a class="btn btn--trim" href="#enquiry">@L["Book a Test Drive"]</a>
                      @if (!string.IsNullOrWhiteSpace(t.SpecPdf))
                      {
                          <a class="btn btn--trim" href="@t.SpecPdf" target="_blank" rel="noopener">@L["Specifications"]</a>
                      }
                    </div>
                  </div>
                </article>
            }
          </div>
        </div>
      </section>
  }
  ```
  `@Html.Raw(r.Text.Localize())` because price rows may carry the master's `<br>`/inline markup (cheatsheet: trim price rows innerHTML). The first CTA is the static `#enquiry` link, the second is the per-trim spec PDF (cheatsheet: 1st a is static #enquiry, 2nd a -> Trim.SpecPdf).
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: rewrite trims render partial for model/price-rows/CTAs (#trims)"`.

---

### Task 71: Warranty partial (#warranty — document links)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleWarranty.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.Headings` (key `SectionKey.Warranty`), `Vehicle.WarrantyLinks` (`WarrantyLink.Label/Url/SortOrder`).
- Produces `<section class="mp-section" id="warranty">` with `<hr class="mp-hr" />`, `.mp-head`, `.mp-warranty__links > a.btn.btn--doc`.

Steps:

- [ ] **Step 1: Create `_VehicleWarranty.cshtml`:**
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.Vehicle
  @{
      var head = Model.Headings.FirstOrDefault(h => h.Key == GAC.Core.Content.SectionKey.Warranty);
      var links = Model.WarrantyLinks.OrderBy(l => l.SortOrder).ToList();
  }
  @if (links.Count > 0 || head != null)
  {
      <section class="mp-section" id="warranty">
        <div class="container">
          <hr class="mp-hr" />
          <partial name="~/Views/Vehicles/_SectionHead.cshtml" model="head" />
          @if (links.Count > 0)
          {
              <div class="mp-warranty__links">
                @foreach (var l in links)
                {
                    <a class="btn btn--doc" href="@l.Url" target="_blank" rel="noopener">@l.Label.Localize()</a>
                }
              </div>
          }
        </div>
      </section>
  }
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: warranty render partial (#warranty document links)"`.

---

### Task 72: Enquiry partial (#enquiry — bg + title/sub/lead, static form via FormsController)

**Files:**
- Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/_VehicleEnquiry.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes `Vehicle.EnquiryBgImage` (string?), `Vehicle.EnquiryTitle/EnquirySub/EnquiryLead` (LocalizedText).
- Produces `<section class="mp-enquiry" id="enquiry">` with inline `background-image` style, `.mp-enquiry__overlay > .mp-enquiry__grid`, `.mp-enquiry__title/__sub/__lead`, the static `.mp-enquiry__actions` (call + find-location), and the static `.mp-form[data-form]` (the form fields stay static markup; submission is wired by the existing FormsController — DO NOT POST to a new endpoint here).

Steps:

- [ ] **Step 1: Create `_VehicleEnquiry.cshtml`** (mirrors `emkoo.html` lines 415-472; localizes the static labels via `L[...]`, fills the editable title/sub/lead/bg from the model). The inline bg uses `@($"background-image:url('{Model.EnquiryBgImage}')")` only when set:
  ```cshtml
  @model GAC.Core.Content.Vehicle
  <section class="mp-enquiry" id="enquiry"@(string.IsNullOrWhiteSpace(Model.EnquiryBgImage) ? "" : $" style=\"background-image:url('{Model.EnquiryBgImage}')\"")>
    <div class="mp-enquiry__overlay">
      <div class="container mp-enquiry__grid">
        <div class="mp-enquiry__intro">
          @if (!string.IsNullOrWhiteSpace(Model.EnquiryTitle.Localize()))
          {
              <h2 class="mp-enquiry__title">@Model.EnquiryTitle.Localize()</h2>
          }
          @if (!string.IsNullOrWhiteSpace(Model.EnquirySub.Localize()))
          {
              <p class="mp-enquiry__sub">@Model.EnquirySub.Localize()</p>
          }
          @if (!string.IsNullOrWhiteSpace(Model.EnquiryLead.Localize()))
          {
              <p class="mp-enquiry__lead">@Model.EnquiryLead.Localize()</p>
          }
          <div class="mp-enquiry__actions">
            <a class="mp-enquiry__action" href="tel:1833334">
              <svg viewBox="0 0 24 24"><path d="M6.6 10.8a15.5 15.5 0 0 0 6.6 6.6l2.2-2.2a1 1 0 0 1 1-.24 11.4 11.4 0 0 0 3.6.58 1 1 0 0 1 1 1V20a1 1 0 0 1-1 1A17 17 0 0 1 3 4a1 1 0 0 1 1-1h3.5a1 1 0 0 1 1 1 11.4 11.4 0 0 0 .58 3.6 1 1 0 0 1-.25 1z"/></svg>
              <span>@L["Call us"]</span>
            </a>
            <a class="mp-enquiry__action" href="/contact-us">
              <svg viewBox="0 0 24 24"><path d="M12 2a7 7 0 0 0-7 7c0 5.25 7 13 7 13s7-7.75 7-13a7 7 0 0 0-7-7zm0 9.5A2.5 2.5 0 1 1 12 6.5a2.5 2.5 0 0 1 0 5z"/></svg>
              <span>@L["Find a location"]</span>
            </a>
          </div>
        </div>

        <form class="mp-form" data-form novalidate>
          <div class="field">
            <label>@L["Message"]</label>
            <textarea rows="3"></textarea>
          </div>
          <div class="field">
            <label>@L["Select Branch"] *</label>
            <select required>
              <option value="">@L["Please select ..."]</option>
              <option>Riyadh Branch</option>
              <option>GAC Motors Jeddah, Malibari Sq Showroom</option>
              <option>GAC Motors Jeddah, Kilo 3 Branch</option>
              <option>Dammam Branch</option>
              <option>GAC Motors Al-Madinah Al-Munawarrah Branch</option>
              <option>GAC Motors Khamis Mushait Branch</option>
              <option>GAC Motors Jazan Branch</option>
            </select>
          </div>
          <div class="field">
            <label>@L["Title"] *</label>
            <select required>
              <option value="">@L["Please select ..."]</option>
              <option>@L["Mr"]</option><option>@L["Ms"]</option><option>@L["Mrs"]</option><option>@L["Miss"]</option>
            </select>
          </div>
          <div class="field"><label>@L["First Name"] *</label><input type="text" required /></div>
          <div class="field"><label>@L["Last Name"] *</label><input type="text" required /></div>
          <div class="field"><label>@L["Email Address"] *</label><input type="email" required /></div>
          <div class="field"><label>@L["Contact Number"] *</label><input type="tel" required /></div>
          <button class="mp-form__submit" type="submit">@L["Submit"]</button>
        </form>
      </div>
    </div>
  </section>
  ```
  NOTE for the assembler: the enquiry form here is a visual reproduction (no real POST target). If the active lead-capture form must persist, the existing `FormsController` POST path is the contract — reconcile with the forms workstream (cross-note below). This task ships the design markup only, matching the master HTML's static form.
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: enquiry render partial (#enquiry bg + title/sub/lead)"`.

---

### Task 73: Rewrite Detail.cshtml to always render the new master template (drop HasStructuredContent gate)

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/Detail.cshtml`

**Interfaces:**
- `@model GAC.Core.Content.Vehicle`.
- Consumes all section partials (Tasks 61-72) and `Vehicle.Sliders` (to interleave the two slider groups by `SortOrder`: first slider after stats, second slider — wrapped `id="interior"` — after design).
- Produces the full page in master section order: hero -> subnav -> overview/stats(#exterior) -> slider 1 (exterior) -> design(#design) -> slider 2 (#interior) -> gallery(#gallery) -> quality(#quality) -> technology(#technology) -> performance(#performance) -> safety(#safety) -> trims(#trims) -> warranty(#warranty) -> enquiry(#enquiry) -> lightbox singleton (once).

Steps:

- [ ] **Step 1: Replace the entire content of `Detail.cshtml`.** Remove the `HasStructuredContent` branch and the `BodyHtml` fallback entirely; vehicles ALWAYS render the new template. Use FULL `~/Views/Vehicles/` paths (PageController route value is "Page", so bare names fail). Interleave sliders by SortOrder — render the first slider plain, the second wrapped in `<section class="mp-slider-wrap" id="interior">`:
  ```cshtml
  @using System.Linq
  @model GAC.Core.Content.Vehicle
  @{ Layout = "_Layout"; }
  @{
      var sliders = Model.Sliders.OrderBy(s => s.SortOrder).ToList();
      var exteriorSlider = sliders.ElementAtOrDefault(0);
      var interiorSlider = sliders.ElementAtOrDefault(1);
  }

  <partial name="~/Views/Vehicles/_VehicleHero.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleSubnav.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleStats.cshtml" model="Model" />

  @if (exteriorSlider != null)
  {
      <section class="mp-slider-wrap">
        <partial name="~/Views/Vehicles/_VehicleSlider.cshtml" model="exteriorSlider" />
      </section>
  }

  <partial name="~/Views/Vehicles/_VehicleDesign.cshtml" model="Model" />

  @if (interiorSlider != null)
  {
      <section class="mp-slider-wrap" id="interior">
        <partial name="~/Views/Vehicles/_VehicleSlider.cshtml" model="interiorSlider" />
      </section>
  }

  <partial name="~/Views/Vehicles/_VehicleGallery.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleQuality.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleTechnology.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehiclePerformance.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleSafety.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleTrims.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleWarranty.cshtml" model="Model" />
  <partial name="~/Views/Vehicles/_VehicleEnquiry.cshtml" model="Model" />

  <partial name="~/Views/Vehicles/_Lightbox.cshtml" model="Model" />
  ```
- [ ] **Step 2: Build to confirm Razor compiles:** `dotnet build C:/Users/anas-/source/repos/GAC/Solution/GAC.Web`.
- [ ] **Step 3: Commit:** `git add -A && git commit -m "feat: render full master vehicle template, drop HasStructuredContent gate"`.

---

### Task 74: Integration test — migrated car renders 200 with all section markers

**Files:**
- Modify `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehiclePagesTests.cs`

**Interfaces:**
- Consumes `DevWebApplicationFactory` (defined at top of `HomePageSmokeTests.cs`; boots Development env against the real shared SQL DB).
- Produces an integration test asserting a known migrated car (`/emkoo`) returns 200 and contains every section's JS-hook marker.

Note: this test depends on the parser/migration workstream having populated `/emkoo`'s structured collections in the shared DB. If the assembler sequences render BEFORE the parser, gate this test's strict-marker assertions behind the data being present, or run it after the parser task. The render partials guard on `Count > 0`, so an un-migrated DB would render 200 but without markers.

Steps:

- [ ] **Step 1: Append a marker-coverage test to `VehiclePagesTests.cs`** (after the existing methods, inside the class):
  ```csharp
    [Fact]
    public async Task Emkoo_RendersAllSectionMarkers()
    {
        var res = await _factory.CreateClient().GetAsync("/emkoo");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();

        Assert.Contains("mp-hero", html);            // hero
        Assert.Contains("mp-subnav", html);          // jump nav
        Assert.Contains("id=\"exterior\"", html);    // overview/stats
        Assert.Contains("mp-stat__value", html);     // stats values
        Assert.Contains("data-slider", html);        // sliders
        Assert.Contains("id=\"design\"", html);      // design tabs
        Assert.Contains("data-tabs-wrap", html);     // tab contract
        Assert.Contains("data-tab-panel=\"d1\"", html);
        Assert.Contains("id=\"interior\"", html);    // 2nd slider wrap
        Assert.Contains("id=\"gallery\"", html);     // gallery
        Assert.Contains("mp-gshot", html);           // gallery shots
        Assert.Contains("data-lightbox", html);      // single lightbox
        Assert.Contains("id=\"technology\"", html);  // technology
        Assert.Contains("mp-card__title", html);     // tech cards
        Assert.Contains("id=\"performance\"", html); // performance tabs
        Assert.Contains("data-tab-panel=\"p1\"", html);
        Assert.Contains("id=\"safety\"", html);      // safety toggles
        Assert.Contains("mp-stoggle", html);
        Assert.Contains("id=\"trims\"", html);       // trims
        Assert.Contains("mp-trim__name", html);
        Assert.Contains("id=\"warranty\"", html);    // warranty
        Assert.Contains("id=\"enquiry\"", html);     // enquiry
    }
  ```
- [ ] **Step 2: Run the test; expect failure if the parser hasn't run** (markers absent) OR pass if `/emkoo` is already migrated: `dotnet test C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests --filter "FullyQualifiedName~VehiclePagesTests.Emkoo_RendersAllSectionMarkers"`. If the partials are correct but markers are missing, the cause is data (parser workstream) not render — the render partials are verified by Razor build + the existing `VehiclePages_Render200` test.
- [ ] **Step 3: Confirm the broad smoke test still passes** (no regression — page still 200 with `mp-hero`): `dotnet test C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests --filter "FullyQualifiedName~VehiclePagesTests"`.
- [ ] **Step 4: Run the full suite to confirm no regressions:** `dotnet test C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests`.
- [ ] **Step 5: Commit:** `git add -A && git commit -m "test: integration marker coverage for full master vehicle template"`.


---

## Phase 5 — Parser & migration

### Task 80: Add AngleSharp NuGet to GAC.Infrastructure

**Files:**
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/GAC.Infrastructure.csproj`

**Interfaces:**
- Produces: `AngleSharp` 1.1.2 assembly available to `GAC.Infrastructure` (and transitively `GAC.Tests`, which references it). Used by `BodyHtmlParser` (Task 82) and `VehicleContentMigrator` (Task 84).

**Steps:**

- [ ] **Step 1: Add the package reference.** Run from the Infrastructure project so the version is pinned compatible with net9 (AngleSharp 1.x targets netstandard2.0 / net6+; 1.1.2 is the chosen pin per SHARED CONTRACTS):
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure"
  dotnet add package AngleSharp --version 1.1.2 --no-restore
  ```
  Then verify the csproj `<ItemGroup>` of package references now reads exactly (add the line manually if `dotnet add` floated the version):
  ```xml
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="9.0.6" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.6">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.6" />
    <PackageReference Include="MailKit" Version="4.16.0" />
    <PackageReference Include="HtmlSanitizer" Version="9.0.892" />
    <PackageReference Include="AngleSharp" Version="1.1.2" />
  ```

- [ ] **Step 2: Restore & build to confirm the package resolves on net9.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet restore GAC.Infrastructure/GAC.Infrastructure.csproj
  dotnet build GAC.Infrastructure/GAC.Infrastructure.csproj -c Debug
  ```
  Expect: build succeeds, no `NU1605`/downgrade and no `NU1701` framework-mismatch warning for AngleSharp.

- [ ] **Step 3: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Infrastructure/GAC.Infrastructure.csproj
  git commit -m "feat: add AngleSharp 1.1.2 to GAC.Infrastructure for body-HTML parsing"
  ```

---

### Task 81: Add an embedded emkoo HTML fixture and a parser unit-test harness skeleton (RED)

This task creates the failing-test scaffold the parser is built against. The fixture is the **existing** seed body for emkoo, embedded into the test assembly so unit tests never touch the DB.

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Fixtures/emkoo.html` (copy of the seed body)
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/GAC.Tests.csproj` (embed the fixture)
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/BodyHtmlParserTests.cs`

**Interfaces:**
- Consumes: `GAC.Infrastructure.Content.BodyHtmlParser` (built in Task 82) — `IDocument ParseHtml(string html)` plus per-section methods. This test file is written first and fails to compile until Task 82 exists.
- Produces: `BodyHtmlParserTests.LoadFixture()` helper returning the emkoo HTML string from the embedded resource.

**Steps:**

- [ ] **Step 1: Copy the emkoo seed body into the test project as a fixture.**
  ```bash
  mkdir -p "C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Fixtures"
  cp "C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/SeedContent/vehicles/emkoo.html" \
     "C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Fixtures/emkoo.html"
  ```

- [ ] **Step 2: Embed the fixture in the test csproj.** Add this `<ItemGroup>` to `GAC.Tests.csproj` (next to the existing ones):
  ```xml
    <ItemGroup>
      <EmbeddedResource Include="Fixtures\emkoo.html" />
    </ItemGroup>
  ```

- [ ] **Step 3: Write the test harness + first failing assertion (won't compile yet — that's the RED state).** Create `BodyHtmlParserTests.cs`:
  ```csharp
  using System.Reflection;
  using AngleSharp.Dom;
  using GAC.Infrastructure.Content;
  using Xunit;

  namespace GAC.Tests;

  public class BodyHtmlParserTests
  {
      // Loads the embedded emkoo seed-body fixture (resource name = "GAC.Tests.Fixtures.emkoo.html").
      internal static string LoadFixture()
      {
          var asm = Assembly.GetExecutingAssembly();
          var name = asm.GetManifestResourceNames()
              .Single(n => n.EndsWith("Fixtures.emkoo.html", StringComparison.Ordinal));
          using var s = asm.GetManifestResourceStream(name)!;
          using var r = new StreamReader(s);
          return r.ReadToEnd();
      }

      private static IDocument Doc() => BodyHtmlParser.ParseHtml(LoadFixture());

      [Fact]
      public void Fixture_LoadsNonEmptyHtml()
      {
          var html = LoadFixture();
          Assert.Contains("mp-hero__title", html);
          Assert.Contains("data-tab-btn", html);
      }

      [Fact]
      public void ParseHtml_ReturnsDocument_WithExpectedRootSections()
      {
          var doc = Doc();
          var ids = doc.QuerySelectorAll("section.mp-section[id]")
                       .Select(e => e.Id).ToList();
          Assert.Contains("exterior", ids);
          Assert.Contains("design", ids);
          Assert.Contains("gallery", ids);
          Assert.Contains("performance", ids);
          Assert.Contains("trims", ids);
          Assert.Contains("warranty", ids);
      }
  }
  ```

- [ ] **Step 4: Run the tests and SEE THEM FAIL with a compile error.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~BodyHtmlParserTests"
  ```
  Expect: build error `CS0246: The type or namespace name 'BodyHtmlParser' could not be found` (and `GAC.Infrastructure.Content` missing). This confirms the RED state — the parser does not exist yet.

- [ ] **Step 5: Commit the failing harness.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Tests/GAC.Tests.csproj GAC.Tests/Fixtures/emkoo.html GAC.Tests/BodyHtmlParserTests.cs
  git commit -m "test: add emkoo body-HTML fixture + parser test harness (red)"
  ```

---

### Task 82: Implement the static BodyHtmlParser utility (one method per section group)

Each method takes an AngleSharp `IDocument` (the parsed `BodyHtml` for **one language**) and returns plain entity lists (no DB, no `VehicleId` set — the migrator fills FKs/SortOrder). All selectors come from the SELECTOR CHEATSHEET. `InnerHtml` (trimmed) is preserved for fields that can contain `<strong>/<br>/<a>`; `TextContent` (trimmed) for plain fields.

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Content/ParsedVehicleContent.cs`
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Content/BodyHtmlParser.cs`

**Interfaces:**
- Consumes: AngleSharp; `GAC.Core.Content.*` entities + `SectionKey`/`FeatureGroup` enums (built in the model task, numbered earlier in this milestone).
- Produces:
  ```csharp
  namespace GAC.Infrastructure.Content;
  public static class BodyHtmlParser
  {
      public static IDocument ParseHtml(string html);                       // null/empty -> empty doc
      public static (string? Img, string? Title, string? Sub) ParseHero(IDocument d);
      public static List<SectionHeading> ParseHeadings(IDocument d);        // keyed by section id
      public static List<StatItem> ParseStats(IDocument d);                 // 4
      public static string? ParseStatsNote(IDocument d);
      public static List<SliderGroup> ParseSliders(IDocument d);            // 2, with Slides
      public static List<FeatureSection> ParseFeatures(IDocument d);        // 6 (3 Design + 3 Performance), with Bullets
      public static List<GalleryTab> ParseGalleryTabs(IDocument d);         // 3 tabs, 15 images
      public static QualityBlock? ParseQuality(IDocument d);
      public static (string? Banner, List<CardItem> Cards) ParseTechnology(IDocument d); // 3 cards
      public static List<SafetyToggle> ParseSafety(IDocument d);            // 3
      public static List<Trim> ParseTrims(IDocument d);                     // 1+, with PriceRows
      public static List<WarrantyLink> ParseWarranty(IDocument d);
      public static (string? Bg, string? Title, string? Sub, string? Lead) ParseEnquiry(IDocument d);
  }
  ```
  `ParsedVehicleContent` is a DTO bundling all of the above for one language (used by the migrator, Task 84).

- [ ] **Step 1: Write the failing per-section tests (extend BodyHtmlParserTests).** Append to `GAC.Tests/BodyHtmlParserTests.cs`:
  ```csharp
      [Fact]
      public void ParseHero_ExtractsImageTitleSub()
      {
          var (img, title, sub) = BodyHtmlParser.ParseHero(Doc());
          Assert.False(string.IsNullOrWhiteSpace(img));
          Assert.False(string.IsNullOrWhiteSpace(title));
      }

      [Fact]
      public void ParseHeadings_KeysEightSections()
      {
          var heads = BodyHtmlParser.ParseHeadings(Doc());
          Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Overview);
          Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Design);
          Assert.Contains(heads, h => h.Key == GAC.Core.Content.SectionKey.Gallery);
          Assert.All(heads, h => Assert.False(string.IsNullOrWhiteSpace(h.Title.En)));
      }

      [Fact]
      public void ParseStats_ReturnsFour_WithLabelAndValue()
      {
          var stats = BodyHtmlParser.ParseStats(Doc());
          Assert.Equal(4, stats.Count);
          Assert.All(stats, s => Assert.False(string.IsNullOrWhiteSpace(s.Value.En)));
          Assert.False(string.IsNullOrWhiteSpace(BodyHtmlParser.ParseStatsNote(Doc())));
      }

      [Fact]
      public void ParseSliders_ReturnsTwoGroups_EachWithSlides()
      {
          var sliders = BodyHtmlParser.ParseSliders(Doc());
          Assert.Equal(2, sliders.Count);
          Assert.All(sliders, g => Assert.True(g.Slides.Count >= 2));
          Assert.All(sliders, g => Assert.False(string.IsNullOrWhiteSpace(g.Title.En)));
      }

      [Fact]
      public void ParseFeatures_ReturnsSix_ThreeDesignThreePerformance_WithBullets()
      {
          var feats = BodyHtmlParser.ParseFeatures(Doc());
          Assert.Equal(6, feats.Count);
          Assert.Equal(3, feats.Count(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design));
          Assert.Equal(3, feats.Count(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Performance));
          Assert.All(feats, f => Assert.False(string.IsNullOrWhiteSpace(f.Heading.En)));
          var first = feats.First(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design);
          Assert.True(first.Bullets.Count >= 4);
          Assert.False(string.IsNullOrWhiteSpace(first.Bullets[0].Label.En));
          Assert.False(string.IsNullOrWhiteSpace(first.Bullets[0].Text.En));
      }

      [Fact]
      public void ParseGalleryTabs_ReturnsThreeTabs_FifteenImagesTotal()
      {
          var tabs = BodyHtmlParser.ParseGalleryTabs(Doc());
          Assert.Equal(3, tabs.Count);
          Assert.Equal(15, tabs.Sum(t => t.Images.Count));
          Assert.All(tabs, t => Assert.False(string.IsNullOrWhiteSpace(t.Label.En)));
      }

      [Fact]
      public void ParseQuality_ExtractsImagesAndText()
      {
          var q = BodyHtmlParser.ParseQuality(Doc());
          Assert.NotNull(q);
          Assert.False(string.IsNullOrWhiteSpace(q!.Content.En));
      }

      [Fact]
      public void ParseTechnology_ReturnsBannerAndThreeCards()
      {
          var (banner, cards) = BodyHtmlParser.ParseTechnology(Doc());
          Assert.False(string.IsNullOrWhiteSpace(banner));
          Assert.Equal(3, cards.Count);
          Assert.All(cards, c => Assert.False(string.IsNullOrWhiteSpace(c.Title.En)));
      }

      [Fact]
      public void ParseSafety_ReturnsThreeToggles()
      {
          var s = BodyHtmlParser.ParseSafety(Doc());
          Assert.Equal(3, s.Count);
          Assert.All(s, t => Assert.False(string.IsNullOrWhiteSpace(t.Title.En)));
      }

      [Fact]
      public void ParseTrims_ReturnsAtLeastOne_WithModelNameAndPriceRows()
      {
          var trims = BodyHtmlParser.ParseTrims(Doc());
          Assert.True(trims.Count >= 1);
          var t = trims[0];
          Assert.False(string.IsNullOrWhiteSpace(t.ModelLabel.En));
          Assert.False(string.IsNullOrWhiteSpace(t.Name.En));
          Assert.True(t.PriceRows.Count >= 2);
          Assert.False(string.IsNullOrWhiteSpace(t.SpecPdf));   // 2nd CTA href
      }

      [Fact]
      public void ParseWarranty_ReturnsLinksWithUrls()
      {
          var links = BodyHtmlParser.ParseWarranty(Doc());
          Assert.True(links.Count >= 1);
          Assert.All(links, l => Assert.False(string.IsNullOrWhiteSpace(l.Url)));
      }

      [Fact]
      public void ParseEnquiry_ExtractsBgTitleSubLead()
      {
          var (bg, title, sub, lead) = BodyHtmlParser.ParseEnquiry(Doc());
          Assert.False(string.IsNullOrWhiteSpace(bg));
          Assert.False(string.IsNullOrWhiteSpace(title));
      }
  ```
  Run and confirm RED (compile error — `BodyHtmlParser` not found):
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~BodyHtmlParserTests"
  ```

- [ ] **Step 2: Create the per-language DTO `ParsedVehicleContent.cs`.**
  ```csharp
  using GAC.Core.Content;

  namespace GAC.Infrastructure.Content;

  /// <summary>Everything parsed out of ONE language of a vehicle's BodyHtml.
  /// Entities carry no VehicleId/SortOrder yet — the migrator assigns those.</summary>
  public sealed class ParsedVehicleContent
  {
      public string? HeroImage { get; set; }
      public string? HeroTitle { get; set; }
      public string? HeroSub { get; set; }

      public List<SectionHeading> Headings { get; set; } = new();
      public List<StatItem> Stats { get; set; } = new();
      public string? StatsNote { get; set; }
      public List<SliderGroup> Sliders { get; set; } = new();
      public List<FeatureSection> Features { get; set; } = new();
      public List<GalleryTab> GalleryTabs { get; set; } = new();
      public QualityBlock? Quality { get; set; }
      public string? TechBanner { get; set; }
      public List<CardItem> Cards { get; set; } = new();
      public List<SafetyToggle> Safety { get; set; } = new();
      public List<Trim> Trims { get; set; } = new();
      public List<WarrantyLink> Warranty { get; set; } = new();

      public string? EnquiryBg { get; set; }
      public string? EnquiryTitle { get; set; }
      public string? EnquirySub { get; set; }
      public string? EnquiryLead { get; set; }
  }
  ```

- [ ] **Step 3: Implement `BodyHtmlParser.cs`.** Write the actual code for every section (no placeholders). The section-id → `SectionKey` map and all selectors are from the cheatsheet.
  ```csharp
  using System.Text.RegularExpressions;
  using AngleSharp;
  using AngleSharp.Dom;
  using GAC.Core.Content;

  namespace GAC.Infrastructure.Content;

  /// <summary>Stateless parser: reads ONE language of a vehicle's BodyHtml (an AngleSharp
  /// IDocument) and returns plain entities. Re-used for EN and AR by passing each language's doc.
  /// Preserves InnerHtml for fields that may hold &lt;strong&gt;/&lt;br&gt;/&lt;a&gt;.</summary>
  public static class BodyHtmlParser
  {
      private static readonly IBrowsingContext Ctx =
          BrowsingContext.New(Configuration.Default);

      // section[id] -> SectionKey
      private static readonly Dictionary<string, SectionKey> SectionKeys = new()
      {
          ["exterior"]    = SectionKey.Overview,
          ["design"]      = SectionKey.Design,
          ["gallery"]     = SectionKey.Gallery,
          ["technology"]  = SectionKey.Technology,
          ["performance"] = SectionKey.Performance,
          ["safety"]      = SectionKey.Safety,
          ["trims"]       = SectionKey.Trims,
          ["warranty"]    = SectionKey.Warranty,
      };

      public static IDocument ParseHtml(string? html)
          => Ctx.OpenAsync(req => req.Content(html ?? string.Empty))
                .GetAwaiter().GetResult();

      // ---- helpers ----
      private static string? Txt(IElement? el) => el is null ? null : el.TextContent.Trim();
      private static string? Inner(IElement? el) => el is null ? null : el.InnerHtml.Trim();
      private static string? Attr(IElement? el, string name) => el?.GetAttribute(name)?.Trim();
      private static LocalizedText LtEn(string? en) => new() { En = en };

      // ---- 1. Hero ----
      public static (string? Img, string? Title, string? Sub) ParseHero(IDocument d)
      {
          var img = d.QuerySelector(".mp-hero__img");
          return (
              Attr(img, "src"),
              Txt(d.QuerySelector(".mp-hero__title")),
              Txt(d.QuerySelector(".mp-hero__sub")));
      }

      // ---- 2. Section headings ----
      public static List<SectionHeading> ParseHeadings(IDocument d)
      {
          var list = new List<SectionHeading>();
          foreach (var sec in d.QuerySelectorAll("section.mp-section[id]"))
          {
              if (!SectionKeys.TryGetValue(sec.Id ?? "", out var key)) continue;
              var head = sec.QuerySelector(".mp-head");
              if (head is null) continue;
              list.Add(new SectionHeading
              {
                  Key = key,
                  Title = LtEn(Txt(head.QuerySelector(".mp-head__title"))),
                  Sub   = LtEn(Inner(head.QuerySelector(".mp-head__sub"))),
                  Body  = LtEn(Inner(head.QuerySelector(".mp-head__body"))),
              });
          }
          return list;
      }

      // ---- 3. Stats + note ----
      public static List<StatItem> ParseStats(IDocument d)
      {
          var stats = new List<StatItem>();
          var i = 0;
          foreach (var st in d.QuerySelectorAll("section#exterior .mp-stat"))
          {
              stats.Add(new StatItem
              {
                  Label = LtEn(Txt(st.QuerySelector(".mp-stat__label"))),
                  Value = LtEn(Txt(st.QuerySelector(".mp-stat__value"))),
                  SortOrder = i++,
              });
          }
          return stats;
      }

      public static string? ParseStatsNote(IDocument d)
          => Txt(d.QuerySelector("section#exterior .mp-note")) ?? Txt(d.QuerySelector(".mp-note"));

      // ---- 4. Sliders ----
      public static List<SliderGroup> ParseSliders(IDocument d)
      {
          var groups = new List<SliderGroup>();
          var gi = 0;
          foreach (var sl in d.QuerySelectorAll(".mp-slider-wrap .mp-slider"))
          {
              var group = new SliderGroup
              {
                  Eyebrow = LtEn(Txt(sl.QuerySelector(".mp-slider__eyebrow"))),
                  Title   = LtEn(Txt(sl.QuerySelector(".mp-slider__title"))),
                  SortOrder = gi++,
              };
              var si = 0;
              foreach (var slide in sl.QuerySelectorAll(".mp-slide"))
              {
                  var img = slide.QuerySelector("img");
                  group.Slides.Add(new SliderSlide
                  {
                      ImagePath = Attr(img, "src"),
                      Alt = LtEn(Attr(img, "alt")),
                      SortOrder = si++,
                  });
              }
              groups.Add(group);
          }
          return groups;
      }

      // ---- 5/9. Feature tabs (Design d1..d3, Performance p1..p3) ----
      public static List<FeatureSection> ParseFeatures(IDocument d)
      {
          var result = new List<FeatureSection>();
          ParseFeatureGroup(d, "section#design", FeatureGroup.Design, result);
          ParseFeatureGroup(d, "section#performance", FeatureGroup.Performance, result);
          return result;
      }

      private static void ParseFeatureGroup(IDocument d, string sectionSel, FeatureGroup grp, List<FeatureSection> sink)
      {
          var section = d.QuerySelector(sectionSel);
          if (section is null) return;
          var order = 0;
          foreach (var btn in section.QuerySelectorAll(".mp-tabs .mp-tabs__btn[data-tab-btn]"))
          {
              var key = Attr(btn, "data-tab-btn");
              var panel = section.QuerySelector($".mp-feature[data-tab-panel=\"{key}\"]");
              if (panel is null) continue;
              var media = panel.QuerySelector(".mp-feature__media img");
              var bodyEl = panel.QuerySelector(".mp-feature__body");
              var feat = new FeatureSection
              {
                  GroupKey = grp,
                  TabLabel = LtEn(Txt(btn)),
                  ImagePath = Attr(media, "src"),
                  Heading = LtEn(Txt(panel.QuerySelector(".mp-feature__title"))),
                  Lead = LtEn(Inner(bodyEl?.QuerySelector("p"))),
                  SortOrder = order++,
              };
              var bi = 0;
              foreach (var li in panel.QuerySelectorAll(".mp-feature__list li"))
              {
                  var strong = li.QuerySelector("strong");
                  string label, text;
                  if (strong is not null)
                  {
                      label = strong.TextContent.Trim().TrimEnd(':').Trim();
                      // text = everything after the <strong> label
                      var full = li.InnerHtml;
                      var idx = full.IndexOf("</strong>", StringComparison.OrdinalIgnoreCase);
                      text = idx >= 0 ? full[(idx + "</strong>".Length)..].Trim() : li.TextContent.Trim();
                  }
                  else { label = ""; text = li.InnerHtml.Trim(); }
                  feat.Bullets.Add(new FeatureBullet
                  {
                      Label = LtEn(label),
                      Text = LtEn(text),
                      SortOrder = bi++,
                  });
              }
              sink.Add(feat);
          }
      }

      // ---- 6. Gallery tabs (gex/gin/gte) ----
      public static List<GalleryTab> ParseGalleryTabs(IDocument d)
      {
          var section = d.QuerySelector("section#gallery");
          var tabs = new List<GalleryTab>();
          if (section is null) return tabs;
          var ti = 0;
          foreach (var btn in section.QuerySelectorAll(".mp-tabs .mp-tabs__btn[data-tab-btn]"))
          {
              var key = Attr(btn, "data-tab-btn");
              var panel = section.QuerySelector($".mp-gpanel[data-tab-panel=\"{key}\"]");
              if (panel is null) continue;
              var tab = new GalleryTab { Label = LtEn(Txt(btn)), SortOrder = ti++ };
              var ii = 0;
              foreach (var a in panel.QuerySelectorAll(".mp-gshot[href]"))
              {
                  var img = a.QuerySelector("img");
                  tab.Images.Add(new GalleryImage
                  {
                      ImagePath = Attr(a, "href") ?? Attr(img, "src"),
                      Alt = LtEn(Attr(img, "alt")),
                      SortOrder = ii++,
                  });
              }
              tabs.Add(tab);
          }
          return tabs;
      }

      // ---- 7. Quality ----
      public static QualityBlock? ParseQuality(IDocument d)
      {
          var section = d.QuerySelector("section#quality");
          if (section is null) return null;
          return new QualityBlock
          {
              MainImage  = Attr(section.QuerySelector(".mp-quality__main img"), "src"),
              ThumbImage = Attr(section.QuerySelector(".mp-quality__thumb img"), "src"),
              Strapline  = LtEn(Inner(section.QuerySelector(".mp-quality__strapline"))),
              Content    = LtEn(Inner(section.QuerySelector(".mp-quality__content"))),
          };
      }

      // ---- 8. Technology (banner + cards) ----
      public static (string? Banner, List<CardItem> Cards) ParseTechnology(IDocument d)
      {
          var section = d.QuerySelector("section#technology");
          var cards = new List<CardItem>();
          if (section is null) return (null, cards);
          var banner = Attr(section.QuerySelector(".mp-tech-banner img"), "src");
          var ci = 0;
          foreach (var card in section.QuerySelectorAll(".mp-card"))
          {
              cards.Add(new CardItem
              {
                  ImagePath = Attr(card.QuerySelector(".mp-card__media img"), "src"),
                  Title = LtEn(Txt(card.QuerySelector(".mp-card__title"))),
                  Text  = LtEn(Inner(card.QuerySelector(".mp-card__text"))),
                  SortOrder = ci++,
              });
          }
          return (banner, cards);
      }

      // ---- 10. Safety toggles ----
      public static List<SafetyToggle> ParseSafety(IDocument d)
      {
          var section = d.QuerySelector("section#safety");
          var list = new List<SafetyToggle>();
          if (section is null) return list;
          var i = 0;
          foreach (var tog in section.QuerySelectorAll(".mp-stoggle"))
          {
              list.Add(new SafetyToggle
              {
                  Title = LtEn(Txt(tog.QuerySelector(".mp-stoggle__head span"))),
                  ImagePath = Attr(tog.QuerySelector(".mp-stoggle__media img"), "src"),
                  Strap = LtEn(Inner(tog.QuerySelector(".mp-stoggle__strap"))),
                  Content = LtEn(Inner(tog.QuerySelector(".mp-stoggle__content"))),
                  SortOrder = i++,
              });
          }
          return list;
      }

      // ---- 11. Trims ----
      public static List<Trim> ParseTrims(IDocument d)
      {
          var section = d.QuerySelector("section#trims");
          var list = new List<Trim>();
          if (section is null) return list;
          var ti = 0;
          foreach (var tr in section.QuerySelectorAll(".mp-trim"))
          {
              var trim = new Trim
              {
                  ImagePath = Attr(tr.QuerySelector(".mp-trim__media img"), "src"),
                  ModelLabel = LtEn(Txt(tr.QuerySelector(".mp-trim__model"))),
                  Name = LtEn(Txt(tr.QuerySelector(".mp-trim__name"))),
                  SortOrder = ti++,
              };
              var pi = 0;
              foreach (var li in tr.QuerySelectorAll(".mp-trim__price li"))
              {
                  trim.PriceRows.Add(new TrimPriceRow { Text = LtEn(Inner(li)), SortOrder = pi++ });
              }
              // 1st CTA is the static #enquiry button; 2nd a[href] is the spec PDF.
              var ctaLinks = tr.QuerySelectorAll(".mp-trim__cta a[href]").ToList();
              var pdf = ctaLinks.Select(a => Attr(a, "href"))
                                .FirstOrDefault(h => !string.IsNullOrWhiteSpace(h) && !h!.StartsWith("#"));
              trim.SpecPdf = pdf;
              list.Add(trim);
          }
          return list;
      }

      // ---- 12. Warranty ----
      public static List<WarrantyLink> ParseWarranty(IDocument d)
      {
          var list = new List<WarrantyLink>();
          var i = 0;
          foreach (var a in d.QuerySelectorAll("section#warranty .mp-warranty__links a.btn--doc"))
          {
              list.Add(new WarrantyLink
              {
                  Label = LtEn(Txt(a)),
                  Url = Attr(a, "href") ?? string.Empty,
                  SortOrder = i++,
              });
          }
          return list;
      }

      // ---- 13. Enquiry ----
      public static (string? Bg, string? Title, string? Sub, string? Lead) ParseEnquiry(IDocument d)
      {
          var section = d.QuerySelector(".mp-enquiry");
          if (section is null) return (null, null, null, null);
          string? bg = null;
          var style = Attr(section, "style");
          if (!string.IsNullOrEmpty(style))
          {
              var m = Regex.Match(style, @"url\(\s*['""]?([^'""\)]+)['""]?\s*\)");
              if (m.Success) bg = m.Groups[1].Value.Trim();
          }
          return (
              bg,
              Txt(section.QuerySelector(".mp-enquiry__title")),
              Txt(section.QuerySelector(".mp-enquiry__sub")),
              Txt(section.QuerySelector(".mp-enquiry__lead")));
      }

      /// <summary>Convenience: run every section parser over one language's doc.</summary>
      public static ParsedVehicleContent ParseAll(IDocument d)
      {
          var (hImg, hTitle, hSub) = ParseHero(d);
          var (banner, cards) = ParseTechnology(d);
          var (bg, eTitle, eSub, eLead) = ParseEnquiry(d);
          return new ParsedVehicleContent
          {
              HeroImage = hImg, HeroTitle = hTitle, HeroSub = hSub,
              Headings = ParseHeadings(d),
              Stats = ParseStats(d),
              StatsNote = ParseStatsNote(d),
              Sliders = ParseSliders(d),
              Features = ParseFeatures(d),
              GalleryTabs = ParseGalleryTabs(d),
              Quality = ParseQuality(d),
              TechBanner = banner,
              Cards = cards,
              Safety = ParseSafety(d),
              Trims = ParseTrims(d),
              Warranty = ParseWarranty(d),
              EnquiryBg = bg, EnquiryTitle = eTitle, EnquirySub = eSub, EnquiryLead = eLead,
          };
      }
  }
  ```

- [ ] **Step 4: Run the parser tests and confirm GREEN.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~BodyHtmlParserTests"
  ```
  Expect: all `BodyHtmlParserTests` pass (4 stats, 2 sliders, 6 features, 15 gallery over 3 tabs, 3 cards, 3 safety, ≥1 trim, headings keyed, enquiry/hero non-empty).

- [ ] **Step 5: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Infrastructure/Content/ParsedVehicleContent.cs GAC.Infrastructure/Content/BodyHtmlParser.cs GAC.Tests/BodyHtmlParserTests.cs
  git commit -m "feat: BodyHtmlParser extracts all 13 sections from vehicle BodyHtml via AngleSharp"
  ```

---

### Task 83: Arabic-language parse coverage (EN + AR over the same fixture)

The parser is language-agnostic, but the migrator runs it twice (once per language) and merges the AR values onto the EN-built entities by position. This task adds tests proving the parser works on an AR body (RTL text, `<strong>` bullets) and that EN/AR position-merge is sound, plus a tiny AR fixture so the suite doesn't depend on a real AR seed body (the seed `BodyHtml_Ar` is intentionally blank today).

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Fixtures/emkoo-ar.html` (small hand-made AR variant: same structure, ≥4 stats, 6 feature panels, AR text)
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/GAC.Tests.csproj` (embed it)
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/BodyHtmlParserTests.cs`

**Interfaces:**
- Consumes: `BodyHtmlParser` (Task 82).
- Produces: confirmation that AR docs parse with identical counts so the migrator's position-merge (Task 84) is safe.

- [ ] **Step 1: Create a structurally-identical AR fixture.** It must contain the same hooks (`section#exterior .mp-stat`×4, `section#design`/`#performance` with `data-tab-btn` d1..d3/p1..p3 and matching `.mp-feature[data-tab-panel]`, `.mp-feature__list li` with `<strong>`, `.mp-enquiry`). Mirror the EN emkoo fixture's structure but with Arabic text. Create `GAC.Tests/Fixtures/emkoo-ar.html`:
  ```html
  <main class="mp-detail" dir="rtl">
    <section class="mp-hero">
      <img class="mp-hero__img" src="/assets/img/emkoo/hero.jpg" alt="إمكو" />
      <h1 class="mp-hero__title">إمكو</h1>
      <p class="mp-hero__sub">دفع رباعي رياضي أنيق</p>
    </section>

    <section class="mp-section" id="exterior">
      <header class="mp-head mp-head--left"><h2 class="mp-head__title">التصميم الخارجي</h2><p class="mp-head__sub">أناقة لا تُضاهى</p></header>
      <div class="mp-stats">
        <div class="mp-stat"><span class="mp-stat__value">177 حصان</span><span class="mp-stat__label">القوة</span></div>
        <div class="mp-stat"><span class="mp-stat__value">270 نيوتن</span><span class="mp-stat__label">العزم</span></div>
        <div class="mp-stat"><span class="mp-stat__value">7 سرعات</span><span class="mp-stat__label">ناقل الحركة</span></div>
        <div class="mp-stat"><span class="mp-stat__value">5 مقاعد</span><span class="mp-stat__label">السعة</span></div>
      </div>
      <p class="mp-note">جميع الأسعار شاملة الضريبة.</p>
    </section>

    <div class="mp-slider-wrap">
      <div class="mp-slider" data-slider>
        <span class="mp-slider__eyebrow">المظهر</span><span class="mp-slider__title">تصميم خارجي</span>
        <div class="mp-slide"><img src="/assets/img/emkoo/s1.jpg" alt="صورة 1" /></div>
        <div class="mp-slide"><img src="/assets/img/emkoo/s2.jpg" alt="صورة 2" /></div>
      </div>
    </div>
    <div class="mp-slider-wrap">
      <div class="mp-slider" data-slider>
        <span class="mp-slider__eyebrow">الداخلية</span><span class="mp-slider__title">تصميم داخلي</span>
        <div class="mp-slide"><img src="/assets/img/emkoo/s3.jpg" alt="صورة 3" /></div>
        <div class="mp-slide"><img src="/assets/img/emkoo/s4.jpg" alt="صورة 4" /></div>
      </div>
    </div>

    <section class="mp-section" id="design">
      <header class="mp-head"><h2 class="mp-head__title">التصميم</h2></header>
      <div class="mp-tabs" data-tabs-wrap>
        <div class="mp-tabs__nav" data-tabs">
          <button class="mp-tabs__btn is-active" data-tab-btn="d1">الخارج</button>
          <button class="mp-tabs__btn" data-tab-btn="d2">الداخل</button>
          <button class="mp-tabs__btn" data-tab-btn="d3">التقنية</button>
        </div>
        <div class="mp-tabs__root" data-tab-root>
          <div class="mp-feature is-active" data-tab-panel="d1">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/d1.jpg" alt="خارجي" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">تفاصيل خارجية</h3><p>واجهة بتصميم عصري.</p>
              <ul class="mp-feature__list">
                <li><strong>مصابيح LED:</strong> رؤية واضحة للطريق.</li>
                <li><strong>عجلات 19 إنش:</strong> راحة على جميع الطرق.</li>
                <li><strong>سقف بانورامي:</strong> إضاءة طبيعية.</li>
                <li><strong>أضواء ترحيب:</strong> لمسة شخصية.</li>
              </ul>
            </div>
          </div>
          <div class="mp-feature" data-tab-panel="d2">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/d2.jpg" alt="داخلي" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">تصميم داخلي</h3><p>راحة أولاً.</p>
              <ul class="mp-feature__list">
                <li><strong>تكييف أوتوماتيكي:</strong> لجميع الركاب.</li>
                <li><strong>مقاعد كهربائية:</strong> تعديل سهل.</li>
                <li><strong>مقاعد مهواة:</strong> برودة في الرحلات.</li>
                <li><strong>إضاءة محيطية:</strong> ألوان حسب مزاجك.</li>
              </ul>
            </div>
          </div>
          <div class="mp-feature" data-tab-panel="d3">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/d3.jpg" alt="تقنية" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">تجربة تقنية</h3><p>اتصال ذكي.</p>
              <ul class="mp-feature__list">
                <li><strong>شاشة لمس:</strong> واجهة سهلة.</li>
                <li><strong>صوت محيطي:</strong> تجربة غامرة.</li>
                <li><strong>شحن لاسلكي:</strong> راحة إضافية.</li>
                <li><strong>تطبيق ذكي:</strong> تحكم عن بعد.</li>
              </ul>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="mp-section" id="performance">
      <header class="mp-head"><h2 class="mp-head__title">الأداء</h2></header>
      <div class="mp-tabs" data-tabs-wrap>
        <div class="mp-tabs__nav" data-tabs">
          <button class="mp-tabs__btn is-active" data-tab-btn="p1">المحرك</button>
          <button class="mp-tabs__btn" data-tab-btn="p2">الهيكل</button>
          <button class="mp-tabs__btn" data-tab-btn="p3">القيادة</button>
        </div>
        <div class="mp-tabs__root" data-tab-root>
          <div class="mp-feature is-active" data-tab-panel="p1">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/p1.jpg" alt="محرك" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">محرك تيربو</h3><p>قوة وكفاءة.</p>
              <ul class="mp-feature__list"><li><strong>تيربو:</strong> استجابة فورية.</li><li><strong>كفاءة وقود:</strong> اقتصادي.</li></ul>
            </div>
          </div>
          <div class="mp-feature" data-tab-panel="p2">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/p2.jpg" alt="هيكل" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">هيكل متين</h3><p>أمان عالٍ.</p>
              <ul class="mp-feature__list"><li><strong>فولاذ عالي القوة:</strong> صلابة.</li><li><strong>توزيع الوزن:</strong> ثبات.</li></ul>
            </div>
          </div>
          <div class="mp-feature" data-tab-panel="p3">
            <div class="mp-feature__media"><img src="/assets/img/emkoo/p3.jpg" alt="قيادة" /></div>
            <div class="mp-feature__body"><h3 class="mp-feature__title">تجربة قيادة</h3><p>سلاسة.</p>
              <ul class="mp-feature__list"><li><strong>تعليق رياضي:</strong> تحكم دقيق.</li><li><strong>أوضاع قيادة:</strong> تخصيص.</li></ul>
            </div>
          </div>
        </div>
      </div>
    </section>

    <section class="mp-enquiry" id="enquiry" style="background-image:url('/assets/img/emkoo/enquiry-bg.jpg')">
      <h2 class="mp-enquiry__title">تواصل معنا</h2>
      <p class="mp-enquiry__sub">اطلب عرض سعر</p>
      <p class="mp-enquiry__lead">سنتواصل خلال 24 ساعة عمل.</p>
    </section>
  </main>
  ```

- [ ] **Step 2: Embed it.** In `GAC.Tests.csproj`, extend the embed group to:
  ```xml
    <ItemGroup>
      <EmbeddedResource Include="Fixtures\emkoo.html" />
      <EmbeddedResource Include="Fixtures\emkoo-ar.html" />
    </ItemGroup>
  ```

- [ ] **Step 3: Add AR tests (these compile against the existing parser — should pass once the fixture exists).** Append to `BodyHtmlParserTests.cs`:
  ```csharp
      internal static string LoadArFixture()
      {
          var asm = Assembly.GetExecutingAssembly();
          var name = asm.GetManifestResourceNames()
              .Single(n => n.EndsWith("Fixtures.emkoo-ar.html", StringComparison.Ordinal));
          using var s = asm.GetManifestResourceStream(name)!;
          using var r = new StreamReader(s);
          return r.ReadToEnd();
      }

      private static IDocument ArDoc() => BodyHtmlParser.ParseHtml(LoadArFixture());

      [Fact]
      public void ParsesArabic_StatsAndFeatures_WithCounts()
      {
          var stats = BodyHtmlParser.ParseStats(ArDoc());
          Assert.Equal(4, stats.Count);
          Assert.Contains("القوة", stats[0].Label.En);   // En slot holds AR text for an AR doc

          var feats = BodyHtmlParser.ParseFeatures(ArDoc());
          Assert.Equal(6, feats.Count);
          Assert.Contains("تفاصيل", feats.First(f => f.GroupKey == GAC.Core.Content.FeatureGroup.Design).Heading.En);
          Assert.False(string.IsNullOrWhiteSpace(feats[0].Bullets[0].Text.En));
      }

      [Fact]
      public void EnAndAr_ProduceSamePositionalCounts_ForMerge()
      {
          var en = BodyHtmlParser.ParseAll(Doc());
          var ar = BodyHtmlParser.ParseAll(ArDoc());
          Assert.Equal(en.Stats.Count, ar.Stats.Count);
          Assert.Equal(en.Features.Count, ar.Features.Count);
          Assert.Equal(en.Sliders.Count, ar.Sliders.Count);
          // bullet counts line up per feature so positional EN/AR merge is safe
          for (var i = 0; i < en.Features.Count; i++)
              Assert.Equal(en.Features[i].Bullets.Count, ar.Features[i].Bullets.Count);
      }
  ```
  Note for the implementer: `ParseAll` returns the AR text in the `.En` slot because the parser is language-blind — the **migrator** (Task 84) takes the `.En` values from the AR doc and writes them to each entity's `_Ar` field.

- [ ] **Step 4: Run and confirm GREEN.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~BodyHtmlParserTests"
  ```

- [ ] **Step 5: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Tests/Fixtures/emkoo-ar.html GAC.Tests/GAC.Tests.csproj GAC.Tests/BodyHtmlParserTests.cs
  git commit -m "test: parser handles Arabic body + EN/AR positional counts match for merge"
  ```

---

### Task 84: VehicleContentMigrator backfill utility (idempotent, EN+AR merge, re-runnable per car)

A one-off Infrastructure utility (sibling of `ContentSeeder`; never called from startup default). Per vehicle: skip if the new structured collections are already populated (preserve admin edits); else parse `BodyHtml_En` and `BodyHtml_Ar`, build entities from EN, merge AR onto them positionally, set FKs + `VehicleId` + the new `Vehicle` scalar fields, attach, `SaveChanges()` once. Returns a per-run report.

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Infrastructure/Content/VehicleContentMigrator.cs`
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleContentMigratorTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `BodyHtmlParser`, `ParsedVehicleContent` (Task 82); `Vehicle` + the new collection navs/scalar fields (`TechBannerImage`, `StatsNote`, `EnquiryBgImage`, `EnquiryTitle/Sub/Lead`, `Headings`, `Stats`, `Sliders`, `GalleryTabs`, `Cards`, `SafetyToggles`, `WarrantyLinks`, `Quality`, `Features.Bullets`, `Trims.PriceRows`).
- Produces:
  ```csharp
  namespace GAC.Infrastructure.Content;
  public sealed record MigratorReport(int VehiclesScanned, int VehiclesMigrated, int VehiclesSkipped, List<string> Notes);
  public static class VehicleContentMigrator
  {
      public static Task<MigratorReport> BackfillAllAsync(ApplicationDbContext db, bool force = false, CancellationToken ct = default);
      public static Task<bool> BackfillVehicleAsync(ApplicationDbContext db, int vehicleId, bool force = false, CancellationToken ct = default);
  }
  ```

- [ ] **Step 1: Write failing migrator tests (in-memory DB seeded by ContentSeeder).** Create `VehicleContentMigratorTests.cs`:
  ```csharp
  using GAC.Core.Content;
  using GAC.Infrastructure.Content;
  using GAC.Infrastructure.Data;
  using Microsoft.EntityFrameworkCore;
  using Microsoft.Extensions.DependencyInjection;
  using Xunit;

  namespace GAC.Tests;

  public class VehicleContentMigratorTests
  {
      private static ApplicationDbContext NewDb(string name)
      {
          var sp = new ServiceCollection()
              .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name))
              .BuildServiceProvider();
          return sp.GetRequiredService<ApplicationDbContext>();
      }

      // Builds an emkoo vehicle whose BodyHtml.En is the real seed body (so parser has real input).
      private static async Task<(ApplicationDbContext Db, int Id)> SeedEmkooAsync(string name)
      {
          var db = NewDb(name);
          var bodyEn = BodyHtmlParserTests.LoadFixture();
          var bodyAr = BodyHtmlParserTests.LoadArFixture();
          var v = new Vehicle
          {
              Slug = "emkoo",
              Name = new LocalizedText { En = "EMKOO" },
              BodyHtml = new LocalizedText { En = bodyEn, Ar = bodyAr },
          };
          db.Vehicles.Add(v);
          await db.SaveChangesAsync();
          return (db, v.Id);
      }

      [Fact]
      public async Task Backfill_PopulatesAllCollections_WithExpectedCounts()
      {
          var (db, id) = await SeedEmkooAsync("mig-counts");
          var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, id);
          Assert.True(ok);

          Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
          Assert.Equal(2, await db.Set<SliderGroup>().CountAsync(g => g.VehicleId == id));
          Assert.Equal(6, await db.Set<FeatureSection>().CountAsync(f => f.VehicleId == id));
          Assert.Equal(3, await db.Set<GalleryTab>().CountAsync(t => t.VehicleId == id));
          Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
          Assert.Equal(3, await db.Set<CardItem>().CountAsync(c => c.VehicleId == id));
          Assert.Equal(3, await db.Set<SafetyToggle>().CountAsync(s => s.VehicleId == id));
          Assert.True(await db.Set<Trim>().CountAsync(t => t.VehicleId == id) >= 1);
          Assert.True(await db.Set<SectionHeading>().CountAsync(h => h.VehicleId == id) >= 6);
      }

      [Fact]
      public async Task Backfill_SetsVehicleScalarFields_AndArabic()
      {
          var (db, id) = await SeedEmkooAsync("mig-scalars");
          await VehicleContentMigrator.BackfillVehicleAsync(db, id);

          var v = await db.Vehicles
              .Include(x => x.Stats)
              .Include(x => x.Features).ThenInclude(f => f.Bullets)
              .FirstAsync(x => x.Id == id);

          Assert.False(string.IsNullOrWhiteSpace(v.TechBannerImage));
          Assert.False(string.IsNullOrWhiteSpace(v.EnquiryBgImage));
          Assert.False(string.IsNullOrWhiteSpace(v.EnquiryTitle.En));
          Assert.False(string.IsNullOrWhiteSpace(v.StatsNote.En));

          // AR merged from the AR body
          Assert.Contains("القوة", v.Stats.OrderBy(s => s.SortOrder).First().Label.Ar);
          var firstBullet = v.Features.OrderBy(f => f.SortOrder).First().Bullets.OrderBy(b => b.SortOrder).First();
          Assert.False(string.IsNullOrWhiteSpace(firstBullet.Label.Ar));
      }

      [Fact]
      public async Task Backfill_IsIdempotent_RunningTwiceDoesNotDuplicate()
      {
          var (db, id) = await SeedEmkooAsync("mig-idem");
          await VehicleContentMigrator.BackfillVehicleAsync(db, id);
          await VehicleContentMigrator.BackfillVehicleAsync(db, id); // second run = skip
          Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
          Assert.Equal(6, await db.Set<FeatureSection>().CountAsync(f => f.VehicleId == id));
          Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
      }

      [Fact]
      public async Task Backfill_PreservesAdminEdits_WhenCollectionsAlreadyPresent()
      {
          var (db, id) = await SeedEmkooAsync("mig-preserve");
          await VehicleContentMigrator.BackfillVehicleAsync(db, id);

          // simulate an admin edit
          var stat = await db.Set<StatItem>().OrderBy(s => s.SortOrder).FirstAsync(s => s.VehicleId == id);
          stat.Value = new LocalizedText { En = "999 HP", Ar = "٩٩٩ حصان" };
          await db.SaveChangesAsync();

          await VehicleContentMigrator.BackfillVehicleAsync(db, id); // must skip, not clobber
          var after = await db.Set<StatItem>().OrderBy(s => s.SortOrder).FirstAsync(s => s.VehicleId == id);
          Assert.Equal("999 HP", after.Value.En);
          Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id));
      }

      [Fact]
      public async Task Backfill_Force_RebuildsCar()
      {
          var (db, id) = await SeedEmkooAsync("mig-force");
          await VehicleContentMigrator.BackfillVehicleAsync(db, id);
          var ok = await VehicleContentMigrator.BackfillVehicleAsync(db, id, force: true);
          Assert.True(ok);
          Assert.Equal(4, await db.Set<StatItem>().CountAsync(s => s.VehicleId == id)); // cleared + rebuilt, no dupes
          Assert.Equal(15, await db.Set<GalleryImage>().CountAsync());
      }

      [Fact]
      public async Task BackfillAll_SkipsVehiclesWithEmptyBody()
      {
          var db = NewDb("mig-empty");
          db.Vehicles.Add(new Vehicle { Slug = "aion-v", Name = new LocalizedText { En = "Aion V" }, BodyHtml = new LocalizedText() });
          await db.SaveChangesAsync();
          var report = await VehicleContentMigrator.BackfillAllAsync(db);
          Assert.Equal(1, report.VehiclesScanned);
          Assert.Equal(0, report.VehiclesMigrated);
      }
  ```
  Run — RED (compile error: `VehicleContentMigrator` not found):
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~VehicleContentMigratorTests"
  ```

- [ ] **Step 2: Implement `VehicleContentMigrator.cs`.**
  ```csharp
  using GAC.Core.Content;
  using GAC.Infrastructure.Data;
  using Microsoft.EntityFrameworkCore;

  namespace GAC.Infrastructure.Content;

  public sealed record MigratorReport(int VehiclesScanned, int VehiclesMigrated, int VehiclesSkipped, List<string> Notes);

  /// <summary>One-off backfill: turns each vehicle's BodyHtml (EN + AR) into the new structured
  /// collections. Idempotent — skips a car that already has structured rows unless force=true.
  /// Never wired into startup; invoked on demand (see admin action / dev hook task).</summary>
  public static class VehicleContentMigrator
  {
      public static async Task<MigratorReport> BackfillAllAsync(ApplicationDbContext db, bool force = false, CancellationToken ct = default)
      {
          var ids = await db.Vehicles.Select(v => v.Id).ToListAsync(ct);
          int migrated = 0, skipped = 0;
          var notes = new List<string>();
          foreach (var id in ids)
          {
              var did = await BackfillVehicleAsync(db, id, force, ct);
              if (did) { migrated++; notes.Add($"#{id}: migrated"); }
              else { skipped++; notes.Add($"#{id}: skipped"); }
          }
          return new MigratorReport(ids.Count, migrated, skipped, notes);
      }

      public static async Task<bool> BackfillVehicleAsync(ApplicationDbContext db, int vehicleId, bool force = false, CancellationToken ct = default)
      {
          var v = await LoadWithCollectionsAsync(db, vehicleId, ct);
          if (v is null) return false;

          var hasStructured =
              v.Stats.Count > 0 || v.Sliders.Count > 0 || v.GalleryTabs.Count > 0 ||
              v.Cards.Count > 0 || v.SafetyToggles.Count > 0 || v.Headings.Count > 0 ||
              v.Features.Any(f => f.GroupKey != default || f.Bullets.Count > 0);

          if (hasStructured && !force) return false;          // preserve admin edits
          if (force) ClearStructured(db, v);

          var en = v.BodyHtml?.En;
          if (string.IsNullOrWhiteSpace(en)) return false;    // nothing to parse (e.g. hidden cars)

          var parsedEn = BodyHtmlParser.ParseAll(BodyHtmlParser.ParseHtml(en));
          var parsedAr = string.IsNullOrWhiteSpace(v.BodyHtml?.Ar)
              ? null
              : BodyHtmlParser.ParseAll(BodyHtmlParser.ParseHtml(v.BodyHtml!.Ar));

          Apply(db, v, parsedEn, parsedAr);
          await db.SaveChangesAsync(ct);
          return true;
      }

      private static async Task<Vehicle?> LoadWithCollectionsAsync(ApplicationDbContext db, int id, CancellationToken ct)
          => await db.Vehicles
              .Include(v => v.Headings)
              .Include(v => v.Stats)
              .Include(v => v.Sliders).ThenInclude(s => s.Slides)
              .Include(v => v.Features).ThenInclude(f => f.Bullets)
              .Include(v => v.GalleryTabs).ThenInclude(t => t.Images)
              .Include(v => v.Cards)
              .Include(v => v.SafetyToggles)
              .Include(v => v.WarrantyLinks)
              .Include(v => v.Trims).ThenInclude(t => t.PriceRows)
              .Include(v => v.Quality)
              .FirstOrDefaultAsync(v => v.Id == id, ct);

      private static void ClearStructured(ApplicationDbContext db, Vehicle v)
      {
          db.Set<SectionHeading>().RemoveRange(v.Headings);
          db.Set<StatItem>().RemoveRange(v.Stats);
          db.Set<SliderSlide>().RemoveRange(v.Sliders.SelectMany(s => s.Slides));
          db.Set<SliderGroup>().RemoveRange(v.Sliders);
          db.Set<FeatureBullet>().RemoveRange(v.Features.SelectMany(f => f.Bullets));
          db.Set<FeatureSection>().RemoveRange(v.Features);
          db.Set<GalleryImage>().RemoveRange(v.GalleryTabs.SelectMany(t => t.Images));
          db.Set<GalleryTab>().RemoveRange(v.GalleryTabs);
          db.Set<CardItem>().RemoveRange(v.Cards);
          db.Set<SafetyToggle>().RemoveRange(v.SafetyToggles);
          db.Set<WarrantyLink>().RemoveRange(v.WarrantyLinks);
          db.Set<TrimPriceRow>().RemoveRange(v.Trims.SelectMany(t => t.PriceRows));
          db.Set<Trim>().RemoveRange(v.Trims);
          if (v.Quality is not null) db.Set<QualityBlock>().Remove(v.Quality);
      }

      // ---- EN/AR merge helpers (AR parser puts text in .En slot; copy to .Ar) ----
      private static LocalizedText Merge(LocalizedText en, LocalizedText? ar)
          => new() { En = en.En, Ar = ar?.En };
      private static string? Pick(string? enImg) => enImg;   // images come from EN body only

      private static void Apply(ApplicationDbContext db, Vehicle v, ParsedVehicleContent en, ParsedVehicleContent? ar)
      {
          // --- Vehicle scalar/localized fields ---
          v.TechBannerImage = en.TechBanner;
          v.EnquiryBgImage = en.EnquiryBg;
          v.StatsNote = new LocalizedText { En = en.StatsNote, Ar = ar?.StatsNote };
          v.EnquiryTitle = new LocalizedText { En = en.EnquiryTitle, Ar = ar?.EnquiryTitle };
          v.EnquirySub = new LocalizedText { En = en.EnquirySub, Ar = ar?.EnquirySub };
          v.EnquiryLead = new LocalizedText { En = en.EnquiryLead, Ar = ar?.EnquiryLead };

          // --- Headings (match AR by SectionKey) ---
          foreach (var h in en.Headings)
          {
              var a = ar?.Headings.FirstOrDefault(x => x.Key == h.Key);
              db.Set<SectionHeading>().Add(new SectionHeading
              {
                  VehicleId = v.Id, Key = h.Key,
                  Title = Merge(h.Title, a?.Title),
                  Sub = Merge(h.Sub, a?.Sub),
                  Body = Merge(h.Body, a?.Body),
              });
          }

          // --- Stats (positional) ---
          for (var i = 0; i < en.Stats.Count; i++)
          {
              var a = ar is not null && i < ar.Stats.Count ? ar.Stats[i] : null;
              db.Set<StatItem>().Add(new StatItem
              {
                  VehicleId = v.Id, SortOrder = i,
                  Label = Merge(en.Stats[i].Label, a?.Label),
                  Value = Merge(en.Stats[i].Value, a?.Value),
              });
          }

          // --- Sliders + slides (positional) ---
          for (var gi = 0; gi < en.Sliders.Count; gi++)
          {
              var eg = en.Sliders[gi];
              var ag = ar is not null && gi < ar.Sliders.Count ? ar.Sliders[gi] : null;
              var group = new SliderGroup
              {
                  VehicleId = v.Id, SortOrder = gi,
                  Eyebrow = Merge(eg.Eyebrow, ag?.Eyebrow),
                  Title = Merge(eg.Title, ag?.Title),
              };
              for (var si = 0; si < eg.Slides.Count; si++)
              {
                  var asl = ag is not null && si < ag.Slides.Count ? ag.Slides[si] : null;
                  group.Slides.Add(new SliderSlide
                  {
                      SortOrder = si,
                      ImagePath = eg.Slides[si].ImagePath,
                      Alt = Merge(eg.Slides[si].Alt, asl?.Alt),
                  });
              }
              db.Set<SliderGroup>().Add(group);
          }

          // --- Features + bullets (positional within whole list) ---
          for (var fi = 0; fi < en.Features.Count; fi++)
          {
              var ef = en.Features[fi];
              var af = ar is not null && fi < ar.Features.Count ? ar.Features[fi] : null;
              var feat = new FeatureSection
              {
                  VehicleId = v.Id, SortOrder = ef.SortOrder, GroupKey = ef.GroupKey,
                  ImagePath = ef.ImagePath,
                  TabLabel = Merge(ef.TabLabel, af?.TabLabel),
                  Heading = Merge(ef.Heading, af?.Heading),
                  Lead = Merge(ef.Lead, af?.Lead),
                  Body = new LocalizedText(),   // legacy field unused by new render
              };
              for (var bi = 0; bi < ef.Bullets.Count; bi++)
              {
                  var ab = af is not null && bi < af.Bullets.Count ? af.Bullets[bi] : null;
                  feat.Bullets.Add(new FeatureBullet
                  {
                      SortOrder = bi,
                      Label = Merge(ef.Bullets[bi].Label, ab?.Label),
                      Text = Merge(ef.Bullets[bi].Text, ab?.Text),
                  });
              }
              db.Set<FeatureSection>().Add(feat);
          }

          // --- Gallery tabs + images (positional) ---
          for (var ti = 0; ti < en.GalleryTabs.Count; ti++)
          {
              var et = en.GalleryTabs[ti];
              var at = ar is not null && ti < ar.GalleryTabs.Count ? ar.GalleryTabs[ti] : null;
              var tab = new GalleryTab { VehicleId = v.Id, SortOrder = ti, Label = Merge(et.Label, at?.Label) };
              for (var ii = 0; ii < et.Images.Count; ii++)
              {
                  var ai = at is not null && ii < at.Images.Count ? at.Images[ii] : null;
                  tab.Images.Add(new GalleryImage
                  {
                      SortOrder = ii,
                      ImagePath = et.Images[ii].ImagePath,
                      Alt = Merge(et.Images[ii].Alt, ai?.Alt),
                  });
              }
              db.Set<GalleryTab>().Add(tab);
          }

          // --- Quality ---
          if (en.Quality is not null)
          {
              db.Set<QualityBlock>().Add(new QualityBlock
              {
                  VehicleId = v.Id,
                  MainImage = en.Quality.MainImage, ThumbImage = en.Quality.ThumbImage,
                  Strapline = new LocalizedText { En = en.Quality.Strapline.En, Ar = ar?.Quality?.Strapline.En },
                  Content = new LocalizedText { En = en.Quality.Content.En, Ar = ar?.Quality?.Content.En },
              });
          }

          // --- Technology cards (positional) ---
          for (var ci = 0; ci < en.Cards.Count; ci++)
          {
              var a = ar is not null && ci < ar.Cards.Count ? ar.Cards[ci] : null;
              db.Set<CardItem>().Add(new CardItem
              {
                  VehicleId = v.Id, SortOrder = ci, ImagePath = en.Cards[ci].ImagePath,
                  Title = Merge(en.Cards[ci].Title, a?.Title),
                  Text = Merge(en.Cards[ci].Text, a?.Text),
              });
          }

          // --- Safety toggles (positional) ---
          for (var si = 0; si < en.Safety.Count; si++)
          {
              var a = ar is not null && si < ar.Safety.Count ? ar.Safety[si] : null;
              db.Set<SafetyToggle>().Add(new SafetyToggle
              {
                  VehicleId = v.Id, SortOrder = si, ImagePath = en.Safety[si].ImagePath,
                  Title = Merge(en.Safety[si].Title, a?.Title),
                  Strap = Merge(en.Safety[si].Strap, a?.Strap),
                  Content = Merge(en.Safety[si].Content, a?.Content),
              });
          }

          // --- Trims + price rows (positional) ---
          for (var ti = 0; ti < en.Trims.Count; ti++)
          {
              var et = en.Trims[ti];
              var at = ar is not null && ti < ar.Trims.Count ? ar.Trims[ti] : null;
              var trim = new Trim
              {
                  VehicleId = v.Id, SortOrder = ti, ImagePath = et.ImagePath, SpecPdf = et.SpecPdf,
                  ModelLabel = Merge(et.ModelLabel, at?.ModelLabel),
                  Name = Merge(et.Name, at?.Name),
                  Highlights = new LocalizedText(),   // legacy field unused by new render
              };
              for (var pi = 0; pi < et.PriceRows.Count; pi++)
              {
                  var ap = at is not null && pi < at.PriceRows.Count ? at.PriceRows[pi] : null;
                  trim.PriceRows.Add(new TrimPriceRow { SortOrder = pi, Text = Merge(et.PriceRows[pi].Text, ap?.Text) });
              }
              db.Set<Trim>().Add(trim);
          }

          // --- Warranty links (positional) ---
          for (var wi = 0; wi < en.Warranty.Count; wi++)
          {
              var a = ar is not null && wi < ar.Warranty.Count ? ar.Warranty[wi] : null;
              db.Set<WarrantyLink>().Add(new WarrantyLink
              {
                  VehicleId = v.Id, SortOrder = wi, Url = en.Warranty[wi].Url,
                  Label = Merge(en.Warranty[wi].Label, a?.Label),
              });
          }
      }
  }
  ```
  Note for the implementer: `_ = Pick;` is unnecessary — the `Pick` helper above is illustrative; remove it if the compiler warns about an unused method, or keep it `private static`. Images are taken from the EN body only (AR bodies reuse the same asset paths).

- [ ] **Step 3: Run the migrator tests and confirm GREEN.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~VehicleContentMigratorTests"
  ```
  Expect: counts (4/2/6/3/15/3/3, ≥1 trim, ≥6 headings), scalar fields set, AR merged, idempotent (run-twice no dupes), preserve-edit (skips), force (rebuild clean), empty-body skipped — all pass.

- [ ] **Step 4: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Infrastructure/Content/VehicleContentMigrator.cs GAC.Tests/VehicleContentMigratorTests.cs
  git commit -m "feat: VehicleContentMigrator backfills structured sections from BodyHtml (idempotent, EN+AR)"
  ```

---

### Task 85: Guarded SQL migration script (history-stamped) for the schema migration

The EF migration `AddVehicleRichSections` is generated in the model/EF task (numbered earlier in this milestone). This task adds the **hand-scoped, history-guarded** SQL the user applies to the shared prod DB (`83.229.86.221/GAC`) — never `dotnet ef database update` / full idempotent script (prod history-gap rule).

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql`

**Interfaces:**
- Consumes: the table/column DDL from the generated `AddVehicleRichSections` migration (read its `Up()` to mirror exact column names/types — `{Field}_En`/`{Field}_Ar` nvarchar(max), enums int, FKs int).
- Produces: a re-runnable script that creates every new table + adds the new `Vehicle` columns, then stamps `__EFMigrationsHistory`.

- [ ] **Step 1: Read the generated migration to copy exact DDL.** After the EF migration exists, open `GAC.Infrastructure/Migrations/*_AddVehicleRichSections.cs` and read its `Up()`. The script below mirrors the schema in the SHARED CONTRACTS; reconcile every column name/nullability with the generated `Up()` before applying.

- [ ] **Step 2: Write the guarded script.** Create `docs/migrations/2026-06-23-AddVehicleRichSections.sql`:
  ```sql
  -- Guarded migration: AddVehicleRichSections (2026-06-23)
  -- History-guarded: only runs if not already stamped. Mirror exact DDL from the
  -- generated GAC.Infrastructure/Migrations/*_AddVehicleRichSections.cs Up() before applying.
  SET XACT_ABORT ON;
  BEGIN TRAN;

  IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'__STAMP_AddVehicleRichSections')
  BEGIN
      -- New Vehicle columns
      IF COL_LENGTH('Vehicles','TechBannerImage') IS NULL ALTER TABLE [Vehicles] ADD [TechBannerImage] nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquiryBgImage')  IS NULL ALTER TABLE [Vehicles] ADD [EnquiryBgImage]  nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','StatsNote_En')    IS NULL ALTER TABLE [Vehicles] ADD [StatsNote_En]    nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','StatsNote_Ar')    IS NULL ALTER TABLE [Vehicles] ADD [StatsNote_Ar]    nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquiryTitle_En') IS NULL ALTER TABLE [Vehicles] ADD [EnquiryTitle_En] nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquiryTitle_Ar') IS NULL ALTER TABLE [Vehicles] ADD [EnquiryTitle_Ar] nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquirySub_En')   IS NULL ALTER TABLE [Vehicles] ADD [EnquirySub_En]   nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquirySub_Ar')   IS NULL ALTER TABLE [Vehicles] ADD [EnquirySub_Ar]   nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquiryLead_En')  IS NULL ALTER TABLE [Vehicles] ADD [EnquiryLead_En]  nvarchar(max) NULL;
      IF COL_LENGTH('Vehicles','EnquiryLead_Ar')  IS NULL ALTER TABLE [Vehicles] ADD [EnquiryLead_Ar]  nvarchar(max) NULL;

      -- New FeatureSection / Trim columns
      IF COL_LENGTH('FeatureSections','GroupKey')   IS NULL ALTER TABLE [FeatureSections] ADD [GroupKey] int NOT NULL DEFAULT 0;
      IF COL_LENGTH('FeatureSections','TabLabel_En') IS NULL ALTER TABLE [FeatureSections] ADD [TabLabel_En] nvarchar(max) NULL;
      IF COL_LENGTH('FeatureSections','TabLabel_Ar') IS NULL ALTER TABLE [FeatureSections] ADD [TabLabel_Ar] nvarchar(max) NULL;
      IF COL_LENGTH('FeatureSections','Lead_En')     IS NULL ALTER TABLE [FeatureSections] ADD [Lead_En] nvarchar(max) NULL;
      IF COL_LENGTH('FeatureSections','Lead_Ar')     IS NULL ALTER TABLE [FeatureSections] ADD [Lead_Ar] nvarchar(max) NULL;
      IF COL_LENGTH('Trims','ModelLabel_En') IS NULL ALTER TABLE [Trims] ADD [ModelLabel_En] nvarchar(max) NULL;
      IF COL_LENGTH('Trims','ModelLabel_Ar') IS NULL ALTER TABLE [Trims] ADD [ModelLabel_Ar] nvarchar(max) NULL;
      IF COL_LENGTH('Trims','ImagePath')     IS NULL ALTER TABLE [Trims] ADD [ImagePath] nvarchar(max) NULL;

      -- New tables
      IF OBJECT_ID('SectionHeadings','U') IS NULL
      CREATE TABLE [SectionHeadings] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [Key] int NOT NULL,
          [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
          [Sub_En] nvarchar(max) NULL, [Sub_Ar] nvarchar(max) NULL,
          [Body_En] nvarchar(max) NULL, [Body_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_SectionHeadings_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('StatItems','U') IS NULL
      CREATE TABLE [StatItems] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL,
          [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
          [Value_En] nvarchar(max) NULL, [Value_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_StatItems_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('SliderGroups','U') IS NULL
      CREATE TABLE [SliderGroups] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL,
          [Eyebrow_En] nvarchar(max) NULL, [Eyebrow_Ar] nvarchar(max) NULL,
          [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_SliderGroups_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('SliderSlides','U') IS NULL
      CREATE TABLE [SliderSlides] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [SliderGroupId] int NOT NULL, [SortOrder] int NOT NULL, [ImagePath] nvarchar(max) NULL,
          [Alt_En] nvarchar(max) NULL, [Alt_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_SliderSlides_SliderGroups] FOREIGN KEY ([SliderGroupId]) REFERENCES [SliderGroups]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('FeatureBullets','U') IS NULL
      CREATE TABLE [FeatureBullets] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [FeatureSectionId] int NOT NULL, [SortOrder] int NOT NULL,
          [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
          [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_FeatureBullets_FeatureSections] FOREIGN KEY ([FeatureSectionId]) REFERENCES [FeatureSections]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('GalleryTabs','U') IS NULL
      CREATE TABLE [GalleryTabs] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL,
          [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_GalleryTabs_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('GalleryImages','U') IS NULL
      CREATE TABLE [GalleryImages] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [GalleryTabId] int NOT NULL, [SortOrder] int NOT NULL, [ImagePath] nvarchar(max) NULL,
          [Alt_En] nvarchar(max) NULL, [Alt_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_GalleryImages_GalleryTabs] FOREIGN KEY ([GalleryTabId]) REFERENCES [GalleryTabs]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('QualityBlocks','U') IS NULL
      CREATE TABLE [QualityBlocks] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [MainImage] nvarchar(max) NULL, [ThumbImage] nvarchar(max) NULL,
          [Strapline_En] nvarchar(max) NULL, [Strapline_Ar] nvarchar(max) NULL,
          [Content_En] nvarchar(max) NULL, [Content_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_QualityBlocks_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('CardItems','U') IS NULL
      CREATE TABLE [CardItems] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL, [ImagePath] nvarchar(max) NULL,
          [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
          [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_CardItems_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('SafetyToggles','U') IS NULL
      CREATE TABLE [SafetyToggles] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL, [ImagePath] nvarchar(max) NULL,
          [Title_En] nvarchar(max) NULL, [Title_Ar] nvarchar(max) NULL,
          [Strap_En] nvarchar(max) NULL, [Strap_Ar] nvarchar(max) NULL,
          [Content_En] nvarchar(max) NULL, [Content_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_SafetyToggles_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('TrimPriceRows','U') IS NULL
      CREATE TABLE [TrimPriceRows] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [TrimId] int NOT NULL, [SortOrder] int NOT NULL,
          [Text_En] nvarchar(max) NULL, [Text_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_TrimPriceRows_Trims] FOREIGN KEY ([TrimId]) REFERENCES [Trims]([Id]) ON DELETE CASCADE);

      IF OBJECT_ID('WarrantyLinks','U') IS NULL
      CREATE TABLE [WarrantyLinks] (
          [Id] int IDENTITY(1,1) NOT NULL PRIMARY KEY,
          [VehicleId] int NOT NULL, [SortOrder] int NOT NULL, [Url] nvarchar(max) NOT NULL,
          [Label_En] nvarchar(max) NULL, [Label_Ar] nvarchar(max) NULL,
          CONSTRAINT [FK_WarrantyLinks_Vehicles] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles]([Id]) ON DELETE CASCADE);

      -- Stamp history with the REAL generated MigrationId (replace __STAMP_ placeholders below
      -- with the actual id from the generated migration filename, e.g. 20260623HHMMSS_AddVehicleRichSections).
      INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion])
      VALUES (N'__STAMP_AddVehicleRichSections', N'9.0.6');
  END
  COMMIT;
  ```
  Note for the implementer: replace both `__STAMP_AddVehicleRichSections` occurrences with the exact `MigrationId` from the generated migration filename (date-prefixed) so EF treats prod as up-to-date and won't replay. The guard `WHERE [MigrationId] = N'<real id>'` must match the stamped value.

- [ ] **Step 3: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add docs/migrations/2026-06-23-AddVehicleRichSections.sql
  git commit -m "feat: guarded history-stamped SQL for AddVehicleRichSections (prod-safe)"
  ```

---

### Task 86: One-off run mechanism — guarded admin-only POST action + dev hook doc

The migrator must be invokable on demand (per AC1 cutover) but never run on startup. This task wires a guarded admin endpoint (mirrors the existing `Areas/Admin` auth pattern, `AdminPolicies.AdminOnly`) plus a documented Program.cs dev hook for local backfills, and an integration test asserting the endpoint is auth-gated.

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Areas/Admin/Controllers/ContentMigrationController.cs`
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Program.cs` (documented, commented-out dev hook)
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Admin/ContentMigrationControllerTests.cs`

**Interfaces:**
- Consumes: `VehicleContentMigrator.BackfillAllAsync` / `BackfillVehicleAsync` (Task 84); `ApplicationDbContext`; `AdminPolicies.AdminOnly` (existing).
- Produces: `POST /Admin/ContentMigration/RunAll` and `POST /Admin/ContentMigration/RunOne` (anti-forgery, AdminOnly), each redirecting back with a TempData report.

- [ ] **Step 1: Write the failing auth test.** Create `GAC.Tests/Admin/ContentMigrationControllerTests.cs`. Mirror the existing admin auth-gate tests (`TestAuthHandler` X-Test-Role pattern used elsewhere in `GAC.Tests/Admin`):
  ```csharp
  using System.Net;
  using System.Net.Http;
  using Microsoft.AspNetCore.Mvc.Testing;
  using Xunit;

  namespace GAC.Tests.Admin;

  public class ContentMigrationControllerTests : IClassFixture<WebApplicationFactory<Program>>
  {
      private readonly WebApplicationFactory<Program> _factory;
      public ContentMigrationControllerTests(WebApplicationFactory<Program> factory) => _factory = factory;

      [Fact]
      public async Task RunAll_WithoutAuth_IsNotOk()
      {
          var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
          var res = await client.PostAsync("/Admin/ContentMigration/RunAll", new StringContent(""));
          // Unauthenticated admin POST -> redirect to login or 401/403, never 200.
          Assert.NotEqual(HttpStatusCode.OK, res.StatusCode);
      }
  }
  ```
  Run — RED (the route does not exist → likely a redirect/404, but the test asserts "not OK"; once the controller exists with `[Authorize]` it stays not-OK for anonymous, proving the gate). Confirm it currently behaves as expected:
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~ContentMigrationControllerTests"
  ```

- [ ] **Step 2: Implement the controller.** Mirror existing admin controllers (area, policy, anti-forgery). Create `ContentMigrationController.cs`:
  ```csharp
  using GAC.Infrastructure.Content;
  using GAC.Infrastructure.Data;
  using GAC.Web.Areas.Admin.Security;   // AdminPolicies (match the namespace used by existing admin controllers)
  using Microsoft.AspNetCore.Authorization;
  using Microsoft.AspNetCore.Mvc;

  namespace GAC.Web.Areas.Admin.Controllers;

  [Area("Admin")]
  [Authorize(Policy = AdminPolicies.AdminOnly)]
  [AutoValidateAntiforgeryToken]
  public class ContentMigrationController : Controller
  {
      private readonly ApplicationDbContext _db;
      public ContentMigrationController(ApplicationDbContext db) => _db = db;

      [HttpGet]
      public IActionResult Index() => View();

      [HttpPost]
      public async Task<IActionResult> RunAll(bool force = false, CancellationToken ct = default)
      {
          var report = await VehicleContentMigrator.BackfillAllAsync(_db, force, ct);
          TempData["MigrationReport"] =
              $"Scanned {report.VehiclesScanned}, migrated {report.VehiclesMigrated}, skipped {report.VehiclesSkipped}.";
          return RedirectToAction(nameof(Index));
      }

      [HttpPost]
      public async Task<IActionResult> RunOne(int vehicleId, bool force = false, CancellationToken ct = default)
      {
          var ok = await VehicleContentMigrator.BackfillVehicleAsync(_db, vehicleId, force, ct);
          TempData["MigrationReport"] = ok ? $"Vehicle #{vehicleId} migrated." : $"Vehicle #{vehicleId} skipped (already has content or empty body).";
          return RedirectToAction(nameof(Index));
      }
  }
  ```
  Note for the implementer: confirm the exact `AdminPolicies` namespace/class used by sibling admin controllers (e.g. `VehiclesController`) and match it — the SHARED CONTRACTS show `[Authorize(Policy = AdminPolicies.ContentEditor)]`; use `AdminPolicies.AdminOnly` here since the backfill is destructive-capable (`force`). Add a minimal `Areas/Admin/Views/ContentMigration/Index.cshtml` with two anti-forgery forms (RunAll, RunOne with a vehicleId input) and a `@TempData["MigrationReport"]` banner, following the existing admin view conventions.

- [ ] **Step 3: Add the documented dev hook to Program.cs (commented out, opt-in via env var).** After the existing seeding block (`await ContentSeeder.SeedAsync(...)`), add:
  ```csharp
  // ── One-off structured-content backfill (DEV ONLY; off by default). ──
  // Turns each vehicle's BodyHtml into the new structured sections. Idempotent: skips
  // cars that already have structured rows. Enable locally by setting GAC_RUN_BACKFILL=1
  // (or run it from /Admin/ContentMigration in the admin panel). Never enable on startup in prod.
  if (Environment.GetEnvironmentVariable("GAC_RUN_BACKFILL") == "1")
  {
      using var backfillScope = app.Services.CreateScope();
      var db = backfillScope.ServiceProvider.GetRequiredService<GAC.Infrastructure.Data.ApplicationDbContext>();
      var report = await GAC.Infrastructure.Content.VehicleContentMigrator.BackfillAllAsync(db);
      app.Logger.LogInformation("Vehicle content backfill: scanned {S}, migrated {M}, skipped {K}.",
          report.VehiclesScanned, report.VehiclesMigrated, report.VehiclesSkipped);
  }
  ```

- [ ] **Step 4: Run the auth test + full build, confirm GREEN.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet build GAC.Web/GAC.Web.csproj -c Debug
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~ContentMigrationControllerTests"
  ```
  Expect: build OK (Razor compiles), anonymous POST is not 200 (auth gate holds).

- [ ] **Step 5: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Web/Areas/Admin/Controllers/ContentMigrationController.cs GAC.Web/Areas/Admin/Views/ContentMigration/Index.cshtml GAC.Web/Program.cs GAC.Tests/Admin/ContentMigrationControllerTests.cs
  git commit -m "feat: guarded admin backfill endpoint + dev env-var hook for VehicleContentMigrator"
  ```

---

### Task 87: gn6 outlier tolerance + a tolerant-counts regression test

`gn6` is a manual clone with a slightly different shape (13 gallery shots, 8 stats, 6 cards per the cheatsheet). The parser already uses count-tolerant iteration (it loops whatever it finds), so this task proves the parser is tolerant (does not hard-require exactly 4 stats / 15 gallery) and documents the gn6 handling.

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/Fixtures/gn6-variant.html` (small fixture: 8 stats, 13 gallery over 3 tabs, 6 cards)
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/GAC.Tests.csproj` (embed it)
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/BodyHtmlParserTests.cs`

**Interfaces:**
- Consumes: `BodyHtmlParser` (Task 82).
- Produces: proof the parser yields 8/13/6 (not clamped to 4/15/3) so gn6 migrates without a code change.

- [ ] **Step 1: Create the gn6 variant fixture** (only the variant sections need differ — exterior with 8 stats, gallery with 13 shots across gex/gin/gte, technology with 6 cards). Create `GAC.Tests/Fixtures/gn6-variant.html`:
  ```html
  <main class="mp-detail">
    <section class="mp-section" id="exterior">
      <header class="mp-head"><h2 class="mp-head__title">Exterior</h2></header>
      <div class="mp-stats">
        <div class="mp-stat"><span class="mp-stat__value">v1</span><span class="mp-stat__label">l1</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v2</span><span class="mp-stat__label">l2</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v3</span><span class="mp-stat__label">l3</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v4</span><span class="mp-stat__label">l4</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v5</span><span class="mp-stat__label">l5</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v6</span><span class="mp-stat__label">l6</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v7</span><span class="mp-stat__label">l7</span></div>
        <div class="mp-stat"><span class="mp-stat__value">v8</span><span class="mp-stat__label">l8</span></div>
      </div>
    </section>
    <section class="mp-section" id="gallery">
      <header class="mp-head"><h2 class="mp-head__title">Gallery</h2></header>
      <div class="mp-tabs" data-tabs-wrap>
        <div class="mp-tabs__nav" data-tabs">
          <button class="mp-tabs__btn is-active" data-tab-btn="gex">Exterior</button>
          <button class="mp-tabs__btn" data-tab-btn="gin">Interior</button>
          <button class="mp-tabs__btn" data-tab-btn="gte">Tech</button>
        </div>
        <div class="mp-tabs__root" data-tab-root>
          <div class="mp-gpanel" data-tab-panel="gex">
            <div class="mp-gallery">
              <a class="mp-gshot" href="/g/1.jpg"><img src="/g/1.jpg" alt="1"/></a>
              <a class="mp-gshot" href="/g/2.jpg"><img src="/g/2.jpg" alt="2"/></a>
              <a class="mp-gshot" href="/g/3.jpg"><img src="/g/3.jpg" alt="3"/></a>
              <a class="mp-gshot" href="/g/4.jpg"><img src="/g/4.jpg" alt="4"/></a>
              <a class="mp-gshot" href="/g/5.jpg"><img src="/g/5.jpg" alt="5"/></a>
            </div>
          </div>
          <div class="mp-gpanel" data-tab-panel="gin">
            <div class="mp-gallery">
              <a class="mp-gshot" href="/g/6.jpg"><img src="/g/6.jpg" alt="6"/></a>
              <a class="mp-gshot" href="/g/7.jpg"><img src="/g/7.jpg" alt="7"/></a>
              <a class="mp-gshot" href="/g/8.jpg"><img src="/g/8.jpg" alt="8"/></a>
              <a class="mp-gshot" href="/g/9.jpg"><img src="/g/9.jpg" alt="9"/></a>
            </div>
          </div>
          <div class="mp-gpanel" data-tab-panel="gte">
            <div class="mp-gallery">
              <a class="mp-gshot" href="/g/10.jpg"><img src="/g/10.jpg" alt="10"/></a>
              <a class="mp-gshot" href="/g/11.jpg"><img src="/g/11.jpg" alt="11"/></a>
              <a class="mp-gshot" href="/g/12.jpg"><img src="/g/12.jpg" alt="12"/></a>
              <a class="mp-gshot" href="/g/13.jpg"><img src="/g/13.jpg" alt="13"/></a>
            </div>
          </div>
        </div>
      </div>
    </section>
    <section class="mp-section" id="technology">
      <header class="mp-head"><h2 class="mp-head__title">Technology</h2></header>
      <div class="mp-tech-banner"><img src="/tech/banner.jpg" alt="banner"/></div>
      <div class="mp-cards">
        <div class="mp-card"><div class="mp-card__media"><img src="/c/1.jpg" alt="c1"/></div><h3 class="mp-card__title">C1</h3><p class="mp-card__text">t1</p></div>
        <div class="mp-card"><div class="mp-card__media"><img src="/c/2.jpg" alt="c2"/></div><h3 class="mp-card__title">C2</h3><p class="mp-card__text">t2</p></div>
        <div class="mp-card"><div class="mp-card__media"><img src="/c/3.jpg" alt="c3"/></div><h3 class="mp-card__title">C3</h3><p class="mp-card__text">t3</p></div>
        <div class="mp-card"><div class="mp-card__media"><img src="/c/4.jpg" alt="c4"/></div><h3 class="mp-card__title">C4</h3><p class="mp-card__text">t4</p></div>
        <div class="mp-card"><div class="mp-card__media"><img src="/c/5.jpg" alt="c5"/></div><h3 class="mp-card__title">C5</h3><p class="mp-card__text">t5</p></div>
        <div class="mp-card"><div class="mp-card__media"><img src="/c/6.jpg" alt="c6"/></div><h3 class="mp-card__title">C6</h3><p class="mp-card__text">t6</p></div>
      </div>
    </section>
  </main>
  ```

- [ ] **Step 2: Embed it.** Extend the embed group in `GAC.Tests.csproj`:
  ```xml
    <ItemGroup>
      <EmbeddedResource Include="Fixtures\emkoo.html" />
      <EmbeddedResource Include="Fixtures\emkoo-ar.html" />
      <EmbeddedResource Include="Fixtures\gn6-variant.html" />
    </ItemGroup>
  ```

- [ ] **Step 3: Add the tolerance test.** Append to `BodyHtmlParserTests.cs`:
  ```csharp
      private static IDocument Gn6Doc()
      {
          var asm = Assembly.GetExecutingAssembly();
          var name = asm.GetManifestResourceNames().Single(n => n.EndsWith("Fixtures.gn6-variant.html", StringComparison.Ordinal));
          using var s = asm.GetManifestResourceStream(name)!;
          using var r = new StreamReader(s);
          return BodyHtmlParser.ParseHtml(r.ReadToEnd());
      }

      [Fact]
      public void Parser_IsCountTolerant_ForGn6Variant()
      {
          var d = Gn6Doc();
          Assert.Equal(8, BodyHtmlParser.ParseStats(d).Count);                       // not clamped to 4
          var tabs = BodyHtmlParser.ParseGalleryTabs(d);
          Assert.Equal(3, tabs.Count);
          Assert.Equal(13, tabs.Sum(t => t.Images.Count));                           // not clamped to 15
          var (banner, cards) = BodyHtmlParser.ParseTechnology(d);
          Assert.False(string.IsNullOrWhiteSpace(banner));
          Assert.Equal(6, cards.Count);                                              // not clamped to 3
      }
  ```

- [ ] **Step 4: Run and confirm GREEN.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~BodyHtmlParserTests"
  ```
  Expect: gn6 tolerance test passes (8 stats / 13 gallery / 6 cards), proving gn6 migrates with no parser change. If a real gn6 body later reveals a structural mismatch (e.g. a missing `data-tab-btn`), the operator re-runs `RunOne(force:true)` for gn6 after a quick fixture/selector tweak — no schema change needed.

- [ ] **Step 5: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add GAC.Tests/Fixtures/gn6-variant.html GAC.Tests/GAC.Tests.csproj GAC.Tests/BodyHtmlParserTests.cs
  git commit -m "test: parser is count-tolerant for the gn6 structural variant (8 stats/13 gallery/6 cards)"
  ```

---

### Task 88: Full-suite green + run-book doc for the one-off cutover

Final gate: run the entire test suite, then write a short operator run-book so the human applies the schema SQL, deploys, and triggers the backfill in the right order (build template → apply schema → backfill → verify → cut render path).

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/docs/migrations/2026-06-23-vehicle-rich-sections-RUNBOOK.md`

**Interfaces:**
- Consumes: everything above (Tasks 80-87) plus the model/EF/render tasks from the rest of this milestone.
- Produces: an ordered, copy-pasteable cutover procedure.

- [ ] **Step 1: Run the full suite (unit + integration) and confirm all green.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  dotnet test GAC.Tests/GAC.Tests.csproj
  ```
  Expect: the full suite passes (existing ~236+ tests plus the new parser/migrator/auth tests). If any vehicle integration test in `VehiclePagesTests`/`VehicleDetailRenderTests` is red, that belongs to the render/model tasks — note it for the assembler, do not fix here.

- [ ] **Step 2: Write the run-book.** Create `docs/migrations/2026-06-23-vehicle-rich-sections-RUNBOOK.md`:
  ```markdown
  # Vehicle Rich Sections — Cutover Run-book (2026-06-23)

  Order matters: build template → apply schema → backfill content → verify parity → cut render path.
  The shared GAC DB (83.229.86.221/GAC) does NOT auto-migrate. Never run `dotnet ef database update`
  on prod (history-gap rule) — use the guarded SQL.

  ## 1. Apply the schema (guarded, history-stamped)
  - Open `docs/migrations/2026-06-23-AddVehicleRichSections.sql`.
  - Replace both `__STAMP_AddVehicleRichSections` placeholders with the REAL MigrationId from
    `GAC.Infrastructure/Migrations/*_AddVehicleRichSections.cs` (the date-prefixed filename id).
  - Run it once against the GAC DB. Re-running is safe (every DDL is `IF NOT EXISTS`-guarded and the
    history row blocks re-entry).

  ## 2. Deploy the Web app
  - Deploy the build that contains the new entities, EF config, render partials, and the admin
    ContentMigration endpoint. (No app auto-migration — step 1 must already be done.)

  ## 3. Run the one-off content backfill
  - Option A (admin UI): sign in as Admin → `/Admin/ContentMigration` → **Run All**. Idempotent;
    skips any car that already has structured rows. Use **Run One** + a vehicle id (with force) to
    re-do a single car after a tweak.
  - Option B (local/dev): set `GAC_RUN_BACKFILL=1` and start the app once; the Program.cs hook runs
    `BackfillAllAsync` and logs scanned/migrated/skipped, then unset the variable.

  ## 4. Verify parity (AC1/AC3)
  - Open 2-3 cars in admin: every section populated EN + AR (stats, sliders, features+bullets,
    gallery, cards, safety, trims, warranty, enquiry).
  - Open the public pages: marker/section counts match the old body (4 stats, 2 sliders, 6 features,
    15 gallery over 3 tabs, 3 cards, 3 safety, ≥1 trim). The integration tests in the render task
    are the permanent regression guard.
  - gn6: confirm 8 stats / 13 gallery / 6 cards rendered. If a selector mismatch surfaces, fix the
    parser selector or do a quick manual admin pass, then `Run One` (force) for gn6.

  ## 5. Cut the render path (done in the render task)
  - `Detail.cshtml` always renders the new template; the `HasStructuredContent` all-or-nothing
    branch is removed. `BodyHtml` columns are KEPT as backup (never dropped) — re-parseable.

  ## Rollback
  - The new tables are additive; `BodyHtml` is untouched. To revert rendering, restore the old
    `Detail.cshtml` branch. To re-extract, `Run One` (force) per car.
  ```

- [ ] **Step 3: Commit.**
  ```bash
  cd "C:/Users/anas-/source/repos/GAC/Solution"
  git add docs/migrations/2026-06-23-vehicle-rich-sections-RUNBOOK.md
  git commit -m "feat: cutover run-book for vehicle rich-sections schema + content backfill"
  ```


---

## Phase 6 — Cutover & regression

### Task 95: Cut the render path over — vehicles ALWAYS render the new rich template

**Files:**
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/Detail.cshtml`
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Infrastructure/VehicleContent.cs`
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleContentTests.cs`
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleDetailRenderTests.cs`

**Interfaces:**
- Consumes: the render partials produced by the render-partials workstream — `~/Views/Vehicles/_VehicleHero.cshtml`, `_VehicleSectionHeadings.cshtml`, `_VehicleStats.cshtml`, `_VehicleSliders.cshtml`, `_VehicleDesignTabs.cshtml`, `_VehicleGallery.cshtml`, `_VehicleQuality.cshtml`, `_VehicleTechnology.cshtml`, `_VehiclePerformanceTabs.cshtml`, `_VehicleSafety.cshtml`, `_VehicleTrims.cshtml`, `_VehicleWarranty.cshtml`, `_VehicleEnquiry.cshtml`, `_VehicleLightbox.cshtml` (each `@model GAC.Core.Content.Vehicle`).
- Produces: a `Detail.cshtml` that unconditionally renders the new template for every vehicle (no `HasStructuredContent` gate, `BodyHtml` retained in data but not rendered). `VehicleContent.HasStructuredContent` is removed.

**Note on the admin escape hatch:** the dev-only raw-HTML escape hatch ALREADY EXISTS in `Areas/Admin/Views/Vehicles/Edit.cshtml` as the `<details><summary>Advanced — raw HTML body (legacy / escape hatch)</summary>` block (lines 25-28). Per the default decision it STAYS as-is (hidden behind `<details>`, not the primary editor). This task does NOT touch it — do not remove it, do not promote it.

- [ ] **Step 1: Write the failing cutover test (render path no longer gated).** Replace the whole body of `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleDetailRenderTests.cs` so it asserts a vehicle renders the rich markers WITHOUT depending on `HasStructuredContent` (which is being deleted). This file currently references `VehicleContent.HasStructuredContent`, so after the helper is removed it must compile against the new shape:

```csharp
using System.Net;
using GAC.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class VehicleDetailRenderTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehicleDetailRenderTests(DevWebApplicationFactory factory) => _factory = factory;

    // After cutover every visible vehicle is served by PageController (controller
    // route value "Page"), so Detail.cshtml must reference the rich partials with
    // FULL view paths (~/Views/Vehicles/_X.cshtml) or the location expander only
    // probes /Views/Page and /Views/Shared and the page 500s.
    [Fact]
    public async Task EveryVisibleVehicle_RendersRichTemplate_NotRawBody()
    {
        var slug = await FirstVisibleSlugAsync();
        Assert.False(string.IsNullOrEmpty(slug),
            "Expected at least one visible vehicle in the database.");

        var res = await _factory.CreateClient().GetAsync("/" + slug);

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("mp-hero", html);          // hero partial resolved
        Assert.Contains("mp-section", html);       // section wrap from the new template
    }

    private async Task<string?> FirstVisibleSlugAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var vehicles = scope.ServiceProvider.GetRequiredService<IVehicleService>();
        var first = (await vehicles.GetVisibleAsync()).FirstOrDefault();
        return first?.Slug;
    }
}
```

- [ ] **Step 2: Update the unit test to drop the deleted helper.** In `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleContentTests.cs`, DELETE the four `HasStructuredContent_*` tests (`HasStructuredContent_FalseWhenAllEmpty`, `HasStructuredContent_TrueWhenAnyFeature`, `HasStructuredContent_TrueWhenAnyTrimSpecOrColor`, and the `using GAC.Core.Content;`/`new Vehicle()` they rely on stay only if still referenced). Keep the `FeatureSection_DefaultLayout_IsImageLeft`, `FeatureLayoutCss_MapsEachVariant`, and `ShowsImage_FalseForTextOnly` tests untouched. The file becomes:

```csharp
using GAC.Core.Content;
using GAC.Web.Infrastructure;
using Xunit;

namespace GAC.Tests;

public class VehicleContentTests
{
    [Fact]
    public void FeatureSection_DefaultLayout_IsImageLeft()
    {
        Assert.Equal(FeatureLayout.ImageLeft, new FeatureSection().Layout);
    }

    [Theory]
    [InlineData(FeatureLayout.ImageLeft, "mp-feature")]
    [InlineData(FeatureLayout.ImageRight, "mp-feature mp-feature--reverse")]
    [InlineData(FeatureLayout.Banner, "mp-feature mp-feature--banner")]
    [InlineData(FeatureLayout.TextOnly, "mp-feature mp-feature--text")]
    public void FeatureLayoutCss_MapsEachVariant(FeatureLayout layout, string expected)
    {
        Assert.Equal(expected, VehicleContent.FeatureLayoutCss(layout));
    }

    [Fact]
    public void ShowsImage_FalseForTextOnly()
    {
        Assert.False(VehicleContent.ShowsImage(FeatureLayout.TextOnly));
        Assert.True(VehicleContent.ShowsImage(FeatureLayout.ImageLeft));
    }
}
```

- [ ] **Step 3: Run the two edited tests and watch them FAIL to compile.** Run from `C:/Users/anas-/source/repos/GAC`:

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehicleDetailRenderTests|FullyQualifiedName~VehicleContentTests"
```

Expect a BUILD error: `VehicleContent` no longer compiles cleanly only after Step 4, but right now `Detail.cshtml` still emits gated content and `VehicleContent.HasStructuredContent` still exists — the new `VehicleDetailRenderTests` should compile and FAIL only if the rich partials aren't wired yet (assertion `mp-section` missing). Record the failing assertion message.

- [ ] **Step 4: Remove the `HasStructuredContent` helper.** In `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Infrastructure/VehicleContent.cs` DELETE the `HasStructuredContent` method (the `<summary>` doc-comment and the method body), keeping `FeatureLayoutCss` and `ShowsImage`:

```csharp
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

/// <summary>Pure helpers for rendering the vehicle detail page.</summary>
public static class VehicleContent
{
    public static string FeatureLayoutCss(FeatureLayout layout) => layout switch
    {
        FeatureLayout.ImageRight => "mp-feature mp-feature--reverse",
        FeatureLayout.Banner => "mp-feature mp-feature--banner",
        FeatureLayout.TextOnly => "mp-feature mp-feature--text",
        _ => "mp-feature"
    };

    public static bool ShowsImage(FeatureLayout layout) => layout != FeatureLayout.TextOnly;
}
```

- [ ] **Step 5: Rewrite `Detail.cshtml` to always render the rich template.** Replace the entire `C:/Users/anas-/source/repos/GAC/Solution/GAC.Web/Views/Vehicles/Detail.cshtml` with the unconditional template below. DROP the `HasStructuredContent` branch and the `@Html.Raw(Model.BodyHtml.Localize())` fallback entirely. Render the lightbox singleton exactly ONCE. Use FULL view paths because PageController owns the route:

```cshtml
@model GAC.Core.Content.Vehicle
@{ Layout = "_Layout"; }

@* Every vehicle renders the developer-owned rich master template, driven entirely
   by the structured model. BodyHtml is retained in the DB as backup but is NEVER
   rendered (the all-or-nothing HasStructuredContent footgun has been removed).
   FULL view paths are required: this page is served by PageController (controller
   route value "Page"), so bare partial names would only probe /Views/Page and
   /Views/Shared and never find these partials under /Views/Vehicles. *@
<partial name="~/Views/Vehicles/_VehicleHero.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleSectionHeadings.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleStats.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleSliders.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleDesignTabs.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleGallery.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleQuality.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleTechnology.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehiclePerformanceTabs.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleSafety.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleTrims.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleWarranty.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleEnquiry.cshtml" model="Model" />
<partial name="~/Views/Vehicles/_VehicleLightbox.cshtml" model="Model" />
```

- [ ] **Step 6: Build and run the edited tests — watch them PASS.** From `C:/Users/anas-/source/repos/GAC`:

```
dotnet build Solution/GAC.sln -c Debug
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehicleDetailRenderTests|FullyQualifiedName~VehicleContentTests"
```

Both test classes must pass. Build must be 0 errors (Razor compiles at build time — any missing rich partial is a build error here, which confirms the render workstream is complete before cutover).

- [ ] **Step 7: Commit.**

```
git add Solution/GAC.Web/Views/Vehicles/Detail.cshtml Solution/GAC.Web/Infrastructure/VehicleContent.cs Solution/GAC.Tests/VehicleContentTests.cs Solution/GAC.Tests/VehicleDetailRenderTests.cs
git commit -m "feat: cut vehicle detail render over to the rich template; drop HasStructuredContent footgun"
```

---

### Task 96: Expand `VehiclePagesTests` to all 11 cars with the richer marker set

**Files:**
- Modify: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehiclePagesTests.cs`

**Interfaces:**
- Consumes: live render output for each slug `/gs4 /m8 /gs8traveller /hyptec-ht /emkoo /emzoom /empow /empow-sport /gn6 /gs8` (and `/gs8traveller`). Requires the `AddVehicleRichSections` migration applied to the shared dev DB AND the parser run for those cars (Task 99 / Task 100) so the markers are present.
- Produces: a permanent regression guard asserting every car renders 200 plus the rich marker set: `mp-hero`, `mp-section`, `mp-stat`, `mp-tabs`, `mp-gshot`, `data-lightbox`, `mp-stoggle`, `mp-trim`.

**Pre-req for this test to pass:** the shared dev DB must already have the new tables (run the guarded SQL from Task 99 against it before running these tests locally) and the parser must have populated the cars (Task 100). These integration tests boot `DevWebApplicationFactory` against the REAL shared SQL DB.

- [ ] **Step 1: Write the failing expanded test.** Replace the entire `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehiclePagesTests.cs` with all 11 slugs (adding `/gn6`) and a richer per-car marker assertion. Keep the existing hidden-vehicle 404 test:

```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class VehiclePagesTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehiclePagesTests(DevWebApplicationFactory factory) => _factory = factory;

    // All 11 production cars must render 200 with the rich master template markers
    // after the AddVehicleRichSections migration + parser backfill.
    [Theory]
    [InlineData("/gs4")]
    [InlineData("/m8")]
    [InlineData("/gs8")]
    [InlineData("/gs8traveller")]
    [InlineData("/hyptec-ht")]
    [InlineData("/emkoo")]
    [InlineData("/emzoom")]
    [InlineData("/empow")]
    [InlineData("/empow-sport")]
    [InlineData("/gn6")]
    public async Task VehiclePages_Render200_WithRichMarkers(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();

        Assert.Contains("mp-hero", html);        // hero
        Assert.Contains("mp-section", html);     // section wrap
        Assert.Contains("mp-stat", html);        // overview stats
        Assert.Contains("mp-tabs", html);        // design/perf/gallery tabs
        Assert.Contains("mp-gshot", html);       // gallery zoom images
        Assert.Contains("data-lightbox", html);  // the single lightbox per page
        Assert.Contains("mp-stoggle", html);     // safety toggles
        Assert.Contains("mp-trim", html);        // trims
    }

    [Theory]
    [InlineData("/aion-v")]
    [InlineData("/aion-es")]
    public async Task HiddenVehicles_Return404(string url)
    {
        var res = await _factory.CreateClient().GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // The lightbox is a singleton: exactly one [data-lightbox] container per page,
    // never one-per-gallery and never in _Layout.
    [Theory]
    [InlineData("/emkoo")]
    [InlineData("/gn6")]
    public async Task VehiclePage_RendersExactlyOneLightbox(string url)
    {
        var html = await (await _factory.CreateClient().GetAsync(url)).Content.ReadAsStringAsync();
        var count = System.Text.RegularExpressions.Regex.Matches(html, "data-lightbox").Count;
        Assert.Equal(1, count);
    }
}
```

- [ ] **Step 2: Run the expanded test and watch it FAIL.** From `C:/Users/anas-/source/repos/GAC`:

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehiclePagesTests"
```

Before the migration + parser have run against the shared dev DB, expect failures: `/gn6` may 404 (if not seeded yet) and the rich markers (`mp-stat`, `mp-stoggle`, etc.) will be missing from cars whose structured rows are empty. Record which markers/slugs fail — that drives the parser work in Task 100.

- [ ] **Step 3: Make it pass.** This test is the regression GATE; it goes green only after Task 99 (schema applied to the shared dev DB) and Task 100 (parser populated all 11 cars including gn6). Do NOT weaken the markers to make it pass early. Re-run:

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehiclePagesTests"
```

When all 11 cars render the full marker set and exactly one lightbox each, the test is green.

- [ ] **Step 4: Commit (after the migration + parser tasks are done so it lands green).**

```
git add Solution/GAC.Tests/VehiclePagesTests.cs
git commit -m "test: assert all 11 cars render the rich marker set (mp-stat/tabs/gshot/lightbox/stoggle/trim)"
```

---

### Task 97: Migration-row integration test — extracted structured content is non-empty for every car (AC1 guard)

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleRichSectionsParityTests.cs`

**Interfaces:**
- Consumes: `IVehicleService.GetBySlugAsync(string slug)` returning a fully `.Include`-d `Vehicle` (Headings, Stats, Sliders+Slides, GalleryTabs+Images, Cards, SafetyToggles, WarrantyLinks, Quality, Features+Bullets, Trims+PriceRows). Boots `DevWebApplicationFactory` against the shared dev DB.
- Produces: an enforced AC1 regression guard — every car's structured collections are non-empty after the parser backfill, so no car opens to a blank admin form.

- [ ] **Step 1: Write the failing parity test.** Create `C:/Users/anas-/source/repos/GAC/Solution/GAC.Tests/VehicleRichSectionsParityTests.cs`:

```csharp
using GAC.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

// AC1 data-continuity guard: after the one-off parser backfill, every car's
// structured collections are populated. Counts mirror the master HTML
// (4 stats / 2 sliders / 3 gallery tabs / 15 gallery images / 3 safety toggles /
// trims+price rows). gn6 is the structural outlier (8 stats / 13 gallery images /
// 6 cards) so its lower bounds are looser but still non-empty.
public class VehicleRichSectionsParityTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public VehicleRichSectionsParityTests(DevWebApplicationFactory factory) => _factory = factory;

    public static IEnumerable<object[]> StandardCars() => new[]
    {
        new object[] { "gs4" }, new object[] { "m8" }, new object[] { "gs8" },
        new object[] { "gs8traveller" }, new object[] { "hyptec-ht" },
        new object[] { "emkoo" }, new object[] { "emzoom" },
        new object[] { "empow" }, new object[] { "empow-sport" },
    };

    [Theory]
    [MemberData(nameof(StandardCars))]
    public async Task StandardCar_HasFullStructuredContent(string slug)
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IVehicleService>();
        var v = await svc.GetBySlugAsync(slug);
        Assert.NotNull(v);

        Assert.True(v!.Headings.Count >= 4, $"{slug}: headings={v.Headings.Count}");
        Assert.True(v.Stats.Count >= 4, $"{slug}: stats={v.Stats.Count}");
        Assert.True(v.Sliders.Count >= 2, $"{slug}: sliders={v.Sliders.Count}");
        Assert.True(v.GalleryTabs.Count >= 3, $"{slug}: galleryTabs={v.GalleryTabs.Count}");
        Assert.True(v.GalleryTabs.Sum(t => t.Images.Count) >= 10, $"{slug}: galleryImages={v.GalleryTabs.Sum(t => t.Images.Count)}");
        Assert.True(v.Cards.Count >= 3, $"{slug}: cards={v.Cards.Count}");
        Assert.True(v.SafetyToggles.Count >= 3, $"{slug}: safety={v.SafetyToggles.Count}");
        Assert.True(v.Trims.Count >= 1, $"{slug}: trims={v.Trims.Count}");
        Assert.True(v.WarrantyLinks.Count >= 1, $"{slug}: warranty={v.WarrantyLinks.Count}");
        Assert.True(v.Features.Count >= 3, $"{slug}: features={v.Features.Count}");
        Assert.False(string.IsNullOrWhiteSpace(v.EnquiryTitle.En), $"{slug}: enquiry title empty");
    }

    [Fact]
    public async Task Gn6_Outlier_HasNonEmptyStructuredContent()
    {
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IVehicleService>();
        var v = await svc.GetBySlugAsync("gn6");
        Assert.NotNull(v);

        Assert.True(v!.Stats.Count >= 4, $"gn6 stats={v.Stats.Count}");
        Assert.True(v.GalleryTabs.Sum(t => t.Images.Count) >= 10, $"gn6 gallery={v.GalleryTabs.Sum(t => t.Images.Count)}");
        Assert.True(v.Cards.Count >= 3, $"gn6 cards={v.Cards.Count}");
        Assert.True(v.SafetyToggles.Count >= 3, $"gn6 safety={v.SafetyToggles.Count}");
        Assert.True(v.Trims.Count >= 1, $"gn6 trims={v.Trims.Count}");
    }
}
```

- [ ] **Step 2: Run it and watch it FAIL.** From `C:/Users/anas-/source/repos/GAC`:

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehicleRichSectionsParityTests"
```

Before the parser backfill the collections are empty -> every `Assert.True(...Count >= N)` fails with the diagnostic message showing the actual count. Record the counts.

- [ ] **Step 3: Make it pass via Task 100 (parser backfill).** After the parser has populated all 11 cars against the shared dev DB, re-run; all assertions go green. If gn6 falls short, that is the cue for the gn6 tolerant pass in Task 100.

- [ ] **Step 4: Commit.**

```
git add Solution/GAC.Tests/VehicleRichSectionsParityTests.cs
git commit -m "test: enforce AC1 — every car's structured content non-empty after parser backfill"
```

---

### Task 98: Document the manual parity comparison (new render markers/images vs original BodyHtml)

**Files:**
- Create: `C:/Users/anas-/source/repos/GAC/Solution/docs/superpowers/qa/2026-06-23-vehicle-rich-render-parity-checklist.md`

**Interfaces:**
- Consumes: the original `Vehicle.BodyHtml_En` (backup, still in DB) and the new live render for 2-3 sample cars.
- Produces: a repeatable, human-runnable checklist proving AC3 (design parity) by comparing marker counts and image `src` sets between old body and new render. No code; documentation deliverable.

- [ ] **Step 1: Write the parity checklist file.** Create `C:/Users/anas-/source/repos/GAC/Solution/docs/superpowers/qa/2026-06-23-vehicle-rich-render-parity-checklist.md` with the exact content below. It tells the operator how to dump both HTMLs and diff the marker/image counts for `emkoo`, `gs4`, and `gn6` (the outlier):

```markdown
# Vehicle rich-render parity checklist (AC3)

Date: 2026-06-23
Goal: prove each migrated car renders visually identical to the original hand-authored
`Vehicle.BodyHtml`. Run for 3 sample cars: `emkoo` (canonical), `gs4` (standard), `gn6` (outlier).

## A. Capture both HTMLs per car

1. Original body (backup column, EN):
   - `sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -y 0 -Q "SET NOCOUNT ON; SELECT BodyHtml_En FROM Vehicles WHERE Slug='emkoo'" -o old-emkoo.html`
2. New live render (app running locally or against testing host):
   - `curl -s https://<host>/emkoo > new-emkoo.html`
3. Repeat for `gs4` and `gn6`.

## B. Marker count comparison (must match per car, except gn6 outlier)

For each marker, count occurrences in OLD vs NEW and record in the table.
Use `grep -o '<marker>' file | wc -l`.

| Marker        | old emkoo | new emkoo | old gs4 | new gs4 | old gn6 | new gn6 | Match? |
|---------------|-----------|-----------|---------|---------|---------|---------|--------|
| `mp-stat`     |     4     |           |    4    |         |    8    |         |        |
| `mp-slider`   |     2     |           |    2    |         |    2    |         |        |
| `mp-feature`  |     6     |           |    6    |         |    6    |         |        |
| `mp-gshot`    |    15     |           |   15    |         |   13    |         |        |
| `mp-card`     |     3     |           |    3    |         |    6    |         |        |
| `mp-stoggle`  |     3     |           |    3    |         |    3    |         |        |
| `mp-trim`     |   (n)     |           |  (n)    |         |  (n)    |         |        |
| `mp-head`     |     8     |           |    8    |         |    8    |         |        |
| `data-lightbox`|    1     |           |    1    |         |    1    |         |        |

NOTE: the NEW render emits exactly ONE `data-lightbox`; the OLD body had one too —
the new template must not multiply it.

## C. Image `src` set comparison

1. Extract image srcs from each file (order-independent set):
   - `grep -oE 'src="[^"]+"' old-emkoo.html | sort -u > old-emkoo.srcs`
   - `grep -oE 'src="[^"]+"' new-emkoo.html | sort -u > new-emkoo.srcs`
2. Diff: `diff old-emkoo.srcs new-emkoo.srcs`
3. PASS = no missing/extra image paths (hero + 15 gallery + tech banner + cards +
   trims + sliders + safety + quality all present). Record any diff lines.

## D. Eyeball

- Open `/emkoo`, `/gs4`, `/gn6` in EN and AR. Verify: hero, stats strip, both sliders
  cycle, design/performance/gallery tabs switch, gallery zoom opens the single lightbox
  and arrows page through, safety toggles expand (first open), trims show price rows,
  enquiry bg image + heading present. RTL mirrors correctly (sliders stay LTR per Phase 4).

## E. Sign-off

- [ ] emkoo markers + srcs + eyeball PASS
- [ ] gs4 markers + srcs + eyeball PASS
- [ ] gn6 markers (outlier counts) + srcs + eyeball PASS
- [ ] All three render in AR with fallback-to-EN where AR blank
```

- [ ] **Step 2: Verify the file reads cleanly (no template tokens left).** Open the file and confirm the table and commands are concrete (no `<TBD>`), then commit:

```
git add Solution/docs/superpowers/qa/2026-06-23-vehicle-rich-render-parity-checklist.md
git commit -m "docs: add AC3 render-vs-BodyHtml parity checklist for emkoo/gs4/gn6"
```

---

### Task 99: Apply the guarded schema migration to the shared DB (dev first, then a prod-ready runbook)

**Files:**
- Consume (already produced by the model/EF workstream): `C:/Users/anas-/source/repos/GAC/Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql` (history-guarded over `[__EFMigrationsHistory]` with the `AddVehicleRichSections` MigrationId + the `(MigrationId, '9.0.6')` stamp row).
- Create: `C:/Users/anas-/source/repos/GAC/Solution/docs/migrations/2026-06-23-AddVehicleRichSections-RUNBOOK.md`

**Interfaces:**
- Consumes: the guarded SQL file and the EF migration `AddVehicleRichSections`.
- Produces: the new tables present in the shared DB (dev now; prod at deploy time per the runbook), so the integration tests in Tasks 96/97 can pass and the app can be deployed.

CRITICAL prod rule (memory): NEVER `dotnet ef database update` or a full `--idempotent` script against the shared prod GAC DB — the prod `__EFMigrationsHistory` has gaps. Apply ONLY the hand-scoped guarded `2026-06-23-AddVehicleRichSections.sql`.

- [ ] **Step 1: Sanity-check the guarded SQL exists and is history-guarded.** Confirm the file opens with the `[__EFMigrationsHistory]` existence check, wraps each `CREATE TABLE`/`ALTER TABLE` in `IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'<ts>_AddVehicleRichSections')`, and ends with the `INSERT INTO [__EFMigrationsHistory] ([MigrationId],[ProductVersion]) VALUES (N'<ts>_AddVehicleRichSections', N'9.0.6')` stamp. (Mirror the shape of `2026-06-21-AddSpecPdfAndDock.schema.sql`.)

```
ls Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql
```

- [ ] **Step 2: Apply to the shared DEV DB.** This is the DB the integration tests boot against. Run the guarded script once:

```
sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw_from_appsettings.Development.json> -b -i Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql
```

Then verify the new tables and the history stamp exist:

```
sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -Q "SELECT name FROM sys.tables WHERE name IN ('SectionHeadings','StatItems','SliderGroups','SliderSlides','FeatureBullets','GalleryTabs','GalleryImages','QualityBlocks','CardItems','SafetyToggles','TrimPriceRows','WarrantyLinks') ORDER BY name; SELECT MigrationId FROM [__EFMigrationsHistory] WHERE MigrationId LIKE '%AddVehicleRichSections';"
```

Expect all 12 new tables + the stamp row. (Re-running the script must be a no-op — guard verification.)

- [ ] **Step 3: Write the prod runbook.** Create `C:/Users/anas-/source/repos/GAC/Solution/docs/migrations/2026-06-23-AddVehicleRichSections-RUNBOOK.md`:

```markdown
# Runbook — AddVehicleRichSections schema + parser backfill + cutover deploy

Shared DB: `83.229.86.221,1433` / `GAC`. Credentials live ONLY in gitignored
`appsettings.Development.json` — never in this doc.

## Order (MUST be schema -> parser -> verify -> deploy)

1. **Back up the body columns** (safety; BodyHtml is the only fallback):
   `sqlcmd ... -Q "SELECT Id, Slug, BodyHtml_En, BodyHtml_Ar INTO Vehicles_BodyHtml_bak_20260623 FROM Vehicles;"`

2. **Apply the guarded schema script (NOT dotnet ef / NOT idempotent):**
   `sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -b -i Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql`
   Verify: all 12 new tables exist + history stamp row present (query in Step 2 of Task 99).

3. **Run the one-off parser ONCE against prod** (Task 100 utility; backfill-only —
   only fills a car whose collections are empty, so it is safe + idempotent):
   run the dev-only migrator hook / admin-only action pointed at the prod connection
   string, OR run the `GAC.Tools` console entry if that is how Task 100 exposes it.
   It reads `BodyHtml_En`/`_Ar`, parses with AngleSharp, writes the structured rows.

4. **Verify parity in prod** before flipping traffic:
   - `sqlcmd ... -Q "SELECT v.Slug, (SELECT COUNT(*) FROM StatItems s WHERE s.VehicleId=v.Id) stats, (SELECT COUNT(*) FROM GalleryImages g JOIN GalleryTabs t ON g.GalleryTabId=t.Id WHERE t.VehicleId=v.Id) gallery, (SELECT COUNT(*) FROM SafetyToggles st WHERE st.VehicleId=v.Id) safety FROM Vehicles v WHERE v.IsVisible=1 ORDER BY v.Slug;"`
   - Every visible car must show stats>=4, gallery>=10, safety>=3 (gn6 outlier counts allowed).
   - Run the AC3 checklist (`2026-06-23-vehicle-rich-render-parity-checklist.md`) for emkoo/gs4/gn6 against the testing host BEFORE prod deploy.

5. **Deploy the Web app** (the new Detail.cshtml + render partials). The app does NOT
   auto-migrate; the schema is already in place from Step 2. Cloudflare is DYNAMIC (no cache).

6. **Smoke-test prod:** open all 11 car slugs, confirm 200 + rich markers + one lightbox each.

## Rollback
- If a car renders wrong: re-run the parser for that car only (it clears+rebuilds that
  car's rows), or restore its BodyHtml from `Vehicles_BodyHtml_bak_20260623`. The new
  template ignores BodyHtml, so to fully revert render: redeploy the previous Web build
  (the schema stays; new tables are additive and harmless).
```

- [ ] **Step 4: Commit the runbook.**

```
git add Solution/docs/migrations/2026-06-23-AddVehicleRichSections-RUNBOOK.md
git commit -m "docs: deploy runbook — apply guarded schema, run parser, verify parity, then deploy Web"
```

---

### Task 100: Run the one-off parser to backfill all 11 cars (incl. gn6 tolerant pass) against the shared dev DB

**Files:**
- Consume (already produced by the parser workstream): the AngleSharp parser utility in `GAC.Infrastructure` (e.g. `VehicleBodyParser` / the dev-only migrator hook) and `ContentSeeder` patterns.
- Modify (only if gn6 needs a tweak): the parser's gn6 handling in `GAC.Infrastructure`.

**Interfaces:**
- Consumes: `Vehicle.BodyHtml_En` / `BodyHtml_Ar` from the shared dev DB for all 11 cars.
- Produces: populated structured collections for every car (backfill-only: skips a car whose collections already have rows, preserving admin edits) — the precondition for Tasks 96/97 to go green.

- [ ] **Step 1: Confirm the schema is present (Task 99 Step 2 done).** The parser writes into the new tables; they must exist first.

```
sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -Q "SELECT COUNT(*) FROM sys.tables WHERE name='GalleryImages';"
```

- [ ] **Step 2: Run the parser ONCE against the shared dev DB.** Use whichever invocation the parser workstream exposed (dev-only `Program.cs` hook guarded by an env flag, or an admin-only action, or a `GAC.Tools` console). It must run in BACKFILL mode (populate only empty cars). Example with the dev hook flag:

```
ASPNETCORE_ENVIRONMENT=Development GAC_RUN_VEHICLE_PARSER=1 dotnet run --project Solution/GAC.Web
```

Watch the log for `parsed <slug>: stats=4 gallery=15 safety=3 ...` per car. It must NOT run inside `ContentSeeder`'s startup default (one-off only).

- [ ] **Step 3: Verify the 9 standard cars parsed fully.** Run the parity query:

```
sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -Q "SELECT v.Slug, (SELECT COUNT(*) FROM StatItems s WHERE s.VehicleId=v.Id) stats, (SELECT COUNT(*) FROM SafetyToggles st WHERE st.VehicleId=v.Id) safety, (SELECT COUNT(*) FROM GalleryImages g JOIN GalleryTabs t ON g.GalleryTabId=t.Id WHERE t.VehicleId=v.Id) gallery FROM Vehicles v ORDER BY v.Slug;"
```

Expect stats>=4, safety>=3, gallery>=10 for gs4/m8/gs8/gs8traveller/hyptec-ht/emkoo/emzoom/empow/empow-sport.

- [ ] **Step 4: Handle the gn6 outlier.** gn6 has 8 stats / 13 gshots / 6 cards. If the parity query shows gn6 with empty or short collections, the standard selectors under-matched. Apply the tolerant pass: re-run the parser for gn6 only (it clears+rebuilds that car) after the parser's gn6 branch is loosened (e.g. take ALL `.mp-stat` / `.mp-gshot` / `.mp-card` regardless of expected count). Re-verify gn6:

```
sqlcmd -S 83.229.86.221,1433 -d GAC -U sa -P <pw> -Q "SELECT (SELECT COUNT(*) FROM StatItems s WHERE s.VehicleId=v.Id) stats, (SELECT COUNT(*) FROM CardItems c WHERE c.VehicleId=v.Id) cards, (SELECT COUNT(*) FROM GalleryImages g JOIN GalleryTabs t ON g.GalleryTabId=t.Id WHERE t.VehicleId=v.Id) gallery FROM Vehicles v WHERE v.Slug='gn6';"
```

Expect gn6 stats>=4, cards>=3, gallery>=10 (non-empty; exact outlier counts acceptable).

- [ ] **Step 5: Run the parity + page tests — they go green now.** From `C:/Users/anas-/source/repos/GAC`:

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehicleRichSectionsParityTests|FullyQualifiedName~VehiclePagesTests"
```

All 11 cars + the AC1 parity assertions pass.

- [ ] **Step 6: Commit any gn6 parser tweak.** (If only data was backfilled with no code change, skip.)

```
git add Solution/GAC.Infrastructure
git commit -m "feat: tolerant gn6 parser pass — backfill outlier stats/cards/gallery"
```

---

### Task 101: Final full-suite green gate

**Files:**
- No source changes expected. Verification-only task gating the whole cutover.

**Interfaces:**
- Consumes: the entire solution after Tasks 95-100 (schema applied to shared dev DB, parser backfilled, render cut over, tests expanded).
- Produces: a clean build + a fully green test suite — the merge/deploy gate.

- [ ] **Step 1: Clean build, 0 errors.** Razor compiles at build time, so a missing partial or stale `@using` fails here. From `C:/Users/anas-/source/repos/GAC`:

```
dotnet build Solution/GAC.sln -c Debug
```

Require `Build succeeded` with 0 errors. Treat warnings as TODO but not blocking.

- [ ] **Step 2: Run the FULL suite green.** Integration tests boot `DevWebApplicationFactory` against the shared dev DB (which now has the schema + parsed content), so they must all pass:

```
dotnet test Solution/GAC.sln
```

Require `Failed: 0`. The total count must exceed the prior baseline (was 236+; this batch adds `VehiclePagesTests` gn6 + lightbox cases and `VehicleRichSectionsParityTests`, and removes the four `HasStructuredContent_*` unit tests). Record the final `Passed:` number.

- [ ] **Step 3: Spot-confirm the cutover invariants in the test output.** Grep the run summary (or re-run the focused filter) to confirm these specific guards passed: `VehiclePagesTests.VehiclePages_Render200_WithRichMarkers` (all 11 incl gn6), `VehiclePagesTests.VehiclePage_RendersExactlyOneLightbox`, `VehicleRichSectionsParityTests.*`, `VehicleDetailRenderTests.EveryVisibleVehicle_RendersRichTemplate_NotRawBody`. If any vehicle 404s or a marker is missing, STOP — the parser (Task 100) or schema (Task 99) is incomplete; do not proceed to deploy.

```
dotnet test Solution/GAC.sln --filter "FullyQualifiedName~VehiclePagesTests|FullyQualifiedName~VehicleRichSectionsParityTests|FullyQualifiedName~VehicleDetailRenderTests"
```

- [ ] **Step 4: Final commit / merge gate.** With the suite green and the AC3 parity checklist (Task 98) signed off for emkoo/gs4/gn6, the cutover is mergeable. Tag the gate:

```
git commit --allow-empty -m "test: full suite green — vehicle rich-render cutover complete (Failed: 0)"
```

Deploy proceeds per the Task 99 runbook (apply guarded SQL to prod -> run parser once against prod -> verify -> deploy Web app).
