/*
  GAC content update — /m8 page heading — 2026-06-22
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors the source change in:
    - GAC.Infrastructure/SeedContent/vehicles/m8.html

  Drops the brand prefix from the M8 page headings:
    "<h2 class="mp-head__title">GAC M8</h2>" -> "<h2 class="mp-head__title">M8</h2>"
  The markup appears twice in the body (overview header + trims header); a single
  REPLACE updates both occurrences.

  Idempotent & guarded: the UPDATE only fires while the original "GAC M8" heading
  is still present, so re-running makes no further changes and admin edits to the
  body are not clobbered.
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'<h2 class="mp-head__title">GAC M8</h2>',
        N'<h2 class="mp-head__title">M8</h2>')
 WHERE [Slug] = 'm8'
   AND [BodyHtml_En] LIKE N'%<h2 class="mp-head__title">GAC M8</h2>%';

COMMIT;
GO
