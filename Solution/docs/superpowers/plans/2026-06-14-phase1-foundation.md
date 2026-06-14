# GAC CMS — Phase 1: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Stand up a runnable ASP.NET Core 9 MVC solution (Option A layering) that serves the existing GAC home page server-side with the pixel-perfect look, backed by SQL Server + Identity, with English/Arabic culture switching (cookie + RTL) working end-to-end.

**Architecture:** Single web app `GAC.Web` (public + future `Areas/Admin`) referencing two class libraries — `GAC.Core` (domain/services) and `GAC.Infrastructure` (EF Core, Identity, seeding). A `GAC.Tests` xUnit project covers culture switching and a home-page smoke test. Phase 1 wires Identity tables, request localization, the language toggle, and ports the shared chrome (`_Layout` + header/footer) and home page from `../HTML`. Domain content entities arrive in Phase 2.

**Tech Stack:** .NET 9, ASP.NET Core MVC, EF Core 9 (SQL Server), ASP.NET Core Identity, xUnit + `Microsoft.AspNetCore.Mvc.Testing`.

**Reference paths:**
- Solution root: `C:\Users\anas-\source\repos\GAC\Solution`
- HTML source of truth: `C:\Users\anas-\source\repos\GAC\HTML` (assets in `HTML/assets`, chrome in `HTML/partials`, home in `HTML/index.html`)
- Spec: `Solution/docs/superpowers/specs/2026-06-14-gac-cms-bilingual-design.md`

**Conventions (from prior projects):**
- Pin every `Microsoft.*` / EF package to `9.0.*` — `dotnet add package` otherwise floats to net10 previews (Old-Timer lesson).
- Connection string + secrets in `appsettings` (Zinah convention).
- App does **not** auto-migrate on startup; migrations are applied explicitly.

All commands below assume the working directory is the **Solution** folder unless stated otherwise:
`cd C:\Users\anas-\source\repos\GAC\Solution`

---

### Task 1: Create the solution, projects, and references

**Files:**
- Create: `Solution/GAC.sln`
- Create: `Solution/GAC.Core/GAC.Core.csproj`
- Create: `Solution/GAC.Infrastructure/GAC.Infrastructure.csproj`
- Create: `Solution/GAC.Web/GAC.Web.csproj`
- Create: `Solution/GAC.Tests/GAC.Tests.csproj`

- [ ] **Step 1: Verify the .NET 9 SDK is present**

Run: `dotnet --info`
Expected: a `9.0.xxx` entry under "`.NET SDKs installed`". If only 10.x is present, stop and install the .NET 9 SDK first.

- [ ] **Step 2: Create the solution and the four projects**

```bash
dotnet new sln -n GAC
dotnet new classlib -n GAC.Core -f net9.0 -o GAC.Core
dotnet new classlib -n GAC.Infrastructure -f net9.0 -o GAC.Infrastructure
dotnet new mvc -n GAC.Web -f net9.0 -o GAC.Web
dotnet new xunit -n GAC.Tests -f net9.0 -o GAC.Tests
```

- [ ] **Step 3: Delete the default placeholder class files**

```bash
rm -f GAC.Core/Class1.cs GAC.Infrastructure/Class1.cs
```

- [ ] **Step 4: Add all projects to the solution**

```bash
dotnet sln GAC.sln add GAC.Core GAC.Infrastructure GAC.Web GAC.Tests
```

- [ ] **Step 5: Wire project references**

```bash
dotnet add GAC.Infrastructure reference GAC.Core
dotnet add GAC.Web reference GAC.Core GAC.Infrastructure
dotnet add GAC.Tests reference GAC.Web GAC.Core GAC.Infrastructure
```

- [ ] **Step 6: Build to confirm the empty solution compiles**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add Solution/GAC.sln Solution/GAC.Core Solution/GAC.Infrastructure Solution/GAC.Web Solution/GAC.Tests
git commit -m "chore: scaffold GAC solution (Web + Core + Infrastructure + Tests)"
```

---

### Task 2: Add and pin NuGet packages

**Files:**
- Modify: `Solution/GAC.Infrastructure/GAC.Infrastructure.csproj`
- Modify: `Solution/GAC.Web/GAC.Web.csproj`
- Modify: `Solution/GAC.Tests/GAC.Tests.csproj`

- [ ] **Step 1: Add EF Core + Identity packages to Infrastructure (pinned to 9.0.*)**

```bash
dotnet add GAC.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer -v 9.0.6
dotnet add GAC.Infrastructure package Microsoft.EntityFrameworkCore.Design -v 9.0.6
dotnet add GAC.Infrastructure package Microsoft.AspNetCore.Identity.EntityFrameworkCore -v 9.0.6
```

- [ ] **Step 2: Add EF Core Design + tools entry to the Web project (needed for `dotnet ef` against the startup project)**

```bash
dotnet add GAC.Web package Microsoft.EntityFrameworkCore.Design -v 9.0.6
```

- [ ] **Step 3: Add the integration-test host package to the Tests project**

```bash
dotnet add GAC.Tests package Microsoft.AspNetCore.Mvc.Testing -v 9.0.6
```

- [ ] **Step 4: Confirm no package floated to a 10.x preview**

Run: `grep -rh "PackageReference" GAC.Infrastructure GAC.Web GAC.Tests --include=*.csproj`
Expected: every `Microsoft.*` / EF version reads `9.0.*` (none `10.*`). If any floated, edit the `.csproj` to `9.0.6` and re-run `dotnet restore`.

- [ ] **Step 5: Install the EF CLI tool if missing**

Run: `dotnet ef --version`
Expected: prints a `9.x` version. If "command not found", run `dotnet tool install --global dotnet-ef --version 9.0.6` then re-check.

- [ ] **Step 6: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 7: Commit**

```bash
git add Solution/GAC.Infrastructure Solution/GAC.Web Solution/GAC.Tests
git commit -m "chore: add EF Core 9 + Identity + MVC test packages (pinned 9.0.*)"
```

---

### Task 3: Connection string and SMTP placeholders in appsettings

**Files:**
- Modify: `Solution/GAC.Web/appsettings.json`

- [ ] **Step 1: Add the GAC connection string and an empty Smtp section**

Replace the contents of `GAC.Web/appsettings.json` with:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "Default": "Server=83.229.86.221,1433;Database=GAC;User Id=sa;Password=REPLACE_WITH_SA_PASSWORD;TrustServerCertificate=True;MultipleActiveResultSets=True"
  },
  "Smtp": {
    "Host": "",
    "Port": 587,
    "User": "",
    "Password": "",
    "FromEmail": "",
    "FromName": "GAC Mutawa Alkadi",
    "AdminNotifyEmail": ""
  }
}
```

> Replace `REPLACE_WITH_SA_PASSWORD` with the real sa password from your usual credentials before running migrations. SMTP stays empty until Phase 5.

- [ ] **Step 2: Commit**

```bash
git add Solution/GAC.Web/appsettings.json
git commit -m "chore: add GAC connection string and SMTP config skeleton"
```

---

### Task 4: ApplicationUser + ApplicationDbContext

**Files:**
- Create: `Solution/GAC.Core/Identity/ApplicationUser.cs`
- Create: `Solution/GAC.Infrastructure/Data/ApplicationDbContext.cs`

- [ ] **Step 1: Create the application user**

`GAC.Core/Identity/ApplicationUser.cs`:

```csharp
using Microsoft.AspNetCore.Identity;

namespace GAC.Core.Identity;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
```

- [ ] **Step 2: Create the DbContext (Identity only for Phase 1)**

`GAC.Infrastructure/Data/ApplicationDbContext.cs`:

```csharp
using GAC.Core.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domain entity DbSets are added in Phase 2.
}
```

- [ ] **Step 3: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Core/Identity Solution/GAC.Infrastructure/Data
git commit -m "feat: add ApplicationUser and Identity DbContext"
```

---

### Task 5: Define roles, role names, and the database seeder

**Files:**
- Create: `Solution/GAC.Core/Identity/Roles.cs`
- Create: `Solution/GAC.Infrastructure/Data/DbSeeder.cs`

- [ ] **Step 1: Create the role-name constants**

`GAC.Core/Identity/Roles.cs`:

```csharp
namespace GAC.Core.Identity;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Editor = "Editor";
    public const string Sales = "Sales";

    public static readonly string[] All = { Admin, Editor, Sales };
}
```

- [ ] **Step 2: Create the seeder (roles + a first admin user)**

`GAC.Infrastructure/Data/DbSeeder.cs`:

```csharp
using GAC.Core.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Infrastructure.Data;

public static class DbSeeder
{
    public const string DefaultAdminEmail = "admin@gacsaudi.local";
    public const string DefaultAdminPassword = "ChangeMe!2026";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(DefaultAdminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = DefaultAdminEmail,
                Email = DefaultAdminEmail,
                EmailConfirmed = true,
                DisplayName = "Administrator"
            };
            var result = await userManager.CreateAsync(admin, DefaultAdminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
```

> The default admin password is a known placeholder; change it from the admin UI (Phase 6) before go-live.

- [ ] **Step 3: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Core/Identity/Roles.cs Solution/GAC.Infrastructure/Data/DbSeeder.cs
git commit -m "feat: add roles and DB seeder (roles + default admin)"
```

---

### Task 6: Wire Program.cs — MVC, EF, Identity, localization

**Files:**
- Modify: `Solution/GAC.Web/Program.cs`

- [ ] **Step 1: Replace Program.cs with the wired-up version**

`GAC.Web/Program.cs`:

```csharp
using System.Globalization;
using GAC.Core.Identity;
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ar") };
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    // Cookie first; ignore Accept-Language so the visible language is driven by our toggle.
    options.RequestCultureProviders = new List<IRequestCultureProvider>
    {
        new CookieRequestCultureProvider()
    };
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Seed roles + default admin at startup (idempotent). Does NOT run migrations.
using (var scope = app.Services.CreateScope())
{
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

public partial class Program { } // exposes Program to the test host
```

- [ ] **Step 2: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 3: Commit**

```bash
git add Solution/GAC.Web/Program.cs
git commit -m "feat: wire EF, Identity, request localization, and startup seeding"
```

---

### Task 7: Language toggle controller (TDD)

The header language switch posts here to set the culture cookie and redirect back.

**Files:**
- Create: `Solution/GAC.Web/Controllers/CultureController.cs`
- Test: `Solution/GAC.Tests/CultureControllerTests.cs`

- [ ] **Step 1: Write the failing test**

`GAC.Tests/CultureControllerTests.cs`:

```csharp
using GAC.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace GAC.Tests;

public class CultureControllerTests
{
    [Fact]
    public void Set_WritesCultureCookie_AndRedirectsToReturnUrl()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = controller.Set("ar", "/gs8");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/gs8", redirect.Url);

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains(".AspNetCore.Culture", setCookie);
        Assert.Contains("c%3Dar", setCookie); // CookieRequestCultureProvider encodes "c=ar|uic=ar"
    }

    [Fact]
    public void Set_RejectsUnsupportedCulture_FallsBackToEnglish()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        controller.Set("fr", "/");

        var setCookie = controller.Response.Headers["Set-Cookie"].ToString();
        Assert.Contains("c%3Den", setCookie);
    }

    [Fact]
    public void Set_RejectsNonLocalReturnUrl_RedirectsHome()
    {
        var controller = new CultureController
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = controller.Set("en", "https://evil.example.com");

        var redirect = Assert.IsType<LocalRedirectResult>(result);
        Assert.Equal("/", redirect.Url);
    }
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test GAC.Tests --filter CultureControllerTests`
Expected: FAIL to compile — `CultureController` does not exist.

- [ ] **Step 3: Implement the controller**

`GAC.Web/Controllers/CultureController.cs`:

```csharp
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class CultureController : Controller
{
    private static readonly string[] Supported = { "en", "ar" };

    [HttpPost]
    [HttpGet] // allow simple <a> links from the header switch too
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(culture) || !Supported.Contains(culture))
            culture = "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(new CultureInfo(culture))),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return LocalRedirect("/");
    }
}
```

- [ ] **Step 4: Run the test to verify it passes**

Run: `dotnet test GAC.Tests --filter CultureControllerTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web/Controllers/CultureController.cs Solution/GAC.Tests/CultureControllerTests.cs
git commit -m "feat: add culture toggle controller with cookie + safe redirect (TDD)"
```

---

### Task 8: Create and apply the initial EF migration

**Files:**
- Create: `Solution/GAC.Infrastructure/Migrations/*` (generated)

- [ ] **Step 1: Confirm the real sa password is set in appsettings.json**

Open `GAC.Web/appsettings.json` and verify `REPLACE_WITH_SA_PASSWORD` has been replaced. If not, stop and set it.

- [ ] **Step 2: Add the initial migration (run from the Solution folder)**

```bash
dotnet ef migrations add InitialIdentity --project GAC.Infrastructure --startup-project GAC.Web --output-dir Migrations
```

Expected: a `Migrations/` folder appears under `GAC.Infrastructure` with `*_InitialIdentity.cs` and the model snapshot.

- [ ] **Step 3: Apply the migration to the GAC database**

```bash
dotnet ef database update --project GAC.Infrastructure --startup-project GAC.Web
```

Expected: `Done.` — connects to `83.229.86.221,1433`, creates the `AspNet*` Identity tables in the `GAC` database. If you get a login failure, re-check the sa password; if "Cannot open database GAC", create the empty `GAC` database first on the server.

- [ ] **Step 4: Commit the migration**

```bash
git add Solution/GAC.Infrastructure/Migrations
git commit -m "feat: initial EF migration (Identity schema)"
```

---

### Task 9: Copy the static design assets into wwwroot

**Files:**
- Create: `Solution/GAC.Web/wwwroot/assets/**` (copied from `../HTML/assets`)

- [ ] **Step 1: Remove the default MVC template static assets that we are replacing**

```bash
rm -rf GAC.Web/wwwroot/lib GAC.Web/wwwroot/css/site.css GAC.Web/wwwroot/js/site.js GAC.Web/wwwroot/favicon.ico
```

- [ ] **Step 2: Copy the GAC design assets (css, js, img) verbatim**

```bash
cp -r ../HTML/assets GAC.Web/wwwroot/assets
```

- [ ] **Step 3: Verify the key files landed**

Run: `ls GAC.Web/wwwroot/assets/css GAC.Web/wwwroot/assets/js`
Expected: the GAC stylesheet(s) under `css/`, and `main.js` under `js/`. (Note: `includes.js` is intentionally copied but will NOT be referenced — server-side partials replace it.)

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Web/wwwroot/assets
git commit -m "chore: import GAC design assets (css/js/img) into wwwroot"
```

---

### Task 10: Port the shared chrome into _Layout + header/footer partials

The goal is byte-for-byte visual parity with `HTML/`, but rendered server-side. We move the markup from `HTML/partials/header.html` and `HTML/partials/footer.html` into Razor partials, and set `<html dir/lang>` from the current culture.

**Files:**
- Modify: `Solution/GAC.Web/Views/Shared/_Layout.cshtml`
- Create: `Solution/GAC.Web/Views/Shared/_Header.cshtml`
- Create: `Solution/GAC.Web/Views/Shared/_Footer.cshtml`
- Delete: `Solution/GAC.Web/Views/Shared/_ValidationScriptsPartial.cshtml` is kept; remove default layout cruft only.

- [ ] **Step 1: Replace `_Layout.cshtml`**

Use the `<head>` asset links found at the top of `HTML/index.html` (copy the exact `<link rel="stylesheet" ...>` tags), rewriting `assets/...` paths to `~/assets/...`. Set direction from culture. Template:

```cshtml
@using System.Globalization
@{
    var culture = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
    var isRtl = culture == "ar";
    var dir = isRtl ? "rtl" : "ltr";
}
<!DOCTYPE html>
<html lang="@culture" dir="@dir">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - GAC Mutawa Alkadi</title>
    <!-- PORT: paste the exact stylesheet <link> tags from HTML/index.html <head> here,
         changing every  href="assets/...."  to  href="~/assets/...."  and adding asp-append-version="true". -->
    <link rel="stylesheet" href="~/assets/css/styles.css" asp-append-version="true" />
    @if (isRtl)
    {
        <link rel="stylesheet" href="~/assets/css/rtl.css" asp-append-version="true" />
    }
    @await RenderSectionAsync("Head", required: false)
</head>
<body>
    <partial name="_Header" />
    <main>
        @RenderBody()
    </main>
    <partial name="_Footer" />
    <script src="~/assets/js/main.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

> Replace the single `styles.css` link with the actual stylesheet filename(s) referenced in `HTML/index.html`. Create an empty `GAC.Web/wwwroot/assets/css/rtl.css` placeholder now (filled in Phase 4): `touch GAC.Web/wwwroot/assets/css/rtl.css`.

- [ ] **Step 2: Create `_Header.cshtml` from the existing header partial**

Copy the entire markup of `HTML/partials/header.html` into `GAC.Web/Views/Shared/_Header.cshtml`. Then apply these mechanical edits:
- Rewrite every `src="assets/..."` / `href="assets/..."` to `~/assets/...`.
- Leave inter-page links as-is for now (e.g. `href="index.html"`); clean routing is wired in Phase 3. They still resolve as static-style links this phase.
- Replace the language switch `<li>` items so the Arabic link posts to the toggle. Replace:

```html
<li><a href="#" class="is-current">English</a></li>
<li><a href="#" lang="ar">عربي</a></li>
```

with:

```cshtml
<li><a asp-controller="Culture" asp-action="Set" asp-route-culture="en"
       asp-route-returnUrl="@Context.Request.Path">English</a></li>
<li><a asp-controller="Culture" asp-action="Set" asp-route-culture="ar"
       asp-route-returnUrl="@Context.Request.Path" lang="ar">عربي</a></li>
```

- [ ] **Step 3: Create `_Footer.cshtml` from the existing footer partial**

Copy the entire markup of `HTML/partials/footer.html` into `GAC.Web/Views/Shared/_Footer.cshtml` and rewrite `assets/...` paths to `~/assets/...`.

- [ ] **Step 4: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web/Views/Shared Solution/GAC.Web/wwwroot/assets/css/rtl.css
git commit -m "feat: port shared chrome to Razor layout + header/footer partials"
```

---

### Task 11: Port the home page into HomeController + Index view

**Files:**
- Modify: `Solution/GAC.Web/Controllers/HomeController.cs`
- Modify: `Solution/GAC.Web/Views/Home/Index.cshtml`
- Delete: `Solution/GAC.Web/Views/Home/Privacy.cshtml` (default cruft)

- [ ] **Step 1: Simplify HomeController to just Index + Error**

`GAC.Web/Controllers/HomeController.cs`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    [ResponseCache(Duration = 0, NoStore = true)]
    public IActionResult Error() => View();
}
```

- [ ] **Step 2: Port the home body into Index.cshtml**

Open `HTML/index.html`. Copy everything **between** `<main ...>` and `</main>` (the page body, excluding the `<div data-include="header">`/`footer` placeholders and the `includes.js`/`main.js` script tags — those now live in `_Layout`). Paste into `GAC.Web/Views/Home/Index.cshtml` below this header:

```cshtml
@{
    ViewData["Title"] = "Home";
}
@* PORT: paste the inner <main> content of HTML/index.html here.
   Rewrite every  src="assets/..."  and  href="assets/..."  to  ~/assets/...
   Leave page-to-page hrefs (e.g. gs8.html) unchanged for this phase. *@
```

> If `HTML/index.html` has no explicit `<main>` wrapper, copy everything between the header placeholder (`<div data-include="header"></div>`) and the footer placeholder (`<div data-include="footer"></div>`).

- [ ] **Step 3: Delete the default Privacy view**

```bash
rm -f GAC.Web/Views/Home/Privacy.cshtml
```

- [ ] **Step 4: Build**

Run: `dotnet build GAC.sln`
Expected: `Build succeeded`.

- [ ] **Step 5: Commit**

```bash
git add Solution/GAC.Web/Controllers/HomeController.cs Solution/GAC.Web/Views/Home
git commit -m "feat: port home page to server-rendered Index view"
```

---

### Task 12: Home-page + localization smoke tests, then run the app

**Files:**
- Create: `Solution/GAC.Tests/HomePageSmokeTests.cs`

- [ ] **Step 1: Write the integration smoke test**

`GAC.Tests/HomePageSmokeTests.cs`:

```csharp
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace GAC.Tests;

public class HomePageSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HomePageSmokeTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Get_Home_ReturnsOk_AndRendersChrome()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("gac-header", html);          // header partial rendered
        Assert.Contains("lang=\"en\"", html);          // default culture
        Assert.Contains("dir=\"ltr\"", html);
    }

    [Fact]
    public async Task Get_Home_WithArabicCookie_RendersRtl()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("dir=\"rtl\"", html);
        Assert.Contains("lang=\"ar\"", html);
    }
}
```

> This test boots the real app, which runs `DbSeeder.SeedAsync` against the live DB. That is acceptable for Phase 1 (idempotent). If the test environment has no DB access, note it and run the app manually instead (Step 3).

- [ ] **Step 2: Run the full test suite**

Run: `dotnet test GAC.sln`
Expected: all tests PASS (CultureController ×3, HomePageSmoke ×2).

- [ ] **Step 3: Run the app and eyeball parity**

Run: `dotnet run --project GAC.Web`
Then open the printed `https://localhost:xxxx/` URL.
Expected:
- Home page renders with the GAC look (header brandbar, mega-menu nav, hero, footer) matching `HTML/index.html`.
- Clicking **عربي** in the language switch reloads the page with `dir="rtl"` and `lang="ar"` on `<html>` (full RTL styling arrives in Phase 4 — layout may look LTR-ish, that's expected).
- Clicking **English** switches back.

- [ ] **Step 4: Commit**

```bash
git add Solution/GAC.Tests/HomePageSmokeTests.cs
git commit -m "test: home page + localization smoke tests"
```

---

## Phase 1 Done — Definition of Done

- `dotnet build GAC.sln` and `dotnet test GAC.sln` both succeed.
- App runs; home page is byte-faithful to `HTML/index.html` in English.
- Language toggle flips `<html dir/lang>` between en/ltr and ar/rtl via the culture cookie.
- Identity tables exist in the `GAC` database; roles (Admin/Editor/Sales) and a default admin user are seeded.
- All work committed on `main`.

## What Phase 1 deliberately defers

- Domain content entities + `LocalizedText` owned type → **Phase 2**.
- Clean URL routing, old-`.html` redirects, dynamic header/megamenu/footer from DB, porting the other ~29 pages → **Phase 3**.
- `rtl.css` real styling + Arabic webfont + AR content → **Phase 4**.
- Forms/leads → Phase 5. Admin area → Phase 6.
