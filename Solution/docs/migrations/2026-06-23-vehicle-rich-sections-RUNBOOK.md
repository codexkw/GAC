# Vehicle Rich Sections — Cutover Run-book (rev. 2026-06-24)

**Correct order: apply schema → DEPLOY the new Web app → backfill content → verify.**
The shared GAC DB (83.229.86.221/GAC) does NOT auto-migrate. Never run `dotnet ef database update`
on prod (history-gap rule) — use the guarded SQL.

> ⚠️ **CRITICAL — DEPLOY BEFORE YOU BACKFILL. Do NOT run the backfill against the shared DB until the
> new Web app is deployed.** The render uses a per-vehicle gate `VehicleContent.HasStructuredContent`
> (true when `Features|SpecGroups|Colors|Trims` is non-empty). The backfill populates `Features` +
> `Trims`, which flips that gate to **true**. The CURRENTLY-DEPLOYED view, on a true gate, renders the
> OLD 2026-06-15 partials (`_VehicleFeatures/_VehicleSpecs/_VehicleColors`) — which do NOT cover the
> new sections — so backfilling first would make every backfilled car's LIVE page render half-broken
> instead of its full `BodyHtml`. Deploying the new build first makes a true gate render the new master
> template (full page). Until the backfill runs, every car keeps rendering its `BodyHtml` fallback, so
> the deploy alone changes nothing visible.

**Design note (this revision):** the cutover KEEPS the `HasStructuredContent` gate as a per-vehicle
fallback (owner decision 2026-06-24) — it does NOT remove it. A car renders the new structured master
template when it has structured rows, else its `BodyHtml`. The deployed build also includes the fix to
`VehicleService.GetBySlugAsync` so the public render eager-loads the full structured graph (without it,
the new sections render empty). **Gating condition: the build you deploy in step 2 MUST contain this
`GetBySlugAsync` eager-load fix** — if an older build (new partials, old query) were deployed, every
backfilled car would render empty sections.

## 1. Apply the schema (guarded, history-stamped)
- STATUS: **already applied to the shared GAC DB (83.229.86.221/GAC) on 2026-06-24.** The 12 new
  tables + new Vehicle/FeatureSection/Trim columns exist and the migration row is stamped, so for
  the CURRENT prod DB this step is DONE — skip it.
- The script `docs/migrations/2026-06-23-AddVehicleRichSections.sql` already carries the REAL
  MigrationId `20260623213147_AddVehicleRichSections` (no `__STAMP_` placeholder remains).
- For any OTHER / fresh database only: run the script once as-is. Re-running is safe (every DDL is
  `IF NOT EXISTS`-guarded and the stamped history row blocks re-entry). Never `dotnet ef database
  update` on prod (history-gap rule).

## 2. Deploy the Web app
- Deploy the build that contains the new entities, EF config, render partials, and the admin
  ContentMigration endpoint. (No app auto-migration — step 1 must already be done.)

## 3. Run the one-off content backfill (ONLY after step 2 deploy is live)
- This is a deliberate ONE-TIME write to the shared prod DB — do it with the site owner's explicit
  go-ahead, and only once the new build is deployed (see the CRITICAL warning above). It only ADDS
  rows to the new tables; it never touches `BodyHtml` (kept as backup), is idempotent, and re-runnable
  per car. Cars with an empty body (`aion-v`, `aion-es` — both hidden) are skipped; the other ~10 cars
  get real EN + AR structured content parsed from their existing bodies.
- **Staged rollout (recommended) — de-risks a render bug:** in the admin panel, do **Run One** for a
  single car first (e.g. emkoo), then open `/emkoo` and confirm it renders 200 with the full set of
  sections (stats, sliders, gallery, technology, safety, trims, warranty, enquiry). Only then **Run
  All** for the rest. (The hermetic `VehicleDetailRenderTests` + `VehicleRichSectionsParityTests`
  already prove this render path on a seeded DB, but a one-car live check costs nothing.)
- Option A (admin UI): sign in as Admin → `/Admin/ContentMigration` → **Run One** (id) then **Run
  All**. Idempotent; skips any car that already has structured rows. **Run One** + id (with force)
  re-does a single car after an admin tweak.
- Option B (local/dev): set `GAC_RUN_BACKFILL=1` and start the app once; the (currently commented)
  `Program.cs` hook runs `BackfillAllAsync` and logs scanned/migrated/skipped, then unset the variable.
- gn6 note: gn6's real prod body is a structural outlier (its `#space`/`#dimensions` custom sections
  have no slot in the fixed model). Backfilling gn6 is lossless (`BodyHtml` kept); once gn6 renders
  from the structured template it shows its standard sections and DROPS `#space`/`#dimensions` — this
  is the accepted owner decision. To keep gn6 on its full `BodyHtml` instead, simply do not backfill
  gn6 (the kept gate leaves an un-backfilled car on its `BodyHtml`).

## 4. Verify parity (AC1/AC3)
- Open 2-3 cars in admin: every section populated EN + AR (stats, sliders, features+bullets,
  gallery, cards, safety, trims, warranty, enquiry).
- Open the public pages: marker/section counts match the old body (4 stats, 2 sliders, 6 features,
  15 gallery over 3 tabs, 3 cards, 3 safety, ≥1 trim). The integration tests in the render task
  are the permanent regression guard.
- gn6 (manual clone / structural outlier): confirm its sections render with whatever counts ITS OWN
  body has — do NOT expect emkoo's 4/15/3. (The 8/13/6 figures in the test suite are a synthetic
  count-tolerance fixture, not gn6's real numbers.) If a selector mismatch surfaces, fix the parser
  selector or do a quick manual admin pass, then `Run One` (force) for gn6.

## 5. The render cutover ships WITH the deploy (no separate step)
- There is no separate "remove the gate" step. The deployed `Detail.cshtml` (commit on this branch)
  renders the new structured master template when `HasStructuredContent(Model)` is true, else the
  `BodyHtml` fallback. The gate is KEPT on purpose so any un-backfilled / empty / future car degrades
  gracefully to `BodyHtml` instead of a blank page. `BodyHtml` columns are KEPT as backup (never
  dropped) — re-parseable at any time via **Run One** (force).

## Rollback
- The new tables are additive; `BodyHtml` is untouched. To revert a single car's rendering to its
  `BodyHtml`, delete its structured rows (the gate then falls back). To revert the whole render,
  redeploy the previous build. To re-extract, **Run One** (force) per car.
