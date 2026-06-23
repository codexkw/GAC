-- AddVehicleRichSections — guarded, additive. Apply to shared GAC DB. Do NOT run dotnet ef database update.
IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;

-- Vehicle new scalar/localized columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    ALTER TABLE [Vehicles] ADD
        [EnquiryBgImage] nvarchar(300) NULL,
        [EnquiryLead_Ar] nvarchar(max) NULL,
        [EnquiryLead_En] nvarchar(max) NULL,
        [EnquirySub_Ar] nvarchar(max) NULL,
        [EnquirySub_En] nvarchar(max) NULL,
        [EnquiryTitle_Ar] nvarchar(max) NULL,
        [EnquiryTitle_En] nvarchar(max) NULL,
        [StatsNote_Ar] nvarchar(max) NULL,
        [StatsNote_En] nvarchar(max) NULL,
        [TechBannerImage] nvarchar(300) NULL;
END;

-- Trim new columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    ALTER TABLE [Trims] ADD
        [ImagePath] nvarchar(300) NULL,
        [ModelLabel_Ar] nvarchar(max) NULL,
        [ModelLabel_En] nvarchar(max) NULL;
END;

-- FeatureSection new columns
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    ALTER TABLE [FeatureSections] ADD
        [GroupKey] int NOT NULL DEFAULT 0,
        [Lead_Ar] nvarchar(max) NULL,
        [Lead_En] nvarchar(max) NULL,
        [TabLabel_Ar] nvarchar(max) NULL,
        [TabLabel_En] nvarchar(max) NULL;
END;

-- CardItems
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [CardItems] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [Text_En] nvarchar(max) NULL,
        [Text_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(300) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_CardItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CardItems_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_CardItems_VehicleId] ON [CardItems] ([VehicleId]);
END;

-- FeatureBullets
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [FeatureBullets] (
        [Id] int NOT NULL IDENTITY,
        [FeatureSectionId] int NOT NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [Text_En] nvarchar(max) NULL,
        [Text_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_FeatureBullets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeatureBullets_FeatureSections_FeatureSectionId] FOREIGN KEY ([FeatureSectionId]) REFERENCES [FeatureSections] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_FeatureBullets_FeatureSectionId] ON [FeatureBullets] ([FeatureSectionId]);
END;

-- GalleryTabs
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [GalleryTabs] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GalleryTabs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GalleryTabs_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_GalleryTabs_VehicleId] ON [GalleryTabs] ([VehicleId]);
END;

-- GalleryImages (depends on GalleryTabs)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [GalleryImages] (
        [Id] int NOT NULL IDENTITY,
        [GalleryTabId] int NOT NULL,
        [ImagePath] nvarchar(300) NULL,
        [Alt_En] nvarchar(max) NULL,
        [Alt_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_GalleryImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_GalleryImages_GalleryTabs_GalleryTabId] FOREIGN KEY ([GalleryTabId]) REFERENCES [GalleryTabs] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_GalleryImages_GalleryTabId] ON [GalleryImages] ([GalleryTabId]);
END;

-- QualityBlocks (0/1 per vehicle -> UNIQUE VehicleId; no SortOrder column)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [QualityBlocks] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [MainImage] nvarchar(300) NULL,
        [ThumbImage] nvarchar(300) NULL,
        [Strapline_En] nvarchar(max) NULL,
        [Strapline_Ar] nvarchar(max) NULL,
        [Content_En] nvarchar(max) NULL,
        [Content_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_QualityBlocks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QualityBlocks_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX [IX_QualityBlocks_VehicleId] ON [QualityBlocks] ([VehicleId]);
END;

-- SafetyToggles
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [SafetyToggles] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(300) NULL,
        [Strap_En] nvarchar(max) NULL,
        [Strap_Ar] nvarchar(max) NULL,
        [Content_En] nvarchar(max) NULL,
        [Content_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SafetyToggles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SafetyToggles_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SafetyToggles_VehicleId] ON [SafetyToggles] ([VehicleId]);
END;

-- SectionHeadings ([Key] is a reserved word, bracket-escaped)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [SectionHeadings] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Key] int NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [Sub_En] nvarchar(max) NULL,
        [Sub_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL,
        [Body_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_SectionHeadings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SectionHeadings_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SectionHeadings_VehicleId] ON [SectionHeadings] ([VehicleId]);
END;

-- SliderGroups
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [SliderGroups] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Eyebrow_En] nvarchar(max) NULL,
        [Eyebrow_Ar] nvarchar(max) NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SliderGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SliderGroups_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SliderGroups_VehicleId] ON [SliderGroups] ([VehicleId]);
END;

-- SliderSlides (depends on SliderGroups)
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [SliderSlides] (
        [Id] int NOT NULL IDENTITY,
        [SliderGroupId] int NOT NULL,
        [ImagePath] nvarchar(300) NULL,
        [Alt_En] nvarchar(max) NULL,
        [Alt_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SliderSlides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SliderSlides_SliderGroups_SliderGroupId] FOREIGN KEY ([SliderGroupId]) REFERENCES [SliderGroups] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_SliderSlides_SliderGroupId] ON [SliderSlides] ([SliderGroupId]);
END;

-- StatItems
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [StatItems] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [Value_En] nvarchar(max) NULL,
        [Value_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_StatItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_StatItems_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_StatItems_VehicleId] ON [StatItems] ([VehicleId]);
END;

-- TrimPriceRows
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [TrimPriceRows] (
        [Id] int NOT NULL IDENTITY,
        [TrimId] int NOT NULL,
        [Text_En] nvarchar(max) NULL,
        [Text_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_TrimPriceRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrimPriceRows_Trims_TrimId] FOREIGN KEY ([TrimId]) REFERENCES [Trims] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_TrimPriceRows_TrimId] ON [TrimPriceRows] ([TrimId]);
END;

-- WarrantyLinks
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    CREATE TABLE [WarrantyLinks] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [Url] nvarchar(500) NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_WarrantyLinks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WarrantyLinks_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
    CREATE INDEX [IX_WarrantyLinks_VehicleId] ON [WarrantyLinks] ([VehicleId]);
END;

-- Stamp history row
IF NOT EXISTS (SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260623213147_AddVehicleRichSections')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260623213147_AddVehicleRichSections', N'9.0.6');
END;

COMMIT;
GO
