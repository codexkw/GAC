/*
  GAC content fix — restore the /gs4 overview heading as "GS4 MAX" — 2026-06-22 (corrected)
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors the source change in:
    - GAC.Infrastructure/SeedContent/vehicles/gs4.html

  WHY THE FIRST VERSION OF THIS SCRIPT DID NOTHING:
  It searched for "<h2 ...>All New GAC GS4 MAX</h2>" and replaced it with
  "<h2 ...>GS4 MAX</h2>". But the live gs4 body no longer contains that text:
  an earlier removal (the old content-updates §4) had already stripped the
  <h2> and <p>, leaving an EMPTY <header class="mp-head mp-head--left"></header>
  wrapper in the overview section. So the old REPLACE matched nothing and the
  page was unchanged.

  This version refills that empty wrapper with the "GS4 MAX" heading (and the
  original "Inspiring Performance" subtitle), so it renders again.

  Robust by construction: the exact whitespace inside the wrapper (CRLF + the
  10/10/8-space indentation left behind by the old strip) is rebuilt with
  NCHAR(13)+NCHAR(10), so THIS file's own line endings do not affect matching.
  Idempotent: once the wrapper is filled, @old no longer matches and re-running
  is a safe no-op.
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @crlf NVARCHAR(2) = NCHAR(13) + NCHAR(10);

-- Exact current (empty) wrapper: open tag, CRLF, 10 spaces, CRLF, 10 spaces,
-- CRLF, 8 spaces, close tag.
DECLARE @old NVARCHAR(MAX) =
      N'<header class="mp-head mp-head--left">' + @crlf
    + N'          ' + @crlf
    + N'          ' + @crlf
    + N'        </header>';

-- Refilled wrapper (matches GAC.Infrastructure/SeedContent/vehicles/gs4.html).
DECLARE @new NVARCHAR(MAX) =
      N'<header class="mp-head mp-head--left">' + @crlf
    + N'          <h2 class="mp-head__title">GS4 MAX</h2>' + @crlf
    + N'          <p class="mp-head__sub">Inspiring Performance</p>' + @crlf
    + N'        </header>';

UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], @old, @new)
 WHERE [Slug] = 'gs4'
   AND CHARINDEX(@old, [BodyHtml_En]) > 0;

DECLARE @rows INT = @@ROWCOUNT;
PRINT CONCAT(N'gs4 overview heading restored. Rows affected: ', @rows,
             N' (expect 1; 0 means the empty wrapper was not found - investigate).');

COMMIT;
GO
