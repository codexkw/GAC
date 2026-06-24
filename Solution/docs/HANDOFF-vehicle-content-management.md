# HANDOFF — Vehicle Content Management (no-HTML admin editing)

**Branch:** `feature/vehicle-content-management` (off `main`, now pushed to `origin`)
**Repo:** github.com/codexkw/GAC (public)
**Status as of 2026-06-24:** Phases 1–5 COMPLETE (57/67 tasks), all committed + pushed. **No production-DB write has happened. The live site is UNCHANGED.**
**Plan:** `Solution/docs/superpowers/plans/2026-06-23-vehicle-content-management.md` (67 tasks, 6 phases)
**Spec:** `Solution/docs/superpowers/specs/2026-06-23-vehicle-content-management-design.md`
**Detailed per-task ledger (git-ignored scratch):** `.superpowers/sdd/progress.md` — read this for the blow-by-blow record.

---

## What this feature does

Lets non-technical admins edit **all** vehicle detail-page content (text, images, PDFs, EN + AR) through structured forms — **no HTML**. A developer-owned master Razor template renders the fixed GAC design for every car from a structured per-vehicle model. A parser converts each car's existing `BodyHtml` into that structured model so every admin field opens pre-filled with the car's current content.

**Key safety property:** the conversion only *copies* — it never deletes or moves `BodyHtml`, which is kept as a backup. The whole thing is reversible and re-runnable per car.

---

## What is built and DONE (Phases 1–5)

- **Phase 1 — Data layer (Tasks 1–12):** 12 new entities/tables (SectionHeading, StatItem, SliderGroup/Slide, FeatureBullet, GalleryTab/Image, QualityBlock, CardItem, SafetyToggle, TrimPriceRow, WarrantyLink) + extended Vehicle/FeatureSection/Trim. Migration `20260623213147_AddVehicleRichSections`. **Guarded SQL `Solution/docs/migrations/2026-06-23-AddVehicleRichSections.sql` is ALREADY APPLIED to the shared prod DB (2026-06-24).**
- **Phase 2 — Admin backend (Tasks 20–31):** `IAdminVehicleService`/`AdminVehicleService` full CRUD over every section + `Areas/Admin/Controllers/VehiclesController`.
- **Phase 3 — Admin views (Tasks 40–51):** per-section editor partials (text/image-picker/PDF, EN+AR side by side).
- **Phase 4 — Render partials (Tasks 60–72):** all public render partials under `Views/Vehicles/` matching the live design + JS hooks. **NOT yet wired into `Detail.cshtml` (that is Task 73, deferred).**
- **Phase 5 — Parser & migration (Tasks 80–88):**
  - `GAC.Infrastructure/Content/BodyHtmlParser.cs` — parses one language's `BodyHtml` into entity lists (13 sections). 17 tests.
  - `GAC.Infrastructure/Content/VehicleContentMigrator.cs` — per car: build from EN, positional AR merge, FK/SortOrder wiring. Idempotent (skip if already structured), `force` clears+rebuilds, images from EN only, **never touches `BodyHtml`**, never runs at startup. 6 tests (in-memory DB).
  - `GAC.Web/Areas/Admin/Controllers/ContentMigrationController.cs` + `Views/ContentMigration/Index.cshtml` — guarded (`AdminPolicies.AdminOnly`, anti-forgery) **Run All / Run One** trigger. Plus a commented-out `Program.cs` dev hook gated on env var `GAC_RUN_BACKFILL=1`.
  - Run-book: `Solution/docs/migrations/2026-06-23-vehicle-rich-sections-RUNBOOK.md`.
  - **Task 85 was already satisfied by Tasks 11–12** (the guarded SQL) — no rework.

**Test state:** full suite **319/320**. The ONE failing test — `VehicleDetailRenderTests.StructuredContentVehicle_Renders_HeroSection` — is expected; it is fixed by the render cutover (Task 73/74).

---

## Read-only dry-run findings (2026-06-24) — what the backfill WILL produce

I parsed all 10 non-empty cars' **real production bodies** in memory (zero writes):

- **9 cars convert perfectly and fully bilingually** (emkoo, empow, empow-sport, emzoom, gs4, gs8, gs8traveller, hyptec-ht, m8): canonical shape 4 stats / 2 sliders / 6 features / 3 gallery tabs·15 images / 3 cards / 3 safety / 8 headings / quality / 1 warranty / tech banner / enquiry. Trim counts vary naturally (1–3). EN/AR parity OK; Arabic is distinct from English (real translations merge correctly).
- **gn6 is a structural OUTLIER.** Its sections are `exterior, design, gallery, technology, SPACE, performance, safety, DIMENSIONS, warranty` — **no `quality`, no `trims`**. Instead it has two custom sections the fixed model has no slot for:
  - `#space` ("Spacious and Luxurious Space") = 3 **cards** (same `.mp-card` markup as `#technology`, but under `id="space"`).
  - `#dimensions` ("Dimensions") = a **stats** block (same `.mp-stat` markup as `#exterior`, under `id="dimensions"`).
  gn6 also has 13 gallery images (not 15). The parser's selectors are section-id-scoped, so gn6's *second* cards section and *second* stats section are NOT captured. gn6's standard sections convert fine; `space`/`dimensions` stay only in `BodyHtml`.
- `aion-v` / `aion-es` have empty bodies (unbuilt EVs) → skipped by the migrator.

(Dry-run bodies were exported read-only to a scratchpad and are not in the repo. A temporary diagnostic test was used and deleted — not committed.)

---

## DECISION MADE BY OWNER (2026-06-24)

> **Backfill ALL 10 cars, including gn6.**

Implication the owner accepted: gn6's standard sections become editable, but its **`Space` and `Dimensions` sections will drop from the structured render** once gn6 renders from the new template (the originals remain safe in `BodyHtml`). (Controller's recommendation had been to backfill the 9 standard cars and leave gn6 on its current HTML render via the per-vehicle fallback; owner chose all-10.)

**Open consideration for the cutover (Task 73):** even with all 10 backfilled, you can still choose to either (a) drop the `HasStructuredContent` gate entirely (gn6 loses space/dimensions — matches the owner's accepted decision), or (b) keep a per-vehicle fallback and special-case gn6, or (c) add a small "repeatable extra section" model extension so gn6's space/dimensions become editable too. Decide at Task 73.

---

## NEXT STEPS to resume (in order)

1. **Run the backfill against prod (all 10 cars).** This is the owner-authorized one-time data write. Options:
   - Deploy the Web app, sign in as Admin → `/Admin/ContentMigration` → **Run All**; or
   - Locally set `GAC_RUN_BACKFILL=1` (uncomment the `Program.cs` hook) and start the app once, then unset; or
   - Call `VehicleContentMigrator.BackfillAllAsync(db)`.
   It only adds rows, keeps `BodyHtml`, is idempotent, re-runnable per car (`Run One` + force).
2. **Task 73 — `Detail.cshtml` render cutover:** wire ALL new render partials; include `_VehicleGallery`/`_Lightbox` exactly once; wire the enquiry section to `FormsController`; resolve the `HasStructuredContent` gate per the cutover decision above.
3. **Task 74 — all-markers integration test** (needs structured data present from step 1).
4. **Phase 6 (Tasks 95–101):** parity verification per car, deploy runbook, full-suite gate, and update/remove the now-obsolete `VehicleDetailRenderTests.StructuredContentVehicle_Renders_HeroSection`.
5. **Final whole-branch code review** (most-capable model) → then `superpowers:finishing-a-development-branch`.
6. **Deploy** the Web app (the schema is already applied to prod).

---

## Critical constraints & gotchas (read before resuming)

- **Prod DB writes require explicit owner authorization.** The shared prod DB is at `83.229.86.221/GAC`. The connection string + SMTP password live ONLY in gitignored `Solution/GAC.Web/appsettings.Development.json` under `ConnectionStrings:Default`. **NEVER put the DB password on a command line** — read it from that file inside the script (e.g. PowerShell `ConvertFrom-Json`).
- **The AddVehicleRichSections schema is ALREADY APPLIED to prod** (2026-06-24, real stamp `20260623213147_AddVehicleRichSections`). Don't re-apply for the current DB. Never `dotnet ef database update` on prod (history-gap rule) — use the guarded SQL only on a fresh DB.
- **Apps don't auto-migrate.** Deploying does not change the DB.
- **AngleSharp 1.1.2** is pinned in `GAC.Infrastructure`. It triggers an NU1608 advisory vs HtmlSanitizer 9.0.892 (wants AngleSharp 0.17.1) — this was runtime-verified safe (HtmlSanitizer tests pass under 1.1.2). Do not "fix" it by downgrading.
- **Tooling note:** this environment normalizes a typed `\uXXXX` escape into the actual glyph before any tool sees it. To write Unicode escapes into a file, build them at runtime in PowerShell (e.g. `[char]0x5C + 'u0600'`) or avoid escapes entirely with ASCII-only assertions (`Assert.NotEqual(x.En, x.Ar)` instead of a hardcoded Arabic word).
- **The progress ledger** `.superpowers/sdd/progress.md` is git-ignored scratch — it holds the full per-task record (commits, review verdicts, every decision). If it's lost, reconstruct from `git log`.

---

## Phase 5 commit map (on this branch)

```
bc30fa4 Task 88  cutover run-book
d271ceb Task 87  gn6 count-tolerance test
2d1bae6 Task 86  guarded admin backfill endpoint + dev hook
72f9f80 Task 84  VehicleContentMigrator (idempotent, EN+AR)
60aacd7 Task 83  fix: real \u escapes in HasArabic + drop BOM
d42854e Task 83  (superseded fix attempt)
0b9c29f Task 83  parser handles real Arabic body + EN/AR parity
af8cf2f Task 82  BodyHtmlParser (13 sections via AngleSharp)
c6afed4 Task 81  emkoo fixture + parser harness (red)
2664ecc Task 80  AngleSharp 1.1.2
```
(Phases 1–4 are the 50 commits before `2664ecc`, back to the branch base on `main`.)
