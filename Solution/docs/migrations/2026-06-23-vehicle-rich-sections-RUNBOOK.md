# Vehicle Rich Sections — Cutover Run-book (2026-06-23)

Order matters: build template → apply schema → backfill content → verify parity → cut render path.
The shared GAC DB (83.229.86.221/GAC) does NOT auto-migrate. Never run `dotnet ef database update`
on prod (history-gap rule) — use the guarded SQL.

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

## 3. Run the one-off content backfill
- This is a deliberate ONE-TIME write to the shared prod DB — do it with the site owner's explicit
  go-ahead. It only ADDS rows to the new tables; it never touches `BodyHtml` (kept as backup), is
  idempotent, and re-runnable per car. Cars with an empty body (currently `aion-v`, `aion-es`) are
  skipped; the other ~10 cars get real EN + AR structured content from their existing bodies.
- Option A (admin UI): sign in as Admin → `/Admin/ContentMigration` → **Run All**. Idempotent;
  skips any car that already has structured rows. Use **Run One** + a vehicle id (with force) to
  re-do a single car after a tweak.
- Option B (local/dev): set `GAC_RUN_BACKFILL=1` and start the app once; the (currently commented)
  Program.cs hook runs `BackfillAllAsync` and logs scanned/migrated/skipped, then unset the variable.

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

## 5. Cut the render path (done in the render task)
- `Detail.cshtml` always renders the new template; the `HasStructuredContent` all-or-nothing
  branch is removed. `BodyHtml` columns are KEPT as backup (never dropped) — re-parseable.

## Rollback
- The new tables are additive; `BodyHtml` is untouched. To revert rendering, restore the old
  `Detail.cshtml` branch. To re-extract, `Run One` (force) per car.
