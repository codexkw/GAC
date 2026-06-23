# Tasks 20–25 Implementation Report

## Overview

All 6 tasks implemented and committed as separate commits on branch
`worktree-agent-a31d18de2bd08b80d` (built on top of `feature/vehicle-content-management`
via fast-forward merge). Build: **0 errors**. Tests: **18/18 AdminVehicleServiceTests GREEN**
(9 pre-existing + 9 new). Full suite: **273/274** (1 pre-existing live-DB failure
`StructuredContentVehicle_Renders_HeroSection` — confirmed failing before these changes too).

---

## Task 20: GetAsync eager-load + UpdateAsync scalar/localized save

**Commit:** `b50af60`

**Files changed:**
- `GAC.Infrastructure/Services/AdminVehicleService.cs` — GetAsync + UpdateAsync
- `GAC.Tests/Admin/AdminVehicleServiceTests.cs` — new test

**Methods updated:**
- `GetAsync`: added `.Include(v => v.Features).ThenInclude(f => f.Bullets)`, `.Include(v => v.Trims).ThenInclude(t => t.PriceRows)`, `.Include(v => v.Headings)`, `.Include(v => v.Stats)`, `.Include(v => v.Sliders).ThenInclude(s => s.Slides)`, `.Include(v => v.GalleryTabs).ThenInclude(g => g.Images)`, `.Include(v => v.Cards)`, `.Include(v => v.SafetyToggles)`, `.Include(v => v.WarrantyLinks)`, `.Include(v => v.Quality)`
- `UpdateAsync`: added `TechBannerImage`, `EnquiryBgImage`, `StatsNote`, `EnquiryTitle`, `EnquirySub`, `EnquiryLead`

**Tests:**
- `UpdateAsync_PersistsEnquiryAndTechFields` — RED (compile error before task-21+ methods existed) → GREEN
- `GetAsync_Includes_NewCollections_AndQuality` — deferred to Task 31 regression sweep; requires `AddCardAsync`/`AddSafetyToggleAsync`/`AddWarrantyLinkAsync`/`UpsertQualityAsync` which arrive in Tasks 26–31

---

## Task 21: UpsertSectionHeading — service + controller

**Commit:** `c43968b`

**Files changed:** all 4

**Interface additions:**
```csharp
Task<int> UpsertSectionHeadingAsync(int vehicleId, SectionKey key, LocalizedText title, LocalizedText sub, LocalizedText body, CancellationToken ct = default);
```

**Service:** finds by `(VehicleId, Key)` composite; inserts on first call, updates in-place on subsequent calls. Returns same `Id` on update.

**Controller action:** `UpsertSectionHeading` (POST).

**Tests:**
- `UpsertSectionHeading_InsertsThenUpdatesInPlace` — RED → GREEN
  - Verifies insert creates 1 row, second upsert returns same Id and updates Title/Body, different key creates 2nd row

---

## Task 22: StatItem add/remove/move — service + controller

**Commit:** `4d39a70`

**Files changed:** all 4

**Interface additions:**
```csharp
Task<int> AddStatAsync(int vehicleId, LocalizedText label, LocalizedText value, CancellationToken ct = default);
Task<bool> RemoveStatAsync(int statId, CancellationToken ct = default);
Task<bool> MoveStatAsync(int statId, int direction, CancellationToken ct = default);
```

**Service:** SortOrder = `CountAsync` sibling scope. Remove/Move reuse `RemoveByIdAsync<StatItem>` / `SwapOrderAsync<StatItem>` helpers.

**Controller actions:** `AddStat`, `RemoveStat`, `MoveStat`.

**Tests:**
- `Stat_AddMoveRemove` — RED → GREEN

---

## Task 23: SliderGroup + SliderSlide (grandchild) — service + controller

**Commit:** `898ea37`

**Files changed:** all 4

**Interface additions:** 6 methods — Add/Remove/Move for SliderGroup and SliderSlide.

**Key details:**
- `AddSliderSlideAsync` takes `sliderGroupId`, guards with `AnyAsync` on `SliderGroup` → returns 0 if group missing.
- `MoveSliderSlideAsync` scopes swap to `SliderGroupId` siblings.

**Controller actions:** `AddSlider`, `RemoveSlider`, `MoveSlider`, `AddSliderSlide`, `RemoveSliderSlide`, `MoveSliderSlide`.

**Tests:**
- `Slider_And_Slide_AddMoveRemove` — RED → GREEN
- `AddSliderSlide_OnMissingGroup_ReturnsZero` — RED → GREEN

---

## Task 24: FeatureSection new fields + FeatureBullet — service + controller

**Commit:** `b57e714`

**Files changed:** all 4

**Service change — `UpdateFeatureAsync`:** added copies of `GroupKey`, `TabLabel`, `Lead`.

**Interface additions:**
```csharp
Task<int> AddFeatureBulletAsync(int featureSectionId, LocalizedText label, LocalizedText text, CancellationToken ct = default);
Task<bool> RemoveFeatureBulletAsync(int bulletId, CancellationToken ct = default);
Task<bool> MoveFeatureBulletAsync(int bulletId, int direction, CancellationToken ct = default);
```

**Service:** `AddFeatureBulletAsync` checks `FeatureSection` exists (parent scope). `MoveFeatureBulletAsync` scopes swap to `FeatureSectionId`.

**Controller actions:** `AddFeatureBullet`, `RemoveFeatureBullet`, `MoveFeatureBullet`. `FeatureSave` unchanged — MVC model-binds `GroupKey`/`TabLabel`/`Lead` from form field names automatically.

**Tests:**
- `Feature_NewFields_Persist_AndBullets_AddMoveRemove` — RED → GREEN

---

## Task 25: GalleryTab + GalleryImage (grandchild) — service + controller

**Commit:** `ebc3ab8`

**Files changed:** all 4

**Interface additions:** 6 methods — Add/Remove/Move for GalleryTab and GalleryImage.

**Key details:**
- `AddGalleryImageAsync` guards against missing tab (returns 0).
- `MoveGalleryImageAsync` scopes swap to `GalleryTabId` siblings.
- `RemoveGalleryImage`/`MoveGalleryImage` do NOT collide with `RemoveImage`/`MoveImage` (those are `VehicleImage`).

**Controller actions:** `AddGalleryTab`, `RemoveGalleryTab`, `MoveGalleryTab`, `AddGalleryImage`, `RemoveGalleryImage`, `MoveGalleryImage`.

**Tests:**
- `GalleryTab_And_Image_AddMoveRemove` — RED → GREEN
- `AddGalleryImage_OnMissingTab_ReturnsZero` — RED → GREEN

---

## Self-Review Notes

- All `LocalizedText` labels (SectionHeading, StatItem, SliderGroup eyebrow/title, GalleryTab label, FeatureBullet label/text) are stored **raw** — correct per brief: plain labels, not rendered as `@Html.Raw`. Only `Feature.Body` and `Trim.Highlights` go through the sanitizer.
- `UpsertSectionHeadingAsync` returns 0 on missing vehicle (consistent with all other Add* methods).
- `FeatureSave` controller not replaced — MVC model-binding handles new fields automatically via form field names `GroupKey`, `TabLabel.En`, `TabLabel.Ar`, `Lead.En`, `Lead.Ar`.

## Concerns / Naming Reconciled

- **`GetAsync_Includes_NewCollections_AndQuality` deferred**: the Task 20 brief test calls `AddCardAsync`, `AddSafetyToggleAsync`, `AddWarrantyLinkAsync`, `UpsertQualityAsync` — all arriving in Tasks 26–31. A comment in `AdminVehicleServiceTests.cs` documents this. Test will be added in the Task 31 regression sweep.
- **Worktree merge required**: the worktree was created from `main` (pre-feature entities). A `git merge feature/vehicle-content-management` fast-forward was needed before entity types existed. This is a worktree setup detail, not a code concern.
- **MSB3492 cache warning**: appears on incremental builds (`Could not read existing .cache file`) — harmless, 0 real `error CS` errors confirmed throughout.
