/*
  GAC content updates — 2026-06-21
  Data-only changes (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors source changes in:
    - GAC.Infrastructure/Data/ContentSeeder.cs (menu + vehicle SortOrder)
    - GAC.Infrastructure/SeedContent/vehicles/gs4.html
    - GAC.Infrastructure/SeedContent/vehicles/hyptec-ht.html

  Covers:
    1. Menu      : replace the "More" dropdown with a single top-level "Fleet Sales"
                   link to /fleet (drops the Finance child entirely).
    2. Vehicles  : display order for the home dropdown, model strip and mega-menu
                   -> EMZOOM, EMKOO, GS4 MAX, HYPTEC HT, GS8, GS8 Traveller, M8,
                      EMPOW, EMPOW R (hidden AION concepts last). GN6 intentionally skipped.
    3. gs4/hyptec-ht: "Request a Quote" trim button -> "Specifications" (matches other models).
    4. gs4      : remove the "All New GAC GS4 MAX / Inspiring Performance" overview heading.
    5. contact-us: British -> American spelling ("Centres"/"centre" -> "Centers"/"center").

  Idempotent & guarded: re-running makes no further changes. The HTML edits only fire
  when the original markup is still present (so admin edits are not clobbered).
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

------------------------------------------------------------------
-- 1. Menu: "More" dropdown -> single "Fleet Sales" top-level link
------------------------------------------------------------------
DECLARE @moreId INT = (SELECT TOP 1 [Id] FROM [MenuItems] WHERE [Label_En] = N'More' AND [ParentId] IS NULL);
IF @moreId IS NOT NULL
BEGIN
    -- remove the old children (the previous "Fleet Sales" + "Finance" entries)
    DELETE FROM [MenuItems] WHERE [ParentId] = @moreId;

    -- turn the former "More" parent into the single Fleet Sales link
    UPDATE [MenuItems]
       SET [Label_En] = N'Fleet Sales',
           [Label_Ar] = N'مبيعات الأساطيل',
           [Url]      = N'/fleet'
     WHERE [Id] = @moreId;
END

------------------------------------------------------------------
-- 2. Vehicle display order (ordered by SortOrder everywhere)
------------------------------------------------------------------
UPDATE [Vehicles] SET [SortOrder] = CASE [Slug]
    WHEN 'gs3emzoom'    THEN 1
    WHEN 'emkoo'        THEN 2
    WHEN 'gs4'          THEN 3
    WHEN 'hyptec-ht'    THEN 4
    WHEN 'gs8'          THEN 5
    WHEN 'gs8traveller' THEN 6
    WHEN 'm8'           THEN 7
    WHEN 'empow'        THEN 8
    WHEN 'empow-sport'  THEN 9
    WHEN 'aion-v'       THEN 10
    WHEN 'aion-es'      THEN 11
    ELSE [SortOrder] END
WHERE [Slug] IN ('gs3emzoom','emkoo','gs4','hyptec-ht','gs8','gs8traveller','m8','empow','empow-sport','aion-v','aion-es');

------------------------------------------------------------------
-- 3. "Request a Quote" trim button -> "Specifications"
------------------------------------------------------------------
UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'<a class="btn btn--trim" href="#enquiry">Request a Quote</a>',
        N'<a class="btn btn--trim" href="/pdfs/gs4-specifications.pdf" target="_blank" rel="noopener">Specifications</a>')
 WHERE [Slug] = 'gs4'
   AND [BodyHtml_En] LIKE N'%<a class="btn btn--trim" href="#enquiry">Request a Quote</a>%';

UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'<a class="btn btn--trim" href="#enquiry">Request a Quote</a>',
        N'<a class="btn btn--trim" href="/pdfs/hyptec-ht-specifications.pdf" target="_blank" rel="noopener">Specifications</a>')
 WHERE [Slug] = 'hyptec-ht'
   AND [BodyHtml_En] LIKE N'%<a class="btn btn--trim" href="#enquiry">Request a Quote</a>%';

------------------------------------------------------------------
-- 4. gs4: remove the "All New GAC GS4 MAX / Inspiring Performance" heading
--    (removes the two text elements; the empty wrapper renders nothing)
------------------------------------------------------------------
UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'<h2 class="mp-head__title">All New GAC GS4 MAX</h2>', N'')
 WHERE [Slug] = 'gs4'
   AND [BodyHtml_En] LIKE N'%<h2 class="mp-head__title">All New GAC GS4 MAX</h2>%';

UPDATE [Vehicles]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'<p class="mp-head__sub">Inspiring Performance</p>', N'')
 WHERE [Slug] = 'gs4'
   AND [BodyHtml_En] LIKE N'%<p class="mp-head__sub">Inspiring Performance</p>%';

------------------------------------------------------------------
-- 5. contact-us: British -> American spelling (Centres/centre -> Centers/center)
------------------------------------------------------------------
UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'Service Centres', N'Service Centers')
 WHERE [Slug] = 'contact-us' AND [BodyHtml_En] LIKE N'%Service Centres%';

UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'Spare-Parts Centres', N'Spare-Parts Centers')
 WHERE [Slug] = 'contact-us' AND [BodyHtml_En] LIKE N'%Spare-Parts Centres%';

UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'spare-parts centre on Google Maps', N'spare-parts center on Google Maps')
 WHERE [Slug] = 'contact-us' AND [BodyHtml_En] LIKE N'%spare-parts centre on Google Maps%';

UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En], N'<!-- Spare-parts centres -->', N'<!-- Spare-parts centers -->')
 WHERE [Slug] = 'contact-us' AND [BodyHtml_En] LIKE N'%<!-- Spare-parts centres -->%';

COMMIT;
GO
