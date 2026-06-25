/*
  GAC content update — contact-us Arabic banner wording — 2026-06-25
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors source change in:
    - GAC.Infrastructure/SeedContent/forms/ar/contact-us.html   (Arabic banner)

  Shortens the directory-banner copy:
    AR: "كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق."
     -> "كل مراكز الخدمة من دون موعد مسبق"

  English banner is intentionally left unchanged.

  Idempotent & guarded: the UPDATE only fires while the original banner text is
  still present, so re-running makes no further changes and admin edits to the
  body are not clobbered.
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

------------------------------------------------------------------
-- Arabic banner
------------------------------------------------------------------
UPDATE [FormPages]
   SET [BodyHtml_Ar] = REPLACE([BodyHtml_Ar],
        N'كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.',
        N'كل مراكز الخدمة من دون موعد مسبق')
 WHERE [Slug] = 'contact-us'
   AND [BodyHtml_Ar] LIKE N'%كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.%';

COMMIT;
GO
