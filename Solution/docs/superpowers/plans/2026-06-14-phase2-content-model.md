# GAC CMS — Phase 2: Content Model Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Define the full bilingual content model (entities + `LocalizedText` owned type), wire it into `ApplicationDbContext`, create + apply the migration to the live `GAC` DB, and seed the EN structural baseline (site settings, menu, home page, the 11 vehicles' core fields, content pages, form pages, sample news/offers).

**Architecture:** Domain entities live in `GAC.Core` (POCO classes, no EF attributes beyond a `[Flags]` enum). EF mapping (owned `LocalizedText`, relationships, indexes, delete behaviors) lives in `GAC.Infrastructure` via `IEntityTypeConfiguration<T>` classes. `int` identity PKs throughout (avoids the Guid collection-nav Add trap). A `ContentSeeder` (idempotent) populates EN baseline data and is invoked at startup after `DbSeeder`. No service/repository layer yet — Phase 3 adds read services when rendering consumes the data.

**Tech Stack:** .NET 9, EF Core 9 (SqlServer), xUnit.

**Reference paths:**
- Solution root: `C:\Users\anas-\source\repos\GAC\Solution` (run commands here)
- Spec: `Solution/docs/superpowers/specs/2026-06-14-gac-cms-bilingual-design.md`
- Phase 1 plan (context): `Solution/docs/superpowers/plans/2026-06-14-phase1-foundation.md`
- Static reference (for seed values): `../HTML/index.html`, `../HTML/partials/header.html`, `../HTML/partials/footer.html`

**Conventions:**
- Pin any new `Microsoft.*`/EF package to `9.0.*`.
- Real connection string is in the gitignored `GAC.Web/appsettings.Development.json`; `dotnet ef` against `--startup-project GAC.Web` must run in the Development environment to pick it up. Set it explicitly for EF commands: prefix with `DOTNET_ENVIRONMENT=Development` (bash) so the design-time factory/Program reads the real connection string. If `dotnet ef` cannot connect, verify `appsettings.Development.json` exists and has `ConnectionStrings:Default`.
- App does NOT auto-migrate; migrations applied explicitly.
- Commits: per task, `-c user.name="anas-nammas" -c user.email="anas-nammas@live.com"`, message ending with a blank line then `Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>`.

---

### Task 1: `LocalizedText` owned value type (TDD)

**Files:**
- Create: `Solution/GAC.Core/Content/LocalizedText.cs`
- Test: `Solution/GAC.Tests/LocalizedTextTests.cs`

- [ ] **Step 1: Write the failing test**

`GAC.Tests/LocalizedTextTests.cs`:
```csharp
using GAC.Core.Content;
using Xunit;

namespace GAC.Tests;

public class LocalizedTextTests
{
    [Fact]
    public void Get_ReturnsArabic_ForArCulture()
    {
        var t = new LocalizedText { En = "Home", Ar = "الرئيسية" };
        Assert.Equal("الرئيسية", t.Get("ar"));
    }

    [Fact]
    public void Get_ReturnsEnglish_ForEnCulture()
    {
        var t = new LocalizedText { En = "Home", Ar = "الرئيسية" };
        Assert.Equal("Home", t.Get("en"));
    }

    [Fact]
    public void Get_FallsBackToEnglish_WhenArabicMissing()
    {
        var t = new LocalizedText { En = "Warranty", Ar = null };
        Assert.Equal("Warranty", t.Get("ar"));
    }

    [Fact]
    public void Get_FallsBackToArabic_WhenEnglishMissing()
    {
        var t = new LocalizedText { En = null, Ar = "عربي" };
        Assert.Equal("عربي", t.Get("en"));
    }

    [Fact]
    public void Get_ReturnsEmptyString_WhenBothNull()
    {
        var t = new LocalizedText();
        Assert.Equal(string.Empty, t.Get("en"));
    }

    [Fact]
    public void ImplicitFromString_SetsEnglish()
    {
        LocalizedText t = "Hello";
        Assert.Equal("Hello", t.En);
        Assert.Null(t.Ar);
    }
}
```

- [ ] **Step 2: Run, confirm FAIL**

Run: `dotnet test GAC.Tests --filter LocalizedTextTests`
Expected: compile failure — `LocalizedText` does not exist.

- [ ] **Step 3: Implement**

`GAC.Core/Content/LocalizedText.cs`:
```csharp
namespace GAC.Core.Content;

/// <summary>
/// A bilingual string (English + Arabic). Mapped as an EF owned type to
/// {Field}_En / {Field}_Ar columns. Exactly two languages, so no translation tables.
/// </summary>
public class LocalizedText
{
    public string? En { get; set; }
    public string? Ar { get; set; }

    /// <summary>Returns the value for the given two-letter culture, falling back to the other language, then empty.</summary>
    public string Get(string culture)
    {
        var primary = culture == "ar" ? Ar : En;
        return primary ?? En ?? Ar ?? string.Empty;
    }

    // Convenience for seeding/assignment: `LocalizedText t = "Hello";`
    public static implicit operator LocalizedText(string en) => new() { En = en };
}
```

- [ ] **Step 4: Run, confirm PASS**

Run: `dotnet test GAC.Tests --filter LocalizedTextTests`
Expected: 6 tests pass.

- [ ] **Step 5: Commit** — `feat: add LocalizedText bilingual value type (TDD)`

---

### Task 2: Enums

**Files:**
- Create: `Solution/GAC.Core/Content/VehicleCategory.cs`
- Create: `Solution/GAC.Core/Content/FormType.cs`
- Create: `Solution/GAC.Core/Content/LeadStatus.cs`

- [ ] **Step 1: Create the enums**

`GAC.Core/Content/VehicleCategory.cs`:
```csharp
namespace GAC.Core.Content;

/// <summary>Body/drivetrain categories. Flags because a model can be e.g. SUV + EV (AION V).</summary>
[Flags]
public enum VehicleCategory
{
    None = 0,
    Sedan = 1,
    Suv = 2,
    Ev = 4
}
```

`GAC.Core/Content/FormType.cs`:
```csharp
namespace GAC.Core.Content;

/// <summary>Which coded form a FormPage renders, and which kind of Lead a submission creates.</summary>
public enum FormType
{
    Contact = 0,
    TestDrive = 1,
    ServiceBooking = 2,
    Quote = 3,
    Fleet = 4,
    RecallEnquiry = 5
}
```

`GAC.Core/Content/LeadStatus.cs`:
```csharp
namespace GAC.Core.Content;

public enum LeadStatus
{
    New = 0,
    Contacted = 1,
    Closed = 2
}
```

- [ ] **Step 2: Build** — `dotnet build GAC.sln` → succeeds.
- [ ] **Step 3: Commit** — `feat: add VehicleCategory, FormType, LeadStatus enums`

---

### Task 3: Vehicle aggregate entities

**Files:**
- Create: `Solution/GAC.Core/Content/Vehicle.cs`
- Create: `Solution/GAC.Core/Content/VehicleImage.cs`
- Create: `Solution/GAC.Core/Content/Trim.cs`
- Create: `Solution/GAC.Core/Content/SpecGroup.cs`
- Create: `Solution/GAC.Core/Content/SpecRow.cs`
- Create: `Solution/GAC.Core/Content/ColorOption.cs`
- Create: `Solution/GAC.Core/Content/FeatureSection.cs`

- [ ] **Step 1: Create the entities**

`GAC.Core/Content/Vehicle.cs`:
```csharp
namespace GAC.Core.Content;

public class Vehicle
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";          // e.g. "gs8" → routes to /gs8 (Phase 3)
    public VehicleCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public decimal? PriceFrom { get; set; }          // SAR; null when "price on request"

    public LocalizedText Name { get; set; } = new();
    public LocalizedText Tagline { get; set; } = new();
    public LocalizedText IntroText { get; set; } = new();

    public string? BrochurePdf { get; set; }         // path under /pdfs or /assets

    // SEO (localized)
    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();

    public List<VehicleImage> Images { get; set; } = new();      // hero + gallery (distinguished by Kind)
    public List<Trim> Trims { get; set; } = new();
    public List<SpecGroup> SpecGroups { get; set; } = new();
    public List<ColorOption> Colors { get; set; } = new();
    public List<FeatureSection> Features { get; set; } = new();
}
```

`GAC.Core/Content/VehicleImage.cs`:
```csharp
namespace GAC.Core.Content;

public enum VehicleImageKind { Hero = 0, Gallery = 1 }

public class VehicleImage
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public VehicleImageKind Kind { get; set; }
    public string Path { get; set; } = "";           // e.g. /assets/img/hero-gs4.jpg
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/Trim.cs`:
```csharp
namespace GAC.Core.Content;

public class Trim
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Name { get; set; } = new();
    public decimal? Price { get; set; }              // SAR; null = "on request"
    public LocalizedText Highlights { get; set; } = new();
    public string? SpecPdf { get; set; }
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/SpecGroup.cs`:
```csharp
namespace GAC.Core.Content;

public class SpecGroup
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public int SortOrder { get; set; }
    public List<SpecRow> Rows { get; set; } = new();
}
```

`GAC.Core/Content/SpecRow.cs`:
```csharp
namespace GAC.Core.Content;

public class SpecRow
{
    public int Id { get; set; }
    public int SpecGroupId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Value { get; set; } = new();
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/ColorOption.cs`:
```csharp
namespace GAC.Core.Content;

public class ColorOption
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Name { get; set; } = new();
    public string Hex { get; set; } = "#000000";
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/FeatureSection.cs`:
```csharp
namespace GAC.Core.Content;

public class FeatureSection
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: Build** → succeeds.
- [ ] **Step 3: Commit** — `feat: add Vehicle aggregate entities (trims, specs, colors, features, images)`

---

### Task 4: Page / news / offer entities

**Files:**
- Create: `Solution/GAC.Core/Content/ContentPage.cs`
- Create: `Solution/GAC.Core/Content/ContentSection.cs`
- Create: `Solution/GAC.Core/Content/FormPage.cs`
- Create: `Solution/GAC.Core/Content/NewsArticle.cs`
- Create: `Solution/GAC.Core/Content/Offer.cs`

- [ ] **Step 1: Create the entities**

`GAC.Core/Content/ContentPage.cs`:
```csharp
namespace GAC.Core.Content;

public class ContentPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();
    public List<ContentSection> Sections { get; set; } = new();
}
```

`GAC.Core/Content/ContentSection.cs`:
```csharp
namespace GAC.Core.Content;

public class ContentSection
{
    public int Id { get; set; }
    public int ContentPageId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();   // rich text (HTML)
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/FormPage.cs`:
```csharp
namespace GAC.Core.Content;

public class FormPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public FormType FormType { get; set; }
    public bool IsVisible { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();
}
```

`GAC.Core/Content/NewsArticle.cs`:
```csharp
namespace GAC.Core.Content;

public class NewsArticle
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public DateOnly PublishedOn { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Excerpt { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/Offer.cs`:
```csharp
namespace GAC.Core.Content;

public class Offer
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 2: Build** → succeeds.
- [ ] **Step 3: Commit** — `feat: add ContentPage/Section, FormPage, NewsArticle, Offer entities`

---

### Task 5: Lead, HomePage, SiteSettings, MenuItem, MediaAsset

**Files:**
- Create: `Solution/GAC.Core/Content/Lead.cs`
- Create: `Solution/GAC.Core/Content/HomePage.cs`
- Create: `Solution/GAC.Core/Content/HeroSlide.cs`
- Create: `Solution/GAC.Core/Content/SiteSettings.cs`
- Create: `Solution/GAC.Core/Content/MenuItem.cs`
- Create: `Solution/GAC.Core/Content/MediaAsset.cs`

- [ ] **Step 1: Create the entities**

`GAC.Core/Content/Lead.cs`:
```csharp
namespace GAC.Core.Content;

public class Lead
{
    public int Id { get; set; }
    public FormType FormType { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public int? VehicleId { get; set; }              // optional related model
    public Vehicle? Vehicle { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public string? SourcePage { get; set; }
    public string? Branch { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
```

`GAC.Core/Content/HomePage.cs` (singleton, Id always 1):
```csharp
namespace GAC.Core.Content;

public class HomePage
{
    public int Id { get; set; }                      // fixed to 1
    public List<HeroSlide> Slides { get; set; } = new();
}
```

`GAC.Core/Content/HeroSlide.cs`:
```csharp
namespace GAC.Core.Content;

public class HeroSlide
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Subheading { get; set; } = new();
    public LocalizedText CtaText { get; set; } = new();
    public string? CtaLink { get; set; }
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/SiteSettings.cs` (singleton, Id always 1):
```csharp
namespace GAC.Core.Content;

public class SiteSettings
{
    public int Id { get; set; }                      // fixed to 1
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? SnapchatUrl { get; set; }
    public string? XUrl { get; set; }
    public LocalizedText FooterTagline { get; set; } = new();
}
```

`GAC.Core/Content/MenuItem.cs`:
```csharp
namespace GAC.Core.Content;

public class MenuItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }               // null = top-level
    public MenuItem? Parent { get; set; }
    public List<MenuItem> Children { get; set; } = new();
    public LocalizedText Label { get; set; } = new();
    public string? Url { get; set; }                 // null/"#" = group toggle only
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/MediaAsset.cs`:
```csharp
namespace GAC.Core.Content;

public class MediaAsset
{
    public int Id { get; set; }
    public string Path { get; set; } = "";           // under wwwroot storage root
    public string? OriginalFileName { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public DateTimeOffset UploadedAt { get; set; }
}
```

- [ ] **Step 2: Build** → succeeds.
- [ ] **Step 3: Commit** — `feat: add Lead, HomePage/HeroSlide, SiteSettings, MenuItem, MediaAsset entities`

---

### Task 6: DbContext DbSets + EF configurations

**Files:**
- Modify: `Solution/GAC.Infrastructure/Data/ApplicationDbContext.cs`
- Create: `Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`

- [ ] **Step 1: Add DbSets + apply configurations in `ApplicationDbContext`**

Replace `ApplicationDbContext.cs` with:
```csharp
using GAC.Core.Content;
using GAC.Core.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace GAC.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Trim> Trims => Set<Trim>();
    public DbSet<SpecGroup> SpecGroups => Set<SpecGroup>();
    public DbSet<SpecRow> SpecRows => Set<SpecRow>();
    public DbSet<ColorOption> ColorOptions => Set<ColorOption>();
    public DbSet<FeatureSection> FeatureSections => Set<FeatureSection>();
    public DbSet<VehicleImage> VehicleImages => Set<VehicleImage>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<ContentSection> ContentSections => Set<ContentSection>();
    public DbSet<FormPage> FormPages => Set<FormPage>();
    public DbSet<NewsArticle> NewsArticles => Set<NewsArticle>();
    public DbSet<Offer> Offers => Set<Offer>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<HomePage> HomePages => Set<HomePage>();
    public DbSet<HeroSlide> HeroSlides => Set<HeroSlide>();
    public DbSet<SiteSettings> SiteSettings => Set<SiteSettings>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);   // IMPORTANT: keep — configures Identity
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

- [ ] **Step 2: Create the EF configurations**

`GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`. Every `LocalizedText` property is mapped with `OwnsOne` so it becomes `{Field}_En` / `{Field}_Ar` nullable columns. Unique index on each `Slug`. Cascade delete from a Vehicle to its children; restrict Lead→Vehicle (don't delete leads when a vehicle is removed).

```csharp
using GAC.Core.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GAC.Infrastructure.Data.Configurations;

// Helper to keep the OwnsOne calls terse.
internal static class OwnedExtensions
{
    public static void OwnsLocalized<TEntity>(
        this EntityTypeBuilder<TEntity> b,
        System.Linq.Expressions.Expression<System.Func<TEntity, LocalizedText?>> nav)
        where TEntity : class
    {
        b.OwnsOne(nav, o =>
        {
            o.Property(p => p.En);
            o.Property(p => p.Ar);
        });
        b.Navigation(nav).IsRequired();   // owned reference always present (columns nullable)
    }
}

public class VehicleConfig : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.HasIndex(v => v.Slug).IsUnique();
        b.Property(v => v.Slug).HasMaxLength(100).IsRequired();
        b.Property(v => v.PriceFrom).HasColumnType("decimal(18,2)");
        b.OwnsLocalized(v => v.Name);
        b.OwnsLocalized(v => v.Tagline);
        b.OwnsLocalized(v => v.IntroText);
        b.OwnsLocalized(v => v.MetaTitle);
        b.OwnsLocalized(v => v.MetaDescription);
        b.HasMany(v => v.Images).WithOne().HasForeignKey(i => i.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Trims).WithOne().HasForeignKey(t => t.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.SpecGroups).WithOne().HasForeignKey(s => s.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Colors).WithOne().HasForeignKey(c => c.VehicleId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(v => v.Features).WithOne().HasForeignKey(f => f.VehicleId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class VehicleImageConfig : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> b)
    {
        b.Property(i => i.Path).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(i => i.Alt);
    }
}

public class TrimConfig : IEntityTypeConfiguration<Trim>
{
    public void Configure(EntityTypeBuilder<Trim> b)
    {
        b.Property(t => t.Price).HasColumnType("decimal(18,2)");
        b.OwnsLocalized(t => t.Name);
        b.OwnsLocalized(t => t.Highlights);
    }
}

public class SpecGroupConfig : IEntityTypeConfiguration<SpecGroup>
{
    public void Configure(EntityTypeBuilder<SpecGroup> b)
    {
        b.OwnsLocalized(s => s.Title);
        b.HasMany(s => s.Rows).WithOne().HasForeignKey(r => r.SpecGroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class SpecRowConfig : IEntityTypeConfiguration<SpecRow>
{
    public void Configure(EntityTypeBuilder<SpecRow> b)
    {
        b.OwnsLocalized(r => r.Label);
        b.OwnsLocalized(r => r.Value);
    }
}

public class ColorOptionConfig : IEntityTypeConfiguration<ColorOption>
{
    public void Configure(EntityTypeBuilder<ColorOption> b)
    {
        b.Property(c => c.Hex).HasMaxLength(9);
        b.OwnsLocalized(c => c.Name);
    }
}

public class FeatureSectionConfig : IEntityTypeConfiguration<FeatureSection>
{
    public void Configure(EntityTypeBuilder<FeatureSection> b)
    {
        b.OwnsLocalized(f => f.Heading);
        b.OwnsLocalized(f => f.Body);
    }
}

public class ContentPageConfig : IEntityTypeConfiguration<ContentPage>
{
    public void Configure(EntityTypeBuilder<ContentPage> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        b.OwnsLocalized(p => p.Title);
        b.OwnsLocalized(p => p.MetaTitle);
        b.OwnsLocalized(p => p.MetaDescription);
        b.HasMany(p => p.Sections).WithOne().HasForeignKey(s => s.ContentPageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContentSectionConfig : IEntityTypeConfiguration<ContentSection>
{
    public void Configure(EntityTypeBuilder<ContentSection> b)
    {
        b.OwnsLocalized(s => s.Heading);
        b.OwnsLocalized(s => s.Body);
    }
}

public class FormPageConfig : IEntityTypeConfiguration<FormPage>
{
    public void Configure(EntityTypeBuilder<FormPage> b)
    {
        b.HasIndex(p => p.Slug).IsUnique();
        b.Property(p => p.Slug).HasMaxLength(100).IsRequired();
        b.OwnsLocalized(p => p.Title);
        b.OwnsLocalized(p => p.IntroText);
        b.OwnsLocalized(p => p.MetaTitle);
        b.OwnsLocalized(p => p.MetaDescription);
    }
}

public class NewsArticleConfig : IEntityTypeConfiguration<NewsArticle>
{
    public void Configure(EntityTypeBuilder<NewsArticle> b)
    {
        b.HasIndex(n => n.Slug).IsUnique();
        b.Property(n => n.Slug).HasMaxLength(120).IsRequired();
        b.OwnsLocalized(n => n.Title);
        b.OwnsLocalized(n => n.Excerpt);
        b.OwnsLocalized(n => n.Body);
    }
}

public class OfferConfig : IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<Offer> b)
    {
        b.HasIndex(o => o.Slug).IsUnique();
        b.Property(o => o.Slug).HasMaxLength(120).IsRequired();
        b.OwnsLocalized(o => o.Title);
        b.OwnsLocalized(o => o.Body);
    }
}

public class LeadConfig : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> b)
    {
        b.Property(l => l.Name).HasMaxLength(200).IsRequired();
        b.HasOne(l => l.Vehicle).WithMany().HasForeignKey(l => l.VehicleId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(l => l.Status);
        b.HasIndex(l => l.CreatedAt);
    }
}

public class HomePageConfig : IEntityTypeConfiguration<HomePage>
{
    public void Configure(EntityTypeBuilder<HomePage> b)
    {
        b.HasMany(h => h.Slides).WithOne().HasForeignKey(s => s.HomePageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class HeroSlideConfig : IEntityTypeConfiguration<HeroSlide>
{
    public void Configure(EntityTypeBuilder<HeroSlide> b)
    {
        b.Property(s => s.ImagePath).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(s => s.Heading);
        b.OwnsLocalized(s => s.Subheading);
        b.OwnsLocalized(s => s.CtaText);
    }
}

public class SiteSettingsConfig : IEntityTypeConfiguration<SiteSettings>
{
    public void Configure(EntityTypeBuilder<SiteSettings> b)
    {
        b.OwnsLocalized(s => s.FooterTagline);
    }
}

public class MenuItemConfig : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> b)
    {
        b.OwnsLocalized(m => m.Label);
        b.HasMany(m => m.Children).WithOne(m => m.Parent!).HasForeignKey(m => m.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class MediaAssetConfig : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> b)
    {
        b.Property(m => m.Path).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(m => m.Alt);
    }
}
```

- [ ] **Step 3: Build** — `dotnet build GAC.sln` → succeeds. If a `LocalizedText` property in any entity is declared non-nullable (it is `= new()`), the `OwnsLocalized` + `Navigation().IsRequired()` pattern maps it correctly. Fix any model-build error reported.

- [ ] **Step 4: Quick model-validity test (TDD safety net)**

Create `Solution/GAC.Tests/DbContextModelTests.cs`:
```csharp
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests;

public class DbContextModelTests
{
    [Fact]
    public void Model_Builds_WithAllContentEntities()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=.;Database=_design;TrustServerCertificate=True") // not connected; model only
            .Options;
        using var ctx = new ApplicationDbContext(options);

        // Accessing the model forces OnModelCreating to run and validates the mapping.
        var entityCount = ctx.Model.GetEntityTypes().Count();
        Assert.True(entityCount > 20); // Identity (7) + content entities + owned types
        Assert.NotNull(ctx.Model.FindEntityType(typeof(GAC.Core.Content.Vehicle)));
    }
}
```
Run: `dotnet test GAC.Tests --filter DbContextModelTests` → passes (it does NOT touch a database; building the model is in-memory).

- [ ] **Step 5: Commit** — `feat: wire content DbSets and EF owned-type/relationship configurations`

---

### Task 7: Migration + apply to GAC DB

**Files:**
- Create: `Solution/GAC.Infrastructure/Migrations/*_AddContentModel.cs` (generated)

- [ ] **Step 1: Add the migration**
```
dotnet ef migrations add AddContentModel --project GAC.Infrastructure --startup-project GAC.Web --output-dir Migrations
```
Inspect the generated `Up()`: it must create tables `Vehicles`, `Trims`, `SpecGroups`, `SpecRows`, `ColorOptions`, `FeatureSections`, `VehicleImages`, `ContentPages`, `ContentSections`, `FormPages`, `NewsArticles`, `Offers`, `Leads`, `HomePages`, `HeroSlides`, `SiteSettings`, `MenuItems`, `MediaAssets`, each with `{Field}_En`/`{Field}_Ar` columns for localized fields, a unique index on each `Slug`, and the FKs. No changes to the Identity tables.

- [ ] **Step 2: Apply to the GAC database**
```
DOTNET_ENVIRONMENT=Development dotnet ef database update --project GAC.Infrastructure --startup-project GAC.Web
```
Expected: `Done.` (connects via the Development connection string). If it can't connect, confirm `GAC.Web/appsettings.Development.json` has `ConnectionStrings:Default`.

- [ ] **Step 3: Verify applied**
```
DOTNET_ENVIRONMENT=Development dotnet ef migrations list --project GAC.Infrastructure --startup-project GAC.Web
```
Both `InitialIdentity` and `AddContentModel` listed, neither pending.

- [ ] **Step 4: Commit** — `feat: migration for content model; applied to GAC DB`

---

### Task 8: EN content seeder + tests

**Files:**
- Create: `Solution/GAC.Infrastructure/Data/ContentSeeder.cs`
- Modify: `Solution/GAC.Web/Program.cs` (invoke ContentSeeder after DbSeeder)
- Test: `Solution/GAC.Tests/ContentSeederTests.cs`

The seeder is **idempotent** (guard each section: only insert when the table/singleton is empty) and seeds **EN** values (Arabic filled in Phase 4). Values are taken from the static reference (`../HTML`). Seed data:

- **SiteSettings** (Id 1): Phone `1833334`, WhatsApp `1833334`, social URLs from `footer.html` (use the hrefs present there; if a platform URL is absent in the footer, leave null), FooterTagline.En = "GAC Mutawa Alkadi Automotive".
- **Menu** (top-level + children, EN labels, Urls as the current `.html` stubs — Phase 3 swaps to clean routes): Home (`/`), Models (`models.html`), Owners (group: Book a Service `book-a-service.html`, Cost of Service `cost-of-service.html`, Warranty `warranty.html`, Recall `recall-enquiry.html`, Road-Side Assistance `road-assistance.html`), Shopping Tools (group: Book a Test Drive `book-a-test-drive.html`, Request a Quote `request-a-quote.html`), Locations (`contact-us.html`), More (group: Fleet Sales `fleet.html`, Finance `finance.html`).
- **HomePage** (Id 1) with HeroSlides for the 9 home hero images (ImagePath `/assets/img/hero-*.jpg|png`, Heading from the model name, CtaLink to the model `.html` where applicable): hero-s7, hero-gs4, hero-hyptec-ht, hero-aion-v, hero-aion-es, hero-empow-sport, hero-gs8-traveller, hero-m8, hero-gs3-emzoom.
- **Vehicles** — the 11 models with core fields (Slug, Category, SortOrder, IsVisible, Name.En, hero VehicleImage). AION V (`aion-v`) and AION ES (`aion-es`) have `IsVisible = false` (they are commented-out/hidden in the live megamenu). Use this exact list:

| Slug | Name.En | Category | IsVisible | Hero image |
|---|---|---|---|---|
| gs8traveller | GS8 Traveller | Suv | true | /assets/img/hero-gs8-traveller.png |
| gs8 | GS8 | Suv | true | /assets/img/m-gs8.jpg |
| gs3emzoom | EMZOOM | Suv | true | /assets/img/hero-gs3-emzoom.jpg |
| emkoo | EMKOO | Suv | true | /assets/img/m-emkoo.png |
| empow | EMPOW | Sedan | true | /assets/img/m-empow.png |
| m8 | M8 | Suv | true | /assets/img/hero-m8.png |
| empow-sport | EMPOW R | Sedan | true | /assets/img/hero-empow-sport.jpg |
| aion-v | AION V | Suv \| Ev | false | /assets/img/hero-aion-v.jpg |
| aion-es | AION ES | Sedan \| Ev | false | /assets/img/hero-aion-es.jpg |
| hyptec-ht | HYPTEC HT | Suv \| Ev | true | /assets/img/m-hyptec-ht.png |
| gs4 | GS4 MAX | Suv | true | /assets/img/hero-gs4.jpg |

(SortOrder = row order above, starting at 1. Trims/specs/colors/features are NOT seeded here — populated in Phase 3 per detail template. PriceFrom = null.)

- **ContentPages** (Slug + Title.En; empty Sections for now): about ("About Us"), warranty ("Warranty"), privacy-policy ("Privacy Policy"), finance ("Tayseer Finance"), cost-of-service ("Cost of Service"), road-assistance ("Roadside Assistance"), news ("News"), offers ("Offers").
- **FormPages** (Slug, FormType, Title.En): book-a-service (ServiceBooking, "Book a Service"), book-a-test-drive (TestDrive, "Book a Test Drive"), request-a-quote (Quote, "Request a Quote"), contact-us (Contact, "Locate Us"), fleet (Fleet, "Fleet"), recall-enquiry (RecallEnquiry, "Recall Enquiry").
- **NewsArticles** (2-3 from the home news cards, EN): use the headings/images present in `../HTML/index.html` news section (e.g. images `/assets/img/news-empow.jpg`, `/assets/img/news-training.jpg`, `/assets/img/news-gs3-award.jpg`). Slugs kebab-cased from titles. PublishedOn = a fixed date passed in (see idempotency/date note).
- **Offers** (1-2 placeholders, EN): e.g. slug `current-offers`, Title.En "Current Offers".

> **Date note:** `Date.now`/`DateTimeOffset.Now` are fine in the seeder (it runs at app startup, not in a workflow). Use `DateTimeOffset.UtcNow` for `Lead`/`MediaAsset` timestamps if needed and `DateOnly.FromDateTime(DateTime.UtcNow)` for news/offer dates, OR hardcode sensible fixed dates (e.g. `new DateOnly(2026, 1, 1)`) — hardcoding keeps the seed deterministic. Prefer hardcoded dates.

- [ ] **Step 1: Implement `ContentSeeder`**

`GAC.Infrastructure/Data/ContentSeeder.cs` — a static `SeedAsync(IServiceProvider services)` that resolves `ApplicationDbContext` and inserts each section only when empty. Use `LocalizedText` for localized fields (EN only). Example skeleton (fill ALL sections per the data above):
```csharp
using GAC.Core.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Infrastructure.Data;

public static class ContentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        if (!await db.SiteSettings.AnyAsync())
        {
            db.SiteSettings.Add(new SiteSettings
            {
                Id = 1,
                Phone = "1833334",
                WhatsApp = "1833334",
                FooterTagline = "GAC Mutawa Alkadi Automotive"
                // social URLs from footer.html
            });
        }

        if (!await db.Vehicles.AnyAsync())
        {
            var vehicles = new (string slug, string name, VehicleCategory cat, bool vis, string hero)[]
            {
                ("gs8traveller","GS8 Traveller",VehicleCategory.Suv,true,"/assets/img/hero-gs8-traveller.png"),
                ("gs8","GS8",VehicleCategory.Suv,true,"/assets/img/m-gs8.jpg"),
                ("gs3emzoom","EMZOOM",VehicleCategory.Suv,true,"/assets/img/hero-gs3-emzoom.jpg"),
                ("emkoo","EMKOO",VehicleCategory.Suv,true,"/assets/img/m-emkoo.png"),
                ("empow","EMPOW",VehicleCategory.Sedan,true,"/assets/img/m-empow.png"),
                ("m8","M8",VehicleCategory.Suv,true,"/assets/img/hero-m8.png"),
                ("empow-sport","EMPOW R",VehicleCategory.Sedan,true,"/assets/img/hero-empow-sport.jpg"),
                ("aion-v","AION V",VehicleCategory.Suv|VehicleCategory.Ev,false,"/assets/img/hero-aion-v.jpg"),
                ("aion-es","AION ES",VehicleCategory.Sedan|VehicleCategory.Ev,false,"/assets/img/hero-aion-es.jpg"),
                ("hyptec-ht","HYPTEC HT",VehicleCategory.Suv|VehicleCategory.Ev,true,"/assets/img/m-hyptec-ht.png"),
                ("gs4","GS4 MAX",VehicleCategory.Suv,true,"/assets/img/hero-gs4.jpg"),
            };
            var order = 1;
            foreach (var v in vehicles)
            {
                db.Vehicles.Add(new Vehicle
                {
                    Slug = v.slug,
                    Name = v.name,
                    Category = v.cat,
                    IsVisible = v.vis,
                    SortOrder = order++,
                    Images = { new VehicleImage { Kind = VehicleImageKind.Hero, Path = v.hero, SortOrder = 0 } }
                });
            }
        }

        // ... HomePage + HeroSlides, Menu (top-level + children), ContentPages,
        //     FormPages, NewsArticles, Offers — each guarded by an AnyAsync() check.

        await db.SaveChangesAsync();
    }
}
```
Implement EVERY section listed in the seed data above (not just the two shown). Each section guarded so re-running is a no-op.

> **Menu nesting note:** to seed parent+children, add the parent, `SaveChangesAsync()` to get its Id, then add children with `ParentId` set — OR build the `Children` collection on a NEW (untracked) parent and `Add` the parent (EF inserts the graph). Do NOT `.Children.Add()` onto an already-tracked/saved parent in a way that relies on it (int identity keys make this safe, unlike the Guid trap, but building the graph before Add is cleanest).

- [ ] **Step 2: Invoke after DbSeeder in `Program.cs`**

In `GAC.Web/Program.cs`, update the startup seeding block:
```csharp
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
    await ContentSeeder.SeedAsync(scope.ServiceProvider);
}
```

- [ ] **Step 3: Test the seeder (idempotent + correct counts)**

`GAC.Tests/ContentSeederTests.cs` — use the EF Core InMemory provider OR SQLite in-memory so the test doesn't depend on the live DB. Add the InMemory package to the test project if not present: `dotnet add GAC.Tests package Microsoft.EntityFrameworkCore.InMemory -v 9.0.6`.
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class ContentSeederTests
{
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase("seed-test"));
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Seeds_ElevenVehicles_WithTwoHidden()
    {
        var sp = BuildServices();
        await ContentSeeder.SeedAsync(sp);

        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(11, await db.Vehicles.CountAsync());
        Assert.Equal(2, await db.Vehicles.CountAsync(v => !v.IsVisible)); // aion-v, aion-es
        Assert.True(await db.Vehicles.AnyAsync(v => v.Slug == "gs8"));
    }

    [Fact]
    public async Task IsIdempotent_RunningTwice_DoesNotDuplicate()
    {
        var sp = BuildServices();
        await ContentSeeder.SeedAsync(sp);
        await ContentSeeder.SeedAsync(sp);

        var db = sp.GetRequiredService<ApplicationDbContext>();
        Assert.Equal(11, await db.Vehicles.CountAsync());
        Assert.Equal(1, await db.SiteSettings.CountAsync());
        Assert.Equal(6, await db.FormPages.CountAsync());
    }
}
```
> Note: the InMemory provider ignores relational features (unique indexes, owned-type column mapping) but correctly exercises the seeder's insert logic and idempotency guards. The relational model itself is validated by Task 6's `DbContextModelTests` + the Task 7 migration.

Run: `dotnet test GAC.sln` → ALL pass (LocalizedText 6 + DbContextModel 1 + ContentSeeder 2 + Phase 1's CultureController 3 + HomePageSmoke 2).

- [ ] **Step 4: Run the app once to seed the live DB**
```
DOTNET_ENVIRONMENT=Development dotnet run --project GAC.Web --urls http://localhost:5080
```
Let it start (the startup seeder runs `ContentSeeder` against the live GAC DB), confirm no startup exception, then stop it. (Optional verification: the home page still renders — it's still the Phase 1 static-ported view; DB-driven rendering is Phase 3.)

- [ ] **Step 5: Commit** — `feat: EN content seeder (site settings, menu, vehicles, pages, forms, news/offers)`

---

## Phase 2 Done — Definition of Done
- `dotnet build` + `dotnet test` pass (14 tests total).
- All content entities exist in `GAC.Core`; EF owned-type mappings + relationships configured in `GAC.Infrastructure`.
- `AddContentModel` migration created and applied to the live GAC DB (all content tables present, not pending).
- `ContentSeeder` populates the EN baseline idempotently and runs at startup; the live DB has 11 vehicles (2 hidden), site settings, menu, home slides, content pages, 6 form pages, sample news/offers.
- All committed on `main`.

## Deferred (later phases)
- DB-driven rendering of home/header/footer/vehicle pages + clean routing + old-`.html` redirects → **Phase 3** (also: deep per-vehicle trim/spec/colour/feature transcription, since that's when the detail template defines the needed fields).
- Arabic (`.Ar`) values + `rtl.css` styling → **Phase 4**.
- Lead capture writes (forms POST) → **Phase 5**. Admin CRUD over all these entities → **Phase 6**.
