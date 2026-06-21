/*
  GAC — Specifications PDF + Action Dock (prod data) — 2026-06-21
  Run AFTER the EF migration AddSpecPdfAndDock has been applied to prod (adds
  Vehicles.SpecPdf + the DockItems table). Data-only, guarded, idempotent.

  1. Seed the 6 action-dock items if the table is empty (matches the old hardcoded dock).
  2. The Specifications button is now field-driven (Vehicles.SpecPdf); the gs4/hyptec
     in-body trim anchors are removed by 2026-06-21-content-updates.sql (section 3).
*/
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [DockItems])
BEGIN
    INSERT INTO [DockItems] ([Label_En],[Label_Ar],[ShortLabel_En],[ShortLabel_Ar],[Url],[Icon],[LinkType],[IsVisible],[SortOrder])
    VALUES
      (N'Chat on WhatsApp', N'تواصل عبر واتساب', N'WhatsApp',   N'واتساب',       NULL,                 N'whatsapp',  1, 1, 1),
      (N'Book a Test Drive',N'احجز تجربة قيادة', N'Test Drive', N'تجربة قيادة',  N'/book-a-test-drive',N'test-drive',0, 1, 2),
      (N'Get Online Quote', N'اطلب عرض سعر',     N'Quote',      N'عرض سعر',      N'/request-a-quote',  N'quote',     0, 1, 3),
      (N'Download Brochure',N'حمّل الكتيب',       N'Brochure',   N'الكتيب',       NULL,                 N'brochure',  3, 1, 4),
      (N'Find Showroom',    N'أوجد المعرض',      N'Showroom',   N'المعرض',       N'/contact-us',       N'location',  0, 1, 5),
      (N'Contact Us',       N'تواصل معنا',       N'Contact',    N'تواصل',        N'/contact-us',       N'mail',      0, 1, 6);
END

COMMIT;
GO
