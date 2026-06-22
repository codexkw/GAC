/*
  GAC content update — contact-us Arabic banner wording — 2026-06-22
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors source change in:
    - GAC.Infrastructure/SeedContent/forms/ar/contact-us.html   (Arabic banner)

  Replaces "كراجاتنا" with "مراكز الخدمة" in the directory banner copy:
    AR: "كل كراجاتنا توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق."
     -> "كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق."

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
        N'كل كراجاتنا توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.',
        N'كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.')
 WHERE [Slug] = 'contact-us'
   AND [BodyHtml_Ar] LIKE N'%كل كراجاتنا توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.%';

COMMIT;
GO
