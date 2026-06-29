# Editable Home Sections & Form-Page Content — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the home "Latest Offers" promo block, the three home "dual" cards, and form-page banner/intro content editable from the admin panel, without losing any data already in the database and without changing the lead forms.

**Architecture:** Extend the existing single `HomePage` aggregate with a `PromoSection` (1:1, with a `PromoCampaign` bullet list) and a `DualCard` collection (fixed 3), reusing the `LocalizedText` owned-type + `_LocalizedField` + media-picker conventions. Add a nullable `BannerImagePath` to `FormPage`. One additive EF migration. Public views read from the DB with fallbacks to today's hardcoded values; seeders are write-only-when-empty.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQL Server; InMemory for tests), xUnit, Razor.

## Global Constraints

- **Data preservation:** migration is additive only — CREATE TABLE + ADD nullable COLUMN; **no DROP, no ALTER/UPDATE of existing columns or rows**. Review the generated migration before completion.
- **Write-only-when-empty:** every seed/backfill writes only where data is absent (new tables guarded by `AnyAsync()`; `BannerImagePath` set only where null/empty; `IntroText` filled only where both `En` and `Ar` are blank). Never overwrite an existing value.
- **Forms unchanged:** the `<form>…</form>` blocks in `Views/Forms/Forms/_*.cshtml` (fields, option/branch arrays, submit) stay byte-for-byte identical.
- **Bilingual:** every text field is `LocalizedText` (En/Ar); admin uses the `_LocalizedField` partial with names `"Field.En"`/`"Field.Ar"`; seed both languages from current resource strings (`SharedResource.ar.resx` for Arabic).
- **Verification uses InMemory DB** (schema built from the model) so the build/tests stay green before the real-DB migration is applied. The real-DB home smoke tests (`HomePageSmokeTests`, `DevWebApplicationFactory`) will only pass after the migration is applied on deploy — do not run them as a gate during this work.
- **Admin conventions:** `[Area("Admin")]`, `[Authorize(Policy = AdminPolicies.ContentEditor)]`, `[AutoValidateAntiforgeryToken]`; image fields use `data-media-input` + `data-media-pick` + one `<partial name="_PickerModal" />`; redirects after save use an explicit `new { area = "Admin" }` (see the News/Offers fix) — but this feature mostly uses single-page editors, so prefer redirecting back to `nameof(Index)` with `new { area = "Admin" }`.

---

### Task 1: Domain entities + EF mapping + DbSets

**Files:**
- Create: `GAC.Core/Content/PromoSection.cs`, `GAC.Core/Content/PromoCampaign.cs`, `GAC.Core/Content/DualCard.cs`
- Modify: `GAC.Core/Content/HomePage.cs` (add `Promo`, `DualCards`)
- Modify: `GAC.Core/Content/FormPage.cs` (add `BannerImagePath`)
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (add 3 configs; extend `HomePageConfig` + `FormPageConfig`)
- Modify: `GAC.Infrastructure/Data/ApplicationDbContext.cs` (add 3 DbSets)
- Test: `GAC.Tests/Content/HomeAggregateMappingTests.cs`

**Interfaces:**
- Produces:
  - `PromoSection { int Id; int HomePageId; string ImagePath; LocalizedText Eyebrow, Heading, Description, CtaText; string? CtaLink; List<PromoCampaign> Campaigns; }`
  - `PromoCampaign { int Id; int PromoSectionId; LocalizedText Text; int SortOrder; }`
  - `DualCard { int Id; int HomePageId; string ImagePath; string? Link; LocalizedText Eyebrow, Title, Description, ButtonText; int SortOrder; }`
  - `HomePage.Promo : PromoSection?`, `HomePage.DualCards : List<DualCard>`
  - `FormPage.BannerImagePath : string?`

- [ ] **Step 1: Write the failing test**

`GAC.Tests/Content/HomeAggregateMappingTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class HomeAggregateMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task HomePage_Promo_Campaigns_And_DualCards_RoundTrip()
    {
        var db = NewDb(nameof(HomePage_Promo_Campaigns_And_DualCards_RoundTrip));
        var home = new HomePage
        {
            Promo = new PromoSection
            {
                ImagePath = "/img/p.jpg",
                Eyebrow = "Promotions", Heading = "Latest Offers",
                Description = "desc", CtaText = "View Offers", CtaLink = "/offers",
                Campaigns =
                {
                    new PromoCampaign { Text = "0% interest", SortOrder = 0 },
                }
            },
            DualCards =
            {
                new DualCard { ImagePath = "/img/c.jpg", Link = "/contact-us",
                    Eyebrow = "Our showrooms", Title = "Locations",
                    Description = "d", ButtonText = "Find Us", SortOrder = 0 },
            }
        };
        db.HomePages.Add(home);
        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();
        var loaded = await db.HomePages
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns)
            .Include(h => h.DualCards)
            .FirstAsync();

        Assert.Equal("Latest Offers", loaded.Promo!.Heading.En);
        Assert.Single(loaded.Promo.Campaigns);
        Assert.Equal("0% interest", loaded.Promo.Campaigns[0].Text.En);
        Assert.Single(loaded.DualCards);
        Assert.Equal("Locations", loaded.DualCards[0].Title.En);
    }

    [Fact]
    public async Task FormPage_BannerImagePath_RoundTrips()
    {
        var db = NewDb(nameof(FormPage_BannerImagePath_RoundTrips));
        db.FormPages.Add(new FormPage { Slug = "fleet", FormType = FormType.Fleet, BannerImagePath = "/img/b.jpg" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var f = await db.FormPages.FirstAsync(x => x.Slug == "fleet");
        Assert.Equal("/img/b.jpg", f.BannerImagePath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~HomeAggregateMappingTests"`
Expected: FAIL — `PromoSection`/`DualCard`/`BannerImagePath` do not exist (compile error).

- [ ] **Step 3: Create the entities**

`GAC.Core/Content/PromoSection.cs`:
```csharp
namespace GAC.Core.Content;

public class PromoSection
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Description { get; set; } = new();
    public LocalizedText CtaText { get; set; } = new();
    public string? CtaLink { get; set; }
    public List<PromoCampaign> Campaigns { get; set; } = new();
}
```

`GAC.Core/Content/PromoCampaign.cs`:
```csharp
namespace GAC.Core.Content;

public class PromoCampaign
{
    public int Id { get; set; }
    public int PromoSectionId { get; set; }
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
```

`GAC.Core/Content/DualCard.cs`:
```csharp
namespace GAC.Core.Content;

public class DualCard
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public string? Link { get; set; }
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Description { get; set; } = new();
    public LocalizedText ButtonText { get; set; } = new();
    public int SortOrder { get; set; }
}
```

Modify `GAC.Core/Content/HomePage.cs` — add to the class body:
```csharp
    public PromoSection? Promo { get; set; }
    public List<DualCard> DualCards { get; set; } = new();
```

Modify `GAC.Core/Content/FormPage.cs` — add:
```csharp
    public string? BannerImagePath { get; set; }
```

- [ ] **Step 4: Add EF configuration**

In `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`:

Extend `HomePageConfig.Configure` (add after the Slides line):
```csharp
        b.HasOne(h => h.Promo).WithOne().HasForeignKey<PromoSection>(p => p.HomePageId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(h => h.DualCards).WithOne().HasForeignKey(c => c.HomePageId).OnDelete(DeleteBehavior.Cascade);
```

Add new configs (mirroring `HeroSlideConfig`):
```csharp
public class PromoSectionConfig : IEntityTypeConfiguration<PromoSection>
{
    public void Configure(EntityTypeBuilder<PromoSection> b)
    {
        b.Property(p => p.ImagePath).HasMaxLength(300).IsRequired();
        b.Property(p => p.CtaLink).HasMaxLength(300);
        b.OwnsLocalized(p => p.Eyebrow);
        b.OwnsLocalized(p => p.Heading);
        b.OwnsLocalized(p => p.Description);
        b.OwnsLocalized(p => p.CtaText);
        b.HasMany(p => p.Campaigns).WithOne().HasForeignKey(c => c.PromoSectionId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class PromoCampaignConfig : IEntityTypeConfiguration<PromoCampaign>
{
    public void Configure(EntityTypeBuilder<PromoCampaign> b)
    {
        b.OwnsLocalized(c => c.Text);
    }
}

public class DualCardConfig : IEntityTypeConfiguration<DualCard>
{
    public void Configure(EntityTypeBuilder<DualCard> b)
    {
        b.Property(c => c.ImagePath).HasMaxLength(300).IsRequired();
        b.Property(c => c.Link).HasMaxLength(300);
        b.OwnsLocalized(c => c.Eyebrow);
        b.OwnsLocalized(c => c.Title);
        b.OwnsLocalized(c => c.Description);
        b.OwnsLocalized(c => c.ButtonText);
    }
}
```

Extend `FormPageConfig.Configure` — add:
```csharp
        b.Property(f => f.BannerImagePath).HasMaxLength(300);
```

- [ ] **Step 5: Register DbSets**

In `GAC.Infrastructure/Data/ApplicationDbContext.cs`, after the `HeroSlides` DbSet:
```csharp
    public DbSet<PromoSection> PromoSections => Set<PromoSection>();
    public DbSet<PromoCampaign> PromoCampaigns => Set<PromoCampaign>();
    public DbSet<DualCard> DualCards => Set<DualCard>();
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~HomeAggregateMappingTests"`
Expected: PASS (2 tests).

- [ ] **Step 7: Commit**

```bash
git add GAC.Core/Content GAC.Infrastructure/Data GAC.Tests/Content
git commit -m "feat(model): add PromoSection/PromoCampaign/DualCard + FormPage.BannerImagePath"
```

---

### Task 2: Additive EF migration

**Files:**
- Create: `GAC.Infrastructure/Migrations/<timestamp>_AddHomeSectionsAndFormBanner.cs` (+ Designer) via tooling
- Modify: `GAC.Infrastructure/Migrations/ApplicationDbContextModelSnapshot.cs` (auto)

**Interfaces:** none (schema only).

- [ ] **Step 1: Generate the migration**

Run (from `Solution/`):
```bash
dotnet ef migrations add AddHomeSectionsAndFormBanner --project GAC.Infrastructure --startup-project GAC.Web
```
Expected: new migration files created under `GAC.Infrastructure/Migrations/`.

- [ ] **Step 2: Verify the migration is additive (data-preservation gate)**

Open the generated migration. Confirm `Up()` contains ONLY:
- `migrationBuilder.CreateTable("PromoSections", ...)`, `"PromoCampaigns"`, `"DualCards"` (with their `_En`/`_Ar` owned columns and FKs), and
- `migrationBuilder.AddColumn<string>(name: "BannerImagePath", table: "FormPages", nullable: true, maxLength: 300)`.

It MUST NOT contain any `DropTable`, `DropColumn`, `AlterColumn`, `RenameColumn`, or `Sql("UPDATE …")` touching existing tables. If EF emitted anything unexpected (e.g. an unrelated `AlterColumn` from snapshot drift), stop and reconcile the model/snapshot before continuing.

- [ ] **Step 3: Confirm it builds**

Run: `dotnet build GAC.sln`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add GAC.Infrastructure/Migrations
git commit -m "feat(db): additive migration for home sections + form banner"
```

---

### Task 3: Load the new content in ContentService

**Files:**
- Modify: `GAC.Infrastructure/Services/ContentService.cs` (`GetHomePageAsync`)
- Test: `GAC.Tests/Content/ContentServiceHomeTests.cs`

**Interfaces:**
- Consumes: entities from Task 1.
- Produces: `GetHomePageAsync()` now returns `HomePage` with `Promo` (+`Campaigns` ordered by `SortOrder`) and `DualCards` (ordered by `SortOrder`) populated.

- [ ] **Step 1: Write the failing test**

`GAC.Tests/Content/ContentServiceHomeTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class ContentServiceHomeTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task GetHomePageAsync_Loads_Promo_And_DualCards()
    {
        var db = NewDb(nameof(GetHomePageAsync_Loads_Promo_And_DualCards));
        db.HomePages.Add(new HomePage
        {
            Promo = new PromoSection { ImagePath = "/p.jpg", Heading = "Latest Offers",
                Campaigns = { new PromoCampaign { Text = "b2", SortOrder = 1 },
                              new PromoCampaign { Text = "b1", SortOrder = 0 } } },
            DualCards = { new DualCard { ImagePath = "/c.jpg", Title = "Locations", SortOrder = 0 } }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var home = await new ContentService(db).GetHomePageAsync();

        Assert.NotNull(home!.Promo);
        Assert.Equal("b1", home.Promo!.Campaigns[0].Text.En);   // ordered by SortOrder
        Assert.Single(home.DualCards);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~ContentServiceHomeTests"`
Expected: FAIL — `home.Promo` is null (not included).

- [ ] **Step 3: Extend the include chain**

In `GAC.Infrastructure/Services/ContentService.cs`, change `GetHomePageAsync` to:
```csharp
    public async Task<HomePage?> GetHomePageAsync()
        => await _db.HomePages
            .Include(h => h.Slides.OrderBy(s => s.SortOrder))
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns.OrderBy(c => c.SortOrder))
            .Include(h => h.DualCards.OrderBy(c => c.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync();
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~ContentServiceHomeTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add GAC.Infrastructure/Services/ContentService.cs GAC.Tests/Content/ContentServiceHomeTests.cs
git commit -m "feat(content): eager-load promo + dual cards in GetHomePageAsync"
```

---

### Task 4: Seed promo, dual cards, and form banners (write-only-when-empty)

**Files:**
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs` (add `SeedPromoAsync`, `SeedDualCardsAsync`, `EnsureFormBannersAsync`; call them in `SeedAsync`)
- Test: `GAC.Tests/Content/SeederHomeSectionsTests.cs`

**Interfaces:**
- Produces idempotent seeding; uses the existing `HomePage` singleton (created by `SeedHomePageAsync`). All English text/image paths are the current hardcoded values; Arabic from `SharedResource.ar.resx`.

Reference values to seed (English / Arabic — copy Arabic verbatim from `GAC.Web/Resources/SharedResource.ar.resx` during implementation; the keys below are the English source strings):

- Promo: image `/assets/img/feature-gs8-traveller.jpg`; Eyebrow "Promotions"; Heading "Latest Offers"; Description "Discover our extensive selection of GAC offers and promotions from Mutawa Alkadi Automotive."; CtaText "View Offers"; CtaLink `/offers`; Campaigns: "Buy now, pay after 2 years", "0% interest".
- Dual cards (SortOrder 0/1/2):
  - `/assets/img/tile-locations.jpg`, link `/contact-us`, "Our showrooms" / "Locations" / "Mutawa Alkadi Automotive has a strong presence across Kuwait with a number of showrooms that stretch across the country." / "Find Us".
  - `/assets/img/tile-service.jpg`, link `/book-a-service`, "Aftersales" / "Book a service" / "We are here to help make sure your GAC is running smoothly so you can continue to drive worry-free." / "Book Now".
  - `/assets/img/tile-parts.jpg`, link `#`, "GAC" / "Parts & Accessories" / "Browse our range of GAC accessories which are most suited to your needs, including functional and practical accessories." / "Discover More".
- Form banners (slug → image): `book-a-service` → `/assets/img/book-a-service/hero.jpg`; `book-a-test-drive` → `/assets/img/book-a-test-drive/hero.jpg`; `request-a-quote` → `/assets/img/book-a-test-drive/hero.jpg`; `fleet` → `/assets/img/fleet/vehicles.jpg`. (Confirm each literal path in the matching `Views/Forms/Forms/_*.cshtml` during implementation. `contact-us` / `recall-enquiry` have no banner default — leave null.)

- [ ] **Step 1: Write the failing test**

`GAC.Tests/Content/SeederHomeSectionsTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederHomeSectionsTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SeedPromo_IsIdempotent_AndSeedsCampaigns()
    {
        var db = NewDb(nameof(SeedPromo_IsIdempotent_AndSeedsCampaigns));
        db.HomePages.Add(new HomePage());           // singleton exists (as in real seed)
        await db.SaveChangesAsync();

        await ContentSeeder.SeedPromoAsync(db);
        await ContentSeeder.SeedPromoAsync(db);      // second run must not duplicate

        Assert.Equal(1, await db.PromoSections.CountAsync());
        Assert.Equal(2, await db.PromoCampaigns.CountAsync());
    }

    [Fact]
    public async Task SeedDualCards_SeedsThree_Once()
    {
        var db = NewDb(nameof(SeedDualCards_SeedsThree_Once));
        db.HomePages.Add(new HomePage());
        await db.SaveChangesAsync();

        await ContentSeeder.SeedDualCardsAsync(db);
        await ContentSeeder.SeedDualCardsAsync(db);

        Assert.Equal(3, await db.DualCards.CountAsync());
    }

    [Fact]
    public async Task EnsureFormBanners_FillsBlanks_ButNeverOverwrites()
    {
        var db = NewDb(nameof(EnsureFormBanners_FillsBlanks_ButNeverOverwrites));
        db.FormPages.Add(new FormPage { Slug = "fleet", FormType = FormType.Fleet });               // blank
        db.FormPages.Add(new FormPage { Slug = "book-a-service", FormType = FormType.ServiceBooking,
            BannerImagePath = "/custom.jpg", IntroText = new LocalizedText { En = "mine" } });       // user-set
        await db.SaveChangesAsync();

        await ContentSeeder.EnsureFormBannersAsync(db);

        var fleet = await db.FormPages.FirstAsync(f => f.Slug == "fleet");
        var bas = await db.FormPages.FirstAsync(f => f.Slug == "book-a-service");
        Assert.False(string.IsNullOrEmpty(fleet.BannerImagePath));      // blank → filled
        Assert.Equal("/custom.jpg", bas.BannerImagePath);              // user value preserved
        Assert.Equal("mine", bas.IntroText.En);                        // user value preserved
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~SeederHomeSectionsTests"`
Expected: FAIL — `SeedPromoAsync`/`SeedDualCardsAsync`/`EnsureFormBannersAsync` don't exist.

- [ ] **Step 3: Implement the seeders**

In `ContentSeeder.cs`, make the three methods `internal static` (so the test in the same assembly-friendly namespace can call them — the test references `ContentSeeder.SeedPromoAsync`; if `ContentSeeder` is `public static`, make these methods `public static`). Add calls inside `SeedAsync` after `SeedHomePageAsync(db)`:
```csharp
        await SeedPromoAsync(db);
        await SeedDualCardsAsync(db);
```
and after `SeedFormPagesAsync(db)`:
```csharp
        await EnsureFormBannersAsync(db);
```

Implementations (guarded; attach to the existing singleton HomePage):
```csharp
    public static async Task SeedPromoAsync(ApplicationDbContext db)
    {
        if (await db.PromoSections.AnyAsync()) return;
        var home = await db.HomePages.FirstOrDefaultAsync();
        if (home is null) return;
        db.PromoSections.Add(new PromoSection
        {
            HomePageId = home.Id,
            ImagePath  = "/assets/img/feature-gs8-traveller.jpg",
            Eyebrow     = new LocalizedText { En = "Promotions", Ar = /* ar */ "العروض" },
            Heading     = new LocalizedText { En = "Latest Offers", Ar = /* ar */ "أحدث العروض" },
            Description = new LocalizedText { En = "Discover our extensive selection of GAC offers and promotions from Mutawa Alkadi Automotive.", Ar = /* ar from resx */ "" },
            CtaText     = new LocalizedText { En = "View Offers", Ar = /* ar */ "عرض العروض" },
            CtaLink     = "/offers",
            Campaigns =
            {
                new PromoCampaign { SortOrder = 0, Text = new LocalizedText { En = "Buy now, pay after 2 years", Ar = /* ar */ "" } },
                new PromoCampaign { SortOrder = 1, Text = new LocalizedText { En = "0% interest", Ar = /* ar */ "" } },
            }
        });
        await db.SaveChangesAsync();
    }

    public static async Task SeedDualCardsAsync(ApplicationDbContext db)
    {
        if (await db.DualCards.AnyAsync()) return;
        var home = await db.HomePages.FirstOrDefaultAsync();
        if (home is null) return;
        db.DualCards.AddRange(
            new DualCard { HomePageId = home.Id, SortOrder = 0, ImagePath = "/assets/img/tile-locations.jpg", Link = "/contact-us",
                Eyebrow = "Our showrooms", Title = "Locations",
                Description = "Mutawa Alkadi Automotive has a strong presence across Kuwait with a number of showrooms that stretch across the country.",
                ButtonText = "Find Us" },
            new DualCard { HomePageId = home.Id, SortOrder = 1, ImagePath = "/assets/img/tile-service.jpg", Link = "/book-a-service",
                Eyebrow = "Aftersales", Title = "Book a service",
                Description = "We are here to help make sure your GAC is running smoothly so you can continue to drive worry-free.",
                ButtonText = "Book Now" },
            new DualCard { HomePageId = home.Id, SortOrder = 2, ImagePath = "/assets/img/tile-parts.jpg", Link = "#",
                Eyebrow = "GAC", Title = "Parts & Accessories",
                Description = "Browse our range of GAC accessories which are most suited to your needs, including functional and practical accessories.",
                ButtonText = "Discover More" });
        await db.SaveChangesAsync();
        // NOTE: fill .Ar for each via the EnsureArabicAsync pattern, keyed by SortOrder.
    }

    public static async Task EnsureFormBannersAsync(ApplicationDbContext db)
    {
        var defaults = new Dictionary<string, string>
        {
            ["book-a-service"]    = "/assets/img/book-a-service/hero.jpg",
            ["book-a-test-drive"] = "/assets/img/book-a-test-drive/hero.jpg",
            ["request-a-quote"]   = "/assets/img/book-a-test-drive/hero.jpg",
            ["fleet"]             = "/assets/img/fleet/vehicles.jpg",
        };
        var changed = false;
        foreach (var (slug, img) in defaults)
        {
            var page = await db.FormPages.FirstOrDefaultAsync(f => f.Slug == slug);
            if (page is null) continue;
            if (string.IsNullOrEmpty(page.BannerImagePath)) { page.BannerImagePath = img; changed = true; }
        }
        if (changed) await db.SaveChangesAsync();
    }
```
For the Arabic values left blank above, follow the existing `EnsureArabicAsync` style: extend that method (or add to these seeders) to fill `.Ar` from the resx values, only where blank. Use the exact Arabic strings from `SharedResource.ar.resx`.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~SeederHomeSectionsTests"`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add GAC.Infrastructure/Data/ContentSeeder.cs GAC.Tests/Content/SeederHomeSectionsTests.cs
git commit -m "feat(seed): idempotent, write-only-when-empty promo/dual/form-banner seeding"
```

---

### Task 5: Admin service — get/save promo + save card

**Files:**
- Modify: `GAC.Core/Services/IAdminHomeService.cs`
- Modify: `GAC.Infrastructure/Services/AdminHomeService.cs`
- Test: `GAC.Tests/Admin/AdminHomeSectionsServiceTests.cs`

**Interfaces:**
- Produces (added to `IAdminHomeService`):
  - `Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default)` — ensures the singleton; loads Promo+Campaigns+DualCards.
  - `Task SavePromoAsync(PromoSection promo, CancellationToken ct = default)` — upsert the singleton promo; **replace** its campaigns from `promo.Campaigns`.
  - `Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default)` — update the existing card by `card.Id` (scalar + localized fields + link); returns false if not found.

- [ ] **Step 1: Write the failing test**

`GAC.Tests/Admin/AdminHomeSectionsServiceTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminHomeSectionsServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SavePromo_Upserts_AndReplacesCampaigns()
    {
        var db = NewDb(nameof(SavePromo_Upserts_AndReplacesCampaigns));
        var svc = new AdminHomeService(db);

        await svc.SavePromoAsync(new PromoSection { ImagePath = "/a.jpg", Heading = "H1",
            Campaigns = { new PromoCampaign { Text = "x", SortOrder = 0 } } });
        await svc.SavePromoAsync(new PromoSection { ImagePath = "/b.jpg", Heading = "H2",
            Campaigns = { new PromoCampaign { Text = "y", SortOrder = 0 }, new PromoCampaign { Text = "z", SortOrder = 1 } } });

        Assert.Equal(1, await db.PromoSections.CountAsync());      // upsert, not insert-twice
        var promo = await db.PromoSections.Include(p => p.Campaigns).FirstAsync();
        Assert.Equal("H2", promo.Heading.En);
        Assert.Equal("/b.jpg", promo.ImagePath);
        Assert.Equal(2, promo.Campaigns.Count);                   // replaced
        Assert.DoesNotContain(promo.Campaigns, c => c.Text.En == "x");
    }

    [Fact]
    public async Task SaveCard_UpdatesExisting()
    {
        var db = NewDb(nameof(SaveCard_UpdatesExisting));
        var home = new HomePage { DualCards = { new DualCard { ImagePath = "/c.jpg", Title = "Old", SortOrder = 0 } } };
        db.HomePages.Add(home); await db.SaveChangesAsync();
        var id = home.DualCards[0].Id;
        var svc = new AdminHomeService(db);

        var ok = await svc.SaveCardAsync(new DualCard { Id = id, ImagePath = "/c2.jpg", Title = "New", Link = "/x" });

        Assert.True(ok);
        var card = await db.DualCards.FindAsync(id);
        Assert.Equal("New", card!.Title.En);
        Assert.Equal("/c2.jpg", card.ImagePath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminHomeSectionsServiceTests"`
Expected: FAIL — methods don't exist.

- [ ] **Step 3: Add interface members**

In `GAC.Core/Services/IAdminHomeService.cs` add:
```csharp
    Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default);
    Task SavePromoAsync(PromoSection promo, CancellationToken ct = default);
    Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default);
```

- [ ] **Step 4: Implement in AdminHomeService**

Reuse the existing private `EnsureHomeAsync` (creates the singleton). Add:
```csharp
    public async Task<HomePage> GetHomeAggregateAsync(CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        return await _db.HomePages
            .Include(h => h.Promo!).ThenInclude(p => p.Campaigns.OrderBy(c => c.SortOrder))
            .Include(h => h.DualCards.OrderBy(c => c.SortOrder))
            .FirstAsync(h => h.Id == home.Id, ct);
    }

    public async Task SavePromoAsync(PromoSection promo, CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        var existing = await _db.PromoSections.Include(p => p.Campaigns)
            .FirstOrDefaultAsync(p => p.HomePageId == home.Id, ct);
        if (existing is null)
        {
            promo.HomePageId = home.Id;
            promo.Id = 0;
            _db.PromoSections.Add(promo);
        }
        else
        {
            existing.ImagePath = promo.ImagePath;
            existing.Eyebrow = promo.Eyebrow; existing.Heading = promo.Heading;
            existing.Description = promo.Description; existing.CtaText = promo.CtaText;
            existing.CtaLink = promo.CtaLink;
            _db.PromoCampaigns.RemoveRange(existing.Campaigns);              // replace
            existing.Campaigns = promo.Campaigns.Select((c, i) =>
                new PromoCampaign { Text = c.Text, SortOrder = i }).ToList();
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> SaveCardAsync(DualCard card, CancellationToken ct = default)
    {
        var e = await _db.DualCards.FirstOrDefaultAsync(c => c.Id == card.Id, ct);
        if (e is null) return false;
        e.ImagePath = card.ImagePath; e.Link = card.Link;
        e.Eyebrow = card.Eyebrow; e.Title = card.Title;
        e.Description = card.Description; e.ButtonText = card.ButtonText;
        await _db.SaveChangesAsync(ct);
        return true;
    }
```
(If `EnsureHomeAsync` is currently `private` returning the `HomePage`, keep it; otherwise adapt. Confirm its exact signature when implementing.)

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminHomeSectionsServiceTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add GAC.Core/Services/IAdminHomeService.cs GAC.Infrastructure/Services/AdminHomeService.cs GAC.Tests/Admin/AdminHomeSectionsServiceTests.cs
git commit -m "feat(admin): home-sections service (get/save promo, save card)"
```

---

### Task 6: Admin controller + view + nav

**Files:**
- Create: `GAC.Web/Areas/Admin/Controllers/HomeSectionsController.cs`
- Create: `GAC.Web/Areas/Admin/Views/HomeSections/Index.cshtml`
- Modify: `GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml` (add link)
- Test: `GAC.Tests/Admin/AdminHomeSectionsRedirectTests.cs` (reuse `AdminRedirectWebApplicationFactory` pattern; fake `IAdminHomeService`)

**Interfaces:**
- Consumes: `IAdminHomeService.GetHomeAggregateAsync/SavePromoAsync/SaveCardAsync`.
- Produces: routes `/Admin/HomeSections` (GET), `/Admin/HomeSections/SavePromo` (POST), `/Admin/HomeSections/SaveCard` (POST). Saves redirect to `Index` with `new { area = "Admin" }`.

- [ ] **Step 1: Write the failing test**

Add `IAdminHomeService` fake to `AdminRedirectWebApplicationFactory` (or a local factory) returning an empty aggregate, then:
`GAC.Tests/Admin/AdminHomeSectionsRedirectTests.cs`:
```csharp
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminHomeSectionsRedirectTests : IClassFixture<AdminRedirectWebApplicationFactory>
{
    private readonly AdminRedirectWebApplicationFactory _factory;
    public AdminHomeSectionsRedirectTests(AdminRedirectWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task SavePromo_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/HomeSections");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/HomeSections/SavePromo", new FormUrlEncodedContent(new Dictionary<string,string>
        {
            ["__RequestVerificationToken"] = token,
            ["ImagePath"] = "/a.jpg",
            ["Heading.En"] = "Latest Offers",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
```
(Requires the fake `IAdminHomeService` to be registered in `AdminRedirectWebApplicationFactory` — add a `FakeHome` implementing the interface, `GetHomeAggregateAsync` returning `new HomePage()`, the save methods no-ops.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminHomeSectionsRedirectTests"`
Expected: FAIL — route `/Admin/HomeSections` 404s.

- [ ] **Step 3: Create the controller**

`GAC.Web/Areas/Admin/Controllers/HomeSectionsController.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class HomeSectionsController : Controller
{
    private readonly IAdminHomeService _svc;
    public HomeSectionsController(IAdminHomeService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.GetHomeAggregateAsync());

    [HttpPost]
    public async Task<IActionResult> SavePromo(PromoSection promo)
    {
        await _svc.SavePromoAsync(promo);
        TempData["Flash"] = "Promo section saved.";
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    [HttpPost]
    public async Task<IActionResult> SaveCard(DualCard card)
    {
        await _svc.SaveCardAsync(card);
        TempData["Flash"] = "Card saved.";
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
```

- [ ] **Step 4: Create the view**

`GAC.Web/Areas/Admin/Views/HomeSections/Index.cshtml` — `@model GAC.Core.Content.HomePage`. One `<form asp-action="SavePromo">` for the promo (image picker on `ImagePath`; `_LocalizedField` for Eyebrow/Heading/Description/CtaText; plain `CtaLink` input; campaign rows bound as `Campaigns[i].Text.En`/`.Ar` + `Campaigns[i].SortOrder`, with a small "add row" JS in `@section Scripts` cloning a row template and reindexing; one Save). Then `@foreach` the 3 `Model.DualCards` rendering a `<form asp-action="SaveCard">` each (hidden `Id`; image picker on `ImagePath`; plain `Link`; `_LocalizedField` for Eyebrow/Title/Description/ButtonText; Save). One shared `<partial name="_PickerModal" />` at the bottom. Follow `Areas/Admin/Views/HomeContent/Edit.cshtml` + `News/Edit.cshtml` for field markup. The campaign list field names MUST be `Campaigns[0].Text.En` etc. so model binding fills `List<PromoCampaign>`.

- [ ] **Step 5: Add nav link**

In `_AdminNav.cshtml`, in the Admin/Editor block (next to "Hero Slides"):
```cshtml
        <a href="/Admin/HomeSections">Home Sections</a>
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminHomeSectionsRedirectTests"`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add GAC.Web/Areas/Admin GAC.Tests/Admin
git commit -m "feat(admin): Home Sections editor (promo + 3 cards)"
```

---

### Task 7: Public home rendering (promo + dual from DB, with fallback)

**Files:**
- Modify: `GAC.Web/Views/Home/Index.cshtml` (promo section ~155-170; dual section ~172-211)
- Test: `GAC.Tests/Home/HomeSectionsRenderTests.cs` (InMemory factory seeding promo + cards; assert rendered)

**Interfaces:**
- Consumes: `Model.Home.Promo`, `Model.Home.DualCards`.

- [ ] **Step 1: Write the failing test**

Use an InMemory-backed factory (subclass of `AdminWebApplicationFactory`-style but for the public Home; or extend `AdminRedirectWebApplicationFactory` to also swap the DbContext to InMemory and seed a promo + cards). Assert:
```csharp
// GET "/" → html Contains the seeded promo heading and a seeded card title,
// and Contains the seeded promo CTA link.
Assert.Contains("Latest Offers", html);
Assert.Contains("Locations", html);
```
(If an InMemory public-render harness is not yet available, create `HomeRenderWebApplicationFactory` that removes the SqlServer `DbContextOptions<ApplicationDbContext>` registration and adds `UseInMemoryDatabase`, then in a fixture seeds one `HomePage` with a promo + 3 cards. This mirrors the data-preservation note: tests never need the real DB.)

- [ ] **Step 2: Run test to verify it fails**

Expected: FAIL — current view renders the resx literals, not the seeded DB values (the seeded heading happens to match, so assert on a NON-default seeded value, e.g. set the test promo heading to "TEST PROMO HEADING" and assert that string appears).

- [ ] **Step 3: Rewrite the promo + dual markup**

Replace the promo `<section class="promo">` body to read from `Model.Home?.Promo` (guard: render the section only when `Model.Home?.Promo` is not null; otherwise keep a static fallback identical to today). Pattern (mirror the hero block):
```cshtml
@{ var promo = Model.Home?.Promo; }
@if (promo != null)
{
  <section class="promo">
    <div class="promo__image promo__image--photo" style="background-image:url('@promo.ImagePath')"></div>
    <div class="promo__content">
      <div class="promo__eyebrow">@promo.Eyebrow.Localize()</div>
      <h2>@promo.Heading.Localize()</h2>
      <p>@promo.Description.Localize()</p>
      <ul class="promo__campaigns">
@foreach (var c in promo.Campaigns)
{
        <li><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M20 6 9 17l-5-5"></path></svg>@c.Text.Localize()</li>
}
      </ul>
      <a class="btn btn--outline-light" href="@UrlHelpers.NormalizeUrl(promo.CtaLink)">@promo.CtaText.Localize()</a>
    </div>
  </section>
}
```
Replace the dual section similarly:
```cshtml
@{ var cards = Model.Home?.DualCards ?? new List<GAC.Core.Content.DualCard>(); }
@if (cards.Count > 0)
{
  <section class="dual"><div class="container"><div class="dual__grid">
@foreach (var card in cards)
{
    <div class="dual__card">
      <a class="dual__media dual__media--photo" href="@UrlHelpers.NormalizeUrl(card.Link)" aria-label="@card.Title.Localize()" style="background-image:url('@card.ImagePath')"></a>
      <div class="dual__body">
        <div class="dual__eyebrow">@card.Eyebrow.Localize()</div>
        <h3 class="dual__title">@card.Title.Localize()</h3>
        <p class="dual__text">@card.Description.Localize()</p>
        <a class="btn btn--ghost" href="@UrlHelpers.NormalizeUrl(card.Link)">@card.ButtonText.Localize()</a>
      </div>
    </div>
}
  </div></div></section>
}
```
Confirm `UrlHelpers` is in scope in the view (the hero block already uses `UrlHelpers.NormalizeUrl`). Keep the exact existing CSS class names.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~HomeSectionsRenderTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add GAC.Web/Views/Home/Index.cshtml GAC.Tests/Home
git commit -m "feat(home): render promo + dual cards from the database"
```

---

### Task 8: Public form-page banner + intro rendering

**Files:**
- Modify: `GAC.Web/Areas/Admin/Views/FormPages/Edit.cshtml` (add banner picker; ensure `IntroText` shown)
- Modify: `GAC.Infrastructure/Services/AdminPageService.cs` (`UpdateFormAsync` persists `BannerImagePath`)
- Modify: `GAC.Web/Views/Forms/Forms/_book-a-service.cshtml`, `_book-a-test-drive.cshtml`, `_request-a-quote.cshtml`, `_fleet.cshtml`, `_recall-enquiry.cshtml`
- Modify: `GAC.Web/Views/Forms/Page.cshtml` (Contact banner, optional)
- Test: `GAC.Tests/Admin/AdminPageServiceFormBannerTests.cs` + a form-render assertion

**Interfaces:**
- Consumes: `FormPage.BannerImagePath`, `FormPage.IntroText`.

- [ ] **Step 1: Write the failing test (service persists banner)**

`GAC.Tests/Admin/AdminPageServiceFormBannerTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminPageServiceFormBannerTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task UpdateForm_Persists_BannerImagePath()
    {
        var db = NewDb(nameof(UpdateForm_Persists_BannerImagePath));
        db.FormPages.Add(new FormPage { Id = 1, Slug = "fleet", FormType = FormType.Fleet });
        await db.SaveChangesAsync();
        var svc = new AdminPageService(db);

        await svc.UpdateFormAsync(new FormPage { Id = 1, Slug = "fleet", FormType = FormType.Fleet,
            BannerImagePath = "/new-banner.jpg", Title = "Fleet" });

        var f = await db.FormPages.FindAsync(1);
        Assert.Equal("/new-banner.jpg", f!.BannerImagePath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminPageServiceFormBannerTests"`
Expected: FAIL — `BannerImagePath` not copied.

- [ ] **Step 3: Persist in the service**

In `AdminPageService.UpdateFormAsync`, add to the copy list:
```csharp
        e.BannerImagePath = page.BannerImagePath;
```

- [ ] **Step 4: Admin Edit view — banner picker**

In `Areas/Admin/Views/FormPages/Edit.cshtml`, add a banner image field (copy the News `Edit.cshtml` image block: an `asp-for="BannerImagePath"` input with `data-media-input`, a `data-media-pick` button, the `data-media-preview` img), keep the existing `IntroText` `_LocalizedField` (it is already editable), and add `<partial name="_PickerModal" />` at the bottom if not present.

- [ ] **Step 5: Public templates — read banner + intro with fallback**

In each of `_book-a-service.cshtml`, `_book-a-test-drive.cshtml`, `_request-a-quote.cshtml`, `_fleet.cshtml`, `_recall-enquiry.cshtml`: replace the hardcoded banner image path with `@(string.IsNullOrEmpty(Model.Page.BannerImagePath) ? "<current-literal-path>" : Model.Page.BannerImagePath)`, and render the intro as:
```cshtml
@if (!string.IsNullOrWhiteSpace(Model.Page.IntroText.Localize()))
{
  <p class="...intro-class...">@Model.Page.IntroText.Localize()</p>
}
else
{
  <p class="...intro-class...">@L["<current intro key>"]</p>
}
```
Keep each partial's existing intro CSS class. **Do not touch the `<form>…</form>` block.** Confirm the exact current literal banner path + intro `@L[...]` key in each file as you edit it.

- [ ] **Step 6: Contact banner (optional)**

In `Views/Forms/Page.cshtml`, in the Contact branch, before `@Html.Raw(Model.Page.BodyHtml.Localize())`, add:
```cshtml
@if (!string.IsNullOrEmpty(Model.Page.BannerImagePath))
{
  <div class="page-hero--banner" style="background-image:url('@Model.Page.BannerImagePath')"></div>
}
```
(Use a wrapper consistent with the page; keep it minimal.)

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test GAC.sln --filter "FullyQualifiedName~AdminPageServiceFormBannerTests"`
Expected: PASS. Also `dotnet build GAC.sln` → 0 errors (Razor compiles).

- [ ] **Step 8: Commit**

```bash
git add GAC.Web/Areas/Admin/Views/FormPages/Edit.cshtml GAC.Infrastructure/Services/AdminPageService.cs GAC.Web/Views/Forms
git commit -m "feat(forms): editable banner image + intro text on form pages"
```

---

### Task 9: Register, full verification, review

**Files:**
- Verify: `GAC.Web/Program.cs` (no new service registration needed — `IAdminHomeService` already registered; confirm)
- Verify: whole solution

- [ ] **Step 1: Confirm DI** — `IAdminHomeService` is already registered (`Program.cs`). No new registration required (the new methods are on the existing service). If a separate service was introduced instead, register it here.

- [ ] **Step 2: Full build + InMemory test run**

Run:
```bash
dotnet build GAC.sln
dotnet test GAC.sln --filter "FullyQualifiedName~Content|FullyQualifiedName~GAC.Tests.Admin|FullyQualifiedName~GAC.Tests.Home"
```
Expected: 0 build errors; all listed tests PASS. (Do NOT run `HomePageSmokeTests` as a gate — those need the migration applied to the real DB.)

- [ ] **Step 3: Review pass**

Run an adversarial review over the diff (correctness, Razor/XSS, EF mapping correctness, data-preservation: confirm no seeder overwrites and the migration is additive). Fix confirmed findings.

- [ ] **Step 4: Final commit (if review changes)**

```bash
git add -A
git commit -m "chore: review fixes for editable home + form content"
```

---

## Self-Review

**Spec coverage:**
- Promo block model/admin/render → Tasks 1,4,5,6,7. ✓
- Dual cards (fixed 3, edit in place, editable links) → Tasks 1,4,5,6,7. ✓
- Form banner + intro → Tasks 1,4,8. ✓
- Additive migration → Task 2 (explicit additive gate). ✓
- Data preservation (write-only-when-empty) → Task 4 test `EnsureFormBanners_FillsBlanks_ButNeverOverwrites`, Global Constraints. ✓
- Seed EN+AR from resx → Task 4 (Arabic via `EnsureArabicAsync` pattern). ✓
- Render fallback so nothing breaks pre-seed → Task 7 (null guards), Task 8 (fallback literals). ✓
- Forms unchanged → Task 8 Step 5 note + Global Constraints. ✓
- InMemory verification only → Global Constraints, Task 9. ✓

**Placeholder scan:** Arabic seed strings are intentionally deferred to "copy verbatim from `SharedResource.ar.resx`" — this is a lookup, not a logic gap; every other step has concrete code. Banner literal paths and intro `@L` keys are "confirm in the file as you edit" because they must match the exact current source — acceptable.

**Type consistency:** `PromoSection`, `PromoCampaign`, `DualCard` field names and the `IAdminHomeService` signatures (`GetHomeAggregateAsync`, `SavePromoAsync`, `SaveCardAsync`) are used identically across Tasks 1, 5, 6, 7. Campaign binding name `Campaigns[i].Text.En` matches `PromoCampaign.Text`. ✓
