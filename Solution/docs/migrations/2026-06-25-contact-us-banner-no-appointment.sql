/*
  GAC content update — contact-us banner wording (AR + EN) — 2026-06-25
  Data-only change (NO schema migration / no __EFMigrationsHistory row).
  Run once against the GAC database. Mirrors source changes in:
    - GAC.Infrastructure/SeedContent/forms/ar/contact-us.html   (Arabic banner)
    - GAC.Infrastructure/SeedContent/forms/contact-us.html      (English banner)

  Shortens the directory-banner copy:
    AR: "كل مراكز الخدمة توفر خدمة سريعة بدون الحاجة لحجز موعد مسبق."
     -> "كل مراكز الخدمة من دون موعد مسبق"
    EN: "Visit any of our service centers for fast service, with no appointment required."
     -> "Visit any of our service centers—no appointment needed"

  Idempotent & guarded: each UPDATE only fires while the original banner text is
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

------------------------------------------------------------------
-- English banner
------------------------------------------------------------------
UPDATE [FormPages]
   SET [BodyHtml_En] = REPLACE([BodyHtml_En],
        N'Visit any of our service centers for fast service, with no appointment required.',
        N'Visit any of our service centers—no appointment needed')
 WHERE [Slug] = 'contact-us'
   AND [BodyHtml_En] LIKE N'%Visit any of our service centers for fast service, with no appointment required.%';

COMMIT;
GO
