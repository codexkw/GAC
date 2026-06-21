/*
  GAC content update — contact-us "quick service" banner — 2026-06-22
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors source changes in:
    - GAC.Infrastructure/SeedContent/forms/contact-us.html      (English banner)
    - GAC.Infrastructure/SeedContent/forms/ar/contact-us.html   (Arabic banner)

  Replaces the directory banner copy on the /contact-us "Locate Us" page:
    EN: "All our garages are quick service — no appointment needed."
     -> "Visit any of our service centers for fast service, with no appointment required."
    AR: "جميع كراجاتنا للخدمة السريعة — دون الحاجة إلى موعد مسبق."
     -> "كل كراجاتنا توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق."

  Idempotent & guarded: each UPDATE only fires while the original banner text is
  still present, so re-running makes no further changes and admin edits to the
  body are not clobbered.
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

------------------------------------------------------------------
-- English banner
------------------------------------------------------------------
UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'All our garages are quick service — no appointment needed.',
        N'Visit any of our service centers for fast service, with no appointment required.')
 WHERE [Slug] = 'contact-us'
   AND [BodyHtml_En] LIKE N'%All our garages are quick service — no appointment needed.%';

------------------------------------------------------------------
-- Arabic banner
------------------------------------------------------------------
UPDATE [FormPages]
   SET [BodyHtml_Ar] = REPLACE([BodyHtml_Ar],
        N'جميع كراجاتنا للخدمة السريعة — دون الحاجة إلى موعد مسبق.',
        N'كل كراجاتنا توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق.')
 WHERE [Slug] = 'contact-us'
   AND [BodyHtml_Ar] LIKE N'%جميع كراجاتنا للخدمة السريعة — دون الحاجة إلى موعد مسبق.%';

COMMIT;
GO
