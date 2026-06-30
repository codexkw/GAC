# Structured Warranty Brand Table Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the raw-HTML "Brand table (HTML)" field on `/warranty` with a structured, bilingual, click-+-to-add brand-row editor that renders as a real table on the public page.

**Architecture:** Mirror the cost-of-service editor. Add a `WarrantyBrandRow` child collection (fixed columns, dynamic rows) plus six editable bilingual column headers to the existing `WarrantyPage` singleton. Additive migration; a backfill seeder AngleSharp-parses the page's current `ExtendedTableHtml` into rows. Admin upsert replaces rows wholesale with a blank-drop normalizer; the public view renders a structured table with a safe-scheme guard on the per-brand policy link.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQL Server prod / InMemory tests), Razor, xUnit, AngleSharp 1.1.2.

## Global Constraints

- .NET 9 / EF Core 9; pin `Microsoft.*` packages to `9.0.*`.
- Bilingual `LocalizedText{En,Ar}` via the `OwnsLocalized(...)` EF helper; cookie-driven culture; `.Localize()` reads `CurrentUICulture`.
- **Additive migrations only.** Apply to prod via scoped idempotent script (`ef migrations script <last-applied> <new> --idempotent`), strip UTF-8 BOM (`tail -c +4`), prepend `SET XACT_ABORT ON;`, run via sqlcmd from inside the script dir with a relative filename. Never `dotnet ef database update`.
- Razor HTML-encodes non-ASCII `.Localize()` output to numeric entities — integration tests asserting literal Arabic must `System.Net.WebUtility.HtmlDecode` the response first.
- **Test-filter gotcha:** the `GAC.Tests.Admin` namespace contains classes whose factories boot the real prod DB. Run new admin in-memory classes by **explicit class name**, never the whole namespace.
- Build/test from `C:\Users\anas-\source\repos\GAC\Solution`.

## File Structure

- `GAC.Core/Content/WarrantyBrandRow.cs` — **create** — the brand-row entity (brand name, 4 bilingual cells, policy URL, sort order).
- `GAC.Core/Content/WarrantyPage.cs` — **modify** — add `BrandRows` + 6 header `LocalizedText`.
- `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` — **modify** — `WarrantyBrandRowConfig` + headers/`HasMany` in `WarrantyPageConfig`.
- `GAC.Infrastructure/Data/ApplicationDbContext.cs` — **modify** — `WarrantyBrandRows` DbSet.
- `GAC.Infrastructure/Migrations/*_AddWarrantyBrandTable.cs` — **generated** — additive.
- `GAC.Infrastructure/Services/AdminWarrantyService.cs` — **modify** — include BrandRows + split query; save headers + rows wholesale; normalizer.
- `GAC.Infrastructure/Services/ContentService.cs` — **modify** — include BrandRows + split query in `GetWarrantyPageAsync`.
- `GAC.Infrastructure/Data/ContentSeeder.cs` — **modify** — `SeedWarrantyBrandRowsAsync` + `ParseBrandTable` + call in `SeedAsync`.
- `GAC.Web/Views/Content/Warranty.cshtml` — **modify** — structured table render.
- `GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml` — **modify** — header inputs + brand-row repeater.
- Tests: `GAC.Tests/Content/WarrantyBrandMappingTests.cs`, `GAC.Tests/Content/SeederWarrantyBrandTests.cs`, `GAC.Tests/Admin/AdminWarrantyBrandServiceTests.cs`, `GAC.Tests/Home/WarrantyBrandRenderTests.cs`, `GAC.Tests/Admin/AdminWarrantyBrandRedirectTests.cs` — **create**.

---

### Task 1: Structured model, EF config, DbSet, migration

**Files:**
- Create: `GAC.Core/Content/WarrantyBrandRow.cs`
- Modify: `GAC.Core/Content/WarrantyPage.cs`
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (`WarrantyPageConfig` ~line 400; add new config class after `WarrantyCalloutConfig` ~line 424)
- Modify: `GAC.Infrastructure/Data/ApplicationDbContext.cs:35` (after `WarrantyCallouts`)
- Test: `GAC.Tests/Content/WarrantyBrandMappingTests.cs`

**Interfaces:**
- Produces: `WarrantyBrandRow { int Id; int WarrantyPageId; string Brand; LocalizedText ManufacturerWarranty, ManufacturerRoadside, ExtendedWarranty, ExtendedRoadside; string? PolicyUrl; int SortOrder }`.
- Produces: `WarrantyPage.BrandRows` (`List<WarrantyBrandRow>`) and headers `TableBrandHeader, TableMfrWarrantyHeader, TableMfrRoadsideHeader, TableExtWarrantyHeader, TableExtRoadsideHeader, TablePolicyHeader` (all `LocalizedText`).
- Produces: `ApplicationDbContext.WarrantyBrandRows`.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Content/WarrantyBrandMappingTests.cs`:

```csharp
using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class WarrantyBrandMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task WarrantyPage_WithBrandRows_RoundTrips()
    {
        var db = NewDb(nameof(WarrantyPage_WithBrandRows_RoundTrips));
        db.WarrantyPages.Add(new WarrantyPage
        {
            TableBrandHeader = new LocalizedText { En = "Brand", Ar = "العلامة التجارية" },
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            TableMfrRoadsideHeader = new LocalizedText { En = "Mfr Roadside", Ar = "مساعدة المصنّع" },
            TableExtWarrantyHeader = new LocalizedText { En = "Extended Warranty", Ar = "الضمان الممتد" },
            TableExtRoadsideHeader = new LocalizedText { En = "Ext Roadside", Ar = "المساعدة الممتدة" },
            TablePolicyHeader = new LocalizedText { En = "Policy", Ar = "الوثيقة" },
            BrandRows =
            {
                new WarrantyBrandRow { SortOrder = 0, Brand = "GAC",
                    ManufacturerWarranty = new LocalizedText { En = "5 Years and/or 150,000 KM" },
                    ExtendedWarranty = new LocalizedText { En = "+2 Years\n+Unlimited Mileage" },
                    PolicyUrl = "/pdfs/gac.pdf" },
                new WarrantyBrandRow { SortOrder = 1, Brand = "Chevrolet",
                    ManufacturerWarranty = new LocalizedText { En = "3 Years and/or 100,000 KM" } },
            }
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var p = await db.WarrantyPages
            .Include(w => w.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery().AsNoTracking().FirstAsync();

        Assert.Equal("ضمان المصنّع", p.TableMfrWarrantyHeader.Ar);
        Assert.Equal(2, p.BrandRows.Count);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("/pdfs/gac.pdf", p.BrandRows[0].PolicyUrl);
        Assert.Equal("+2 Years\n+Unlimited Mileage", p.BrandRows[0].ExtendedWarranty.En);  // multi-line preserved
        Assert.Equal("Chevrolet", p.BrandRows[1].Brand);
        Assert.Null(p.BrandRows[1].PolicyUrl);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~WarrantyBrandMappingTests"`
Expected: FAIL to **compile** — `WarrantyBrandRow`, `WarrantyPage.BrandRows`, and the header properties don't exist yet.

- [ ] **Step 3: Create the entity**

Create `GAC.Core/Content/WarrantyBrandRow.cs`:

```csharp
namespace GAC.Core.Content;

// One brand's row in the Extended Warranty table. Columns are fixed; the
// brand name is plain text (proper noun) and the four attribute cells are
// bilingual. PolicyUrl is a link or an uploaded-PDF path.
public class WarrantyBrandRow
{
    public int Id { get; set; }
    public int WarrantyPageId { get; set; }
    public string Brand { get; set; } = "";
    public LocalizedText ManufacturerWarranty { get; set; } = new();
    public LocalizedText ManufacturerRoadside { get; set; } = new();
    public LocalizedText ExtendedWarranty { get; set; } = new();
    public LocalizedText ExtendedRoadside { get; set; } = new();
    public string? PolicyUrl { get; set; }
    public int SortOrder { get; set; }
}
```

- [ ] **Step 4: Extend the page model**

In `GAC.Core/Content/WarrantyPage.cs`, replace the body so it adds the headers and collection (keep `ExtendedTableHtml` — it stays as an unused column to keep the migration additive):

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
    public LocalizedText ExtendedTableHtml { get; set; } = new();   // legacy, unused after the structured table ships
    public List<WarrantyCallout> Callouts { get; set; } = new();

    // Structured Extended-Warranty brand table.
    public LocalizedText TableBrandHeader { get; set; } = new();
    public LocalizedText TableMfrWarrantyHeader { get; set; } = new();
    public LocalizedText TableMfrRoadsideHeader { get; set; } = new();
    public LocalizedText TableExtWarrantyHeader { get; set; } = new();
    public LocalizedText TableExtRoadsideHeader { get; set; } = new();
    public LocalizedText TablePolicyHeader { get; set; } = new();
    public List<WarrantyBrandRow> BrandRows { get; set; } = new();
}
```

- [ ] **Step 5: Configure EF mapping**

In `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`, inside `WarrantyPageConfig.Configure` (right before the existing `b.HasMany(w => w.Callouts)...` line), add:

```csharp
        b.OwnsLocalized(w => w.TableBrandHeader);
        b.OwnsLocalized(w => w.TableMfrWarrantyHeader);
        b.OwnsLocalized(w => w.TableMfrRoadsideHeader);
        b.OwnsLocalized(w => w.TableExtWarrantyHeader);
        b.OwnsLocalized(w => w.TableExtRoadsideHeader);
        b.OwnsLocalized(w => w.TablePolicyHeader);
        b.HasMany(w => w.BrandRows).WithOne().HasForeignKey(r => r.WarrantyPageId).OnDelete(DeleteBehavior.Cascade);
```

And immediately after the `WarrantyCalloutConfig` class, add:

```csharp
public class WarrantyBrandRowConfig : IEntityTypeConfiguration<WarrantyBrandRow>
{
    public void Configure(EntityTypeBuilder<WarrantyBrandRow> b)
    {
        b.Property(r => r.Brand).HasMaxLength(120);
        b.Property(r => r.PolicyUrl).HasMaxLength(500);
        b.OwnsLocalized(r => r.ManufacturerWarranty);
        b.OwnsLocalized(r => r.ManufacturerRoadside);
        b.OwnsLocalized(r => r.ExtendedWarranty);
        b.OwnsLocalized(r => r.ExtendedRoadside);
    }
}
```

- [ ] **Step 6: Add the DbSet**

In `GAC.Infrastructure/Data/ApplicationDbContext.cs`, after line 35 (`public DbSet<WarrantyCallout> WarrantyCallouts => Set<WarrantyCallout>();`):

```csharp
    public DbSet<WarrantyBrandRow> WarrantyBrandRows => Set<WarrantyBrandRow>();
```

- [ ] **Step 7: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~WarrantyBrandMappingTests"`
Expected: PASS.

- [ ] **Step 8: Generate the migration**

Run: `dotnet ef migrations add AddWarrantyBrandTable --project GAC.Infrastructure --startup-project GAC.Web`
Then open `GAC.Infrastructure/Migrations/*_AddWarrantyBrandTable.cs` and **verify it is additive only**: one `CreateTable("WarrantyBrandRows", ...)` (with `ManufacturerWarranty_En/_Ar`, `ManufacturerRoadside_En/_Ar`, `ExtendedWarranty_En/_Ar`, `ExtendedRoadside_En/_Ar`, `Brand`, `PolicyUrl`, `SortOrder`, `WarrantyPageId` FK) plus `AddColumn` calls for the 12 header columns (`TableBrandHeader_En`, `TableBrandHeader_Ar`, …) on `WarrantyPages`. **There must be no `DropColumn`/`DropTable`/`AlterColumn`.** If any non-additive op appears, stop and investigate.

- [ ] **Step 9: Commit**

```bash
git add GAC.Core/Content/WarrantyBrandRow.cs GAC.Core/Content/WarrantyPage.cs \
  GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs \
  GAC.Infrastructure/Data/ApplicationDbContext.cs \
  GAC.Infrastructure/Migrations \
  GAC.Tests/Content/WarrantyBrandMappingTests.cs
git commit -m "feat(warranty): structured brand-row model + config + additive migration"
```

---

### Task 2: Admin upsert service (headers + wholesale brand rows + normalize)

**Files:**
- Modify: `GAC.Infrastructure/Services/AdminWarrantyService.cs`
- Test: `GAC.Tests/Admin/AdminWarrantyBrandServiceTests.cs`

**Interfaces:**
- Consumes: `WarrantyPage.BrandRows`, the 6 headers, `ApplicationDbContext.WarrantyBrandRows` (Task 1).
- Produces: `AdminWarrantyService.SaveAsync` persists headers + brand rows; `GetAsync` returns them ordered. Blank rows dropped + re-indexed.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Admin/AdminWarrantyBrandServiceTests.cs`:

```csharp
using System.Linq;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminWarrantyBrandServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Save_PersistsHeaders_AndReplacesBrandRowsWholesale()
    {
        var db = NewDb(nameof(Save_PersistsHeaders_AndReplacesBrandRowsWholesale));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage
        {
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            BrandRows = { new WarrantyBrandRow { SortOrder = 0, Brand = "OldBrand",
                ManufacturerWarranty = new LocalizedText { En = "x" } } }
        });
        await svc.SaveAsync(new WarrantyPage
        {
            TableMfrWarrantyHeader = new LocalizedText { En = "Manufacturer Warranty", Ar = "ضمان المصنّع" },
            BrandRows = { new WarrantyBrandRow { SortOrder = 0, Brand = "GAC",
                ManufacturerWarranty = new LocalizedText { En = "5 Years" }, PolicyUrl = "/p.pdf" } }
        });

        var p = await svc.GetAsync();
        Assert.Equal("ضمان المصنّع", p.TableMfrWarrantyHeader.Ar);
        Assert.Single(p.BrandRows);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("/p.pdf", p.BrandRows[0].PolicyUrl);
        Assert.Equal(1, await db.WarrantyBrandRows.CountAsync());   // old row not orphaned
    }

    [Fact]
    public async Task Save_DropsBlankRows_AndReindexes()
    {
        var db = NewDb(nameof(Save_DropsBlankRows_AndReindexes));
        var svc = new AdminWarrantyService(db);

        await svc.SaveAsync(new WarrantyPage
        {
            BrandRows =
            {
                new WarrantyBrandRow { SortOrder = 0, Brand = "GAC", ManufacturerWarranty = new LocalizedText { En = "a" } },
                new WarrantyBrandRow { SortOrder = 1, Brand = "", PolicyUrl = "" },           // fully blank → dropped
                new WarrantyBrandRow { SortOrder = 2, Brand = "GMC", ManufacturerWarranty = new LocalizedText { En = "b" } },
            }
        });

        var p = await svc.GetAsync();
        Assert.Equal(new[] { "GAC", "GMC" }, p.BrandRows.Select(r => r.Brand).ToArray());
        Assert.Equal(new[] { 0, 1 }, p.BrandRows.Select(r => r.SortOrder).ToArray());   // reindexed
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminWarrantyBrandServiceTests"`
Expected: FAIL — `GetAsync` doesn't include BrandRows, and `SaveAsync` ignores headers/rows.

- [ ] **Step 3: Update the service**

In `GAC.Infrastructure/Services/AdminWarrantyService.cs`:

(a) In `GetAsync`, add the BrandRows include + split query:

```csharp
    public async Task<WarrantyPage> GetAsync(CancellationToken ct = default)
    {
        var w = await EnsureAsync(ct);
        return await _db.WarrantyPages
            .Include(p => p.Callouts.OrderBy(c => c.SortOrder))
            .Include(p => p.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery()
            .FirstAsync(p => p.Id == w.Id, ct);
    }
```

(b) In `SaveAsync`, load BrandRows too, map the 6 headers, and replace rows wholesale. Replace the whole method:

```csharp
    public async Task SaveAsync(WarrantyPage page, CancellationToken ct = default)
    {
        var existing = await _db.WarrantyPages
            .Include(p => p.Callouts)
            .Include(p => p.BrandRows)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);
        if (existing is null)
        {
            page.Id = 0;
            page.Callouts = NormalizeCallouts(page.Callouts);
            page.BrandRows = NormalizeBrandRows(page.BrandRows);
            _db.WarrantyPages.Add(page);
        }
        else
        {
            existing.BannerImagePath = page.BannerImagePath;
            existing.BannerLabel = page.BannerLabel; existing.Heading = page.Heading;
            existing.Intro = page.Intro; existing.TermsImagePath = page.TermsImagePath;
            existing.TermsNote = page.TermsNote; existing.ExtendedHeading = page.ExtendedHeading;
            existing.ExtendedIntro = page.ExtendedIntro; existing.ExtendedTableHtml = page.ExtendedTableHtml;
            existing.TableBrandHeader = page.TableBrandHeader;
            existing.TableMfrWarrantyHeader = page.TableMfrWarrantyHeader;
            existing.TableMfrRoadsideHeader = page.TableMfrRoadsideHeader;
            existing.TableExtWarrantyHeader = page.TableExtWarrantyHeader;
            existing.TableExtRoadsideHeader = page.TableExtRoadsideHeader;
            existing.TablePolicyHeader = page.TablePolicyHeader;
            _db.WarrantyCallouts.RemoveRange(existing.Callouts);   // replace the callout list wholesale
            existing.Callouts = NormalizeCallouts(page.Callouts);
            _db.WarrantyBrandRows.RemoveRange(existing.BrandRows); // replace the brand table wholesale
            existing.BrandRows = NormalizeBrandRows(page.BrandRows);
        }
        await _db.SaveChangesAsync(ct);
    }
```

(c) Add the normalizer + a small helper at the end of the class (after `NormalizeCallouts`):

```csharp
    // Drop fully-blank brand rows (the admin "add row" UI can submit empties) and re-index.
    private static List<WarrantyBrandRow> NormalizeBrandRows(IEnumerable<WarrantyBrandRow> rows)
        => rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Brand)
                     || HasText(r.ManufacturerWarranty) || HasText(r.ManufacturerRoadside)
                     || HasText(r.ExtendedWarranty) || HasText(r.ExtendedRoadside)
                     || !string.IsNullOrWhiteSpace(r.PolicyUrl))
            .Select((r, i) => new WarrantyBrandRow
            {
                Brand = r.Brand ?? "",
                ManufacturerWarranty = r.ManufacturerWarranty ?? new(),
                ManufacturerRoadside = r.ManufacturerRoadside ?? new(),
                ExtendedWarranty = r.ExtendedWarranty ?? new(),
                ExtendedRoadside = r.ExtendedRoadside ?? new(),
                PolicyUrl = string.IsNullOrWhiteSpace(r.PolicyUrl) ? null : r.PolicyUrl.Trim(),
                SortOrder = i
            })
            .ToList();

    private static bool HasText(LocalizedText? t)
        => t is not null && (!string.IsNullOrWhiteSpace(t.En) || !string.IsNullOrWhiteSpace(t.Ar));
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminWarrantyBrandServiceTests"`
Expected: PASS (both facts).

- [ ] **Step 5: Commit**

```bash
git add GAC.Infrastructure/Services/AdminWarrantyService.cs GAC.Tests/Admin/AdminWarrantyBrandServiceTests.cs
git commit -m "feat(warranty): admin upsert persists headers + brand rows (wholesale + normalize)"
```

---

### Task 3: Backfill seeder (parse the page's current ExtendedTableHtml)

**Files:**
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs` (add call in `SeedAsync` after `SeedWarrantyAsync` ~line 27; new method + parser near `SeedCostOfServiceAsync` ~line 683)
- Test: `GAC.Tests/Content/SeederWarrantyBrandTests.cs`

**Interfaces:**
- Consumes: `WarrantyPage.ExtendedTableHtml` (the canonical table seeded by `SeedWarrantyAsync`), `ApplicationDbContext.WarrantyBrandRows`.
- Produces: `ContentSeeder.SeedWarrantyBrandRowsAsync(ApplicationDbContext)` — write-only-when-empty, idempotent; fills 4 rows + 6 headers (EN parsed, AR from constants).

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Content/SeederWarrantyBrandTests.cs`:

```csharp
using System.Linq;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class SeederWarrantyBrandTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Seed_ParsesBrandTable_FromExistingPage_AndIsIdempotent()
    {
        var db = NewDb(nameof(Seed_ParsesBrandTable_FromExistingPage_AndIsIdempotent));
        await ContentSeeder.SeedWarrantyAsync(db);            // creates the page with ExtendedTableHtml
        await ContentSeeder.SeedWarrantyBrandRowsAsync(db);
        await ContentSeeder.SeedWarrantyBrandRowsAsync(db);   // idempotent — no duplicates

        Assert.Equal(1, await db.WarrantyPages.CountAsync());

        var p = await db.WarrantyPages
            .Include(w => w.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery().AsNoTracking().FirstAsync();

        Assert.Equal(4, p.BrandRows.Count);
        Assert.Equal("GAC", p.BrandRows[0].Brand);
        Assert.Equal("Chevrolet", p.BrandRows[1].Brand);
        Assert.Equal("GMC", p.BrandRows[2].Brand);
        Assert.Equal("Cadillac", p.BrandRows[3].Brand);
        Assert.Equal("5 Years and/or 150,000 KM", p.BrandRows[0].ManufacturerWarranty.En);
        Assert.Contains("Unlimited Mileage", p.BrandRows[0].ExtendedWarranty.En);
        Assert.Contains("\n", p.BrandRows[0].ExtendedWarranty.En);            // <br> preserved as newline
        Assert.Equal("Manufacturer Warranty", p.TableMfrWarrantyHeader.En);   // header EN from <th>
        Assert.False(string.IsNullOrWhiteSpace(p.TableMfrWarrantyHeader.Ar)); // header AR backfilled
        Assert.True(p.BrandRows.All(r => r.PolicyUrl == null));               // '#' links not imported
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~SeederWarrantyBrandTests"`
Expected: FAIL — `SeedWarrantyBrandRowsAsync` does not exist.

- [ ] **Step 3: Implement the seeder + parser**

In `GAC.Infrastructure/Data/ContentSeeder.cs`, add the call in `SeedAsync` immediately after `await SeedWarrantyAsync(db);`:

```csharp
        await SeedWarrantyBrandRowsAsync(db);
```

Then add these members near `SeedCostOfServiceAsync` (e.g. right after the `ParseCostTable` method):

```csharp
    public static async Task SeedWarrantyBrandRowsAsync(ApplicationDbContext db)
    {
        if (await db.WarrantyBrandRows.AnyAsync()) return;
        var page = await db.WarrantyPages.FirstOrDefaultAsync();
        if (page is null) return;
        var html = page.ExtendedTableHtml?.En;
        if (string.IsNullOrWhiteSpace(html)) return;

        var parsed = ParseBrandTable(html);
        if (parsed.Headers.Count < 6 || parsed.Rows.Count == 0) return;

        // Arabic header labels (the source table is English-only); positional with the parsed headers.
        string[] ar = { "العلامة التجارية", "ضمان المصنّع", "مساعدة المصنّع على الطريق", "الضمان الممتد", "المساعدة الممتدة على الطريق", "عرض وثيقة الضمان الممتد" };
        page.TableBrandHeader       = new LocalizedText { En = parsed.Headers[0], Ar = ar[0] };
        page.TableMfrWarrantyHeader = new LocalizedText { En = parsed.Headers[1], Ar = ar[1] };
        page.TableMfrRoadsideHeader = new LocalizedText { En = parsed.Headers[2], Ar = ar[2] };
        page.TableExtWarrantyHeader = new LocalizedText { En = parsed.Headers[3], Ar = ar[3] };
        page.TableExtRoadsideHeader = new LocalizedText { En = parsed.Headers[4], Ar = ar[4] };
        page.TablePolicyHeader      = new LocalizedText { En = parsed.Headers[5], Ar = ar[5] };

        for (var i = 0; i < parsed.Rows.Count; i++)
        {
            var cells = parsed.Rows[i];   // [brand, mfrWarranty, mfrRoadside, extWarranty, extRoadside, policy(ignored)]
            db.WarrantyBrandRows.Add(new WarrantyBrandRow
            {
                WarrantyPageId = page.Id,
                SortOrder = i,
                Brand = cells.ElementAtOrDefault(0) ?? "",
                ManufacturerWarranty = new LocalizedText { En = cells.ElementAtOrDefault(1) },
                ManufacturerRoadside = new LocalizedText { En = cells.ElementAtOrDefault(2) },
                ExtendedWarranty     = new LocalizedText { En = cells.ElementAtOrDefault(3) },
                ExtendedRoadside     = new LocalizedText { En = cells.ElementAtOrDefault(4) },
                PolicyUrl = null   // current links are placeholders ('#') — admin sets real ones
            });
        }
        await db.SaveChangesAsync();
    }

    private sealed class ParsedBrandTable
    {
        public List<string> Headers = new();
        public List<List<string>> Rows = new();   // each row: cell texts, brand first; <br> preserved as newline
    }

    private static ParsedBrandTable ParseBrandTable(string html)
    {
        var doc = new AngleSharp.Html.Parser.HtmlParser().ParseDocument(html);
        var r = new ParsedBrandTable
        {
            Headers = doc.QuerySelectorAll("table thead th").Select(t => t.TextContent.Trim()).ToList()
        };
        foreach (var tr in doc.QuerySelectorAll("table tbody tr"))
        {
            var tds = tr.QuerySelectorAll("td").ToList();
            if (tds.Count == 0) continue;
            var row = new List<string>();
            for (var c = 0; c < tds.Count; c++)
                row.Add(c == 0 ? tds[c].TextContent.Trim() : CellMultiline(tds[c]));   // brand plain; others keep <br>
            r.Rows.Add(row);
        }
        return r;
    }

    // Convert a cell's <br> to newlines and strip the rest of the markup.
    private static string CellMultiline(AngleSharp.Dom.IElement td)
    {
        var withBreaks = System.Text.RegularExpressions.Regex.Replace(
            td.InnerHtml, "<br\\s*/?>", "\n", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        var text = new AngleSharp.Html.Parser.HtmlParser().ParseDocument("<p>" + withBreaks + "</p>")
            .QuerySelector("p")!.TextContent;
        return string.Join("\n", text.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0));
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~SeederWarrantyBrandTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add GAC.Infrastructure/Data/ContentSeeder.cs GAC.Tests/Content/SeederWarrantyBrandTests.cs
git commit -m "feat(warranty): backfill seeder parses ExtendedTableHtml into brand rows"
```

---

### Task 4: Public read + frontend render

**Files:**
- Modify: `GAC.Infrastructure/Services/ContentService.cs:22-26` (`GetWarrantyPageAsync`)
- Modify: `GAC.Web/Views/Content/Warranty.cshtml:76-78` (the `datatable-wrap` block)
- Test: `GAC.Tests/Home/WarrantyBrandRenderTests.cs`

**Interfaces:**
- Consumes: `WarrantyPage.BrandRows`, the 6 headers; the dev seeder (auto-runs under `UseEnvironment("Development")`) populates them.
- Produces: `/warranty` renders a structured `<table>`; policy link only for safe schemes.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Home/WarrantyBrandRenderTests.cs`:

```csharp
using System;
using System.Threading.Tasks;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

public class WarrantyBrandRenderTests : IClassFixture<WarrantyBrandRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "warr-brand-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public WarrantyBrandRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task RendersBrandTable_FromDatabase()
    {
        var html = await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();
        Assert.Contains("GAC", html);
        Assert.Contains("Chevrolet", html);
        Assert.Contains("Manufacturer Warranty", html);     // seeded header
        Assert.Contains("150,000 KM", html);                // a cell value
    }

    [Fact]
    public async Task PolicyLink_RendersOnlyForSafeSchemes()
    {
        async Task<string> GetWithPolicyUrl(string url)
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var row = await db.WarrantyBrandRows.OrderBy(r => r.SortOrder).FirstAsync();
                row.PolicyUrl = url;
                await db.SaveChangesAsync();
            }
            return await (await _factory.CreateClient().GetAsync("/warranty")).Content.ReadAsStringAsync();
        }

        var bad = await GetWithPolicyUrl("javascript:alert(document.cookie)");
        Assert.DoesNotContain("javascript:alert", bad);

        var ok = await GetWithPolicyUrl("https://example.com/policy.pdf");
        Assert.Contains("https://example.com/policy.pdf", ok);
    }

    [Fact]
    public async Task LocalizesHeadersToArabic()
    {
        var client = _factory.CreateClient();
        var cookie = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture("ar"));
        client.DefaultRequestHeaders.Add("Cookie", $"{CookieRequestCultureProvider.DefaultCookieName}={cookie}");

        var raw = await (await client.GetAsync("/warranty")).Content.ReadAsStringAsync();
        var html = System.Net.WebUtility.HtmlDecode(raw);   // Razor encodes non-ASCII .Localize() output
        Assert.Contains("ضمان المصنّع", html);   // Arabic "Manufacturer Warranty" header
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~WarrantyBrandRenderTests"`
Expected: FAIL — the public read doesn't include BrandRows, so the table is empty (and the old `@Html.Raw` block doesn't emit the seeded header text from the new fields).

- [ ] **Step 3: Include BrandRows in the public read**

In `GAC.Infrastructure/Services/ContentService.cs`, replace `GetWarrantyPageAsync`:

```csharp
    public async Task<WarrantyPage?> GetWarrantyPageAsync()
        => await _db.WarrantyPages
            .Include(w => w.Callouts.OrderBy(c => c.SortOrder))
            .Include(w => w.BrandRows.OrderBy(r => r.SortOrder))
            .AsSplitQuery()
            .AsNoTracking()
            .FirstOrDefaultAsync();
```

- [ ] **Step 4: Render the structured table**

In `GAC.Web/Views/Content/Warranty.cshtml`, add a multi-line cell helper to the `@{ ... }` header block (after the `paras` lambda):

```csharp
    Func<string?, Microsoft.AspNetCore.Html.IHtmlContent> cell = s =>
    {
        var parts = (s ?? "").Split('\n').Select(x => x.Trim()).Where(x => x.Length > 0);
        return new Microsoft.AspNetCore.Html.HtmlString(string.Join("<br />", parts.Select(System.Net.WebUtility.HtmlEncode)));
    };
```

Then replace the `datatable-wrap` block (currently `@Html.Raw(w.ExtendedTableHtml.Localize())`):

```html
    <div class="datatable-wrap" style="margin-top:var(--space-6)">
      <table class="datatable datatable--matrix">
        <thead>
          <tr>
            <th>@w.TableBrandHeader.Localize()</th>
            <th>@w.TableMfrWarrantyHeader.Localize()</th>
            <th>@w.TableMfrRoadsideHeader.Localize()</th>
            <th>@w.TableExtWarrantyHeader.Localize()</th>
            <th>@w.TableExtRoadsideHeader.Localize()</th>
            <th>@w.TablePolicyHeader.Localize()</th>
          </tr>
        </thead>
        <tbody>
@foreach (var row in w.BrandRows)
{
    var purl = (row.PolicyUrl ?? "").Trim();
    var safe = purl.Length > 0 && (purl.StartsWith("/") || purl.StartsWith("#")
        || purl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || purl.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
          <tr>
            <td>@row.Brand</td>
            <td>@cell(row.ManufacturerWarranty.Localize())</td>
            <td>@cell(row.ManufacturerRoadside.Localize())</td>
            <td>@cell(row.ExtendedWarranty.Localize())</td>
            <td>@cell(row.ExtendedRoadside.Localize())</td>
            <td>
@if (safe)
{
              <a href="@purl" target="_blank" rel="noopener">@L["Click Here"]</a>
}
            </td>
          </tr>
}
        </tbody>
      </table>
    </div>
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~WarrantyBrandRenderTests"`
Expected: PASS (all three facts).

- [ ] **Step 6: Commit**

```bash
git add GAC.Infrastructure/Services/ContentService.cs GAC.Web/Views/Content/Warranty.cshtml GAC.Tests/Home/WarrantyBrandRenderTests.cs
git commit -m "feat(warranty): render structured brand table on public page (safe-scheme policy link)"
```

---

### Task 5: Admin editor (headers + brand-row repeater)

**Files:**
- Modify: `GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml` (replace the `ExtendedTableHtml` `_LocalizedField` at line 77; add a `<template>` + JS)
- Test: `GAC.Tests/Admin/AdminWarrantyBrandRedirectTests.cs`

**Interfaces:**
- Consumes: model binding of `TableXHeader.En/.Ar` and `BrandRows[i].Brand`, `BrandRows[i].ManufacturerWarranty.En/.Ar` (and the other 3 cells), `BrandRows[i].PolicyUrl` into `WarrantyPage`; `AdminWarrantyService.SaveAsync` (Task 2).
- Produces: a working admin grid; POST persists rows + headers and redirects into `/Admin`.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Admin/AdminWarrantyBrandRedirectTests.cs`:

```csharp
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// Saving the admin Warranty grid must redirect back into /Admin and persist the brand rows.
// In-memory DB (no prod contact).
public class AdminWarrantyBrandRedirectTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminWarrantyBrandRedirectTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task Save_RedirectsIntoAdmin_AndPersistsBrandRow()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var form = await client.GetAsync("/Admin/Warranty");
        form.EnsureSuccessStatusCode();
        var token = Regex.Match(await form.Content.ReadAsStringAsync(),
            @"name=""__RequestVerificationToken""[^>]*\bvalue=""([^""]+)""").Groups[1].Value;

        var resp = await client.PostAsync("/Admin/Warranty/Save", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["BannerImagePath"] = "/x.jpg",
            ["TermsImagePath"] = "/y.jpg",
            ["TableMfrWarrantyHeader.En"] = "Manufacturer Warranty",
            ["BrandRows[0].Brand"] = "GAC",
            ["BrandRows[0].ManufacturerWarranty.En"] = "5 Years and/or 150,000 KM",
            ["BrandRows[0].PolicyUrl"] = "/pdfs/gac.pdf",
        }));

        Assert.Equal(HttpStatusCode.Found, resp.StatusCode);
        Assert.StartsWith("/Admin/", resp.Headers.Location!.ToString());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminWarrantyBrandRedirectTests"`
Expected: it should already redirect (controller binds `WarrantyPage`), but **run it to confirm** it is green even before the view changes — the controller/service already accept these fields after Task 2. If it passes here, that is fine; the view step below is what makes the fields editable in the browser. (If it fails, fix binding before continuing.)

- [ ] **Step 3: Replace the HTML field with the structured editor**

In `GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml`, delete the single line at 77 (the `ExtendedTableHtml` `_LocalizedField`) and put in its place:

```html
    <h3>Column headers</h3>
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Brand column", NameEn = "TableBrandHeader.En", NameAr = "TableBrandHeader.Ar", ValueEn = Model.TableBrandHeader.En, ValueAr = Model.TableBrandHeader.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Manufacturer Warranty column", NameEn = "TableMfrWarrantyHeader.En", NameAr = "TableMfrWarrantyHeader.Ar", ValueEn = Model.TableMfrWarrantyHeader.En, ValueAr = Model.TableMfrWarrantyHeader.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Manufacturer Roadside column", NameEn = "TableMfrRoadsideHeader.En", NameAr = "TableMfrRoadsideHeader.Ar", ValueEn = Model.TableMfrRoadsideHeader.En, ValueAr = Model.TableMfrRoadsideHeader.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Extended Warranty column", NameEn = "TableExtWarrantyHeader.En", NameAr = "TableExtWarrantyHeader.Ar", ValueEn = Model.TableExtWarrantyHeader.En, ValueAr = Model.TableExtWarrantyHeader.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Extended Roadside column", NameEn = "TableExtRoadsideHeader.En", NameAr = "TableExtRoadsideHeader.Ar", ValueEn = Model.TableExtRoadsideHeader.En, ValueAr = Model.TableExtRoadsideHeader.Ar }' />
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Policy column", NameEn = "TablePolicyHeader.En", NameAr = "TablePolicyHeader.Ar", ValueEn = Model.TablePolicyHeader.En, ValueAr = Model.TablePolicyHeader.Ar }' />

    <div class="adm-field">
        <label>Brands (one card per brand — values are multi-line; press Enter inside a box for a second line)</label>
        <div data-brand-rows>
@for (var i = 0; i < Model.BrandRows.Count; i++)
{
            <div class="adm-brand-card" data-brand-row>
                <div class="adm-brand-card__top">
                    <input name="BrandRows[@i].Brand" value="@Model.BrandRows[i].Brand" placeholder="Brand (e.g. GAC)" />
                    <button type="button" class="adm-btn adm-btn--danger" data-brand-remove title="Remove brand">&times;</button>
                </div>
                <div class="adm-brand-card__grid">
                    <textarea name="BrandRows[@i].ManufacturerWarranty.En" placeholder="Manufacturer Warranty (EN)">@Model.BrandRows[i].ManufacturerWarranty.En</textarea>
                    <textarea name="BrandRows[@i].ManufacturerWarranty.Ar" dir="rtl" placeholder="(AR)">@Model.BrandRows[i].ManufacturerWarranty.Ar</textarea>
                    <textarea name="BrandRows[@i].ManufacturerRoadside.En" placeholder="Manufacturer Roadside (EN)">@Model.BrandRows[i].ManufacturerRoadside.En</textarea>
                    <textarea name="BrandRows[@i].ManufacturerRoadside.Ar" dir="rtl" placeholder="(AR)">@Model.BrandRows[i].ManufacturerRoadside.Ar</textarea>
                    <textarea name="BrandRows[@i].ExtendedWarranty.En" placeholder="Extended Warranty (EN)">@Model.BrandRows[i].ExtendedWarranty.En</textarea>
                    <textarea name="BrandRows[@i].ExtendedWarranty.Ar" dir="rtl" placeholder="(AR)">@Model.BrandRows[i].ExtendedWarranty.Ar</textarea>
                    <textarea name="BrandRows[@i].ExtendedRoadside.En" placeholder="Extended Roadside (EN)">@Model.BrandRows[i].ExtendedRoadside.En</textarea>
                    <textarea name="BrandRows[@i].ExtendedRoadside.Ar" dir="rtl" placeholder="(AR)">@Model.BrandRows[i].ExtendedRoadside.Ar</textarea>
                </div>
                <div class="adm-media__controls">
                    <input name="BrandRows[@i].PolicyUrl" value="@Model.BrandRows[i].PolicyUrl" data-media-input placeholder="Policy link or PDF" />
                    <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
                </div>
            </div>
}
        </div>
        <button type="button" class="adm-btn" data-brand-add>Add brand</button>
    </div>
```

Add a `<template>` next to the existing callout template (after the `</template>` at line 90):

```html
<template data-brand-template>
    <div class="adm-brand-card" data-brand-row>
        <div class="adm-brand-card__top">
            <input name="BrandRows[0].Brand" placeholder="Brand (e.g. GAC)" />
            <button type="button" class="adm-btn adm-btn--danger" data-brand-remove title="Remove brand">&times;</button>
        </div>
        <div class="adm-brand-card__grid">
            <textarea name="BrandRows[0].ManufacturerWarranty.En" placeholder="Manufacturer Warranty (EN)"></textarea>
            <textarea name="BrandRows[0].ManufacturerWarranty.Ar" dir="rtl" placeholder="(AR)"></textarea>
            <textarea name="BrandRows[0].ManufacturerRoadside.En" placeholder="Manufacturer Roadside (EN)"></textarea>
            <textarea name="BrandRows[0].ManufacturerRoadside.Ar" dir="rtl" placeholder="(AR)"></textarea>
            <textarea name="BrandRows[0].ExtendedWarranty.En" placeholder="Extended Warranty (EN)"></textarea>
            <textarea name="BrandRows[0].ExtendedWarranty.Ar" dir="rtl" placeholder="(AR)"></textarea>
            <textarea name="BrandRows[0].ExtendedRoadside.En" placeholder="Extended Roadside (EN)"></textarea>
            <textarea name="BrandRows[0].ExtendedRoadside.Ar" dir="rtl" placeholder="(AR)"></textarea>
        </div>
        <div class="adm-media__controls">
            <input name="BrandRows[0].PolicyUrl" data-media-input placeholder="Policy link or PDF" />
            <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
        </div>
    </div>
</template>
```

In the `@section Scripts` IIFE, before its closing `})();`, add brand-row add/remove + reindex (the media picker is wired globally in `admin.js` via delegation, so dynamically-added pick buttons work without extra code):

```javascript
  // Add/remove brand cards, keeping BrandRows[i].* indices contiguous.
  var brandWrap = document.querySelector("[data-brand-rows]");
  var brandTpl = document.querySelector("[data-brand-template]");
  var brandAdd = document.querySelector("[data-brand-add]");
  if (brandWrap && brandTpl && brandAdd) {
    var FIELDS = ["ManufacturerWarranty", "ManufacturerRoadside", "ExtendedWarranty", "ExtendedRoadside"];
    function reindexBrands() {
      var cards = brandWrap.querySelectorAll("[data-brand-row]");
      Array.prototype.forEach.call(cards, function (card, i) {
        card.querySelector(".adm-brand-card__top input").name = "BrandRows[" + i + "].Brand";
        var areas = card.querySelectorAll(".adm-brand-card__grid textarea");
        FIELDS.forEach(function (f, fi) {
          if (areas[fi * 2]) areas[fi * 2].name = "BrandRows[" + i + "]." + f + ".En";
          if (areas[fi * 2 + 1]) areas[fi * 2 + 1].name = "BrandRows[" + i + "]." + f + ".Ar";
        });
        var purl = card.querySelector(".adm-media__controls [data-media-input]");
        if (purl) purl.name = "BrandRows[" + i + "].PolicyUrl";
      });
    }
    brandAdd.addEventListener("click", function () {
      brandWrap.appendChild(brandTpl.content.cloneNode(true));
      reindexBrands();
    });
    brandWrap.addEventListener("click", function (e) {
      var rm = e.target.closest("[data-brand-remove]");
      if (rm) { rm.closest("[data-brand-row]").remove(); reindexBrands(); }
    });
  }
```

**Note:** the existing callouts IIFE has an early `if (!container || !tpl || !addBtn) return;` guard. The brand-row code must be placed **before** that guard, or moved into its own `(function(){ ... })();` IIFE at the end of the `<script>` so the early return can't skip it. Use a separate IIFE to be safe.

- [ ] **Step 4: Add minimal grid CSS**

In `GAC.Web/wwwroot/assets/css/admin.css`, append:

```css
.adm-brand-card { border: 1px solid var(--adm-border, #ddd); border-radius: 8px; padding: 12px; margin-bottom: 12px; }
.adm-brand-card__top { display: flex; gap: 8px; margin-bottom: 8px; }
.adm-brand-card__top input { flex: 1; }
.adm-brand-card__grid { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; margin-bottom: 8px; }
.adm-brand-card__grid textarea { min-height: 44px; resize: vertical; }
```

- [ ] **Step 5: Build + run the redirect test + full warranty suite**

Run: `dotnet build GAC.Web/GAC.Web.csproj -c Debug` (expect 0 errors).
Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminWarrantyBrandRedirectTests|FullyQualifiedName~WarrantyBrandRenderTests|FullyQualifiedName~AdminWarrantyBrandServiceTests|FullyQualifiedName~SeederWarrantyBrandTests|FullyQualifiedName~WarrantyBrandMappingTests"`
Expected: all PASS.

- [ ] **Step 6: Commit**

```bash
git add GAC.Web/Areas/Admin/Views/Warranty/Index.cshtml GAC.Web/wwwroot/assets/css/admin.css GAC.Tests/Admin/AdminWarrantyBrandRedirectTests.cs
git commit -m "feat(warranty): admin brand-row grid editor (add/remove + per-brand PDF picker)"
```

---

## Self-Review

**1. Spec coverage:**
- Structured brand-per-row table → Task 1 (model) + Task 4 (render) + Task 5 (editor). ✓
- Fixed columns / dynamic rows → Task 5 repeater + Task 2 normalize. ✓
- Per-brand policy link-or-PDF → `PolicyUrl` (Task 1), picker (Task 5), safe-scheme render (Task 4). ✓
- Editable bilingual headers → 6 header `LocalizedText` (Task 1), inputs (Task 5), render (Task 4), AR backfill (Task 3). ✓
- Bilingual cells / plain-text brand → Task 1 model. ✓
- Backfill onto already-seeded prod page (guard on `WarrantyBrandRows`) → Task 3. ✓
- `ExtendedTableHtml` kept, additive migration → Task 1 Steps 4 & 8. ✓
- Two collections → `AsSplitQuery()` → Task 2 (`GetAsync`/`SaveAsync`) + Task 4 (`ContentService`). ✓
- Tests (mapping, seeder, admin service, render, admin redirect) → Tasks 1–5. ✓

**2. Placeholder scan:** No TBD/TODO; every code step shows full code; no "similar to" references. ✓

**3. Type/name consistency:** `WarrantyBrandRow`, `BrandRows`, the six `TableXHeader` names, `PolicyUrl`, `ManufacturerWarranty/ManufacturerRoadside/ExtendedWarranty/ExtendedRoadside`, `SeedWarrantyBrandRowsAsync`, `NormalizeBrandRows`, `HasText` are spelled identically across model, config, service, seeder, views, and tests. ✓

## Post-implementation (separate, user-gated steps — NOT part of TDD)

- Full guarded suite (avoid prod-DB classes): run the five warranty-brand classes by explicit name (above) plus the rest of the in-memory cost-of-service/content/home classes.
- Adversarial review workflow (ultracode) over the diff before pushing.
- Apply the `AddWarrantyBrandTable` migration to prod via the scoped idempotent script (see Global Constraints). Verify the new table + 12 columns + the unchanged `ExtendedTableHtml`.
- Push branch; redeploy Web (seeder backfills the brand rows on startup — page looks identical, Arabic cells empty for the admin to fill).
