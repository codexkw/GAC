BEGIN TRANSACTION;
ALTER TABLE [FeatureSections] ADD [Layout] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260615173920_AddFeatureLayout', N'9.0.6');

COMMIT;
GO

