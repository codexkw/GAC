# Editable Warranty Page + Dynamic Cars Grid — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `/warranty` editable from a structured admin editor (banner, intro, terms callouts, extended-warranty text + an HTML brand table), and render its cars grid live from the visible Vehicles, each with a new per-vehicle warranty-booklet PDF.

**Architecture:** New singleton `WarrantyPage` aggregate (+ `WarrantyCallout` child rows) mirroring the `HomePage` pattern, plus a nullable `Vehicle.WarrantyBookletPdf` column. `PageController` special-cases the `warranty` slug to render a dedicated view from `WarrantyPage` + `IVehicleService.GetVisibleAsync()`. A new Admin "Warranty page" editor + service mirror `AdminHomeService`/`HomeSectionsController`. One additive migration; seeders are write-only-when-empty; public view falls back to today's content pre-seed.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQL Server; InMemory for tests), xUnit, Razor.

## Global Constraints

- **Data preservation:** migration is additive only — CREATE TABLE ×2 + ADD nullable COLUMN; **no DROP, no ALTER/UPDATE of existing columns/rows**. Review the generated migration.
- **Write-only-when-empty:** `SeedWarrantyAsync` guarded by `WarrantyPages.AnyAsync()`; Arabic filled only where blank; `WarrantyBookletPdf` left null. Never overwrite an existing value.
- **Bilingual:** every text field is `LocalizedText` (En/Ar); admin uses `_LocalizedField` with names `"Field.En"`/`"Field.Ar"`; seed both languages.
- **Verification uses InMemory DB** (schema from the model) so build/tests stay green before the real-DB migration is applied. Never run a test that boots `AdminWebApplicationFactory` (it hits the real prod DB at startup) — use `InMemoryTestDb.Swap`-based factories or pure `NewDb` service tests only.
- **Admin conventions:** `[Area("Admin")]`, `[Authorize(Policy = AdminPolicies.ContentEditor)]`, `[AutoValidateAntiforgeryToken]`; image fields use `data-media-input` + `data-media-pick` + one `<partial name="_PickerModal" />`; saves redirect to `nameof(Index)` with `new { area = "Admin" }`.
- **Forms/SEO unchanged:** title/meta/visibility stay on the existing `ContentPage` "warranty" record; only the page body moves to `WarrantyPage`.

---

### Task 1: Domain entities + Vehicle field + EF mapping + DbSets

**Files:**
- Create: `GAC.Core/Content/WarrantyPage.cs`, `GAC.Core/Content/WarrantyCallout.cs`
- Modify: `GAC.Core/Content/Vehicle.cs` (add `WarrantyBookletPdf`)
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (2 configs; extend `VehicleConfig`)
- Modify: `GAC.Infrastructure/Data/ApplicationDbContext.cs` (2 DbSets)
- Test: `GAC.Tests/Content/WarrantyMappingTests.cs`

**Interfaces — Produces:**
- `WarrantyPage { int Id; string BannerImagePath; LocalizedText BannerLabel, Heading, Intro; string TermsImagePath; LocalizedText TermsNote, ExtendedHeading, ExtendedIntro, ExtendedTableHtml; List<WarrantyCallout> Callouts; }`
- `WarrantyCallout { int Id; int WarrantyPageId; LocalizedText Lead, Text; int SortOrder; }`
- `Vehicle.WarrantyBookletPdf : string?`

- [ ] **Step 1: Write the failing test** — `GAC.Tests/Content/WarrantyMappingTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class WarrantyMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task WarrantyPage_WithCallouts_RoundTrips()
    {
        var db = NewDb(nameof(WarrantyPage_WithCallouts_RoundTrips));
        db.WarrantyPages.Add(new WarrantyPage
        {
            BannerImagePath = "/b.jpg", BannerLabel = "Label", Heading = "Warranty",
            Intro = "i", TermsImagePath = "/t.jpg", TermsNote = "*terms",
            ExtendedHeading = "Ext", ExtendedIntro = "p", ExtendedTableHtml = "<table></table>",
            Callouts = { new WarrantyCallout { Lead = "5y", Text = "rest", SortOrder = 0 } }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var w = await db.WarrantyPages.Include(x => x.Callouts).FirstAsync();
        Assert.Equal("Warranty", w.Heading.En);
        Assert.Single(w.Callouts);
        Assert.Equal("5y", w.Callouts[0].Lead.En);
    }

    [Fact]
    public async Task Vehicle_WarrantyBookletPdf_RoundTrips()
    {
        var db = NewDb(nameof(Vehicle_WarrantyBookletPdf_RoundTrips));
        db.Vehicles.Add(new Vehicle { Slug = "gs8", Name = "GS8", WarrantyBookletPdf = "/w.pdf" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        var v = await db.Vehicles.FirstAsync(x => x.Slug == "gs8");
        Assert.Equal("/w.pdf", v.WarrantyBookletPdf);
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `dotnet test GAC.sln --filter "FullyQualifiedName~WarrantyMappingTests"` → FAIL (types/`WarrantyBookletPdf` missing, compile error).

- [ ] **Step 3: Create the entities** — `GAC.Core/Content/WarrantyPage.cs`:
```csharp
namespace GAC.Core.Content;

public class WarrantyPage
{
    public int Id { get; set; }
    public string BannerImagePath { get; set; } = "";
    public LocalizedText BannerLabel { get; set; } = new();
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Intro { get; set; } = new();
    public string TermsImagePath { get; set; } = "";
    public LocalizedText TermsNote { get; set; } = new();
    public LocalizedText ExtendedHeading { get; set; } = new();
    public LocalizedText ExtendedIntro { get; set; } = new();
    public LocalizedText ExtendedTableHtml { get; set; } = new();
    public List<WarrantyCallout> Callouts { get; set; } = new();
}
```
`GAC.Core/Content/WarrantyCallout.cs`:
```csharp
namespace GAC.Core.Content;

public class WarrantyCallout
{
    public int Id { get; set; }
    public int WarrantyPageId { get; set; }
    public LocalizedText Lead { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
```
Modify `GAC.Core/Content/Vehicle.cs` — add next to `SpecPdf`:
```csharp
    public string? WarrantyBookletPdf { get; set; }
```

- [ ] **Step 4: Add EF configuration** — in `ContentConfigurations.cs`, add (mirror `HeroSlideConfig`):
```csharp
public class WarrantyPageConfig : IEntityTypeConfiguration<WarrantyPage>
{
    public void Configure(EntityTypeBuilder<WarrantyPage> b)
    {
        b.Property(w => w.BannerImagePath).HasMaxLength(300).IsRequired();
        b.Property(w => w.TermsImagePath).HasMaxLength(300).IsRequired();
        b.OwnsLocalized(w => w.BannerLabel);
        b.OwnsLocalized(w => w.Heading);
        b.OwnsLocalized(w => w.Intro);
        b.OwnsLocalized(w => w.TermsNote);
        b.OwnsLocalized(w => w.ExtendedHeading);
        b.OwnsLocalized(w => w.ExtendedIntro);
        b.OwnsLocalized(w => w.ExtendedTableHtml);
        b.HasMany(w => w.Callouts).WithOne().HasForeignKey(c => c.WarrantyPageId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class WarrantyCalloutConfig : IEntityTypeConfiguration<WarrantyCallout>
{
    public void Configure(EntityTypeBuilder<WarrantyCallout> b)
    {
        b.OwnsLocalized(c => c.Lead);
        b.OwnsLocalized(c => c.Text);
    }
}
```
Extend `VehicleConfig.Configure` — add:
```csharp
        b.Property(v => v.WarrantyBookletPdf).HasMaxLength(300);
```
(If `OwnsLocalized` for `ExtendedTableHtml` truncates at a column max, confirm `OwnsLocalized` maps to `nvarchar(max)` like other body fields; `BodyHtml` uses the same helper, so it does.)

- [ ] **Step 5: Register DbSets** — in `ApplicationDbContext.cs`, after the home-sections DbSets:
```csharp
    public DbSet<WarrantyPage> WarrantyPages => Set<WarrantyPage>();
    public DbSet<WarrantyCallout> WarrantyCallouts => Set<WarrantyCallout>();
```

- [ ] **Step 6: Run test to verify it passes** — `dotnet test GAC.sln --filter "FullyQualifiedName~WarrantyMappingTests"` → PASS (2).

- [ ] **Step 7: Commit**
```bash
git add GAC.Core/Content GAC.Infrastructure/Data GAC.Tests/Content/WarrantyMappingTests.cs
git commit -m "feat(model): add WarrantyPage/WarrantyCallout + Vehicle.WarrantyBookletPdf"
```

---

### Task 2: Additive EF migration

**Files:**
- Create: `GAC.Infrastructure/Migrations/<timestamp>_AddWarrantyPage.cs` (+ Designer) via tooling
- Modify: `ApplicationDbContextModelSnapshot.cs` (auto)

- [ ] **Step 1: Generate** — from `Solution/`:
```bash
dotnet ef migrations add AddWarrantyPage --project GAC.Infrastructure --startup-project GAC.Web
```
- [ ] **Step 2: Verify additive (data-preservation gate)** — `Up()` MUST contain ONLY `CreateTable("WarrantyPages")`, `CreateTable("WarrantyCallouts")` (with `_En`/`_Ar` owned columns + FK/index), and `AddColumn<string>(name: "WarrantyBookletPdf", table: "Vehicles", nullable: true, maxLength: 300)`. It MUST NOT contain any `DropTable`/`DropColumn`/`AlterColumn`/`RenameColumn`/`Sql("UPDATE…")` against existing tables (those belong only in `Down()`). If EF emits snapshot-drift changes, stop and reconcile.
- [ ] **Step 3: Build** — `dotnet build GAC.sln` → 0 errors.
- [ ] **Step 4: Commit**
```bash
git add GAC.Infrastructure/Migrations
git commit -m "feat(db): additive migration for warranty page"
```

---

### Task 3: Load WarrantyPage + seed it (write-only-when-empty)

**Files:**
- Modify: `GAC.Core/Services/IContentService.cs` (add `GetWarrantyPageAsync`)
- Modify: `GAC.Infrastructure/Services/ContentService.cs` (implement)
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs` (`SeedWarrantyAsync` + call in `SeedAsync`; extend `EnsureArabicAsync`)
- Test: `GAC.Tests/Content/SeederWarrantyTests.cs`

**Interfaces:**
- Produces: `Task<WarrantyPage?> IContentService.GetWarrantyPageAsync()` — loads the singleton with `Callouts` ordered by `SortOrder`, `AsNoTracking`.
- Produces: `ContentSeeder.SeedWarrantyAsync(ApplicationDbContext)` — idempotent seed of the singleton from current content.

- [ ] **Step 1: Write the failing test** — `GAC.Tests/Content/SeederWarrantyTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederWarrantyTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task SeedWarranty_IsIdempotent_AndSeedsCallouts()
    {
        var db = NewDb(nameof(SeedWarranty_IsIdempotent_AndSeedsCallouts));
        await ContentSeeder.SeedWarrantyAsync(db);
        await ContentSeeder.SeedWarrantyAsync(db);     // second run must not duplicate

        Assert.Equal(1, await db.WarrantyPages.CountAsync());
        Assert.Equal(2, await db.WarrantyCallouts.CountAsync());
        var w = await new ContentService(db).GetWarrantyPageAsync();
        Assert.Equal("Warranty", w!.Heading.En);
        Assert.Equal(2, w.Callouts.Count);
        Assert.False(string.IsNullOrWhiteSpace(w.ExtendedTableHtml.En));   // brand table seeded
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `dotnet test ... --filter "FullyQualifiedName~SeederWarrantyTests"` → FAIL (`SeedWarrantyAsync`/`GetWarrantyPageAsync` missing).

- [ ] **Step 3: Add the content-service method** — in `IContentService.cs` add `Task<WarrantyPage?> GetWarrantyPageAsync();`; in `ContentService.cs`:
```csharp
    public async Task<WarrantyPage?> GetWarrantyPageAsync()
        => await _db.WarrantyPages
            .Include(w => w.Callouts.OrderBy(c => c.SortOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync();
```

- [ ] **Step 4: Implement the seeder** — in `ContentSeeder.cs`, add a call in `SeedAsync` after `SeedContentPagesAsync(db)`:
```csharp
        await SeedWarrantyAsync(db);
```
and the method (EN copied verbatim from `SeedContent/content/warranty.html`; Arabic translations):
```csharp
    public static async Task SeedWarrantyAsync(ApplicationDbContext db)
    {
        if (await db.WarrantyPages.AnyAsync()) return;
        db.WarrantyPages.Add(new WarrantyPage
        {
            BannerImagePath = "/assets/img/warranty/banner.jpg",
            BannerLabel = new LocalizedText { En = "GAC Mutawa Alkadi Automotive Warranty", Ar = "ضمان جي إيه سي مطوع القاضي للسيارات" },
            Heading = new LocalizedText { En = "Warranty", Ar = "الضمان" },
            Intro = new LocalizedText
            {
                En = "We, at GAC Mutawa Alkadi Automotive, believe that a premium and reliable vehicle should also be backed up by reliable warranty options.\nWe provide professional services delivered by professional experts by offering GAC Motor genuine spare parts and accessories at our professional service facility.",
                Ar = "نحن في جي إيه سي مطوع القاضي للسيارات نؤمن بأن السيارة الفاخرة والموثوقة يجب أن تكون مدعومة أيضاً بخيارات ضمان موثوقة.\nنقدّم خدمات احترافية يؤديها خبراء محترفون من خلال توفير قطع غيار وإكسسوارات جي إيه سي موتور الأصلية في منشأة الخدمة الاحترافية لدينا."
            },
            TermsImagePath = "/assets/img/warranty/callout.jpg",
            TermsNote = new LocalizedText { En = "*terms and conditions apply", Ar = "*تطبق الشروط والأحكام" },
            ExtendedHeading = new LocalizedText { En = "Extended Warranty Program", Ar = "برنامج الضمان الممتد" },
            ExtendedIntro = new LocalizedText
            {
                En = "Buy your new AAC vehicle today with complete peace of mind and stay protected longer with Extended Warranty and Roadside Assistance Programs offered through our licensed partners!\nThe program is offered as a 1-Year or 2-Year Extended Warranty and/or Roadside Assistance Program offering AAC Customers peace of mind through extended time and mileage depending on brand and model purchased. Both, the Extended Warranty and Roadside Assistance Programs offered AAC are done so through credible 3rd party companies to match and mirror the terms and conditions offered by the manufacturing brands.\nFor as long as you are driving an AAC vehicle, know that we have your best interest and care in mind.",
                Ar = ""   // optional translation; English fallback at render
            },
            ExtendedTableHtml = new LocalizedText
            {
                En = "<table class=\"datatable datatable--matrix\">\n  <thead>\n    <tr><th>Brand</th><th>Manufacturer Warranty</th><th>Manufacturer Roadside Assistance</th><th>Extended Warranty</th><th>Extended Roadside Assistance</th><th>View Extended Warranty Policy</th></tr>\n  </thead>\n  <tbody>\n    <tr><td>GAC</td><td>5 Years and/or 150,000 KM</td><td>—</td><td>+2 Years<br>+Unlimited Mileage</td><td>+2 Years<br>+Unlimited Mileage</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>Chevrolet</td><td>3 Years and/or 100,000 KM</td><td>3 Years and/or 100,000 KM</td><td>+2 Years<br>+50,000 KM</td><td>+2 Years<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>GMC</td><td>3 Years and/or 100,000 KM</td><td>3 Years and/or 100,000 KM</td><td>+2 Years<br>+50,000 KM</td><td>+2 Years<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n    <tr><td>Cadillac</td><td>4 Years and/or 100,000 KM</td><td>4 Years and/or 100,000 KM</td><td>+1 Year<br>+50,000 KM</td><td>+1 Year<br>+50,000 KM</td><td><a href=\"#\">Click Here</a></td></tr>\n  </tbody>\n</table>",
                Ar = ""
            },
            Callouts =
            {
                new WarrantyCallout { SortOrder = 0,
                    Lead = new LocalizedText { En = "5 years extended warranty, unlimited mileage", Ar = "ضمان ممتد 5 سنوات، كيلومترات غير محدودة" },
                    Text = new LocalizedText { En = "for Mutawa Alkadi showroom customers", Ar = "لعملاء معرض مطوع القاضي" } },
                new WarrantyCallout { SortOrder = 1,
                    Lead = new LocalizedText { En = "5 years or 150,000 Km", Ar = "5 سنوات أو 150,000 كم" },
                    Text = new LocalizedText { En = "whichever comes first for all other sale channels", Ar = "أيهما أقرب لجميع قنوات البيع الأخرى" } },
            }
        });
        await db.SaveChangesAsync();
    }
```

- [ ] **Step 5: Run test to verify it passes** — `dotnet test ... --filter "FullyQualifiedName~SeederWarrantyTests"` → PASS.

- [ ] **Step 6: Commit**
```bash
git add GAC.Core/Services/IContentService.cs GAC.Infrastructure/Services/ContentService.cs GAC.Infrastructure/Data/ContentSeeder.cs GAC.Tests/Content/SeederWarrantyTests.cs
git commit -m "feat(content): load + seed editable warranty page (write-only-when-empty)"
```

---

### Task 4: Admin service — get/save warranty

**Files:**
- Create: `GAC.Core/Services/IAdminWarrantyService.cs`, `GAC.Infrastructure/Services/AdminWarrantyService.cs`
- Modify: `GAC.Web/Program.cs` (register `IAdminWarrantyService`)
- Test: `GAC.Tests/Admin/AdminWarrantyServiceTests.cs`

**Interfaces — Produces:**
- `Task<WarrantyPage> GetAsync(CancellationToken ct = default)` — ensures + loads the singleton (incl. ordered `Callouts`).
- `Task SaveAsync(WarrantyPage page, CancellationToken ct = default)` — upsert the singleton scalar+localized fields; **replace** callout rows (drop blanks, re-index).

- [ ] **Step 1: Write the failing test** — `GAC.Tests/Admin/AdminWarrantyServiceTests.cs`:
```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminWarrantyServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Save_Upserts_AndReplacesCallouts_DroppingBlanks()
    {
        var db = NewDb(nameof(Save_Upserts_AndReplacesCallouts_DroppingBlanks));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage { BannerImagePath = "/a.jpg", TermsImagePath = "/t.jpg",
            Heading = "H1", Callouts = { new WarrantyCallout { Lead = "x", Text = "y", SortOrder = 0 } } });
        await svc.SaveAsync(new WarrantyPage { BannerImagePath = "/b.jpg", TermsImagePath = "/t2.jpg",
            Heading = "H2", Callouts = {
                new WarrantyCallout { Lead = "p", Text = "q", SortOrder = 0 },
                new WarrantyCallout { Lead = "", Text = "", SortOrder = 1 },       // blank → dropped
                new WarrantyCallout { Lead = "r", Text = "s", SortOrder = 2 } } });

        Assert.Equal(1, await db.WarrantyPages.CountAsync());      // upsert, not insert-twice
        var w = await db.WarrantyPages.Include(p => p.Callouts).FirstAsync();
        Assert.Equal("H2", w.Heading.En);
        Assert.Equal("/b.jpg", w.BannerImagePath);
        Assert.Equal(2, w.Callouts.Count);                        // replaced, blank dropped
        Assert.DoesNotContain(w.Callouts, c => c.Lead.En == "x");
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — `dotnet test ... --filter "FullyQualifiedName~AdminWarrantyServiceTests"` → FAIL (type missing).

- [ ] **Step 3: Create interface** — `GAC.Core/Services/IAdminWarrantyService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminWarrantyService
{
    Task<WarrantyPage> GetAsync(CancellationToken ct = default);
    Task SaveAsync(WarrantyPage page, CancellationToken ct = default);
}
```

- [ ] **Step 4: Implement** — `GAC.Infrastructure/Services/AdminWarrantyService.cs`:
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminWarrantyService : IAdminWarrantyService
{
    private readonly ApplicationDbContext _db;
    public AdminWarrantyService(ApplicationDbContext db) => _db = db;

    private async Task<WarrantyPage> EnsureAsync(CancellationToken ct)
    {
        var w = await _db.WarrantyPages.FirstOrDefaultAsync(ct);
        if (w is null)
        {
            w = new WarrantyPage();
            _db.WarrantyPages.Add(w);
            await _db.SaveChangesAsync(ct);
        }
        return w;
    }

    public async Task<WarrantyPage> GetAsync(CancellationToken ct = default)
    {
        var w = await EnsureAsync(ct);
        return await _db.WarrantyPages
            .Include(p => p.Callouts.OrderBy(c => c.SortOrder))
            .FirstAsync(p => p.Id == w.Id, ct);
    }

    public async Task SaveAsync(WarrantyPage page, CancellationToken ct = default)
    {
        var existing = await _db.WarrantyPages.Include(p => p.Callouts).FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            page.Id = 0;
            page.Callouts = NormalizeCallouts(page.Callouts);
            _db.WarrantyPages.Add(page);
        }
        else
        {
            existing.BannerImagePath = page.BannerImagePath;
            existing.BannerLabel = page.BannerLabel; existing.Heading = page.Heading;
            existing.Intro = page.Intro; existing.TermsImagePath = page.TermsImagePath;
            existing.TermsNote = page.TermsNote; existing.ExtendedHeading = page.ExtendedHeading;
            existing.ExtendedIntro = page.ExtendedIntro; existing.ExtendedTableHtml = page.ExtendedTableHtml;
            _db.WarrantyCallouts.RemoveRange(existing.Callouts);
            existing.Callouts = NormalizeCallouts(page.Callouts);
        }
        await _db.SaveChangesAsync(ct);
    }

    private static List<WarrantyCallout> NormalizeCallouts(IEnumerable<WarrantyCallout> callouts)
        => callouts
            .Where(c => !string.IsNullOrWhiteSpace(c.Lead?.En) || !string.IsNullOrWhiteSpace(c.Lead?.Ar)
                     || !string.IsNullOrWhiteSpace(c.Text?.En) || !string.IsNullOrWhiteSpace(c.Text?.Ar))
            .Select((c, i) => new WarrantyCallout { Lead = c.Lead ?? new(), Text = c.Text ?? new(), SortOrder = i })
            .ToList();
}
```

- [ ] **Step 5: Register DI** — in `GAC.Web/Program.cs`, next to the `IAdminHomeService` registration:
```csharp
builder.Services.AddScoped<IAdminWarrantyService, AdminWarrantyService>();
```

- [ ] **Step 6: Run test to verify it passes** — PASS.

- [ ] **Step 7: Commit**
```bash
git add GAC.Core/Services/IAdminWarrantyService.cs GAC.Infrastructure/Services/AdminWarrantyService.cs GAC.Web/Program.cs GAC.Tests/Admin/AdminWarrantyServiceTests.cs
git commit -m "feat(admin): warranty-page service (get aggregate, upsert + replace callouts)"
```

---

### Task 5: Admin controller + view + nav + per-vehicle booklet field

**Files:**
- Create: `GAC.Web/Areas/Admin/Controllers/WarrantyController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml`
- Modify: `GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml` (nav link)
- Modify: `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml` (booklet field)
- Modify: `GAC.Infrastructure/Services/AdminVehicleService.cs` (copy `WarrantyBookletPdf` in update)
- Test: `GAC.Tests/Admin/AdminWarrantyRedirectTests.cs` (in-memory factory — `AdminInMemoryWebApplicationFactory`)

**Interfaces:**
- Consumes: `IAdminWarrantyService.GetAsync/SaveAsync`.
- Produces: routes `/Admin/Warranty` (GET), `/Admin/Warranty/Save` (POST → redirect to Index).

- [ ] **Step 1: Write the failing test** — `GAC.Tests/Admin/AdminWarrantyRedirectTests.cs` (uses the existing `AdminInMemoryWebApplicationFactory`, real services on in-memory DB):
```csharp
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminWarrantyRedirectTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminWarrantyRedirectTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Save_RedirectsIntoAdmin()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/Warranty");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/Warranty/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BannerImagePath"] = "/a.jpg",
            ["TermsImagePath"] = "/t.jpg",
            ["Heading.En"] = "Warranty",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — FAIL (route `/Admin/Warranty` 404s).

- [ ] **Step 3: Create the controller** — `GAC.Web/Areas/Admin/Controllers/WarrantyController.cs`:
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
public class WarrantyController : Controller
{
    private readonly IAdminWarrantyService _svc;
    public WarrantyController(IAdminWarrantyService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.GetAsync());

    [HttpPost]
    public async Task<IActionResult> Save(WarrantyPage page)
    {
        await _svc.SaveAsync(page);
        TempData["Flash"] = "Warranty page saved.";
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
```

- [ ] **Step 4: Create the view** — `GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml`, `@model GAC.Core.Content.WarrantyPage`. One `<form method="post" asp-action="Save" class="adm-form">` containing: image picker on `BannerImagePath`; `_LocalizedField` for `BannerLabel`, `Heading`, `Intro` (Multiline), `TermsNote`, `ExtendedHeading`, `ExtendedIntro` (Multiline); image picker on `TermsImagePath`; a `Code = true` `_LocalizedField` for `ExtendedTableHtml`; an add/remove callout-rows control binding `Callouts[i].Lead.En/.Ar` + `Callouts[i].Text.En/.Ar` (mirror the promo-bullets UI in `HomeSections/Index.cshtml` including the `<template>` + `@section Scripts` reindex logic, with two inputs per row); one Save button; one `<partial name="_PickerModal" />`. Field names MUST be `Callouts[0].Lead.En` etc. so binding fills `List<WarrantyCallout>`.

- [ ] **Step 5: Add nav link** — in `_AdminNav.cshtml`, next to "Home Sections":
```cshtml
        <a href="/Admin/Warranty">Warranty page</a>
```

- [ ] **Step 6: Per-vehicle booklet field** — in `Areas/Admin/Views/Vehicles/Edit.cshtml`, after the Brochure PDF block, add (mirror it):
```cshtml
    <div class="adm-field">
        <label asp-for="WarrantyBookletPdf">Warranty booklet PDF</label>
        <div class="adm-media">
            <input asp-for="WarrantyBookletPdf" data-media-input />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </div>
    </div>
```
In `AdminVehicleService.cs`, in the update method next to `existing.BrochurePdf = vehicle.BrochurePdf;`:
```csharp
        existing.WarrantyBookletPdf = vehicle.WarrantyBookletPdf;
```

- [ ] **Step 7: Run test to verify it passes** — PASS.

- [ ] **Step 8: Commit**
```bash
git add GAC.Web/Areas/Admin GAC.Infrastructure/Services/AdminVehicleService.cs GAC.Tests/Admin/AdminWarrantyRedirectTests.cs
git commit -m "feat(admin): Warranty page editor + per-vehicle warranty booklet PDF"
```

---

### Task 6: Public warranty view + route

**Files:**
- Create: `GAC.Web/Models/WarrantyPageViewModel.cs`
- Create: `GAC.Web/Views/Content/Warranty.cshtml`
- Modify: `GAC.Web/Controllers/PageController.cs` (special-case `warranty`)
- Test: `GAC.Tests/Home/WarrantyRenderTests.cs` (in-memory `WebApplicationFactory` + `InMemoryTestDb.Swap`)

**Interfaces:**
- Consumes: `IContentService.GetWarrantyPageAsync`, `IVehicleService.GetVisibleAsync`, `Vehicle.WarrantyBookletPdf`, `UrlHelpers.ThumbPath`.
- Produces: `WarrantyPageViewModel { WarrantyPage Warranty; IReadOnlyList<Vehicle> Vehicles; }`.

- [ ] **Step 1: Write the failing test** — `GAC.Tests/Home/WarrantyRenderTests.cs`:
```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

public class WarrantyRenderTests : IClassFixture<WarrantyRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "warranty-render-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public WarrantyRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Warranty_RendersStructuredFields_DynamicCars_AndTable()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var w = await db.WarrantyPages.FirstAsync();
            w.Heading = new LocalizedText { En = "ZZZ-WARR-HEAD", Ar = "ZZZ-WARR-HEAD" };
            w.ExtendedTableHtml = new LocalizedText { En = "<table id=\"zzz-table\"></table>", Ar = "" };
            var v = await db.Vehicles.OrderBy(x => x.SortOrder).FirstAsync();
            v.IsVisible = true;
            v.Name = new LocalizedText { En = "ZZZ-CAR", Ar = "ZZZ-CAR" };
            v.WarrantyBookletPdf = "/zzz-booklet.pdf";
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();

        Assert.Contains("ZZZ-WARR-HEAD", html);          // structured heading from DB
        Assert.Contains("ZZZ-CAR", html);                // dynamic car name from Vehicles
        Assert.Contains("/zzz-booklet.pdf", html);       // per-vehicle booklet link
        Assert.Contains("id=\"zzz-table\"", html);       // brand table HTML rendered raw
    }
}
```

- [ ] **Step 2: Run test to verify it fails** — FAIL (`/warranty` still renders the old `Content/Page.cshtml` blob; markers absent).

- [ ] **Step 3: Create the view model** — `GAC.Web/Models/WarrantyPageViewModel.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Web.Models;

public class WarrantyPageViewModel
{
    public WarrantyPage Warranty { get; set; } = new();
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
```

- [ ] **Step 4: Route in PageController** — in `PageController.Show`, inside the `if (content != null)` branch, BEFORE returning the generic view:
```csharp
        if (content != null)
        {
            ViewData["Seo"] = SeoBuilder.ForContentPage(content, baseUrl);
            if (content.Slug == "warranty")
            {
                var warranty = await _content.GetWarrantyPageAsync() ?? new GAC.Core.Content.WarrantyPage();
                var vehicles = await _vehicles.GetVisibleAsync();
                return View("~/Views/Content/Warranty.cshtml",
                    new GAC.Web.Models.WarrantyPageViewModel { Warranty = warranty, Vehicles = vehicles });
            }
            return View("~/Views/Content/Page.cshtml", content);
        }
```

- [ ] **Step 5: Create the view** — `GAC.Web/Views/Content/Warranty.cshtml`, `@model GAC.Web.Models.WarrantyPageViewModel`, `Layout = "_Layout"`. Reproduce `SeedContent/content/warranty.html` markup with the SAME CSS classes, reading from `Model.Warranty` (`@w.BannerImagePath`, `@w.BannerLabel.Localize()`, `@w.Heading.Localize()`, each non-empty line of `@w.Intro.Localize()` → `<p class="muted">`, terms image + `@foreach (var c in w.Callouts)` → `<div class="callout"><svg…/><div><strong>@c.Lead.Localize()</strong> @c.Text.Localize()</div></div>`, `@w.TermsNote.Localize()`, the extended heading/intro, and `@Html.Raw(w.ExtendedTableHtml.Localize())`). The cars grid:
```cshtml
<section class="section"><div class="container"><div class="wgrid">
@foreach (var v in Model.Vehicles)
{
    <div class="wcard">
      <img src="@UrlHelpers.ThumbPath(v)" alt="@v.Name.Localize()" />
      <div class="wcard__name">@v.Name.Localize()</div>
      @if (!string.IsNullOrEmpty(v.WarrantyBookletPdf))
      {
        <a class="btn btn--doc" href="@v.WarrantyBookletPdf" target="_blank" rel="noopener">@L["Warranty Booklet"]</a>
      }
    </div>
}
</div></div></section>
```
Confirm `UrlHelpers` and `L` are in scope (add `@using GAC.Web.Infrastructure` / they are available via `_ViewImports`). Null-guard the whole page so an unseeded `WarrantyPage` still renders today's defaults.

- [ ] **Step 6: Run test to verify it passes** — `dotnet test ... --filter "FullyQualifiedName~WarrantyRenderTests"` → PASS.

- [ ] **Step 7: Commit**
```bash
git add GAC.Web/Models/WarrantyPageViewModel.cs GAC.Web/Views/Content/Warranty.cshtml GAC.Web/Controllers/PageController.cs GAC.Tests/Home/WarrantyRenderTests.cs
git commit -m "feat(warranty): render editable warranty page + dynamic cars grid"
```

---

### Task 7: ContentPages admin note + full verification + review

**Files:**
- Modify: `GAC.Web/Areas/Admin/Views/ContentPages/Edit.cshtml` (hide BodyHtml + add note for the warranty slug)
- Verify: whole solution

- [ ] **Step 1: Hide raw body for warranty** — in `ContentPages/Edit.cshtml`, wrap the `Page body (HTML)` `_LocalizedField` so it only renders when `Model.Slug != "warranty"`, and for warranty show:
```cshtml
@if (Model.Slug == "warranty")
{
    <p class="adm-note">This page's content is edited in <a asp-area="Admin" asp-controller="Warranty" asp-action="Index">Warranty page</a>. Only its title and meta are editable here.</p>
}
else
{
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Page body (HTML)", NameEn = "BodyHtml.En", NameAr = "BodyHtml.Ar", ValueEn = Model.BodyHtml.En, ValueAr = Model.BodyHtml.Ar, Code = true }' />
}
```

- [ ] **Step 2: Full build + InMemory test run** — from `Solution/`:
```bash
dotnet build GAC.sln
dotnet test GAC.sln --filter "FullyQualifiedName~GAC.Tests.Content|FullyQualifiedName~AdminWarrantyServiceTests|FullyQualifiedName~AdminWarrantyRedirectTests|FullyQualifiedName~WarrantyRenderTests"
```
Expected: 0 build errors; all listed PASS. (Never run `AdminWebApplicationFactory`-only classes — they hit prod.)

- [ ] **Step 3: Review pass** — adversarial review over the diff (correctness, Razor/XSS: confirm `@Html.Raw` is used ONLY for the admin-authored `ExtendedTableHtml` and the intro/labels are encoded; EF mapping; data-preservation: migration additive, seeders guarded). Fix confirmed findings.

- [ ] **Step 4: Commit (if review changes)**
```bash
git add -A
git commit -m "chore: review fixes for editable warranty page"
```

---

## Self-Review

**Spec coverage:**
- WarrantyPage + WarrantyCallout model → Task 1. ✓
- Vehicle.WarrantyBookletPdf → Task 1 (field), Task 5 (admin), Task 6 (render). ✓
- Additive migration → Task 2 (explicit additive gate). ✓
- Load + seed (write-only-when-empty) → Task 3. ✓
- Admin structured editor (banner/intro/terms callouts/extended + HTML table) → Tasks 4,5. ✓
- Dynamic cars grid from visible vehicles → Task 6. ✓
- Title/meta/SEO stay on ContentPage; body moves → Task 6 (route) + Task 7 (admin note). ✓
- In-memory verification only → Global Constraints + Task 7. ✓

**Placeholder scan:** Arabic for `ExtendedIntro`/`ExtendedTableHtml` intentionally left `""` (English fallback at render) — a content choice, not a logic gap. Callout/heading Arabic is concrete. The brand-table `href="#"` links are copied verbatim from today's content (unchanged placeholders).

**Type consistency:** `WarrantyPage`/`WarrantyCallout` field names, `IAdminWarrantyService.GetAsync/SaveAsync`, `IContentService.GetWarrantyPageAsync`, and the `Callouts[i].Lead.En` binding names are used identically across Tasks 1, 3, 4, 5, 6. `WarrantyPageViewModel.{Warranty,Vehicles}` consistent in Tasks 6. ✓
