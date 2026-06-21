/*
  GAC content update — /gs4 overview heading — 2026-06-22
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors the source change in:
    - GAC.Infrastructure/SeedContent/vehicles/gs4.html

  Shortens the gs4 overview heading (the "Inspiring Performance" subtitle is kept):
    "<h2 class="mp-head__title">All New GAC GS4 MAX</h2>"
     -> "<h2 class="mp-head__title">GS4 MAX</h2>"

  Idempotent & guarded: the UPDATE only fires while the original "All New GAC GS4 MAX"
  heading is still present, so re-running makes no further changes and admin edits are
  not clobbered.

  NOTE: assumes the live gs4 body currently contains the full heading text. If the
  heading had previously been stripped from the DB, this is a safe no-op — verify the
  live body first.
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'<h2 class="mp-head__title">All New GAC GS4 MAX</h2>',
        N'<h2 class="mp-head__title">GS4 MAX</h2>')
 WHERE [Slug] = 'gs4'
   AND [BodyHtml_En] LIKE N'%<h2 class="mp-head__title">All New GAC GS4 MAX</h2>%';

COMMIT;
GO
