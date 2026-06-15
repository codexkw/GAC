# Phase 6b — Editable HTML Bodies Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the currently-hardcoded vehicle-detail, content-page, and contact-us "Locate Us" markup DB-editable as a bilingual raw-HTML `BodyHtml` field, rendered through generic templates and editable in the admin panel — with zero visual change to the live site.

**Architecture:** Add an owned `LocalizedText BodyHtml` to `Vehicle`, `ContentPage`, and `FormPage` (additive `AddBodyHtml` migration — the first Phase-6 migration, applied to the GAC DB). The exact existing partial markup is transcribed into per-slug **embedded-resource `.html` files** under `GAC.Infrastructure/SeedContent/`; an idempotent `ContentSeeder.EnsureBodiesAsync` backfills `BodyHtml.En` (Arabic left blank → English fallback, matching the Phase-4 prose decision). Three generic templates render `@Html.Raw(BodyHtml.Localize())` and the 16 per-slug partials (9 vehicle + 6 content + 1 contact-us) are deleted; the 5 **functional** lead-form partials (real `<form>` + anti-forgery) stay, so `Forms/Page.cshtml` branches on `FormType.Contact`. The admin Vehicles/ContentPages/FormPages editors gain a raw-HTML code-editor field (the `_LocalizedField` `Code=true` editor already exists from Phase 6a).

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (SQL Server, owned types → `Foo_En`/`Foo_Ar` columns), xUnit + `WebApplicationFactory`, embedded resources.

---

## Background the implementer MUST know

- **`LocalizedText`** (`GAC.Core/Content/LocalizedText.cs`): `{ string? En; string? Ar; }`, `Get(culture)` falls back En→Ar→"". `.Localize()` extension reads `CurrentUICulture`. Implicit `string`→`LocalizedText` (sets `En`). Owned type configured via `b.OwnsLocalized(x => x.Foo)` (see `ContentConfigurations.cs`), producing nullable `nvarchar(max)` columns `{Entity}_Foo_En` / `{Entity}_Foo_Ar`.
- **`@Html.Raw` does NOT re-parse Razor.** A runtime string is emitted verbatim. So stored bodies must contain the *final* characters: a single `@` (not `@@`), literal titles/names (not `@Model...`), real `"`/`&amp;` as-is.
- **App does not auto-migrate** (`Program.cs` comment: "Does NOT run migrations") but **does seed at startup** (idempotent). Tests boot the real Development DB via `DevWebApplicationFactory` (`HomePageSmokeTests.cs`). Therefore the migration must be applied to the GAC DB, and `EnsureBodiesAsync` must be added only *after* the columns exist.
- **Secrets / public repo (binding):** repo `codexkw/GAC` is PUBLIC. Real DB/SMTP creds live ONLY in gitignored `GAC.Web/appsettings.Development.json`; committed `appsettings.json` keeps `__SET_LOCALLY__`. Scoped `git add` with explicit paths only — never `git add -A`/`.`. Never print or commit the connection string or the `sa` password. Scan staged files (incl. `.md`) for secrets before every commit.
- **EF design-time + seeding interaction:** running `dotnet ef` builds the app host, which *may* execute the startup seeding block. This is why Task 1 (migration) precedes Task 5 (wiring `EnsureBodiesAsync`): during Task 1 the seeding pipeline does NOT yet touch `BodyHtml`, so nothing queries columns that don't exist. Run all `dotnet ef` commands with `ASPNETCORE_ENVIRONMENT=Development` so the real connection string resolves. A fallback design-time factory is provided in Task 1 only if the straightforward commands fail.

### Visible vehicle slugs → seeded Hero image path → English `Name` (for `@hero` / `@Model.Name.Localize()` substitution)

| slug | Hero path (`@hero`) | Name (`@Model.Name.Localize()`) |
|---|---|---|
| `gs8traveller` | `/assets/img/hero-gs8-traveller.png` | `GS8 Traveller` |
| `gs8` | `/assets/img/m-gs8.jpg` | `GS8` |
| `gs3emzoom` | `/assets/img/hero-gs3-emzoom.jpg` | `EMZOOM` |
| `emkoo` | `/assets/img/m-emkoo.png` | `EMKOO` |
| `empow` | `/assets/img/m-empow.png` | `EMPOW` |
| `m8` | `/assets/img/hero-m8.png` | `M8` |
| `empow-sport` | `/assets/img/hero-empow-sport.jpg` | `EMPOW R` |
| `hyptec-ht` | `/assets/img/m-hyptec-ht.png` | `HYPTEC HT` |
| `gs4` | `/assets/img/hero-gs4.jpg` | `GS4 MAX` |

(The 2 hidden vehicles `aion-v`/`aion-es` have no detail partials and 404; no body is seeded for them.)

### Content-page slugs → English `Title` (for `@Model.Title.Localize()` substitution)

| slug | Title |
|---|---|
| `about` | `About Us` |
| `warranty` | `Warranty` |
| `privacy-policy` | `Privacy Policy` |
| `finance` | `Tayseer Finance` |
| `cost-of-service` | `Cost of Service` |
| `road-assistance` | `Roadside Assistance` |

### Transcription procedure (used in Tasks 2–4) — turn a `.cshtml` partial into a static seed `.html`

Copy the partial's body **verbatim**, then apply ONLY these deterministic edits:
1. **Delete** the leading `@model …` line and any `@{ … }` code block (and the blank line they leave).
2. **Substitute Razor tokens** with their literal rendered value:
   - `@Model.Title.Localize()` → the page's English Title (content table above).
   - `@Model.Name.Localize()` → the vehicle's English Name (vehicle table above).
   - `@hero` → the vehicle's seeded Hero path (vehicle table above).
3. **Un-escape Razor:** replace every `@@` with a single `@` (appears in `_contact-us` Google-Maps URLs).
4. Leave everything else byte-for-byte identical — including `"`, `&amp;`, `&gt;`, SVG markup, `data-*` hooks, `class="mp-*"`, and the static (non-posting) enquiry `<form … data-form novalidate>` on vehicle pages (it has no `action`/anti-forgery and is purely client-side; storing it as static HTML preserves identical behaviour).

**Verification for every produced `.html` (grep, expect ZERO matches):** `@model`, `@{`, `@@`, `@Model`, `@hero`. And confirm the expected literal is present (the Hero path / Name / Title).

---

## File Structure

**Create:**
- `GAC.Infrastructure/SeedContent/content/{about,warranty,privacy-policy,finance,cost-of-service,road-assistance}.html` (6)
- `GAC.Infrastructure/SeedContent/forms/contact-us.html` (1)
- `GAC.Infrastructure/SeedContent/vehicles/{gs8traveller,gs8,gs3emzoom,emkoo,empow,m8,empow-sport,hyptec-ht,gs4}.html` (9)
- `GAC.Infrastructure/Migrations/<timestamp>_AddBodyHtml.cs` (+ `.Designer.cs`, generated)

**Modify:**
- `GAC.Core/Content/Vehicle.cs`, `ContentPage.cs`, `FormPage.cs` (add `BodyHtml`)
- `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (3 × `OwnsLocalized`)
- `GAC.Infrastructure/GAC.Infrastructure.csproj` (embed `SeedContent/**/*.html`)
- `GAC.Infrastructure/Data/ContentSeeder.cs` (`EnsureBodiesAsync` + call it)
- `GAC.Web/Views/Vehicles/Detail.cshtml`, `Views/Content/Page.cshtml`, `Views/Forms/Page.cshtml`
- `GAC.Infrastructure/Services/AdminVehicleService.cs`, `AdminPageService.cs`
- `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`, `ContentPages/Edit.cshtml`, `FormPages/Edit.cshtml`
- `GAC.Tests/ContentSeederTests.cs`, `Admin/AdminVehicleServiceTests.cs`, `Admin/AdminPageServiceTests.cs`, `VehiclePagesTests.cs`, `ContentPagesTests.cs`, `FormPagesTests.cs`
- `docs/HANDOFF.md`

**Delete (Task 6):** all 9 `Views/Vehicles/Models/_*.cshtml`, all 6 `Views/Content/Pages/_*.cshtml`, and `Views/Forms/Forms/_contact-us.cshtml` (keep the other 5 `Views/Forms/Forms/_*.cshtml`).

---

## Task 1: Add `BodyHtml` to entities + EF config + `AddBodyHtml` migration (applied to DB)

**Files:**
- Modify: `GAC.Core/Content/Vehicle.cs`, `GAC.Core/Content/ContentPage.cs`, `GAC.Core/Content/FormPage.cs`
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`
- Create: `GAC.Infrastructure/Migrations/<timestamp>_AddBodyHtml.cs` (generated)
- Test: `GAC.Tests/DbContextModelTests.cs`

- [ ] **Step 1: Add `BodyHtml` to the three entities**

In `Vehicle.cs`, after the `IntroText` line (keep grouping with the other bilingual fields):
```csharp
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();
```
In `ContentPage.cs`, after `Title`:
```csharp
    public LocalizedText Title { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();
```
In `FormPage.cs`, after `IntroText`:
```csharp
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();
```

- [ ] **Step 2: Configure the owned column in `ContentConfigurations.cs`**

In `VehicleConfig.Configure`, add after `b.OwnsLocalized(v => v.IntroText);`:
```csharp
        b.OwnsLocalized(v => v.BodyHtml);
```
In `ContentPageConfig.Configure`, add after `b.OwnsLocalized(p => p.Title);`:
```csharp
        b.OwnsLocalized(p => p.BodyHtml);
```
In `FormPageConfig.Configure`, add after `b.OwnsLocalized(p => p.IntroText);`:
```csharp
        b.OwnsLocalized(p => p.BodyHtml);
```

- [ ] **Step 3: Add a model test for the new columns**

In `GAC.Tests/DbContextModelTests.cs`, follow the file's existing style to add a fact that the three `BodyHtml` owned navigations are mapped. (Read the file first to match its helper/assert pattern.) Example shape:
```csharp
    [Fact]
    public void BodyHtml_IsMapped_OnVehicleContentPageAndFormPage()
    {
        using var ctx = NewContext(); // use the file's existing context factory helper
        foreach (var clr in new[] { typeof(Vehicle), typeof(ContentPage), typeof(FormPage) })
        {
            var et = ctx.Model.FindEntityType(clr)!;
            Assert.NotNull(et.FindNavigation("BodyHtml"));
        }
    }
```
If `DbContextModelTests.cs` uses a different mechanism (e.g. a shared options builder), mirror that exactly instead of `NewContext()`.

- [ ] **Step 4: Build**

Run: `dotnet build Solution/GAC.sln`
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Generate the migration**

From the `Solution` directory, with the Development environment so the connection string resolves:
```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef migrations add AddBodyHtml --project GAC.Infrastructure --startup-project GAC.Web --output-dir Migrations
```
Expected: `Done. To undo this action, use 'ef migrations remove'`. New files appear under `GAC.Infrastructure/Migrations/`. Open the generated `*_AddBodyHtml.cs` and confirm it only **adds** columns `Vehicle_BodyHtml_En`/`_Ar`, `ContentPage_BodyHtml_En`/`_Ar`, `FormPage_BodyHtml_En`/`_Ar` (all `nvarchar(max)`, nullable) and contains no destructive `DropColumn`/`DropTable`.

> **Fallback (only if `dotnet ef` fails because the startup seeding can't connect or errors):** create `GAC.Infrastructure/Data/DesignTimeDbContextFactory.cs` so EF skips the web host entirely. It reads the connection from the `ConnectionStrings__Default` environment variable (set it for the shell from the value in `appsettings.Development.json` — do NOT hardcode or commit it):
> ```csharp
> using Microsoft.EntityFrameworkCore;
> using Microsoft.EntityFrameworkCore.Design;
>
> namespace GAC.Infrastructure.Data;
>
> public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
> {
>     public ApplicationDbContext CreateDbContext(string[] args)
>     {
>         var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
>             ?? throw new InvalidOperationException(
>                 "Set ConnectionStrings__Default for design-time migrations.");
>         var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
>         return new ApplicationDbContext(options);
>     }
> }
> ```
> This file is safe to commit (no secret). Then re-run the `migrations add` command.

- [ ] **Step 6: Apply the migration to the GAC database**

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet ef database update --project GAC.Infrastructure --startup-project GAC.Web
```
Expected: `Applying migration '<timestamp>_AddBodyHtml'.` then `Done.` (Additive, nullable columns — no data loss.) Do not echo the connection string.

- [ ] **Step 7: Run the model test**

Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~DbContextModelTests"`
Expected: PASS.

- [ ] **Step 8: Commit**
```powershell
git add Solution/GAC.Core/Content/Vehicle.cs Solution/GAC.Core/Content/ContentPage.cs Solution/GAC.Core/Content/FormPage.cs Solution/GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs Solution/GAC.Infrastructure/Migrations Solution/GAC.Tests/DbContextModelTests.cs
# include Solution/GAC.Infrastructure/Data/DesignTimeDbContextFactory.cs only if the fallback was used
git commit -m "feat(admin): add BodyHtml owned field + AddBodyHtml migration (Phase 6b)"
```

---

## Task 2: Embed seed-content infrastructure + content (6) and contact-us (1) seed bodies

**Files:**
- Modify: `GAC.Infrastructure/GAC.Infrastructure.csproj`
- Create: `GAC.Infrastructure/SeedContent/content/{about,warranty,privacy-policy,finance,cost-of-service,road-assistance}.html`
- Create: `GAC.Infrastructure/SeedContent/forms/contact-us.html`
- Source partials (read-only): `GAC.Web/Views/Content/Pages/_*.cshtml`, `GAC.Web/Views/Forms/Forms/_contact-us.cshtml`

- [ ] **Step 1: Embed the seed files in the csproj**

In `GAC.Infrastructure.csproj`, add this ItemGroup (e.g. right after the existing `<ProjectReference>` ItemGroup):
```xml
  <ItemGroup>
    <EmbeddedResource Include="SeedContent\**\*.html" />
  </ItemGroup>
```

- [ ] **Step 2: Transcribe the 6 content pages**

For each content slug, read `GAC.Web/Views/Content/Pages/_<slug>.cshtml` and produce `GAC.Infrastructure/SeedContent/content/<slug>.html` using the **Transcription procedure** above. The only Razor in these is `@Model.Title.Localize()` → the English Title from the content table (e.g. `_road-assistance.cshtml`'s `<h1>@Model.Title.Localize()</h1>` becomes `<h1>Roadside Assistance</h1>`). No `@@` expected here. Drop the `@model` line.

- [ ] **Step 3: Transcribe contact-us**

Read `GAC.Web/Views/Forms/Forms/_contact-us.cshtml` → produce `GAC.Infrastructure/SeedContent/forms/contact-us.html`. Drop the `@model` line. It has **no** `@Model` references. Replace every `@@` with `@` (the Google-Maps URLs contain `/@@29.…` → `/@29.…`). Keep `&amp;` entities and all SVG/markup verbatim.

- [ ] **Step 4: Verify the produced files**

Run (from repo root):
```powershell
Select-String -Path "Solution/GAC.Infrastructure/SeedContent/content/*.html","Solution/GAC.Infrastructure/SeedContent/forms/*.html" -Pattern '@model','@\{','@@','@Model'
```
Expected: **no matches**. Spot-check `content/road-assistance.html` contains `<h1>Roadside Assistance</h1>` and `forms/contact-us.html` contains `/@29.3138141` (single `@`).

- [ ] **Step 5: Build (confirms the csproj change is valid)**

Run: `dotnet build Solution/GAC.Infrastructure`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**
```powershell
git add Solution/GAC.Infrastructure/GAC.Infrastructure.csproj Solution/GAC.Infrastructure/SeedContent/content Solution/GAC.Infrastructure/SeedContent/forms
git commit -m "feat(admin): embed content + contact-us seed bodies (Phase 6b)"
```

---

## Task 3: Vehicle seed bodies — group A (`gs8traveller`, `gs8`, `gs3emzoom`, `emkoo`, `empow`)

**Files:**
- Create: `GAC.Infrastructure/SeedContent/vehicles/{gs8traveller,gs8,gs3emzoom,emkoo,empow}.html`
- Source partials (read-only): `GAC.Web/Views/Vehicles/Models/_<slug>.cshtml`

- [ ] **Step 1: Transcribe each of the 5 vehicles**

For each slug, read `GAC.Web/Views/Vehicles/Models/_<slug>.cshtml` and produce `GAC.Infrastructure/SeedContent/vehicles/<slug>.html` via the **Transcription procedure**. Per file: delete the `@model` line **and** the `@{ var hero = … }` block; substitute `@hero` → the Hero path and `@Model.Name.Localize()` → the Name (vehicle table above, e.g. `gs8` → Hero `/assets/img/m-gs8.jpg`, Name `GS8`). Grep each source for any other `@…` token before finishing — if one exists, resolve it to its literal rendered value.

- [ ] **Step 2: Verify**
```powershell
Select-String -Path "Solution/GAC.Infrastructure/SeedContent/vehicles/gs8traveller.html","Solution/GAC.Infrastructure/SeedContent/vehicles/gs8.html","Solution/GAC.Infrastructure/SeedContent/vehicles/gs3emzoom.html","Solution/GAC.Infrastructure/SeedContent/vehicles/emkoo.html","Solution/GAC.Infrastructure/SeedContent/vehicles/empow.html" -Pattern '@model','@\{','@@','@Model','@hero'
```
Expected: **no matches**. Spot-check `gs8.html` contains `src="/assets/img/m-gs8.jpg"` and `<h1 class="mp-hero__title">GS8</h1>`.

- [ ] **Step 3: Commit**
```powershell
git add Solution/GAC.Infrastructure/SeedContent/vehicles/gs8traveller.html Solution/GAC.Infrastructure/SeedContent/vehicles/gs8.html Solution/GAC.Infrastructure/SeedContent/vehicles/gs3emzoom.html Solution/GAC.Infrastructure/SeedContent/vehicles/emkoo.html Solution/GAC.Infrastructure/SeedContent/vehicles/empow.html
git commit -m "feat(admin): embed vehicle seed bodies group A (Phase 6b)"
```

---

## Task 4: Vehicle seed bodies — group B (`m8`, `empow-sport`, `hyptec-ht`, `gs4`)

**Files:**
- Create: `GAC.Infrastructure/SeedContent/vehicles/{m8,empow-sport,hyptec-ht,gs4}.html`
- Source partials (read-only): `GAC.Web/Views/Vehicles/Models/_<slug>.cshtml`

- [ ] **Step 1: Transcribe each of the 4 vehicles**

Same **Transcription procedure** as Task 3. Substitutions: `m8` → Hero `/assets/img/hero-m8.png`, Name `M8`; `empow-sport` → Hero `/assets/img/hero-empow-sport.jpg`, Name `EMPOW R`; `hyptec-ht` → Hero `/assets/img/m-hyptec-ht.png`, Name `HYPTEC HT`; `gs4` → Hero `/assets/img/hero-gs4.jpg`, Name `GS4 MAX`. Grep each source for any other `@…` token and resolve it.

- [ ] **Step 2: Verify**
```powershell
Select-String -Path "Solution/GAC.Infrastructure/SeedContent/vehicles/m8.html","Solution/GAC.Infrastructure/SeedContent/vehicles/empow-sport.html","Solution/GAC.Infrastructure/SeedContent/vehicles/hyptec-ht.html","Solution/GAC.Infrastructure/SeedContent/vehicles/gs4.html" -Pattern '@model','@\{','@@','@Model','@hero'
```
Expected: **no matches**. Spot-check `gs4.html` contains `src="/assets/img/hero-gs4.jpg"` and `<h1 class="mp-hero__title">GS4 MAX</h1>`.

- [ ] **Step 3: Commit**
```powershell
git add Solution/GAC.Infrastructure/SeedContent/vehicles/m8.html Solution/GAC.Infrastructure/SeedContent/vehicles/empow-sport.html Solution/GAC.Infrastructure/SeedContent/vehicles/hyptec-ht.html Solution/GAC.Infrastructure/SeedContent/vehicles/gs4.html
git commit -m "feat(admin): embed vehicle seed bodies group B (Phase 6b)"
```

---

## Task 5: `EnsureBodiesAsync` seeding backfill + tests

**Files:**
- Modify: `GAC.Infrastructure/Data/ContentSeeder.cs`
- Test: `GAC.Tests/ContentSeederTests.cs`

- [ ] **Step 1: Write failing tests**

Add to `GAC.Tests/ContentSeederTests.cs` (uses the existing in-memory `BuildServices` helper; embedded resources are read from the assembly so this works in-memory):
```csharp
    [Fact]
    public async Task Seeds_BodyHtml_ForVehiclesContentAndContactUs()
    {
        var sp = BuildServices("seed-bodies");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        var gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        Assert.Contains("mp-hero__title", gs4.BodyHtml.En);
        Assert.Contains("GS4 MAX", gs4.BodyHtml.En);

        var about = await db.ContentPages.SingleAsync(p => p.Slug == "about");
        Assert.False(string.IsNullOrWhiteSpace(about.BodyHtml.En));

        var contact = await db.FormPages.SingleAsync(p => p.Slug == "contact-us");
        Assert.Contains("dir-grid", contact.BodyHtml.En);

        // Hidden vehicles have no seed body.
        var aion = await db.Vehicles.SingleAsync(v => v.Slug == "aion-v");
        Assert.True(string.IsNullOrEmpty(aion.BodyHtml.En));

        // Arabic is intentionally left blank (English fallback at render time).
        Assert.True(string.IsNullOrEmpty(gs4.BodyHtml.Ar));
    }

    [Fact]
    public async Task BodyBackfill_IsIdempotent_AndPreservesEditedBody()
    {
        var sp = BuildServices("seed-bodies-idem");
        await ContentSeeder.SeedAsync(sp);
        var db = sp.GetRequiredService<ApplicationDbContext>();

        // Simulate an admin edit, then re-run the seeder.
        var gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        gs4.BodyHtml = new LocalizedText { En = "<p>edited</p>", Ar = "<p>محرر</p>" };
        await db.SaveChangesAsync();

        await ContentSeeder.SeedAsync(sp);
        gs4 = await db.Vehicles.SingleAsync(v => v.Slug == "gs4");
        Assert.Equal("<p>edited</p>", gs4.BodyHtml.En);   // backfill did NOT clobber a non-blank body
        Assert.Equal("<p>محرر</p>", gs4.BodyHtml.Ar);
    }
```

- [ ] **Step 2: Run to confirm they fail**

Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~ContentSeederTests"`
Expected: the two new tests FAIL (bodies are empty — `EnsureBodiesAsync` not implemented yet).

- [ ] **Step 3: Implement `EnsureBodiesAsync`**

In `ContentSeeder.cs`, add `using System.Reflection;` at the top. Add the call at the end of `SeedAsync` (after `EnsureArabicAsync`):
```csharp
        await EnsureArabicAsync(db);
        await EnsureBodiesAsync(db);
```
Then add the method (place it just below `EnsureArabicAsync`):
```csharp
    // ──────────────────────────────────────────────
    //  HTML body backfill (Phase 6b). Idempotent: only sets a row's English
    //  BodyHtml when it is currently blank. Arabic is left null → English
    //  fallback at render time (consistent with the Phase-4 prose decision).
    //  Source markup lives in embedded SeedContent/<area>/<slug>.html files.
    // ──────────────────────────────────────────────
    private static async Task EnsureBodiesAsync(ApplicationDbContext db)
    {
        var changed = false;

        foreach (var v in await db.Vehicles.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(v.BodyHtml?.En)) continue;
            var html = ReadSeedBody("vehicles", v.Slug);
            if (html is null) continue; // hidden vehicles have no seed body
            v.BodyHtml = new LocalizedText { En = html };
            changed = true;
        }

        foreach (var p in await db.ContentPages.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(p.BodyHtml?.En)) continue;
            var html = ReadSeedBody("content", p.Slug);
            if (html is null) continue;
            p.BodyHtml = new LocalizedText { En = html };
            changed = true;
        }

        // Only the contact-us "Locate Us" directory has a body; the 5 functional
        // form pages keep their server-rendered partials.
        foreach (var f in await db.FormPages.ToListAsync())
        {
            if (!string.IsNullOrWhiteSpace(f.BodyHtml?.En)) continue;
            var html = ReadSeedBody("forms", f.Slug);
            if (html is null) continue;
            f.BodyHtml = new LocalizedText { En = html };
            changed = true;
        }

        if (changed) await db.SaveChangesAsync();
    }

    private static readonly Assembly SeedAssembly = typeof(ContentSeeder).Assembly;

    /// <summary>Reads SeedContent/&lt;area&gt;/&lt;slug&gt;.html as an embedded resource, or null if absent.
    /// Matches case-insensitively and treats '-'/'_' as equivalent to tolerate manifest-name mangling.</summary>
    private static string? ReadSeedBody(string area, string slug)
    {
        static string Norm(string s) => s.Replace('-', '_').ToLowerInvariant();
        var wanted = Norm($".SeedContent.{area}.{slug}.html");
        var name = SeedAssembly.GetManifestResourceNames()
            .FirstOrDefault(n => Norm(n).EndsWith(wanted, StringComparison.Ordinal));
        if (name is null) return null;
        using var stream = SeedAssembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
```

- [ ] **Step 4: Run the seeder tests**

Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~ContentSeederTests"`
Expected: all PASS (including the existing idempotency/count tests).

- [ ] **Step 5: Commit**
```powershell
git add Solution/GAC.Infrastructure/Data/ContentSeeder.cs Solution/GAC.Tests/ContentSeederTests.cs
git commit -m "feat(admin): backfill BodyHtml from embedded seed files (Phase 6b)"
```

---

## Task 6: Generic templates + delete the 16 per-slug partials

**Files:**
- Modify: `GAC.Web/Views/Vehicles/Detail.cshtml`, `GAC.Web/Views/Content/Page.cshtml`, `GAC.Web/Views/Forms/Page.cshtml`
- Delete: 9 × `GAC.Web/Views/Vehicles/Models/_*.cshtml`, 6 × `GAC.Web/Views/Content/Pages/_*.cshtml`, `GAC.Web/Views/Forms/Forms/_contact-us.cshtml`
- Test: `GAC.Tests/VehiclePagesTests.cs`, `ContentPagesTests.cs`, `FormPagesTests.cs`

> Bodies are seeded into the **real** DB the moment the app/test host next boots (startup seeding runs `EnsureBodiesAsync`; columns exist from Task 1). So the render-marker assertions below will pass against freshly-seeded bodies.

- [ ] **Step 1: Replace `Views/Vehicles/Detail.cshtml`** with:
```cshtml
@model GAC.Core.Content.Vehicle
@{ Layout = "_Layout"; }
@Html.Raw(Model.BodyHtml.Localize())
```

- [ ] **Step 2: Replace `Views/Content/Page.cshtml`** with:
```cshtml
@model GAC.Core.Content.ContentPage
@{ Layout = "_Layout"; }
@Html.Raw(Model.BodyHtml.Localize())
```

- [ ] **Step 3: Replace `Views/Forms/Page.cshtml`** with (branch: contact-us → body; functional forms → existing partial):
```cshtml
@model GAC.Web.Models.FormPageViewModel
@{ Layout = "_Layout"; }
@if (TempData["FormSubmitted"] != null)
{
  <section class="section">
    <div class="container">
      <div style="max-width:680px;margin:48px auto;padding:32px;text-align:center;border:1px solid var(--c-line);border-radius:8px;background:var(--c-bg-2)">
        <h3 style="margin-bottom:8px">@L["Thanks — we received your request."]</h3>
        <p class="muted">@L["A representative will contact you within one business day."]</p>
      </div>
    </div>
  </section>
}
else if (Model.Page.FormType == GAC.Core.Content.FormType.Contact)
{
  @Html.Raw(Model.Page.BodyHtml.Localize())
}
else
{
  <partial name="~/Views/Forms/Forms/_@(Model.Page.Slug).cshtml" model="Model" />
}
```

- [ ] **Step 4: Delete the migrated partials**
```powershell
Remove-Item Solution/GAC.Web/Views/Vehicles/Models/_*.cshtml
Remove-Item Solution/GAC.Web/Views/Content/Pages/_*.cshtml
Remove-Item Solution/GAC.Web/Views/Forms/Forms/_contact-us.cshtml
```
Confirm the 5 functional form partials remain: `Get-ChildItem Solution/GAC.Web/Views/Forms/Forms/` should list `_book-a-service.cshtml`, `_book-a-test-drive.cshtml`, `_request-a-quote.cshtml`, `_fleet.cshtml`, `_recall-enquiry.cshtml`.

- [ ] **Step 5: Strengthen the render tests with body markers**

In `VehiclePagesTests.cs`, after the status assertion in `VehiclePages_Render200`, add a body-marker check:
```csharp
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("mp-hero", html);   // the DB body rendered, not an empty page
```
In `ContentPagesTests.cs` `ContentPages_Render200`, add:
```csharp
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("section", html);
```
In `FormPagesTests.cs`, add a dedicated fact that contact-us now renders the directory body (and keep the existing 200 theory for the 5 functional forms):
```csharp
    [Fact]
    public async Task ContactUs_RendersDirectoryBody_FromDb()
    {
        var res = await _factory.CreateClient().GetAsync("/contact-us");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("dir-grid", html);          // the seeded "Locate Us" directory
        Assert.DoesNotContain("data-form", html);   // contact-us has no functional form
    }
```

- [ ] **Step 6: Build + run the page render tests**

Run: `dotnet build Solution/GAC.sln` (expected: 0 errors — a missing partial reference would fail here since Razor compiles at build time).
Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~VehiclePagesTests|FullyQualifiedName~ContentPagesTests|FullyQualifiedName~FormPagesTests"`
Expected: all PASS.

- [ ] **Step 7: Commit**
```powershell
git add Solution/GAC.Web/Views/Vehicles/Detail.cshtml Solution/GAC.Web/Views/Content/Page.cshtml Solution/GAC.Web/Views/Forms/Page.cshtml Solution/GAC.Web/Views/Vehicles/Models Solution/GAC.Web/Views/Content/Pages Solution/GAC.Web/Views/Forms/Forms Solution/GAC.Tests/VehiclePagesTests.cs Solution/GAC.Tests/ContentPagesTests.cs Solution/GAC.Tests/FormPagesTests.cs
git commit -m "feat(admin): render BodyHtml via generic templates; delete per-slug partials (Phase 6b)"
```

---

## Task 7: Admin editors — raw-HTML body field + service persistence

**Files:**
- Modify: `GAC.Infrastructure/Services/AdminVehicleService.cs`, `GAC.Infrastructure/Services/AdminPageService.cs`
- Modify: `GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml`, `ContentPages/Edit.cshtml`, `FormPages/Edit.cshtml`
- Test: `GAC.Tests/Admin/AdminVehicleServiceTests.cs`, `GAC.Tests/Admin/AdminPageServiceTests.cs`

- [ ] **Step 1: Write failing service tests**

Read the two test files to match their existing detached-binding pattern (the Phase-6a tests pass *fresh detached* entities into update methods — preserve that). Add:

To `AdminVehicleServiceTests.cs` — assert `UpdateAsync` persists `BodyHtml`:
```csharp
    [Fact]
    public async Task UpdateAsync_PersistsBodyHtml()
    {
        // Arrange: seed a vehicle via the file's existing context helper, capture its Id.
        // Act: call UpdateAsync with a fresh detached Vehicle carrying BodyHtml + the other required fields.
        // Assert: reloaded vehicle has the new BodyHtml.En.
    }
```
Implement it concretely using the file's helpers — set `BodyHtml = new LocalizedText { En = "<p>new body</p>" }` on the detached update instance (and copy the same Name/Slug the existing tests use so validation/round-trip holds), then reload and `Assert.Equal("<p>new body</p>", reloaded.BodyHtml.En);`.

To `AdminPageServiceTests.cs` — extend the existing content + form update tests (or add two facts) to set and assert `BodyHtml.En` round-trips through `UpdateContentAsync` and `UpdateFormAsync`, and that other fields (Slug, Sections, FormType) are still preserved.

- [ ] **Step 2: Run to confirm failure**

Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~AdminVehicleServiceTests|FullyQualifiedName~AdminPageServiceTests"`
Expected: the new assertions FAIL (services don't copy `BodyHtml` yet).

- [ ] **Step 3: Copy `BodyHtml` in the services**

In `AdminVehicleService.UpdateAsync`, add after `existing.IntroText = vehicle.IntroText;`:
```csharp
        existing.BodyHtml = vehicle.BodyHtml;
```
In `AdminPageService.UpdateContentAsync`, extend the copy line:
```csharp
        e.Title = page.Title; e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription;
        e.BodyHtml = page.BodyHtml;
        e.IsVisible = page.IsVisible;
```
In `AdminPageService.UpdateFormAsync`, extend the copy line:
```csharp
        e.Title = page.Title; e.IntroText = page.IntroText;
        e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription;
        e.BodyHtml = page.BodyHtml; e.IsVisible = page.IsVisible;
```

- [ ] **Step 4: Add the body editor to the three admin Edit views**

In `Areas/Admin/Views/Vehicles/Edit.cshtml`, add after the `MetaDescription` `_LocalizedField` partial (line ~24), before the `Price from` field:
```cshtml
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Page body (HTML)", NameEn = "BodyHtml.En", NameAr = "BodyHtml.Ar", ValueEn = Model.BodyHtml.En, ValueAr = Model.BodyHtml.Ar, Code = true }' />
```

In `Areas/Admin/Views/ContentPages/Edit.cshtml`, **replace** the line `<p class="adm-note">Page body is edited in Phase 6b.</p>` with:
```cshtml
    <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Page body (HTML)", NameEn = "BodyHtml.En", NameAr = "BodyHtml.Ar", ValueEn = Model.BodyHtml.En, ValueAr = Model.BodyHtml.Ar, Code = true }' />
```

In `Areas/Admin/Views/FormPages/Edit.cshtml`, **replace** the line `<p class="adm-note">Page body is edited in Phase 6b.</p>` with a conditional (only the contact/"Locate Us" page uses a body; functional forms render their server-side form):
```cshtml
    @if (Model.FormType == GAC.Core.Content.FormType.Contact)
    {
        <partial name="_LocalizedField" model='new LocalizedFieldModel { Label = "Page body (HTML)", NameEn = "BodyHtml.En", NameAr = "BodyHtml.Ar", ValueEn = Model.BodyHtml.En, ValueAr = Model.BodyHtml.Ar, Code = true }' />
    }
    else
    {
        <p class="adm-note">This page renders a functional lead form; only its title, intro and meta are editable.</p>
    }
```
(`FormType` is already enum-typed on `Model`; `GAC.Core.Content` is fully qualified to avoid a new `@using`.)

> Note: the `Vehicle.Save` and `FormPage.Save`/`ContentPage.Save` POST actions model-bind the full entity, so the new `BodyHtml.En`/`BodyHtml.Ar` textarea fields bind automatically — no controller change is needed.

- [ ] **Step 5: Build + run admin service tests**

Run: `dotnet build Solution/GAC.sln` (expected 0 errors — the new `_LocalizedField` partial usages compile at build time).
Run: `dotnet test Solution/GAC.Tests --filter "FullyQualifiedName~AdminVehicleServiceTests|FullyQualifiedName~AdminPageServiceTests"`
Expected: all PASS.

- [ ] **Step 6: Commit**
```powershell
git add Solution/GAC.Infrastructure/Services/AdminVehicleService.cs Solution/GAC.Infrastructure/Services/AdminPageService.cs Solution/GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml Solution/GAC.Web/Areas/Admin/Views/ContentPages/Edit.cshtml Solution/GAC.Web/Areas/Admin/Views/FormPages/Edit.cshtml Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs Solution/GAC.Tests/Admin/AdminPageServiceTests.cs
git commit -m "feat(admin): editable raw-HTML body in Vehicles/ContentPages/FormPages editors (Phase 6b)"
```

---

## Task 8: Full suite, handoff, finish

**Files:**
- Modify: `docs/HANDOFF.md`

- [ ] **Step 1: Full test suite**

Run: `dotnet test Solution/GAC.sln`
Expected: ALL tests pass (the 163 from Phase 6a + the new 6b tests). Investigate and fix any regression before proceeding.

- [ ] **Step 2: Secret scan of staged history for this phase**

Run a scan over the Phase-6b diff for accidental secrets (connection strings, the `sa` password, SMTP password):
```powershell
git log --oneline -10
Select-String -Path "Solution/GAC.Infrastructure/Data/*.cs","Solution/GAC.Infrastructure/SeedContent/**/*.html","docs/*.md" -Pattern 'P@ssw0rd','Codex@123','83.229.86.221','Password=','User Id=sa'
```
Expected: **no matches**. (Seed bodies are public marketing HTML; verify no stray credential slipped in.)

- [ ] **Step 3: Update `docs/HANDOFF.md`**

Read the file, then: change the header/§1 to "Phases 1–5 and 6a **and 6b** complete"; in §4 mark Phase 6b ✅; add a §5e "Phase 6b — editable HTML bodies" describing: `BodyHtml` owned field on Vehicle/ContentPage/FormPage; `AddBodyHtml` migration **applied to the GAC DB**; embedded `SeedContent/**/*.html` seed bodies + idempotent `EnsureBodiesAsync` backfill; generic `@Html.Raw` templates replacing the 16 per-slug partials; `Forms/Page.cshtml` branches on `FormType.Contact`; raw-HTML `Code=true` editor added to the three admin editors; **accepted tradeoff** — the body holds the whole page incl. hero/title, so the structured `Name`/`Images` fields still drive `/models` + mega-menu but editing the detail-page hero/title means editing the HTML; **Arabic bodies blank → English fallback** until an editor translates. Update §8 deferred items: remove the "Phase 6b" entry; note any newly-deferred follow-ups (e.g. translate Arabic bodies via admin; optional client-side HTML-editor enhancement). Re-point §9 "next phase" to whatever remains (or "Phase 6 complete").

- [ ] **Step 4: Commit the handoff**
```powershell
git add docs/HANDOFF.md
git commit -m "docs: handoff update for Phase 6b (editable HTML bodies)"
```

- [ ] **Step 5: Finish the branch**

Use **superpowers:finishing-a-development-branch** to verify the full suite once more and push to public `main` (matching the prior-phase rhythm). After push, update memory: in `MEMORY.md` bump the GAC line to note "P1-5 + 6a + 6b DONE + PUSHED" with the new latest SHA; update `gac_cms_pivot.md` with a short "PHASE 6b DONE" paragraph (migration applied to prod; bodies DB-editable; tradeoff noted).

---

## Self-review (completed during planning)

- **Spec coverage:** editable HTML body per vehicle (Tasks 1,3,4,5,6,7) ✅; all 6 content pages + contact-us (Tasks 2,5,6,7) ✅; raw-HTML editor (Task 7, reusing Phase-6a `Code=true`) ✅; `AddBodyHtml` migration applied to prod (Task 1) ✅; idempotent seed backfill, Arabic blank→EN fallback (Task 5) ✅; generic templates replace 9+6+1 partials, functional forms kept (Task 6) ✅; `@Html.Raw` = trusted admin-only content (no user input) ✅.
- **Sequencing risk handled:** migration applied (Task 1) before `EnsureBodiesAsync` is wired (Task 5), so startup/`dotnet ef` seeding never queries columns that don't yet exist.
- **Type consistency:** `BodyHtml` is `LocalizedText` on all three entities; admin form field names `BodyHtml.En`/`BodyHtml.Ar` bind to the model-bound entity; `ReadSeedBody(area, slug)` areas (`vehicles`/`content`/`forms`) match the `SeedContent/<area>/` folders.
- **No placeholders:** every code step shows the exact code or an unambiguous edit anchor; transcription tasks reference the source partial + an explicit substitution table.
