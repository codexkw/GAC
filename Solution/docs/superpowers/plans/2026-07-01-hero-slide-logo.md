# Hero Slide Logo Image Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give each home-page hero slide an optional uploadable logo image that replaces the `<h1>` text title when set (text heading kept as fallback + alt).

**Architecture:** One nullable string column on `HeroSlide` (`LogoImagePath`), mirroring the existing `ImagePath` media-picker pattern. The public hero renders the logo `<img>` when set, else the current `<h1>`. Admin gains a logo media-picker field. Additive migration; no seed change.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9 (SQL Server prod / InMemory tests), Razor, xUnit.

## Global Constraints

- .NET 9 / EF Core 9; pin `Microsoft.*` to `9.0.*`.
- Additive migration only. Apply to prod via scoped idempotent script + the .NET `SqlConnection` apply method (sqlcmd.exe is flaky on this box); never `dotnet ef database update`.
- `GAC.Tests.Admin` namespace contains prod-DB-booting classes — run admin in-memory classes by **explicit class name**.
- Build/test from `C:\Users\anas-\source\repos\GAC\Solution`.

## File Structure

- `GAC.Core/Content/HeroSlide.cs` — **modify** — add `LogoImagePath`.
- `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` — **modify** — `HeroSlideConfig` maxlength.
- `GAC.Infrastructure/Services/AdminHomeService.cs:45` — **modify** — map `LogoImagePath` in `UpdateSlideAsync`.
- `GAC.Infrastructure/Migrations/*_AddHeroSlideLogo.cs` — **generated** — additive.
- `GAC.Web/Views/Home/Index.cshtml:20` — **modify** — conditional logo/heading render.
- `GAC.Web/wwwroot/assets/css/styles.css` — **modify** — `.hero__logo` styles + mobile overrides.
- `GAC.Web/Areas/Admin/Views/HomeContent/Edit.cshtml` — **modify** — logo media-picker field.
- Tests: `GAC.Tests/Content/HeroSlideLogoMappingTests.cs`, `GAC.Tests/Home/HeroLogoRenderTests.cs`, `GAC.Tests/Admin/AdminHeroLogoFieldTests.cs` — **create**.

---

### Task 1: Model field + config + admin-service mapping + migration

**Files:**
- Modify: `GAC.Core/Content/HeroSlide.cs`
- Modify: `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs` (`HeroSlideConfig` ~line 238)
- Modify: `GAC.Infrastructure/Services/AdminHomeService.cs:45` (`UpdateSlideAsync`)
- Test: `GAC.Tests/Content/HeroSlideLogoMappingTests.cs`

**Interfaces:**
- Produces: `HeroSlide.LogoImagePath` (`string?`).
- Produces: `AdminHomeService.UpdateSlideAsync` persists `LogoImagePath`.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Content/HeroSlideLogoMappingTests.cs`:

```csharp
using System.Threading.Tasks;
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Content;

public class HeroSlideLogoMappingTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task HeroSlide_LogoImagePath_RoundTrips()
    {
        var db = NewDb(nameof(HeroSlide_LogoImagePath_RoundTrips));
        db.HeroSlides.Add(new HeroSlide { ImagePath = "/bg.jpg", LogoImagePath = "/logo/gs8.png",
            Heading = new LocalizedText { En = "GS8" } });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var s = await db.HeroSlides.FirstAsync();
        Assert.Equal("/logo/gs8.png", s.LogoImagePath);
    }

    [Fact]
    public async Task UpdateSlideAsync_Persists_LogoImagePath()
    {
        var db = NewDb(nameof(UpdateSlideAsync_Persists_LogoImagePath));
        var svc = new AdminHomeService(db);
        var id = await svc.CreateSlideAsync(new HeroSlide { ImagePath = "/bg.jpg",
            Heading = new LocalizedText { En = "GS8" } });

        await svc.UpdateSlideAsync(new HeroSlide { Id = id, ImagePath = "/bg.jpg",
            Heading = new LocalizedText { En = "GS8" }, LogoImagePath = "/logo/gs8.png" });

        var s = await db.HeroSlides.FirstAsync(x => x.Id == id);
        Assert.Equal("/logo/gs8.png", s.LogoImagePath);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~HeroSlideLogoMappingTests"`
Expected: FAIL to compile — `HeroSlide.LogoImagePath` doesn't exist.

- [ ] **Step 3: Add the model field**

In `GAC.Core/Content/HeroSlide.cs`, add after `ImagePath`:

```csharp
    public string? LogoImagePath { get; set; }
```

- [ ] **Step 4: Configure the column**

In `GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs`, in `HeroSlideConfig.Configure`, after the `ImagePath` line:

```csharp
        b.Property(s => s.LogoImagePath).HasMaxLength(300);
```

- [ ] **Step 5: Map it in the admin service**

In `GAC.Infrastructure/Services/AdminHomeService.cs`, in `UpdateSlideAsync` (line ~45), extend the mapping line to include the logo:

```csharp
        e.ImagePath = slide.ImagePath; e.LogoImagePath = slide.LogoImagePath; e.Heading = slide.Heading; e.Subheading = slide.Subheading;
        e.CtaText = slide.CtaText; e.CtaLink = slide.CtaLink;
```

(`CreateSlideAsync` adds the bound `slide` directly, so it already persists `LogoImagePath` — no change there.)

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~HeroSlideLogoMappingTests"`
Expected: PASS (both facts).

- [ ] **Step 7: Generate the migration and verify additive**

Run: `dotnet ef migrations add AddHeroSlideLogo --project GAC.Infrastructure --startup-project GAC.Web`
Open `GAC.Infrastructure/Migrations/*_AddHeroSlideLogo.cs` and verify `Up` is exactly one `migrationBuilder.AddColumn<string>(name: "LogoImagePath", table: "HeroSlides", ... nullable: true, maxLength: 300)` — **no** DropColumn/AlterColumn/DropTable.

- [ ] **Step 8: Commit**

```bash
git add GAC.Core/Content/HeroSlide.cs \
  GAC.Infrastructure/Data/Configurations/ContentConfigurations.cs \
  GAC.Infrastructure/Services/AdminHomeService.cs \
  GAC.Infrastructure/Migrations \
  GAC.Tests/Content/HeroSlideLogoMappingTests.cs
git commit -m "feat(hero): HeroSlide.LogoImagePath field + config + admin mapping + additive migration"
```

---

### Task 2: Public render + CSS

**Files:**
- Modify: `GAC.Web/Views/Home/Index.cshtml:20`
- Modify: `GAC.Web/wwwroot/assets/css/styles.css` (add `.hero__logo` near `.hero__title` ~line 272; add mobile overrides at the two `.hero__title` breakpoints ~lines 1176 and 1508)
- Test: `GAC.Tests/Home/HeroLogoRenderTests.cs`

**Interfaces:**
- Consumes: `HeroSlide.LogoImagePath` (Task 1); the dev seeder auto-populates slides under `UseEnvironment("Development")`.
- Produces: `/` renders `<img class="hero__logo">` when a slide has a logo, else `<h1 class="hero__title">`.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Home/HeroLogoRenderTests.cs`:

```csharp
using System;
using System.Linq;
using System.Threading.Tasks;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests.Home;

public class HeroLogoRenderTests : IClassFixture<HeroLogoRenderTests.Factory>
{
    public class Factory : WebApplicationFactory<Program>
    {
        private readonly string _db = "hero-logo-" + Guid.NewGuid();
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureTestServices(s => InMemoryTestDb.Swap(s, _db));
        }
    }

    private readonly Factory _factory;
    public HeroLogoRenderTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Hero_RendersLogoWhenSet_ElseHeadingText()
    {
        // Give the first slide a logo, ensure a second slide has none.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var slides = await db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync();
            slides[0].LogoImagePath = "/media/zzz-logo.png";
            slides[1].LogoImagePath = null;
            await db.SaveChangesAsync();
        }

        var html = await (await _factory.CreateClient().GetAsync("/")).Content.ReadAsStringAsync();

        Assert.Contains("hero__logo", html);                 // logo img class rendered
        Assert.Contains("/media/zzz-logo.png", html);        // the logo src
        Assert.Contains("hero__title", html);                // a no-logo slide still shows the text <h1>
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~HeroLogoRenderTests"`
Expected: FAIL — `hero__logo` / `/media/zzz-logo.png` not present (the view always renders `<h1>` today).

- [ ] **Step 3: Update the view**

In `GAC.Web/Views/Home/Index.cshtml`, replace line 20 (`<h1 class="hero__title">@slide.Heading.Localize()</h1>`) with:

```razor
@if (!string.IsNullOrWhiteSpace(slide.LogoImagePath))
{
          <img class="hero__logo" src="@slide.LogoImagePath" alt="@slide.Heading.Localize()" />
}
else
{
          <h1 class="hero__title">@slide.Heading.Localize()</h1>
}
```

- [ ] **Step 4: Add the CSS**

In `GAC.Web/wwwroot/assets/css/styles.css`, immediately after the `.hero__title { ... }` rule (~line 277), add:

```css
.hero__logo {
  display: block;
  max-height: clamp(2.5rem, 5.5vw, 4.5rem);
  width: auto;
  max-width: min(90%, 520px);
  margin-bottom: var(--space-5);
  filter: drop-shadow(0 2px 30px rgba(0,0,0,.4));
}
```

Then at the `.hero__title` mobile override near line 1176, on the next line after it add:

```css
  .hero__logo { max-height: clamp(2rem, 8vw, 3rem); }
```

And at the `.hero__title` override near line 1508 (`.hero__title { font-size: clamp(2rem, 8vw, 3rem); margin-bottom: 16px; }`), on the next line add:

```css
  .hero__logo { max-height: clamp(2rem, 8vw, 3rem); margin-bottom: 16px; }
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~HeroLogoRenderTests"`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add GAC.Web/Views/Home/Index.cshtml GAC.Web/wwwroot/assets/css/styles.css GAC.Tests/Home/HeroLogoRenderTests.cs
git commit -m "feat(hero): render per-slide logo image in place of the title text (with CSS)"
```

---

### Task 3: Admin editor logo field

**Files:**
- Modify: `GAC.Web/Areas/Admin/Views/HomeContent/Edit.cshtml` (after the Heading partial ~line 21)
- Test: `GAC.Tests/Admin/AdminHeroLogoFieldTests.cs`

**Interfaces:**
- Consumes: model binding of `LogoImagePath` into `HeroSlide` (Task 1); `_PickerModal` + `admin.js` media-picker delegation already present on the page.
- Produces: the slide edit form exposes a `LogoImagePath` input + picker.

- [ ] **Step 1: Write the failing test**

Create `GAC.Tests/Admin/AdminHeroLogoFieldTests.cs`:

```csharp
using System.Threading.Tasks;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

// The hero-slide editor must expose a Logo image field. In-memory DB (no prod contact).
public class AdminHeroLogoFieldTests : IClassFixture<AdminInMemoryWebApplicationFactory>
{
    private readonly AdminInMemoryWebApplicationFactory _factory;
    public AdminHeroLogoFieldTests(AdminInMemoryWebApplicationFactory f) => _factory = f;

    [Fact]
    public async Task EditForm_HasLogoImagePathField()
    {
        var client = _factory.ClientForRole(Roles.Editor);
        var resp = await client.GetAsync("/Admin/HomeContent/Create");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("name=\"LogoImagePath\"", html);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminHeroLogoFieldTests"`
Expected: FAIL — the form has no `LogoImagePath` input.

- [ ] **Step 3: Add the logo field to the editor**

In `GAC.Web/Areas/Admin/Views/HomeContent/Edit.cshtml`, immediately after the Heading partial (line 21), add:

```html
    <div class="adm-field">
        <label asp-for="LogoImagePath">Logo image (optional — replaces the heading text when set)</label>
        <input asp-for="LogoImagePath" data-media-input />
        <button type="button" class="adm-btn" data-media-pick>Choose&hellip;</button>
    </div>
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~AdminHeroLogoFieldTests"`
Expected: PASS.

- [ ] **Step 5: Build + run all three new classes**

Run: `dotnet build GAC.Web/GAC.Web.csproj -c Debug` (expect 0 errors).
Run: `dotnet test GAC.Tests/GAC.Tests.csproj --filter "FullyQualifiedName~HeroSlideLogoMappingTests|FullyQualifiedName~HeroLogoRenderTests|FullyQualifiedName~AdminHeroLogoFieldTests"`
Expected: all PASS.

- [ ] **Step 6: Commit**

```bash
git add GAC.Web/Areas/Admin/Views/HomeContent/Edit.cshtml GAC.Tests/Admin/AdminHeroLogoFieldTests.cs
git commit -m "feat(hero): admin logo image field on the hero-slide editor"
```

---

## Self-Review

**1. Spec coverage:**
- `LogoImagePath` model + config + additive migration → Task 1. ✓
- **Admin `UpdateSlideAsync` mapping** (field-by-field service — would silently drop the logo otherwise) → Task 1 Step 5. ✓
- Conditional render (logo replaces `<h1>`, heading as `alt`, text fallback) → Task 2. ✓
- `.hero__logo` styling + mobile overrides → Task 2 Step 4. ✓
- Admin media-picker field → Task 3. ✓
- No seed change → confirmed (no seed task). ✓
- Tests (mapping, render, admin field) → Tasks 1–3. ✓

**2. Placeholder scan:** No TBD/TODO; every code step shows full code. ✓

**3. Type/name consistency:** `LogoImagePath` spelled identically across model, config, service, view, admin view, and tests. `hero__logo` class consistent between view and CSS and render test. ✓

## Post-implementation (separate, user-gated steps — NOT part of TDD)

- Guarded suite: run `~GAC.Tests.Content.` + `~GAC.Tests.Home.` fully + the new admin class by explicit name; confirm no regressions.
- Push branch; apply `AddHeroSlideLogo` to prod via scoped idempotent script + .NET `SqlConnection`; redeploy Web (no seed change — slides show text until logos uploaded); merge to main.
