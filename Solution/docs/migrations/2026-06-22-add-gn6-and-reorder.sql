/*
  GAC — add the GN6 vehicle (cloned from gs4) + apply new display order — 2026-06-22
  Data-only change. NO schema migration / no __EFMigrationsHistory row / no DDL.
  Run once against the GAC database (shared dev + live prod @83.229.86.221).
  Mirrors the source change in GAC.Infrastructure/Data/ContentSeeder.cs
  (the seeder only affects FRESH/empty DBs because it is guarded by AnyAsync();
   for the already-populated live DB this script is what adds GN6).

  What it does:
    (A) Insert a new "gn6" Vehicle by CLONING the gs4 row (same content/images),
        overriding Slug='gn6', Name='GN6', SortOrder=5. Also clones gs4's child
        VehicleImages rows (Hero + Gallery) so the model card/strip has imagery.
        GN6 is visible immediately. Per request, content + images are gs4's and
        will be replaced from the admin panel.
    (B) Re-assert the full display order:
        1 gs3emzoom, 2 emkoo, 3 gs4, 4 hyptec-ht, 5 gn6, 6 gs8, 7 gs8traveller,
        8 m8, 9 empow, 10 empow-sport, 11 aion-v, 12 aion-es (last two hidden).

  Key facts (verified against the entity + applied migrations):
    - Vehicles.Id and VehicleImages.Id are INT IDENTITY -> OMITTED from inserts
      (auto-generated). Never use NEWID() here (columns are int, not uniqueidentifier).
    - Column set below = exactly the live schema after InitialIdentity + AddContentModel
      + AddBodyHtml + AddSpecPdfAndDock. The (unapplied) AddFeatureLayout migration only
      touches FeatureSections.Layout, NOT Vehicles, so this script is safe regardless.
    - The visible vehicle list (home dropdown + header mega-menu) is read via
      GetVisibleAsync() = Where(IsVisible).OrderBy(SortOrder); a visible gn6 at
      SortOrder 5 appears automatically in BOTH, between hyptec-ht and gs8.

  Idempotent: (A) is guarded by IF NOT EXISTS(gn6) so re-running inserts nothing;
  (B) uses absolute UPDATEs. Re-running the whole script is a safe no-op.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

-- ============================================================
-- (A) Clone gs4 -> gn6 (row + its image rows). Guarded/idempotent.
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Vehicles WHERE Slug = N'gn6')
BEGIN
    INSERT INTO dbo.Vehicles
    (
        Slug, Category, SortOrder, IsVisible, PriceFrom,
        Name_En, Name_Ar,
        Tagline_En, Tagline_Ar,
        IntroText_En, IntroText_Ar,
        BrochurePdf,
        MetaTitle_En, MetaTitle_Ar,
        MetaDescription_En, MetaDescription_Ar,
        BodyHtml_En, BodyHtml_Ar,
        SpecPdf
    )
    SELECT
        N'gn6',                 -- Slug (unique)
        v.Category,             -- clone (Suv)
        5,                      -- SortOrder (final position; section B re-asserts)
        v.IsVisible,            -- clone (visible)
        v.PriceFrom,
        N'GN6',                 -- Name_En (override)
        N'GN6',                 -- Name_Ar (model name stays Latin, like GS8/M8)
        v.Tagline_En, v.Tagline_Ar,
        v.IntroText_En, v.IntroText_Ar,
        v.BrochurePdf,          -- clone gs4 (placeholder; edit in admin)
        v.MetaTitle_En, v.MetaTitle_Ar,
        v.MetaDescription_En, v.MetaDescription_Ar,
        v.BodyHtml_En, v.BodyHtml_Ar,  -- clone gs4 body verbatim (placeholder)
        v.SpecPdf               -- clone gs4 (placeholder)
    FROM dbo.Vehicles v
    WHERE v.Slug = N'gs4';

    -- Clone gs4's child image rows onto the new gn6 row (VehicleImages.Id is identity -> omit)
    INSERT INTO dbo.VehicleImages
    (
        VehicleId, Kind, Path, Alt_En, Alt_Ar, SortOrder
    )
    SELECT
        (SELECT Id FROM dbo.Vehicles WHERE Slug = N'gn6'),
        i.Kind, i.Path, i.Alt_En, i.Alt_Ar, i.SortOrder
    FROM dbo.VehicleImages i
    INNER JOIN dbo.Vehicles v ON v.Id = i.VehicleId
    WHERE v.Slug = N'gs4';
END;

-- ============================================================
-- (B) Re-assert the full target display order (idempotent)
-- ============================================================
UPDATE dbo.Vehicles SET SortOrder = 1  WHERE Slug = N'gs3emzoom';
UPDATE dbo.Vehicles SET SortOrder = 2  WHERE Slug = N'emkoo';
UPDATE dbo.Vehicles SET SortOrder = 3  WHERE Slug = N'gs4';
UPDATE dbo.Vehicles SET SortOrder = 4  WHERE Slug = N'hyptec-ht';
UPDATE dbo.Vehicles SET SortOrder = 5  WHERE Slug = N'gn6';
UPDATE dbo.Vehicles SET SortOrder = 6  WHERE Slug = N'gs8';
UPDATE dbo.Vehicles SET SortOrder = 7  WHERE Slug = N'gs8traveller';
UPDATE dbo.Vehicles SET SortOrder = 8  WHERE Slug = N'm8';
UPDATE dbo.Vehicles SET SortOrder = 9  WHERE Slug = N'empow';
UPDATE dbo.Vehicles SET SortOrder = 10 WHERE Slug = N'empow-sport';
UPDATE dbo.Vehicles SET SortOrder = 11 WHERE Slug = N'aion-v';
UPDATE dbo.Vehicles SET SortOrder = 12 WHERE Slug = N'aion-es';

COMMIT TRANSACTION;

-- Verify (optional): should list the 12 models in order with gn6 at 5.
SELECT SortOrder, Slug, Name_En, IsVisible FROM dbo.Vehicles ORDER BY SortOrder;
GO
