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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] nvarchar(450) NOT NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] nvarchar(450) NOT NULL,
        [DisplayName] nvarchar(max) NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] nvarchar(450) NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
        CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] nvarchar(450) NOT NULL,
        [RoleId] nvarchar(450) NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] nvarchar(450) NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614181726_InitialIdentity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614181726_InitialIdentity', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [ContentPages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(100) NOT NULL,
        [IsVisible] bit NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [MetaTitle_En] nvarchar(max) NULL,
        [MetaTitle_Ar] nvarchar(max) NULL,
        [MetaDescription_En] nvarchar(max) NULL,
        [MetaDescription_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_ContentPages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [FormPages] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(100) NOT NULL,
        [FormType] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [IntroText_En] nvarchar(max) NULL,
        [IntroText_Ar] nvarchar(max) NULL,
        [MetaTitle_En] nvarchar(max) NULL,
        [MetaTitle_Ar] nvarchar(max) NULL,
        [MetaDescription_En] nvarchar(max) NULL,
        [MetaDescription_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_FormPages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [HomePages] (
        [Id] int NOT NULL IDENTITY,
        CONSTRAINT [PK_HomePages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [MediaAssets] (
        [Id] int NOT NULL IDENTITY,
        [Path] nvarchar(300) NOT NULL,
        [OriginalFileName] nvarchar(max) NULL,
        [Alt_En] nvarchar(max) NULL,
        [Alt_Ar] nvarchar(max) NULL,
        [UploadedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_MediaAssets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [ParentId] int NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [Url] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MenuItems_MenuItems_ParentId] FOREIGN KEY ([ParentId]) REFERENCES [MenuItems] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [NewsArticles] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(120) NOT NULL,
        [IsPublished] bit NOT NULL,
        [PublishedOn] date NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [Excerpt_En] nvarchar(max) NULL,
        [Excerpt_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL,
        [Body_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_NewsArticles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [Offers] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(120) NOT NULL,
        [IsActive] bit NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL,
        [Body_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(max) NULL,
        [ValidUntil] date NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Offers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [SiteSettings] (
        [Id] int NOT NULL IDENTITY,
        [Phone] nvarchar(max) NULL,
        [WhatsApp] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [InstagramUrl] nvarchar(max) NULL,
        [FacebookUrl] nvarchar(max) NULL,
        [TiktokUrl] nvarchar(max) NULL,
        [SnapchatUrl] nvarchar(max) NULL,
        [XUrl] nvarchar(max) NULL,
        [FooterTagline_En] nvarchar(max) NULL,
        [FooterTagline_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_SiteSettings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [Vehicles] (
        [Id] int NOT NULL IDENTITY,
        [Slug] nvarchar(100) NOT NULL,
        [Category] int NOT NULL,
        [SortOrder] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [PriceFrom] decimal(18,2) NULL,
        [Name_En] nvarchar(max) NULL,
        [Name_Ar] nvarchar(max) NULL,
        [Tagline_En] nvarchar(max) NULL,
        [Tagline_Ar] nvarchar(max) NULL,
        [IntroText_En] nvarchar(max) NULL,
        [IntroText_Ar] nvarchar(max) NULL,
        [BrochurePdf] nvarchar(max) NULL,
        [MetaTitle_En] nvarchar(max) NULL,
        [MetaTitle_Ar] nvarchar(max) NULL,
        [MetaDescription_En] nvarchar(max) NULL,
        [MetaDescription_Ar] nvarchar(max) NULL,
        CONSTRAINT [PK_Vehicles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [ContentSections] (
        [Id] int NOT NULL IDENTITY,
        [ContentPageId] int NOT NULL,
        [Heading_En] nvarchar(max) NULL,
        [Heading_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL,
        [Body_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ContentSections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContentSections_ContentPages_ContentPageId] FOREIGN KEY ([ContentPageId]) REFERENCES [ContentPages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [HeroSlides] (
        [Id] int NOT NULL IDENTITY,
        [HomePageId] int NOT NULL,
        [ImagePath] nvarchar(300) NOT NULL,
        [Heading_En] nvarchar(max) NULL,
        [Heading_Ar] nvarchar(max) NULL,
        [Subheading_En] nvarchar(max) NULL,
        [Subheading_Ar] nvarchar(max) NULL,
        [CtaText_En] nvarchar(max) NULL,
        [CtaText_Ar] nvarchar(max) NULL,
        [CtaLink] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_HeroSlides] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_HeroSlides_HomePages_HomePageId] FOREIGN KEY ([HomePageId]) REFERENCES [HomePages] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [ColorOptions] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Name_En] nvarchar(max) NULL,
        [Name_Ar] nvarchar(max) NULL,
        [Hex] nvarchar(9) NOT NULL,
        [ImagePath] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_ColorOptions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ColorOptions_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [FeatureSections] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Heading_En] nvarchar(max) NULL,
        [Heading_Ar] nvarchar(max) NULL,
        [Body_En] nvarchar(max) NULL,
        [Body_Ar] nvarchar(max) NULL,
        [ImagePath] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_FeatureSections] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_FeatureSections_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [Leads] (
        [Id] int NOT NULL IDENTITY,
        [FormType] int NOT NULL,
        [Status] int NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Phone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Message] nvarchar(max) NULL,
        [VehicleId] int NULL,
        [PreferredDate] date NULL,
        [SourcePage] nvarchar(max) NULL,
        [Branch] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_Leads] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Leads_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [SpecGroups] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Title_En] nvarchar(max) NULL,
        [Title_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SpecGroups] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SpecGroups_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [Trims] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Name_En] nvarchar(max) NULL,
        [Name_Ar] nvarchar(max) NULL,
        [Price] decimal(18,2) NULL,
        [Highlights_En] nvarchar(max) NULL,
        [Highlights_Ar] nvarchar(max) NULL,
        [SpecPdf] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_Trims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Trims_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [VehicleImages] (
        [Id] int NOT NULL IDENTITY,
        [VehicleId] int NOT NULL,
        [Kind] int NOT NULL,
        [Path] nvarchar(300) NOT NULL,
        [Alt_En] nvarchar(max) NULL,
        [Alt_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_VehicleImages] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VehicleImages_Vehicles_VehicleId] FOREIGN KEY ([VehicleId]) REFERENCES [Vehicles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE TABLE [SpecRows] (
        [Id] int NOT NULL IDENTITY,
        [SpecGroupId] int NOT NULL,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [Value_En] nvarchar(max) NULL,
        [Value_Ar] nvarchar(max) NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_SpecRows] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SpecRows_SpecGroups_SpecGroupId] FOREIGN KEY ([SpecGroupId]) REFERENCES [SpecGroups] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_ColorOptions_VehicleId] ON [ColorOptions] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ContentPages_Slug] ON [ContentPages] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_ContentSections_ContentPageId] ON [ContentSections] ([ContentPageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_FeatureSections_VehicleId] ON [FeatureSections] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_FormPages_Slug] ON [FormPages] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_HeroSlides_HomePageId] ON [HeroSlides] ([HomePageId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_Leads_CreatedAt] ON [Leads] ([CreatedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_Leads_Status] ON [Leads] ([Status]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_Leads_VehicleId] ON [Leads] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_MenuItems_ParentId] ON [MenuItems] ([ParentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_NewsArticles_Slug] ON [NewsArticles] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Offers_Slug] ON [Offers] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_SpecGroups_VehicleId] ON [SpecGroups] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_SpecRows_SpecGroupId] ON [SpecRows] ([SpecGroupId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_Trims_VehicleId] ON [Trims] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE INDEX [IX_VehicleImages_VehicleId] ON [VehicleImages] ([VehicleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Vehicles_Slug] ON [Vehicles] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260614193458_AddContentModel'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260614193458_AddContentModel', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [BodyHtml_Ar] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [BodyHtml_En] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [FormPages] ADD [BodyHtml_Ar] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [FormPages] ADD [BodyHtml_En] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [ContentPages] ADD [BodyHtml_Ar] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    ALTER TABLE [ContentPages] ADD [BodyHtml_En] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615104433_AddBodyHtml'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615104433_AddBodyHtml', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615173920_AddFeatureLayout'
)
BEGIN
    ALTER TABLE [FeatureSections] ADD [Layout] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260615173920_AddFeatureLayout'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260615173920_AddFeatureLayout', N'9.0.6');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621103724_AddSpecPdfAndDock'
)
BEGIN
    ALTER TABLE [Vehicles] ADD [SpecPdf] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621103724_AddSpecPdfAndDock'
)
BEGIN
    CREATE TABLE [DockItems] (
        [Id] int NOT NULL IDENTITY,
        [Label_En] nvarchar(max) NULL,
        [Label_Ar] nvarchar(max) NULL,
        [ShortLabel_En] nvarchar(max) NULL,
        [ShortLabel_Ar] nvarchar(max) NULL,
        [Url] nvarchar(300) NULL,
        [Icon] nvarchar(50) NOT NULL,
        [LinkType] int NOT NULL,
        [IsVisible] bit NOT NULL,
        [SortOrder] int NOT NULL,
        CONSTRAINT [PK_DockItems] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260621103724_AddSpecPdfAndDock'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260621103724_AddSpecPdfAndDock', N'9.0.6');
END;

COMMIT;
GO

