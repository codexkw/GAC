# Phase 6a — Admin Foundation + CRUD — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the `Areas/Admin` panel (auth-gated, role-based) so Admin/Editor/Sales users can manage leads and all already-DB-driven content (vehicles + images, menu, hero slides, news, offers, page titles, site settings, media, users) — no schema change.

**Architecture:** A new `GAC.Web/Areas/Admin` with its own `_AdminLayout` (no public chrome). New **admin write-services** in `GAC.Core/Services` (interfaces) + `GAC.Infrastructure/Services` (impls), separate from the read-only public services, using **tracked** EF + `SaveChangesAsync`. Authorization via three policies (`ContentEditor`=Admin∪Editor, `LeadsAccess`=Admin∪Sales, `AdminOnly`=Admin); Admin is a superset. Edits go live immediately.

**Tech Stack:** ASP.NET Core 9 MVC (Areas), EF Core 9.0.6 (SQL Server), ASP.NET Core Identity (cookie auth), Razor + bilingual `LocalizedText` editor partial, xUnit (service unit tests on InMemory DB + WebApplicationFactory integration tests with a test auth handler).

---

## Conventions (apply to every task)

- **Spec:** `docs/superpowers/specs/2026-06-15-phase6-admin-design.md`. Read §3–§5.
- **Admin services live in** `GAC.Core/Services/IAdmin*.cs` (interfaces) and `GAC.Infrastructure/Services/Admin*.cs` (impls). Register each in `GAC.Web/Program.cs`.
- **Admin write-services use tracked queries** (NOT `AsNoTracking()`), then `SaveChangesAsync()`.
- **List/read for admin includes hidden/unpublished** rows (admins must see everything).
- **Every admin controller** carries `[Area("Admin")]`, `[Authorize(Policy = "...")]`, and `[AutoValidateAntiforgeryToken]`.
- **Conventional area routing** gives URLs like `/Admin/Vehicles/Edit/3` (case-insensitive). `AccountController` uses attribute routes for the three special paths `/admin/login`, `/admin/logout`, `/admin/denied`.
- **Bilingual fields:** every `LocalizedText` is edited with the shared `_LocalizedField` partial (Task 3) rendering an English and an Arabic input.
- **Test split (matches Phase 5):** write-logic is covered by **service unit tests** on an InMemory DB; **integration tests** cover access control (anonymous→login, wrong role→denied) and GET rendering. We do **not** POST through anti-forgery in integration tests.
- **Build/test commands** (run from repo root `C:\Users\anas-\source\repos\GAC`):
  - `dotnet build Solution/GAC.sln -c Debug`
  - `dotnet test Solution/GAC.sln` (integration tests need the DB reachable; the suite was 83 green at the start of Phase 6a)
- **Public-repo secret discipline:** scoped `git add` with explicit paths only — never `git add -A`/`.`. Never add `appsettings.Development.json`.

---

## File structure (created/modified across Phase 6a)

**Foundation (Task 1):**
- Create: `GAC.Web/Areas/Admin/Views/_ViewStart.cshtml`, `GAC.Web/Areas/Admin/Views/_ViewImports.cshtml`
- Create: `GAC.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`, `_AdminNav.cshtml`
- Create: `GAC.Web/wwwroot/assets/css/admin.css`, `GAC.Web/wwwroot/assets/js/admin.js`
- Create: `GAC.Web/Areas/Admin/Controllers/AccountController.cs`, `DashboardController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Account/Login.cshtml`, `Denied.cshtml`; `Dashboard/Index.cshtml`
- Create: `GAC.Web/Areas/Admin/AdminPolicies.cs` (policy-name constants)
- Modify: `GAC.Web/Program.cs` (policies, cookie paths, area route)
- Create (tests): `GAC.Tests/Admin/TestAuthHandler.cs`, `AdminWebApplicationFactory.cs`, `AdminAuthTests.cs`

**Shared editor + media (Task 3):**
- Create: `GAC.Web/Areas/Admin/Views/Shared/EditorTemplates/_LocalizedField.cshtml` (+ helper VM)
- Create: `GAC.Core/Services/IMediaService.cs`, `GAC.Core/Content/MediaUploadResult.cs`, `GAC.Core/Services/MediaOptions.cs`
- Create: `GAC.Infrastructure/Services/MediaService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/MediaController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Media/Index.cshtml`, `_PickerModal.cshtml`
- Modify: `GAC.Web/Program.cs` (register `IMediaService`, bind `MediaOptions`)
- Create (tests): `GAC.Tests/Admin/MediaServiceTests.cs`

**Per-area CRUD (Tasks 2, 4–10):** each adds an `IAdmin*Service` (+impl), an `Areas/Admin/Controllers/*Controller.cs`, `Areas/Admin/Views/<Controller>/*.cshtml`, a `GAC.Web/Areas/Admin/Models/*ViewModel.cs` where needed, a `Program.cs` registration line, and a `GAC.Tests/Admin/*ServiceTests.cs` + access test.

---

## Task 1: Admin foundation — area, layout, auth, policies, login, dashboard

**Files:**
- Create: `GAC.Web/Areas/Admin/AdminPolicies.cs`
- Modify: `GAC.Web/Program.cs`
- Create: `GAC.Web/Areas/Admin/Views/_ViewImports.cshtml`, `_ViewStart.cshtml`
- Create: `GAC.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`, `_AdminNav.cshtml`
- Create: `GAC.Web/wwwroot/assets/css/admin.css`, `GAC.Web/wwwroot/assets/js/admin.js`
- Create: `GAC.Web/Areas/Admin/Controllers/AccountController.cs`, `DashboardController.cs`
- Create: `GAC.Web/Areas/Admin/Models/LoginViewModel.cs`
- Create: `GAC.Web/Areas/Admin/Views/Account/Login.cshtml`, `Denied.cshtml`
- Create: `GAC.Web/Areas/Admin/Views/Dashboard/Index.cshtml`
- Test: `GAC.Tests/Admin/TestAuthHandler.cs`, `GAC.Tests/Admin/AdminWebApplicationFactory.cs`, `GAC.Tests/Admin/AdminAuthTests.cs`

- [ ] **Step 1: Policy-name constants**

Create `GAC.Web/Areas/Admin/AdminPolicies.cs`:

```csharp
namespace GAC.Web.Areas.Admin;

public static class AdminPolicies
{
    public const string ContentEditor = "ContentEditor"; // Admin or Editor
    public const string LeadsAccess = "LeadsAccess";     // Admin or Sales
    public const string AdminOnly = "AdminOnly";         // Admin only
}
```

- [ ] **Step 2: Register policies, cookie paths, and area route in `Program.cs`**

In `GAC.Web/Program.cs`, after the `AddIdentity(...)` block (currently lines 17–24) add the cookie configuration:

```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/admin/login";
    options.AccessDeniedPath = "/admin/denied";
    options.LogoutPath = "/admin/logout";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.ContentEditor,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin, GAC.Core.Identity.Roles.Editor));
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.LeadsAccess,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin, GAC.Core.Identity.Roles.Sales));
    options.AddPolicy(GAC.Web.Areas.Admin.AdminPolicies.AdminOnly,
        p => p.RequireRole(GAC.Core.Identity.Roles.Admin));
});
```

Then, where routes are mapped (currently lines 68–70), add the area route **before** the `default` route:

```csharp
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

- [ ] **Step 3: Area view infrastructure**

`GAC.Web/Areas/Admin/Views/_ViewImports.cshtml`:

```cshtml
@using GAC.Web
@using GAC.Web.Areas.Admin
@using GAC.Core.Content
@using GAC.Core.Identity
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

`GAC.Web/Areas/Admin/Views/_ViewStart.cshtml`:

```cshtml
@{ Layout = "_AdminLayout"; }
```

`GAC.Web/Areas/Admin/Views/Shared/_AdminLayout.cshtml`:

```cshtml
<!DOCTYPE html>
<html lang="en" dir="ltr">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>GAC Admin — @ViewData["Title"]</title>
    <link rel="stylesheet" href="/assets/css/admin.css" />
</head>
<body class="adm">
    <header class="adm-top">
        <a class="adm-brand" href="/Admin">GAC Admin</a>
        <div class="adm-spacer"></div>
        @if (User?.Identity?.IsAuthenticated == true)
        {
            <span class="adm-user">@User.Identity!.Name</span>
            <form method="post" action="/admin/logout" class="adm-logoutform">
                @Html.AntiForgeryToken()
                <button type="submit" class="adm-logout">Log out</button>
            </form>
        }
    </header>
    <div class="adm-body">
        @if (User?.Identity?.IsAuthenticated == true)
        {
            <partial name="_AdminNav" />
        }
        <main class="adm-main">
            @if (TempData["Flash"] != null)
            {
                <div class="adm-flash">@TempData["Flash"]</div>
            }
            @RenderBody()
        </main>
    </div>
    <script src="/assets/js/admin.js"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```

`GAC.Web/Areas/Admin/Views/Shared/_AdminNav.cshtml` (links are shown by role; controllers still enforce):

```cshtml
<nav class="adm-nav">
    <a href="/Admin">Dashboard</a>
    @if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Sales))
    {
        <a href="/Admin/Leads">Leads</a>
    }
    @if (User.IsInRole(Roles.Admin) || User.IsInRole(Roles.Editor))
    {
        <a href="/Admin/Vehicles">Vehicles</a>
        <a href="/Admin/Menu">Menu</a>
        <a href="/Admin/HomeContent">Hero Slides</a>
        <a href="/Admin/News">News</a>
        <a href="/Admin/Offers">Offers</a>
        <a href="/Admin/ContentPages">Content Pages</a>
        <a href="/Admin/FormPages">Form Pages</a>
        <a href="/Admin/Media">Media</a>
    }
    @if (User.IsInRole(Roles.Admin))
    {
        <a href="/Admin/Settings">Site Settings</a>
        <a href="/Admin/Users">Users</a>
    }
</nav>
```

- [ ] **Step 4: Admin CSS + JS (minimal)**

`GAC.Web/wwwroot/assets/css/admin.css` — a small, self-contained stylesheet (no dependency on public `styles.css`). Provide at minimum: `body.adm` base font/reset, `.adm-top` (flex top bar), `.adm-body` (flex: nav + main), `.adm-nav a` (block links), `.adm-main` padding, `.adm-flash` (green notice), tables `.adm-table` (full-width, bordered rows), forms `.adm-field` (label block + input full-width), `.adm-btn`/`.adm-btn--danger` buttons, `.adm-localized` (two side-by-side inputs), `.adm-modal`/`.adm-modal__backdrop` (hidden by default, shown via `.is-open`). Keep it ~120 lines, plain CSS.

`GAC.Web/wwwroot/assets/js/admin.js` — start with just the media-picker wiring stub (completed in Task 3). For Task 1, create the file with:

```javascript
// Admin panel scripts. Media picker wiring added in Task 3.
(function () { "use strict"; })();
```

- [ ] **Step 5: Account + Dashboard controllers**

`GAC.Web/Areas/Admin/Models/LoginViewModel.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace GAC.Web.Areas.Admin.Models;

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}
```

`GAC.Web/Areas/Admin/Controllers/AccountController.cs`:

```csharp
using GAC.Core.Identity;
using GAC.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AutoValidateAntiforgeryToken]
public class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signIn;

    public AccountController(SignInManager<ApplicationUser> signIn) => _signIn = signIn;

    [HttpGet("/admin/login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return Redirect("/Admin");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost("/admin/login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await _signIn.PasswordSignInAsync(
            model.Email, model.Password, isPersistent: true, lockoutOnFailure: false);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        return Redirect(string.IsNullOrEmpty(model.ReturnUrl) ? "/Admin" : model.ReturnUrl);
    }

    [HttpPost("/admin/logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return Redirect("/admin/login");
    }

    [HttpGet("/admin/denied")]
    [AllowAnonymous]
    public IActionResult Denied() => View();
}
```

`GAC.Web/Areas/Admin/Controllers/DashboardController.cs`:

```csharp
using GAC.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    public DashboardController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewData["NewLeads"] = await _db.Leads.CountAsync(l => l.Status == GAC.Core.Content.LeadStatus.New);
        ViewData["Vehicles"] = await _db.Vehicles.CountAsync();
        ViewData["News"] = await _db.NewsArticles.CountAsync();
        return View();
    }
}
```

- [ ] **Step 6: Account + Dashboard views**

`GAC.Web/Areas/Admin/Views/Account/Login.cshtml`:

```cshtml
@model GAC.Web.Areas.Admin.Models.LoginViewModel
@{ ViewData["Title"] = "Sign in"; Layout = "_AdminLayout"; }
<div class="adm-login">
    <h1>GAC Admin</h1>
    <form method="post" action="/admin/login">
        @Html.AntiForgeryToken()
        <input type="hidden" asp-for="ReturnUrl" />
        <div asp-validation-summary="All" class="adm-error"></div>
        <div class="adm-field">
            <label asp-for="Email">Email</label>
            <input asp-for="Email" autocomplete="username" />
        </div>
        <div class="adm-field">
            <label asp-for="Password">Password</label>
            <input asp-for="Password" type="password" autocomplete="current-password" />
        </div>
        <button type="submit" class="adm-btn">Sign in</button>
    </form>
</div>
```

`GAC.Web/Areas/Admin/Views/Account/Denied.cshtml`:

```cshtml
@{ ViewData["Title"] = "Access denied"; }
<h1>Access denied</h1>
<p>Your account does not have permission to view that section.</p>
<p><a href="/Admin">Back to dashboard</a></p>
```

`GAC.Web/Areas/Admin/Views/Dashboard/Index.cshtml`:

```cshtml
@{ ViewData["Title"] = "Dashboard"; }
<h1>Dashboard</h1>
<div class="adm-cards">
    <div class="adm-card"><span class="adm-card__n">@ViewData["NewLeads"]</span><span>New leads</span></div>
    <div class="adm-card"><span class="adm-card__n">@ViewData["Vehicles"]</span><span>Vehicles</span></div>
    <div class="adm-card"><span class="adm-card__n">@ViewData["News"]</span><span>News articles</span></div>
</div>
```

- [ ] **Step 7: Test auth handler + factory**

`GAC.Tests/Admin/TestAuthHandler.cs` — authenticates as the role in header `X-Test-Role`; no header → anonymous (so the real cookie challenge redirects to `/admin/login`):

```csharp
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GAC.Tests.Admin;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string RoleHeader = "X-Test-Role";

    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RoleHeader, out var role) || string.IsNullOrWhiteSpace(role))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, $"test-{role}@gacsaudi.local"),
            new Claim(ClaimTypes.Role, role.ToString())
        };
        var ticket = new AuthenticationTicket(
            new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName)), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
```

`GAC.Tests/Admin/AdminWebApplicationFactory.cs` — keeps the real cookie *challenge* (so anonymous redirects to the login page) but makes `Test` the default *authenticate* scheme:

```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GAC.Tests.Admin;

public class AdminWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName);
        });
    }

    public System.Net.Http.HttpClient ClientForRole(string? role)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        if (!string.IsNullOrEmpty(role))
            client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, role);
        return client;
    }
}
```

- [ ] **Step 8: Write the failing access-control tests**

`GAC.Tests/Admin/AdminAuthTests.cs`:

```csharp
using System.Net;
using GAC.Core.Identity;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminAuthTests : IClassFixture<AdminWebApplicationFactory>
{
    private readonly AdminWebApplicationFactory _factory;
    public AdminAuthTests(AdminWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Dashboard_Anonymous_RedirectsToLogin()
    {
        var res = await _factory.ClientForRole(null).GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode);
        Assert.Contains("/admin/login", res.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Dashboard_Admin_Ok()
    {
        var res = await _factory.ClientForRole(Roles.Admin).GetAsync("/Admin");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Login_Anonymous_Ok()
    {
        var res = await _factory.ClientForRole(null).GetAsync("/admin/login");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
```

- [ ] **Step 9: Run the tests — expect FAIL then PASS**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminAuthTests"`
Expected first: build/compile until controllers+factory exist, then 3 pass. Fix until green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Web/Areas Solution/GAC.Web/Program.cs Solution/GAC.Web/wwwroot/assets/css/admin.css Solution/GAC.Web/wwwroot/assets/js/admin.js Solution/GAC.Tests/Admin
git commit -m "feat(admin): area foundation — auth, policies, layout, login, dashboard"
```

---

## Task 2: Leads inbox (LeadsAccess: Admin or Sales)

**Files:**
- Create: `GAC.Core/Services/IAdminLeadService.cs`
- Create: `GAC.Infrastructure/Services/AdminLeadService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/LeadsController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Leads/Index.cshtml`, `Details.cshtml`
- Modify: `GAC.Web/Program.cs` (register service)
- Test: `GAC.Tests/Admin/AdminLeadServiceTests.cs`

- [ ] **Step 1: Service interface**

`GAC.Core/Services/IAdminLeadService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public record LeadFilter(FormType? FormType, LeadStatus? Status, DateOnly? From, DateOnly? To);

public interface IAdminLeadService
{
    Task<IReadOnlyList<Lead>> ListAsync(LeadFilter filter, CancellationToken ct = default);
    Task<Lead?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> SetStatusAsync(int id, LeadStatus status, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the failing service tests**

`GAC.Tests/Admin/AdminLeadServiceTests.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminLeadServiceTests
{
    private static ApplicationDbContext NewDb(string name) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(name).Options);

    private static async Task Seed(ApplicationDbContext db)
    {
        db.Leads.AddRange(
            new Lead { FormType = FormType.TestDrive, Status = LeadStatus.New, Name = "A", CreatedAt = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero) },
            new Lead { FormType = FormType.Quote, Status = LeadStatus.Contacted, Name = "B", CreatedAt = new DateTimeOffset(2026, 6, 10, 0, 0, 0, TimeSpan.Zero) });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task List_FiltersByStatus()
    {
        var db = NewDb(nameof(List_FiltersByStatus)); await Seed(db);
        var svc = new AdminLeadService(db);
        var rows = await svc.ListAsync(new LeadFilter(null, LeadStatus.New, null, null));
        Assert.Single(rows);
        Assert.Equal("A", rows[0].Name);
    }

    [Fact]
    public async Task SetStatus_Updates()
    {
        var db = NewDb(nameof(SetStatus_Updates)); await Seed(db);
        var svc = new AdminLeadService(db);
        var id = (await db.Leads.FirstAsync()).Id;
        Assert.True(await svc.SetStatusAsync(id, LeadStatus.Closed));
        Assert.Equal(LeadStatus.Closed, (await db.Leads.FindAsync(id))!.Status);
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes)); await Seed(db);
        var svc = new AdminLeadService(db);
        var id = (await db.Leads.FirstAsync()).Id;
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Leads.FindAsync(id));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminLeadServiceTests"`
Expected: FAIL (no `AdminLeadService`).

- [ ] **Step 4: Implement the service**

`GAC.Infrastructure/Services/AdminLeadService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminLeadService : IAdminLeadService
{
    private readonly ApplicationDbContext _db;
    public AdminLeadService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Lead>> ListAsync(LeadFilter f, CancellationToken ct = default)
    {
        var q = _db.Leads.Include(l => l.Vehicle).AsQueryable();
        if (f.FormType is { } ft) q = q.Where(l => l.FormType == ft);
        if (f.Status is { } st) q = q.Where(l => l.Status == st);
        if (f.From is { } from) q = q.Where(l => l.CreatedAt >= new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        if (f.To is { } to) q = q.Where(l => l.CreatedAt < new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero));
        return await q.OrderByDescending(l => l.CreatedAt).ToListAsync(ct);
    }

    public async Task<Lead?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Leads.Include(l => l.Vehicle).FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<bool> SetStatusAsync(int id, LeadStatus status, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return false;
        lead.Status = status;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var lead = await _db.Leads.FindAsync([id], ct);
        if (lead is null) return false;
        _db.Leads.Remove(lead);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Register the service in `Program.cs`**

Add after the existing `AddScoped<ILeadService, LeadService>()` line:

```csharp
builder.Services.AddScoped<IAdminLeadService, AdminLeadService>();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminLeadServiceTests"`
Expected: 3 PASS.

- [ ] **Step 7: Controller**

`GAC.Web/Areas/Admin/Controllers/LeadsController.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.LeadsAccess)]
[AutoValidateAntiforgeryToken]
public class LeadsController : Controller
{
    private readonly IAdminLeadService _leads;
    public LeadsController(IAdminLeadService leads) => _leads = leads;

    public async Task<IActionResult> Index(FormType? formType, LeadStatus? status, DateOnly? from, DateOnly? to)
    {
        var rows = await _leads.ListAsync(new LeadFilter(formType, status, from, to));
        ViewData["formType"] = formType; ViewData["status"] = status;
        ViewData["from"] = from; ViewData["to"] = to;
        return View(rows);
    }

    public async Task<IActionResult> Details(int id)
    {
        var lead = await _leads.GetAsync(id);
        return lead is null ? NotFound() : View(lead);
    }

    [HttpPost]
    public async Task<IActionResult> SetStatus(int id, LeadStatus status)
    {
        if (!await _leads.SetStatusAsync(id, status)) return NotFound();
        TempData["Flash"] = "Lead status updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _leads.DeleteAsync(id);
        TempData["Flash"] = "Lead deleted.";
        return RedirectToAction(nameof(Index));
    }
}
```

- [ ] **Step 8: Views**

`GAC.Web/Areas/Admin/Views/Leads/Index.cshtml` — a filter form (selects for FormType + Status from the enums, two date inputs, a "Filter" submit, GET method) and a table of leads with columns Created (`@l.CreatedAt.ToString("yyyy-MM-dd")`), Name, FormType, Status, Phone, Email, and a "View" link to `Details`. Model: `@model IReadOnlyList<GAC.Core.Content.Lead>`. Use `Html.GetEnumSelectList<FormType>()` / `<LeadStatus>()` for the filter dropdowns, each with an "(all)" empty option; preselect from `ViewData`.

`GAC.Web/Areas/Admin/Views/Leads/Details.cshtml` — `@model GAC.Core.Content.Lead`. Show all fields (Name, FormType, Status, Phone, Email, Message, Vehicle?.Name via `.Localize()`, PreferredDate, Branch, SourcePage, CreatedAt). Include a POST form to `SetStatus` with a `LeadStatus` dropdown + "Update" button, and a POST form to `Delete` with an `onsubmit="return confirm('Delete this lead?')"` guard. Both forms need `@Html.AntiForgeryToken()` (or use `asp-action` tag-helper forms which emit it automatically) and a hidden `id`.

- [ ] **Step 9: Access-control integration test**

Append to `GAC.Tests/Admin/AdminAuthTests.cs`:

```csharp
[Theory]
[InlineData(Roles.Admin, HttpStatusCode.OK)]
[InlineData(Roles.Sales, HttpStatusCode.OK)]
[InlineData(Roles.Editor, HttpStatusCode.Found)] // Editor lacks LeadsAccess → challenged/denied
public async Task Leads_AccessByRole(string role, HttpStatusCode expected)
{
    var res = await _factory.ClientForRole(role).GetAsync("/Admin/Leads");
    Assert.Equal(expected, res.StatusCode);
}
```

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminAuthTests|FullyQualifiedName~AdminLeadServiceTests"`
Expected: all green. (Editor → redirect to AccessDenied = `302 Found`.)

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminLeadService.cs Solution/GAC.Infrastructure/Services/AdminLeadService.cs Solution/GAC.Web/Areas/Admin/Controllers/LeadsController.cs Solution/GAC.Web/Areas/Admin/Views/Leads Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminLeadServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): leads inbox — list/filter, detail, status change, delete"
```

---

## Task 3: Shared bilingual editor partial + media library + picker (ContentEditor)

**Files:**
- Create: `GAC.Web/Areas/Admin/Models/LocalizedFieldModel.cs`
- Create: `GAC.Web/Areas/Admin/Views/Shared/_LocalizedField.cshtml`
- Create: `GAC.Core/Services/MediaOptions.cs`, `GAC.Core/Content/MediaUploadResult.cs`, `GAC.Core/Services/IMediaService.cs`
- Create: `GAC.Infrastructure/Services/MediaService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/MediaController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Media/Index.cshtml`, `Shared/_PickerModal.cshtml`
- Modify: `GAC.Web/Program.cs` (bind `MediaOptions`, register `IMediaService`), `GAC.Web/wwwroot/assets/js/admin.js`
- Test: `GAC.Tests/Admin/MediaServiceTests.cs`

- [ ] **Step 1: Bilingual editor partial**

`GAC.Web/Areas/Admin/Models/LocalizedFieldModel.cs`:

```csharp
namespace GAC.Web.Areas.Admin.Models;

public class LocalizedFieldModel
{
    public string Label { get; set; } = "";
    public string NameEn { get; set; } = "";   // form field name, e.g. "Name.En"
    public string NameAr { get; set; } = "";   // form field name, e.g. "Name.Ar"
    public string? ValueEn { get; set; }
    public string? ValueAr { get; set; }
    public bool Multiline { get; set; }
    public bool Code { get; set; }              // monospace raw-HTML editor (Phase 6b)
}
```

`GAC.Web/Areas/Admin/Views/Shared/_LocalizedField.cshtml`:

```cshtml
@model GAC.Web.Areas.Admin.Models.LocalizedFieldModel
<div class="adm-field adm-localized">
    <span class="adm-localized__label">@Model.Label</span>
    <div class="adm-localized__pair">
        <div>
            <label>English</label>
            @if (Model.Multiline || Model.Code)
            {
                <textarea name="@Model.NameEn" rows="@(Model.Code ? 18 : 4)"
                          class="@(Model.Code ? "adm-code" : "")">@Model.ValueEn</textarea>
            }
            else
            {
                <input name="@Model.NameEn" value="@Model.ValueEn" />
            }
        </div>
        <div dir="rtl">
            <label>Arabic</label>
            @if (Model.Multiline || Model.Code)
            {
                <textarea name="@Model.NameAr" rows="@(Model.Code ? 18 : 4)"
                          class="@(Model.Code ? "adm-code" : "")">@Model.ValueAr</textarea>
            }
            else
            {
                <input name="@Model.NameAr" value="@Model.ValueAr" />
            }
        </div>
    </div>
</div>
```

Usage from any edit view binds to a `LocalizedText` property `Foo`:
```cshtml
<partial name="_LocalizedField" model="new LocalizedFieldModel {
    Label = "Name", NameEn = "Name.En", NameAr = "Name.Ar",
    ValueEn = Model.Name.En, ValueAr = Model.Name.Ar }" />
```
Because the input names are `Name.En` / `Name.Ar`, the default model binder populates the entity's `LocalizedText Name { En, Ar }` owned type directly on POST.

- [ ] **Step 2: Media options + result + interface**

`GAC.Core/Services/MediaOptions.cs`:

```csharp
namespace GAC.Core.Services;

public class MediaOptions
{
    // Absolute filesystem folder where uploads are written. Defaults to wwwroot/uploads (set in Program.cs).
    public string Root { get; set; } = "";
    // Public URL prefix that maps to Root (default "/uploads").
    public string PublicPrefix { get; set; } = "/uploads";
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
```

`GAC.Core/Content/MediaUploadResult.cs`:

```csharp
namespace GAC.Core.Content;

public record MediaUploadResult(bool Ok, string? Path, string? Error);
```

`GAC.Core/Services/IMediaService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IMediaService
{
    Task<MediaUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken ct = default);
    Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
```

- [ ] **Step 3: Write the failing media service tests**

`GAC.Tests/Admin/MediaServiceTests.cs`:

```csharp
using System.Text;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace GAC.Tests.Admin;

public class MediaServiceTests : IDisposable
{
    private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "gacmedia-" + System.Guid.NewGuid().ToString("N"));

    private ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    private MediaService NewSvc(ApplicationDbContext db) =>
        new(db, Options.Create(new MediaOptions { Root = _root, PublicPrefix = "/uploads", MaxBytes = 1024 }));

    [Fact]
    public async Task Upload_Image_WritesFile_AndTracksAsset()
    {
        var db = NewDb(nameof(Upload_Image_WritesFile_AndTracksAsset));
        var svc = NewSvc(db);
        var bytes = Encoding.UTF8.GetBytes("fake-image");
        var res = await svc.UploadAsync(new MemoryStream(bytes), "Photo Name.png", "image/png", bytes.Length);

        Assert.True(res.Ok);
        Assert.NotNull(res.Path);
        Assert.StartsWith("/uploads/", res.Path);
        Assert.True(System.IO.File.Exists(System.IO.Path.Combine(_root, System.IO.Path.GetFileName(res.Path!))));
        Assert.Equal(1, await db.MediaAssets.CountAsync());
    }

    [Fact]
    public async Task Upload_RejectsNonImage()
    {
        var db = NewDb(nameof(Upload_RejectsNonImage));
        var res = await NewSvc(db).UploadAsync(new MemoryStream([1, 2, 3]), "x.exe", "application/octet-stream", 3);
        Assert.False(res.Ok);
        Assert.Equal(0, await db.MediaAssets.CountAsync());
    }

    [Fact]
    public async Task Upload_RejectsOversize()
    {
        var db = NewDb(nameof(Upload_RejectsOversize));
        var res = await NewSvc(db).UploadAsync(new MemoryStream(new byte[2048]), "big.png", "image/png", 2048);
        Assert.False(res.Ok);
    }

    public void Dispose()
    {
        if (System.IO.Directory.Exists(_root)) System.IO.Directory.Delete(_root, true);
    }
}
```

- [ ] **Step 4: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~MediaServiceTests"`
Expected: FAIL (no `MediaService`).

- [ ] **Step 5: Implement the media service**

`GAC.Infrastructure/Services/MediaService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GAC.Infrastructure.Services;

public class MediaService : IMediaService
{
    private static readonly HashSet<string> AllowedExt =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".svg" };
    private static readonly HashSet<string> AllowedCt =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "image/gif", "image/svg+xml" };

    private readonly ApplicationDbContext _db;
    private readonly MediaOptions _opt;

    public MediaService(ApplicationDbContext db, IOptions<MediaOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task<MediaUploadResult> UploadAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(ext) || !AllowedExt.Contains(ext) || !AllowedCt.Contains(contentType))
            return new MediaUploadResult(false, null, "Only image files (jpg, png, webp, gif, svg) are allowed.");
        if (length <= 0 || length > _opt.MaxBytes)
            return new MediaUploadResult(false, null, $"File must be between 1 byte and {_opt.MaxBytes / 1024} KB.");

        Directory.CreateDirectory(_opt.Root);
        var safeName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var fullPath = Path.Combine(_opt.Root, safeName);
        await using (var fs = File.Create(fullPath))
            await content.CopyToAsync(fs, ct);

        var publicPath = $"{_opt.PublicPrefix.TrimEnd('/')}/{safeName}";
        var asset = new MediaAsset
        {
            Path = publicPath,
            OriginalFileName = originalFileName,
            UploadedAt = DateTimeOffset.UtcNow
        };
        _db.MediaAssets.Add(asset);
        await _db.SaveChangesAsync(ct);
        return new MediaUploadResult(true, publicPath, null);
    }

    public async Task<IReadOnlyList<MediaAsset>> ListAsync(CancellationToken ct = default)
        => await _db.MediaAssets.AsNoTracking().OrderByDescending(m => m.UploadedAt).ToListAsync(ct);

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var asset = await _db.MediaAssets.FindAsync([id], ct);
        if (asset is null) return false;
        var full = Path.Combine(_opt.Root, Path.GetFileName(asset.Path));
        if (File.Exists(full)) File.Delete(full);
        _db.MediaAssets.Remove(asset);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 6: Bind options + register service in `Program.cs`**

Add (the default Root resolves to `wwwroot/uploads`):

```csharp
builder.Services.Configure<GAC.Core.Services.MediaOptions>(opt =>
{
    builder.Configuration.GetSection("Media").Bind(opt);
    if (string.IsNullOrWhiteSpace(opt.Root))
        opt.Root = Path.Combine(builder.Environment.WebRootPath ?? "wwwroot", "uploads");
});
builder.Services.AddScoped<IMediaService, MediaService>();
```

- [ ] **Step 7: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~MediaServiceTests"`
Expected: 3 PASS.

- [ ] **Step 8: Media controller**

`GAC.Web/Areas/Admin/Controllers/MediaController.cs`:

```csharp
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class MediaController : Controller
{
    private readonly IMediaService _media;
    public MediaController(IMediaService media) => _media = media;

    public async Task<IActionResult> Index() => View(await _media.ListAsync());

    // Returns JSON for the picker's AJAX upload, or redirects for the plain library page.
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile? file, bool json = false)
    {
        if (file is null || file.Length == 0)
            return json ? BadRequest(new { error = "No file." }) : RedirectToAction(nameof(Index));

        await using var stream = file.OpenReadStream();
        var res = await _media.UploadAsync(stream, file.FileName, file.ContentType, file.Length);
        if (json) return res.Ok ? Ok(new { path = res.Path }) : BadRequest(new { error = res.Error });

        TempData["Flash"] = res.Ok ? "Uploaded." : res.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _media.DeleteAsync(id);
        TempData["Flash"] = "Deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> List() => Json((await _media.ListAsync()).Select(m => new { m.Id, m.Path }));
}
```

- [ ] **Step 9: Media library view + picker modal**

`GAC.Web/Areas/Admin/Views/Media/Index.cshtml` — `@model IReadOnlyList<GAC.Core.Content.MediaAsset>`. An upload `<form method="post" asp-action="Upload" enctype="multipart/form-data">` with `<input type="file" name="file" accept="image/*">` + submit; below, a grid of thumbnails (`<img src="@m.Path">`) each with the path text and a POST `Delete` form (confirm guard). Use tag-helper forms (`asp-action`) so the anti-forgery token is emitted automatically.

`GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml` — a single reusable modal placed once per edit page that has image fields:

```cshtml
<div class="adm-modal" id="mediaPicker" aria-hidden="true">
  <div class="adm-modal__backdrop" data-picker-close></div>
  <div class="adm-modal__panel">
    <div class="adm-modal__head">
      <strong>Choose image</strong>
      <button type="button" data-picker-close>&times;</button>
    </div>
    <form class="adm-picker-upload" data-picker-upload enctype="multipart/form-data">
      @Html.AntiForgeryToken()
      <input type="file" name="file" accept="image/*" />
      <button type="submit" class="adm-btn">Upload</button>
    </form>
    <div class="adm-picker-grid" data-picker-grid></div>
  </div>
</div>
```

An image field on any edit form uses this markup so the picker can populate it:
```cshtml
<div class="adm-field">
  <label>Image</label>
  <input name="ImagePath" value="@Model.ImagePath" data-media-input />
  <button type="button" class="adm-btn" data-media-pick>Choose…</button>
</div>
```

- [ ] **Step 10: Picker JavaScript**

Replace the body of `GAC.Web/wwwroot/assets/js/admin.js` with:

```javascript
// Admin panel scripts: reusable media picker.
(function () {
  "use strict";
  var modal = document.getElementById("mediaPicker");
  if (!modal) return;

  var grid = modal.querySelector("[data-picker-grid]");
  var token = modal.querySelector('input[name="__RequestVerificationToken"]');
  var activeInput = null;

  function open(input) { activeInput = input; modal.classList.add("is-open"); loadGrid(); }
  function close() { modal.classList.remove("is-open"); activeInput = null; }

  function loadGrid() {
    fetch("/Admin/Media/List", { headers: { "Accept": "application/json" } })
      .then(function (r) { return r.json(); })
      .then(function (items) {
        grid.innerHTML = "";
        items.forEach(function (it) {
          var img = document.createElement("img");
          img.src = it.path; img.title = it.path; img.className = "adm-picker-thumb";
          img.addEventListener("click", function () {
            if (activeInput) activeInput.value = it.path;
            close();
          });
          grid.appendChild(img);
        });
      });
  }

  document.addEventListener("click", function (e) {
    var pick = e.target.closest("[data-media-pick]");
    if (pick) { open(pick.parentElement.querySelector("[data-media-input]")); return; }
    if (e.target.closest("[data-picker-close]")) { close(); }
  });

  var uploadForm = modal.querySelector("[data-picker-upload]");
  if (uploadForm) {
    uploadForm.addEventListener("submit", function (e) {
      e.preventDefault();
      var data = new FormData(uploadForm);
      data.append("json", "true");
      fetch("/Admin/Media/Upload", {
        method: "POST",
        headers: { "RequestVerificationToken": token ? token.value : "" },
        body: data
      })
        .then(function (r) { return r.json(); })
        .then(function (res) {
          if (res.path && activeInput) { activeInput.value = res.path; loadGrid(); }
          else if (res.error) { alert(res.error); }
        });
    });
  }
})();
```

- [ ] **Step 11: Access test for media + run full suite**

Append to `GAC.Tests/Admin/AdminAuthTests.cs`:

```csharp
[Theory]
[InlineData(Roles.Admin, HttpStatusCode.OK)]
[InlineData(Roles.Editor, HttpStatusCode.OK)]
[InlineData(Roles.Sales, HttpStatusCode.Found)] // Sales lacks ContentEditor
public async Task Media_AccessByRole(string role, HttpStatusCode expected)
{
    var res = await _factory.ClientForRole(role).GetAsync("/Admin/Media");
    Assert.Equal(expected, res.StatusCode);
}
```

Run: `dotnet test Solution/GAC.sln`
Expected: full suite green (83 prior + new admin tests).

- [ ] **Step 12: Commit**

```bash
git add Solution/GAC.Core/Services/IMediaService.cs Solution/GAC.Core/Services/MediaOptions.cs Solution/GAC.Core/Content/MediaUploadResult.cs Solution/GAC.Infrastructure/Services/MediaService.cs Solution/GAC.Web/Areas/Admin/Models/LocalizedFieldModel.cs Solution/GAC.Web/Areas/Admin/Views/Shared/_LocalizedField.cshtml Solution/GAC.Web/Areas/Admin/Views/Shared/_PickerModal.cshtml Solution/GAC.Web/Areas/Admin/Controllers/MediaController.cs Solution/GAC.Web/Areas/Admin/Views/Media Solution/GAC.Web/Program.cs Solution/GAC.Web/wwwroot/assets/js/admin.js Solution/GAC.Tests/Admin/MediaServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): shared bilingual editor partial + media library and picker"
```

---

## Task 4: Vehicles CRUD + images (ContentEditor) — reference content CRUD

This task establishes the CRUD pattern reused by Tasks 5–10. It drives both `/models` and the mega-menu (which read visible vehicles).

**Files:**
- Create: `GAC.Core/Services/IAdminVehicleService.cs`
- Create: `GAC.Infrastructure/Services/AdminVehicleService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Vehicles/Index.cshtml`, `Edit.cshtml`, `_Images.cshtml`
- Modify: `GAC.Web/Program.cs` (register service)
- Test: `GAC.Tests/Admin/AdminVehicleServiceTests.cs`

- [ ] **Step 1: Service interface**

`GAC.Core/Services/IAdminVehicleService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminVehicleService
{
    Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken ct = default);          // incl. hidden, ordered
    Task<Vehicle?> GetAsync(int id, CancellationToken ct = default);                 // +Images
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(Vehicle vehicle, CancellationToken ct = default);          // returns new Id
    Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken ct = default);         // scalar + localized fields
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default);     // -1 up, +1 down (swaps SortOrder)
    Task<int> AddImageAsync(int vehicleId, string path, VehicleImageKind kind, CancellationToken ct = default);
    Task<bool> RemoveImageAsync(int imageId, CancellationToken ct = default);
    Task<bool> MoveImageAsync(int imageId, int direction, CancellationToken ct = default);
}
```

- [ ] **Step 2: Write the failing service tests**

`GAC.Tests/Admin/AdminVehicleServiceTests.cs`:

```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminVehicleServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Then_Get_RoundTrips()
    {
        var db = NewDb(nameof(Create_Then_Get_RoundTrips));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "x1", Name = "X1", SortOrder = 5, IsVisible = true });
        var v = await svc.GetAsync(id);
        Assert.NotNull(v);
        Assert.Equal("x1", v!.Slug);
    }

    [Fact]
    public async Task SlugExists_DetectsDuplicate_IgnoringSelf()
    {
        var db = NewDb(nameof(SlugExists_DetectsDuplicate_IgnoringSelf));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "dup", Name = "D" });
        Assert.True(await svc.SlugExistsAsync("dup"));
        Assert.False(await svc.SlugExistsAsync("dup", exceptId: id));
    }

    [Fact]
    public async Task Move_SwapsSortOrder()
    {
        var db = NewDb(nameof(Move_SwapsSortOrder));
        var svc = new AdminVehicleService(db);
        var a = await svc.CreateAsync(new Vehicle { Slug = "a", Name = "A", SortOrder = 1 });
        var b = await svc.CreateAsync(new Vehicle { Slug = "b", Name = "B", SortOrder = 2 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(1, (await db.Vehicles.FindAsync(b))!.SortOrder);
        Assert.Equal(2, (await db.Vehicles.FindAsync(a))!.SortOrder);
    }

    [Fact]
    public async Task AddImage_Then_Remove()
    {
        var db = NewDb(nameof(AddImage_Then_Remove));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "img", Name = "I" });
        var imgId = await svc.AddImageAsync(id, "/uploads/a.png", VehicleImageKind.Gallery);
        Assert.Equal(1, await db.VehicleImages.CountAsync());
        Assert.True(await svc.RemoveImageAsync(imgId));
        Assert.Equal(0, await db.VehicleImages.CountAsync());
    }

    [Fact]
    public async Task Delete_Removes()
    {
        var db = NewDb(nameof(Delete_Removes));
        var svc = new AdminVehicleService(db);
        var id = await svc.CreateAsync(new Vehicle { Slug = "del", Name = "D" });
        Assert.True(await svc.DeleteAsync(id));
        Assert.Null(await db.Vehicles.FindAsync(id));
    }
}
```

- [ ] **Step 3: Run tests to verify they fail**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminVehicleServiceTests"`
Expected: FAIL (no `AdminVehicleService`).

- [ ] **Step 4: Implement the service**

`GAC.Infrastructure/Services/AdminVehicleService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminVehicleService : IAdminVehicleService
{
    private readonly ApplicationDbContext _db;
    public AdminVehicleService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Vehicle>> ListAsync(CancellationToken ct = default)
        => await _db.Vehicles.OrderBy(v => v.SortOrder).ToListAsync(ct);

    public async Task<Vehicle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.Vehicles.Include(v => v.Images).FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => await _db.Vehicles.AnyAsync(v => v.Slug == slug && (exceptId == null || v.Id != exceptId), ct);

    public async Task<int> CreateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);
        return vehicle.Id;
    }

    public async Task<bool> UpdateAsync(Vehicle vehicle, CancellationToken ct = default)
    {
        var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.Id == vehicle.Id, ct);
        if (existing is null) return false;
        existing.Slug = vehicle.Slug;
        existing.Category = vehicle.Category;
        existing.SortOrder = vehicle.SortOrder;
        existing.IsVisible = vehicle.IsVisible;
        existing.PriceFrom = vehicle.PriceFrom;
        existing.BrochurePdf = vehicle.BrochurePdf;
        existing.Name = vehicle.Name;
        existing.Tagline = vehicle.Tagline;
        existing.IntroText = vehicle.IntroText;
        existing.MetaTitle = vehicle.MetaTitle;
        existing.MetaDescription = vehicle.MetaDescription;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var v = await _db.Vehicles.FindAsync([id], ct);
        if (v is null) return false;
        _db.Vehicles.Remove(v);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var all = await _db.Vehicles.OrderBy(v => v.SortOrder).ToListAsync(ct);
        var idx = all.FindIndex(v => v.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= all.Count) return false;
        (all[idx].SortOrder, all[swap].SortOrder) = (all[swap].SortOrder, all[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> AddImageAsync(int vehicleId, string path, VehicleImageKind kind, CancellationToken ct = default)
    {
        var nextOrder = await _db.VehicleImages.Where(i => i.VehicleId == vehicleId).CountAsync(ct);
        var img = new VehicleImage { VehicleId = vehicleId, Path = path, Kind = kind, SortOrder = nextOrder };
        _db.VehicleImages.Add(img);
        await _db.SaveChangesAsync(ct);
        return img.Id;
    }

    public async Task<bool> RemoveImageAsync(int imageId, CancellationToken ct = default)
    {
        var img = await _db.VehicleImages.FindAsync([imageId], ct);
        if (img is null) return false;
        _db.VehicleImages.Remove(img);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveImageAsync(int imageId, int direction, CancellationToken ct = default)
    {
        var img = await _db.VehicleImages.FindAsync([imageId], ct);
        if (img is null) return false;
        var siblings = await _db.VehicleImages
            .Where(i => i.VehicleId == img.VehicleId).OrderBy(i => i.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(i => i.Id == imageId);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Register in `Program.cs`**

```csharp
builder.Services.AddScoped<IAdminVehicleService, AdminVehicleService>();
```

- [ ] **Step 6: Run tests to verify they pass**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminVehicleServiceTests"`
Expected: 5 PASS.

- [ ] **Step 7: Controller**

`GAC.Web/Areas/Admin/Controllers/VehiclesController.cs`:

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
public class VehiclesController : Controller
{
    private readonly IAdminVehicleService _svc;
    public VehiclesController(IAdminVehicleService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAsync());

    public IActionResult Create() => View("Edit", new Vehicle { IsVisible = true });

    public async Task<IActionResult> Edit(int id)
    {
        var v = await _svc.GetAsync(id);
        return v is null ? NotFound() : View(v);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Vehicle vehicle)
    {
        if (string.IsNullOrWhiteSpace(vehicle.Slug))
            ModelState.AddModelError(nameof(vehicle.Slug), "Slug is required.");
        if (await _svc.SlugExistsAsync(vehicle.Slug, vehicle.Id == 0 ? null : vehicle.Id))
            ModelState.AddModelError(nameof(vehicle.Slug), "That slug is already in use.");
        if (!ModelState.IsValid) return View("Edit", vehicle);

        if (vehicle.Id == 0)
        {
            var newId = await _svc.CreateAsync(vehicle);
            TempData["Flash"] = "Vehicle created.";
            return RedirectToAction(nameof(Edit), new { id = newId });
        }
        await _svc.UpdateAsync(vehicle);
        TempData["Flash"] = "Vehicle saved.";
        return RedirectToAction(nameof(Edit), new { id = vehicle.Id });
    }

    [HttpPost] public async Task<IActionResult> Delete(int id)
    { await _svc.DeleteAsync(id); TempData["Flash"] = "Vehicle deleted."; return RedirectToAction(nameof(Index)); }

    [HttpPost] public async Task<IActionResult> Move(int id, int direction)
    { await _svc.MoveAsync(id, direction); return RedirectToAction(nameof(Index)); }

    [HttpPost] public async Task<IActionResult> AddImage(int vehicleId, string path, VehicleImageKind kind)
    { await _svc.AddImageAsync(vehicleId, path, kind); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> RemoveImage(int imageId, int vehicleId)
    { await _svc.RemoveImageAsync(imageId); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }

    [HttpPost] public async Task<IActionResult> MoveImage(int imageId, int vehicleId, int direction)
    { await _svc.MoveImageAsync(imageId, direction); return RedirectToAction(nameof(Edit), new { id = vehicleId }); }
}
```

- [ ] **Step 8: Views**

`GAC.Web/Areas/Admin/Views/Vehicles/Index.cshtml` — `@model IReadOnlyList<GAC.Core.Content.Vehicle>`. A "New vehicle" link to `Create`; a table with columns SortOrder (+ Move up/down POST buttons to `Move` with `direction` -1/+1), Slug, Name (`@v.Name.Localize()`), Category, Visible (yes/no), and Edit / Delete (POST, confirm) actions.

`GAC.Web/Areas/Admin/Views/Vehicles/Edit.cshtml` — `@model GAC.Core.Content.Vehicle`. A `<form method="post" asp-action="Save">` containing:
- `<input type="hidden" asp-for="Id" />`
- `<div asp-validation-summary="All"></div>`
- text input `asp-for="Slug"`
- the bilingual partials for `Name`, `Tagline`, `IntroText`, `MetaTitle` (multiline), `MetaDescription` (multiline) using `_LocalizedField` (e.g. `NameEn="Name.En"`, `ValueEn="Model.Name.En"`, etc. — `IntroText`/`MetaDescription` set `Multiline = true`).
- number input `asp-for="PriceFrom"`
- text input `asp-for="BrochurePdf"`
- checkbox `asp-for="IsVisible"`
- number input `asp-for="SortOrder"`
- **Category flags:** three checkboxes named `Category` with values `Sedan`, `Suv`, `Ev` (the `[Flags] VehicleCategory` enum binds multiple checked values; precheck via `Model.Category.HasFlag(VehicleCategory.Sedan)` etc.).
- a Save submit button.

Below the form (only when `Model.Id != 0`), render `<partial name="_Images" model="Model" />` and include `<partial name="_PickerModal" />` once.

`GAC.Web/Areas/Admin/Views/Vehicles/_Images.cshtml` — `@model GAC.Core.Content.Vehicle`. Lists `Model.Images` ordered by SortOrder (thumbnail, Kind, Move up/down POST to `MoveImage`, Remove POST to `RemoveImage` — each hidden `vehicleId=@Model.Id`). Below, an "Add image" POST form to `AddImage` with hidden `vehicleId`, a `path` text input wired to the media picker (`data-media-input` + a `data-media-pick` button), and a `kind` select (`Html.GetEnumSelectList<VehicleImageKind>()`).

- [ ] **Step 9: Access + round-trip integration test**

Append to `GAC.Tests/Admin/AdminAuthTests.cs`:

```csharp
[Theory]
[InlineData(Roles.Admin, HttpStatusCode.OK)]
[InlineData(Roles.Editor, HttpStatusCode.OK)]
[InlineData(Roles.Sales, HttpStatusCode.Found)]
public async Task Vehicles_AccessByRole(string role, HttpStatusCode expected)
{
    var res = await _factory.ClientForRole(role).GetAsync("/Admin/Vehicles");
    Assert.Equal(expected, res.StatusCode);
}
```

Run: `dotnet test Solution/GAC.sln`
Expected: full suite green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminVehicleService.cs Solution/GAC.Infrastructure/Services/AdminVehicleService.cs Solution/GAC.Web/Areas/Admin/Controllers/VehiclesController.cs Solution/GAC.Web/Areas/Admin/Views/Vehicles Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminVehicleServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): vehicles CRUD + image management (drives /models + mega-menu)"
```

---

## Task 5: Menu CRUD (ContentEditor) — drives header nav

**Files:**
- Create: `GAC.Core/Services/IAdminMenuService.cs`, `GAC.Infrastructure/Services/AdminMenuService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/MenuController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Menu/Index.cshtml`, `Edit.cshtml`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/Admin/AdminMenuServiceTests.cs`

- [ ] **Step 1: Interface**

`GAC.Core/Services/IAdminMenuService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminMenuService
{
    Task<IReadOnlyList<MenuItem>> ListAllAsync(CancellationToken ct = default);  // flat, ordered, +Parent
    Task<MenuItem?> GetAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(MenuItem item, CancellationToken ct = default);
    Task<bool> UpdateAsync(MenuItem item, CancellationToken ct = default);       // Label, Url, ParentId, SortOrder
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);             // also deletes children
    Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default); // swap within same ParentId scope
}
```

- [ ] **Step 2: Failing tests**

`GAC.Tests/Admin/AdminMenuServiceTests.cs`:

```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GAC.Tests.Admin;

public class AdminMenuServiceTests
{
    private static ApplicationDbContext NewDb(string n) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(n).Options);

    [Fact]
    public async Task Create_Update_RoundTrips()
    {
        var db = NewDb(nameof(Create_Update_RoundTrips));
        var svc = new AdminMenuService(db);
        var id = await svc.CreateAsync(new MenuItem { Label = "Home", Url = "/", SortOrder = 0 });
        var item = await svc.GetAsync(id);
        item!.Url = "/home";
        Assert.True(await svc.UpdateAsync(item));
        Assert.Equal("/home", (await db.MenuItems.FindAsync(id))!.Url);
    }

    [Fact]
    public async Task Delete_CascadesChildren()
    {
        var db = NewDb(nameof(Delete_CascadesChildren));
        var svc = new AdminMenuService(db);
        var parent = await svc.CreateAsync(new MenuItem { Label = "More", SortOrder = 0 });
        await svc.CreateAsync(new MenuItem { Label = "Fleet", ParentId = parent, SortOrder = 0 });
        Assert.True(await svc.DeleteAsync(parent));
        Assert.Equal(0, await db.MenuItems.CountAsync());
    }

    [Fact]
    public async Task Move_SwapsWithinSiblings()
    {
        var db = NewDb(nameof(Move_SwapsWithinSiblings));
        var svc = new AdminMenuService(db);
        var a = await svc.CreateAsync(new MenuItem { Label = "A", SortOrder = 0 });
        var b = await svc.CreateAsync(new MenuItem { Label = "B", SortOrder = 1 });
        Assert.True(await svc.MoveAsync(b, -1));
        Assert.Equal(0, (await db.MenuItems.FindAsync(b))!.SortOrder);
    }
}
```

- [ ] **Step 3: Run → FAIL.** `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~AdminMenuServiceTests"`

- [ ] **Step 4: Implement**

`GAC.Infrastructure/Services/AdminMenuService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminMenuService : IAdminMenuService
{
    private readonly ApplicationDbContext _db;
    public AdminMenuService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuItem>> ListAllAsync(CancellationToken ct = default)
        => await _db.MenuItems.Include(m => m.Parent)
            .OrderBy(m => m.ParentId).ThenBy(m => m.SortOrder).ToListAsync(ct);

    public async Task<MenuItem?> GetAsync(int id, CancellationToken ct = default)
        => await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task<int> CreateAsync(MenuItem item, CancellationToken ct = default)
    {
        _db.MenuItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item.Id;
    }

    public async Task<bool> UpdateAsync(MenuItem item, CancellationToken ct = default)
    {
        var e = await _db.MenuItems.FirstOrDefaultAsync(m => m.Id == item.Id, ct);
        if (e is null) return false;
        e.Label = item.Label; e.Url = item.Url; e.ParentId = item.ParentId; e.SortOrder = item.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var item = await _db.MenuItems.FindAsync([id], ct);
        if (item is null) return false;
        var children = await _db.MenuItems.Where(m => m.ParentId == id).ToListAsync(ct);
        _db.MenuItems.RemoveRange(children);
        _db.MenuItems.Remove(item);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveAsync(int id, int direction, CancellationToken ct = default)
    {
        var item = await _db.MenuItems.FindAsync([id], ct);
        if (item is null) return false;
        var siblings = await _db.MenuItems
            .Where(m => m.ParentId == item.ParentId).OrderBy(m => m.SortOrder).ToListAsync(ct);
        var idx = siblings.FindIndex(m => m.Id == id);
        var swap = idx + direction;
        if (swap < 0 || swap >= siblings.Count) return false;
        (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Register** in `Program.cs`: `builder.Services.AddScoped<IAdminMenuService, AdminMenuService>();`

- [ ] **Step 6: Run → PASS.**

- [ ] **Step 7: Controller** `GAC.Web/Areas/Admin/Controllers/MenuController.cs` — same shape as `VehiclesController` (`Index`, `Create`→`View("Edit", new MenuItem())`, `Edit`, `Save`, `Delete`, `Move`), `[Authorize(Policy = AdminPolicies.ContentEditor)]`. In `Save`, validate Label.En is non-empty; on create redirect to Index. Populate a parent dropdown by passing `await _svc.ListAllAsync()` (top-level items only, i.e. `ParentId == null`) via `ViewBag.Parents`.

- [ ] **Step 8: Views**
  - `Index.cshtml` (`@model IReadOnlyList<GAC.Core.Content.MenuItem>`): table — Label (`@m.Label.Localize()`, indent children), Url, Parent (`@m.Parent?.Label.Localize()`), Move up/down, Edit, Delete (confirm). "New item" link.
  - `Edit.cshtml` (`@model GAC.Core.Content.MenuItem`): hidden `Id`; `_LocalizedField` for `Label`; text `asp-for="Url"`; number `asp-for="SortOrder"`; a `<select asp-for="ParentId">` with an empty "(top level)" option plus options from `ViewBag.Parents` (exclude self); Save button.

- [ ] **Step 9: Access test** — append a `Menu_AccessByRole` theory to `AdminAuthTests` (Admin OK, Editor OK, Sales Found), targeting `/Admin/Menu`. Run full suite → green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminMenuService.cs Solution/GAC.Infrastructure/Services/AdminMenuService.cs Solution/GAC.Web/Areas/Admin/Controllers/MenuController.cs Solution/GAC.Web/Areas/Admin/Views/Menu Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminMenuServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): menu CRUD (drives header nav)"
```

---

## Task 6: Hero slides (ContentEditor) — drives home hero slider

**Files:**
- Create: `GAC.Core/Services/IAdminHomeService.cs`, `GAC.Infrastructure/Services/AdminHomeService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/HomeContentController.cs`
- Create: `GAC.Web/Areas/Admin/Views/HomeContent/Index.cshtml`, `Edit.cshtml`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/Admin/AdminHomeServiceTests.cs`

- [ ] **Step 1: Interface**

`GAC.Core/Services/IAdminHomeService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminHomeService
{
    Task<IReadOnlyList<HeroSlide>> ListSlidesAsync(CancellationToken ct = default);
    Task<HeroSlide?> GetSlideAsync(int id, CancellationToken ct = default);
    Task<int> CreateSlideAsync(HeroSlide slide, CancellationToken ct = default); // attaches to the singleton HomePage
    Task<bool> UpdateSlideAsync(HeroSlide slide, CancellationToken ct = default);
    Task<bool> DeleteSlideAsync(int id, CancellationToken ct = default);
    Task<bool> MoveSlideAsync(int id, int direction, CancellationToken ct = default);
}
```

- [ ] **Step 2: Failing tests** `GAC.Tests/Admin/AdminHomeServiceTests.cs` — cover Create (auto-creates/attaches the singleton `HomePage` if missing), Update round-trip, Move swap, Delete. Pattern identical to `AdminVehicleServiceTests`; seed nothing (the service must create the `HomePage` on first slide). Example create test:

```csharp
[Fact]
public async Task CreateSlide_AttachesToHomePage()
{
    var db = NewDb(nameof(CreateSlide_AttachesToHomePage));
    var svc = new AdminHomeService(db);
    var id = await svc.CreateSlideAsync(new HeroSlide { Heading = "Hi", ImagePath = "/x.jpg" });
    Assert.Equal(1, await db.HeroSlides.CountAsync());
    Assert.Equal(1, await db.HomePages.CountAsync());
    Assert.NotEqual(0, (await db.HeroSlides.FindAsync(id))!.HomePageId);
}
```

- [ ] **Step 3: Run → FAIL.**

- [ ] **Step 4: Implement** `GAC.Infrastructure/Services/AdminHomeService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminHomeService : IAdminHomeService
{
    private readonly ApplicationDbContext _db;
    public AdminHomeService(ApplicationDbContext db) => _db = db;

    private async Task<HomePage> EnsureHomeAsync(CancellationToken ct)
    {
        var home = await _db.HomePages.FirstOrDefaultAsync(ct);
        if (home is null)
        {
            home = new HomePage();
            _db.HomePages.Add(home);
            await _db.SaveChangesAsync(ct);
        }
        return home;
    }

    public async Task<IReadOnlyList<HeroSlide>> ListSlidesAsync(CancellationToken ct = default)
        => await _db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync(ct);

    public async Task<HeroSlide?> GetSlideAsync(int id, CancellationToken ct = default)
        => await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<int> CreateSlideAsync(HeroSlide slide, CancellationToken ct = default)
    {
        var home = await EnsureHomeAsync(ct);
        slide.HomePageId = home.Id;
        slide.SortOrder = await _db.HeroSlides.CountAsync(ct);
        _db.HeroSlides.Add(slide);
        await _db.SaveChangesAsync(ct);
        return slide.Id;
    }

    public async Task<bool> UpdateSlideAsync(HeroSlide slide, CancellationToken ct = default)
    {
        var e = await _db.HeroSlides.FirstOrDefaultAsync(s => s.Id == slide.Id, ct);
        if (e is null) return false;
        e.ImagePath = slide.ImagePath; e.Heading = slide.Heading; e.Subheading = slide.Subheading;
        e.CtaText = slide.CtaText; e.CtaLink = slide.CtaLink;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteSlideAsync(int id, CancellationToken ct = default)
    {
        var s = await _db.HeroSlides.FindAsync([id], ct);
        if (s is null) return false;
        _db.HeroSlides.Remove(s);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> MoveSlideAsync(int id, int direction, CancellationToken ct = default)
    {
        var all = await _db.HeroSlides.OrderBy(s => s.SortOrder).ToListAsync(ct);
        var idx = all.FindIndex(s => s.Id == id);
        if (idx < 0) return false;
        var swap = idx + direction;
        if (swap < 0 || swap >= all.Count) return false;
        (all[idx].SortOrder, all[swap].SortOrder) = (all[swap].SortOrder, all[idx].SortOrder);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Register** `builder.Services.AddScoped<IAdminHomeService, AdminHomeService>();`

- [ ] **Step 6: Run → PASS.**

- [ ] **Step 7: Controller** `HomeContentController` — actions `Index`, `Create`→`View("Edit", new HeroSlide())`, `Edit`, `Save` (create vs update by `Id == 0`), `Delete`, `Move`. `[Authorize(Policy = AdminPolicies.ContentEditor)]`.

- [ ] **Step 8: Views**
  - `Index.cshtml` (`@model IReadOnlyList<GAC.Core.Content.HeroSlide>`): table — thumbnail (`@s.ImagePath`), Heading (`@s.Heading.Localize()`), Move up/down, Edit, Delete (confirm); "New slide" link.
  - `Edit.cshtml` (`@model GAC.Core.Content.HeroSlide`): hidden `Id`; image field for `ImagePath` (media picker `data-media-input`/`data-media-pick`); `_LocalizedField` for `Heading`, `Subheading` (multiline), `CtaText`; text `asp-for="CtaLink"`; Save. Include `<partial name="_PickerModal" />`.

- [ ] **Step 9: Access test** — `HeroSlides_AccessByRole` theory targeting `/Admin/HomeContent` (Admin/Editor OK, Sales Found). Full suite → green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminHomeService.cs Solution/GAC.Infrastructure/Services/AdminHomeService.cs Solution/GAC.Web/Areas/Admin/Controllers/HomeContentController.cs Solution/GAC.Web/Areas/Admin/Views/HomeContent Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminHomeServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): hero slides CRUD (drives home hero slider)"
```

---

## Task 7: News + Offers CRUD (ContentEditor)

News and Offers are near-identical simple slug entities; build both in one task with two services.

**Files:**
- Create: `GAC.Core/Services/IAdminNewsService.cs`, `IAdminOfferService.cs`
- Create: `GAC.Infrastructure/Services/AdminNewsService.cs`, `AdminOfferService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/NewsController.cs`, `OffersController.cs`
- Create: `GAC.Web/Areas/Admin/Views/News/Index.cshtml`, `Edit.cshtml`; `Offers/Index.cshtml`, `Edit.cshtml`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/Admin/AdminNewsServiceTests.cs`, `AdminOfferServiceTests.cs`

- [ ] **Step 1: Interfaces**

`GAC.Core/Services/IAdminNewsService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminNewsService
{
    Task<IReadOnlyList<NewsArticle>> ListAsync(CancellationToken ct = default);   // incl. unpublished
    Task<NewsArticle?> GetAsync(int id, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default);
    Task<int> CreateAsync(NewsArticle a, CancellationToken ct = default);
    Task<bool> UpdateAsync(NewsArticle a, CancellationToken ct = default);
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}
```

`GAC.Core/Services/IAdminOfferService.cs` — identical shape with `Offer` instead of `NewsArticle`.

- [ ] **Step 2: Failing tests** — `AdminNewsServiceTests` and `AdminOfferServiceTests`, each covering Create→Get round-trip, SlugExists (ignoring self), Update toggling the publish/active flag, Delete. Same structure as `AdminVehicleServiceTests`.

- [ ] **Step 3: Run → FAIL.**

- [ ] **Step 4: Implement** both services. `AdminNewsService` (Offers analogous, swapping `IsPublished`→`IsActive` and the DbSet):

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminNewsService : IAdminNewsService
{
    private readonly ApplicationDbContext _db;
    public AdminNewsService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<NewsArticle>> ListAsync(CancellationToken ct = default)
        => await _db.NewsArticles.OrderByDescending(a => a.PublishedOn).ThenBy(a => a.SortOrder).ToListAsync(ct);

    public async Task<NewsArticle?> GetAsync(int id, CancellationToken ct = default)
        => await _db.NewsArticles.FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<bool> SlugExistsAsync(string slug, int? exceptId = null, CancellationToken ct = default)
        => await _db.NewsArticles.AnyAsync(a => a.Slug == slug && (exceptId == null || a.Id != exceptId), ct);

    public async Task<int> CreateAsync(NewsArticle a, CancellationToken ct = default)
    {
        _db.NewsArticles.Add(a);
        await _db.SaveChangesAsync(ct);
        return a.Id;
    }

    public async Task<bool> UpdateAsync(NewsArticle a, CancellationToken ct = default)
    {
        var e = await _db.NewsArticles.FirstOrDefaultAsync(x => x.Id == a.Id, ct);
        if (e is null) return false;
        e.Slug = a.Slug; e.IsPublished = a.IsPublished; e.PublishedOn = a.PublishedOn;
        e.Title = a.Title; e.Excerpt = a.Excerpt; e.Body = a.Body; e.ImagePath = a.ImagePath; e.SortOrder = a.SortOrder;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var a = await _db.NewsArticles.FindAsync([id], ct);
        if (a is null) return false;
        _db.NewsArticles.Remove(a);
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

`AdminOfferService` — same, with `_db.Offers`, fields `Slug, IsActive, Title, Body, ImagePath, ValidUntil, SortOrder`, ordered by `SortOrder`.

- [ ] **Step 5: Register** both: `AddScoped<IAdminNewsService, AdminNewsService>()`, `AddScoped<IAdminOfferService, AdminOfferService>()`.

- [ ] **Step 6: Run → PASS.**

- [ ] **Step 7: Controllers** `NewsController` + `OffersController` — standard `Index/Create/Edit/Save/Delete`, `[Authorize(Policy = AdminPolicies.ContentEditor)]`; `Save` validates Slug non-empty + uniqueness via `SlugExistsAsync`.

- [ ] **Step 8: Views**
  - News `Index` (`@model IReadOnlyList<GAC.Core.Content.NewsArticle>`): table — PublishedOn, Title (`.Localize()`), Published? (yes/no), Edit, Delete; "New article" link.
  - News `Edit` (`@model GAC.Core.Content.NewsArticle`): hidden `Id`; text `asp-for="Slug"`; date `asp-for="PublishedOn"`; checkbox `asp-for="IsPublished"`; number `asp-for="SortOrder"`; image field for `ImagePath` (picker); `_LocalizedField` for `Title`, `Excerpt` (multiline), `Body` (multiline); Save. Include `_PickerModal`.
  - Offers `Index`/`Edit`: same, with `IsActive` checkbox, optional date `asp-for="ValidUntil"`, `_LocalizedField` for `Title` + `Body` (multiline).

- [ ] **Step 9: Access tests** — `News_AccessByRole` (`/Admin/News`) and `Offers_AccessByRole` (`/Admin/Offers`) theories (Admin/Editor OK, Sales Found). Full suite → green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminNewsService.cs Solution/GAC.Core/Services/IAdminOfferService.cs Solution/GAC.Infrastructure/Services/AdminNewsService.cs Solution/GAC.Infrastructure/Services/AdminOfferService.cs Solution/GAC.Web/Areas/Admin/Controllers/NewsController.cs Solution/GAC.Web/Areas/Admin/Controllers/OffersController.cs Solution/GAC.Web/Areas/Admin/Views/News Solution/GAC.Web/Areas/Admin/Views/Offers Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminNewsServiceTests.cs Solution/GAC.Tests/Admin/AdminOfferServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): news + offers CRUD"
```

---

## Task 8: Content pages + form pages edit (ContentEditor)

These are **edit-only** (no create/delete — slugs are fixed and seeded, each with a hardcoded partial). Phase 6a edits **Title + Meta** for content pages and **Title + Intro + Meta** for form pages. (Bodies become editable in Phase 6b.)

**Files:**
- Create: `GAC.Core/Services/IAdminPageService.cs`, `GAC.Infrastructure/Services/AdminPageService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/ContentPagesController.cs`, `FormPagesController.cs`
- Create: `GAC.Web/Areas/Admin/Views/ContentPages/Index.cshtml`, `Edit.cshtml`; `FormPages/Index.cshtml`, `Edit.cshtml`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/Admin/AdminPageServiceTests.cs`

- [ ] **Step 1: Interface**

`GAC.Core/Services/IAdminPageService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminPageService
{
    Task<IReadOnlyList<ContentPage>> ListContentAsync(CancellationToken ct = default);
    Task<ContentPage?> GetContentAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateContentAsync(ContentPage page, CancellationToken ct = default); // Title, Meta, IsVisible

    Task<IReadOnlyList<FormPage>> ListFormsAsync(CancellationToken ct = default);
    Task<FormPage?> GetFormAsync(int id, CancellationToken ct = default);
    Task<bool> UpdateFormAsync(FormPage page, CancellationToken ct = default);       // Title, Intro, Meta, IsVisible
}
```

- [ ] **Step 2: Failing tests** `GAC.Tests/Admin/AdminPageServiceTests.cs` — seed one `ContentPage` and one `FormPage`; assert `UpdateContentAsync` changes `Title.En`/`MetaTitle.En` and `UpdateFormAsync` changes `IntroText.En`. Pattern as before.

- [ ] **Step 3: Run → FAIL.**

- [ ] **Step 4: Implement** `GAC.Infrastructure/Services/AdminPageService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminPageService : IAdminPageService
{
    private readonly ApplicationDbContext _db;
    public AdminPageService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ContentPage>> ListContentAsync(CancellationToken ct = default)
        => await _db.ContentPages.OrderBy(p => p.Slug).ToListAsync(ct);

    public async Task<ContentPage?> GetContentAsync(int id, CancellationToken ct = default)
        => await _db.ContentPages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> UpdateContentAsync(ContentPage page, CancellationToken ct = default)
    {
        var e = await _db.ContentPages.FirstOrDefaultAsync(p => p.Id == page.Id, ct);
        if (e is null) return false;
        e.Title = page.Title; e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription;
        e.IsVisible = page.IsVisible;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<FormPage>> ListFormsAsync(CancellationToken ct = default)
        => await _db.FormPages.OrderBy(p => p.Slug).ToListAsync(ct);

    public async Task<FormPage?> GetFormAsync(int id, CancellationToken ct = default)
        => await _db.FormPages.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<bool> UpdateFormAsync(FormPage page, CancellationToken ct = default)
    {
        var e = await _db.FormPages.FirstOrDefaultAsync(p => p.Id == page.Id, ct);
        if (e is null) return false;
        e.Title = page.Title; e.IntroText = page.IntroText;
        e.MetaTitle = page.MetaTitle; e.MetaDescription = page.MetaDescription; e.IsVisible = page.IsVisible;
        await _db.SaveChangesAsync(ct);
        return true;
    }
}
```

- [ ] **Step 5: Register** `builder.Services.AddScoped<IAdminPageService, AdminPageService>();`

- [ ] **Step 6: Run → PASS.**

- [ ] **Step 7: Controllers** `ContentPagesController` (`Index`, `Edit`, `Save`) and `FormPagesController` (`Index`, `Edit`, `Save`), `[Authorize(Policy = AdminPolicies.ContentEditor)]`. No Create/Delete. `Save` calls the matching `Update*Async` then redirects to `Edit`.

- [ ] **Step 8: Views**
  - ContentPages `Index` (`@model IReadOnlyList<GAC.Core.Content.ContentPage>`): table — Slug, Title (`.Localize()`), Visible?, Edit.
  - ContentPages `Edit` (`@model GAC.Core.Content.ContentPage`): hidden `Id`; read-only Slug display; checkbox `asp-for="IsVisible"`; `_LocalizedField` for `Title`, `MetaTitle`, `MetaDescription` (multiline); Save. A note: "Page body is edited in Phase 6b."
  - FormPages `Index`/`Edit`: same, plus `_LocalizedField` for `IntroText` (multiline); show the read-only `FormType`.

- [ ] **Step 9: Access tests** — `ContentPages_AccessByRole` (`/Admin/ContentPages`) and `FormPages_AccessByRole` (`/Admin/FormPages`) theories (Admin/Editor OK, Sales Found). Full suite → green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminPageService.cs Solution/GAC.Infrastructure/Services/AdminPageService.cs Solution/GAC.Web/Areas/Admin/Controllers/ContentPagesController.cs Solution/GAC.Web/Areas/Admin/Controllers/FormPagesController.cs Solution/GAC.Web/Areas/Admin/Views/ContentPages Solution/GAC.Web/Areas/Admin/Views/FormPages Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminPageServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): content pages + form pages metadata editing"
```

---

## Task 9: Site settings (AdminOnly)

**Files:**
- Create: `GAC.Core/Services/IAdminSettingsService.cs`, `GAC.Infrastructure/Services/AdminSettingsService.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/SettingsController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Settings/Edit.cshtml`
- Modify: `GAC.Web/Program.cs`
- Test: `GAC.Tests/Admin/AdminSettingsServiceTests.cs`

- [ ] **Step 1: Interface**

`GAC.Core/Services/IAdminSettingsService.cs`:

```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface IAdminSettingsService
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);          // creates the singleton if missing
    Task UpdateAsync(SiteSettings settings, CancellationToken ct = default);
}
```

- [ ] **Step 2: Failing test** — `GetAsync` returns/creates a singleton; `UpdateAsync` persists `Phone` + `FooterTagline.En`. Assert only one `SiteSettings` row exists after update.

- [ ] **Step 3: Run → FAIL.**

- [ ] **Step 4: Implement** `GAC.Infrastructure/Services/AdminSettingsService.cs`:

```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GAC.Infrastructure.Services;

public class AdminSettingsService : IAdminSettingsService
{
    private readonly ApplicationDbContext _db;
    public AdminSettingsService(ApplicationDbContext db) => _db = db;

    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        var s = await _db.SiteSettings.FirstOrDefaultAsync(ct);
        if (s is null)
        {
            s = new SiteSettings();
            _db.SiteSettings.Add(s);
            await _db.SaveChangesAsync(ct);
        }
        return s;
    }

    public async Task UpdateAsync(SiteSettings settings, CancellationToken ct = default)
    {
        var e = await GetAsync(ct);
        e.Phone = settings.Phone; e.WhatsApp = settings.WhatsApp; e.Email = settings.Email;
        e.InstagramUrl = settings.InstagramUrl; e.FacebookUrl = settings.FacebookUrl;
        e.TiktokUrl = settings.TiktokUrl; e.SnapchatUrl = settings.SnapchatUrl; e.XUrl = settings.XUrl;
        e.FooterTagline = settings.FooterTagline;
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 5: Register** `builder.Services.AddScoped<IAdminSettingsService, AdminSettingsService>();`

- [ ] **Step 6: Run → PASS.**

- [ ] **Step 7: Controller** `SettingsController` — `[Authorize(Policy = AdminPolicies.AdminOnly)]`. `Edit` (GET) → `View(await _svc.GetAsync())`; `Save` (POST) → `UpdateAsync`, flash, redirect to `Edit`. Default route action is `Edit` (set `[Route]` or rely on link `/Admin/Settings/Edit`; the nav links to `/Admin/Settings` so add an `Index` that redirects to `Edit`, or name the action `Index`). Use action name **`Index`** (GET) + **`Save`** (POST) so `/Admin/Settings` works directly.

- [ ] **Step 8: View** `Settings/Index.cshtml` (`@model GAC.Core.Content.SiteSettings`): a `<form method="post" asp-action="Save">` with text inputs `asp-for` for `Phone`, `WhatsApp`, `Email`, `InstagramUrl`, `FacebookUrl`, `TiktokUrl`, `SnapchatUrl`, `XUrl`, and `_LocalizedField` for `FooterTagline`; Save button.

- [ ] **Step 9: Access test** — `Settings_AccessByRole` theory targeting `/Admin/Settings`: **Admin OK, Editor Found, Sales Found** (AdminOnly). Full suite → green.

- [ ] **Step 10: Commit**

```bash
git add Solution/GAC.Core/Services/IAdminSettingsService.cs Solution/GAC.Infrastructure/Services/AdminSettingsService.cs Solution/GAC.Web/Areas/Admin/Controllers/SettingsController.cs Solution/GAC.Web/Areas/Admin/Views/Settings Solution/GAC.Web/Program.cs Solution/GAC.Tests/Admin/AdminSettingsServiceTests.cs Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): site settings editing (Admin only)"
```

---

## Task 10: User management (AdminOnly)

User logic is thin orchestration over ASP.NET Core Identity managers, so the controller uses `UserManager<ApplicationUser>` + `RoleManager<IdentityRole>` directly (Identity managers are impractical to unit-test without a real store). Coverage is via integration access tests.

**Files:**
- Create: `GAC.Web/Areas/Admin/Models/UserViewModels.cs`
- Create: `GAC.Web/Areas/Admin/Controllers/UsersController.cs`
- Create: `GAC.Web/Areas/Admin/Views/Users/Index.cshtml`, `Create.cshtml`, `Edit.cshtml`
- Test: extend `GAC.Tests/Admin/AdminAuthTests.cs`

- [ ] **Step 1: View models**

`GAC.Web/Areas/Admin/Models/UserViewModels.cs`:

```csharp
using System.ComponentModel.DataAnnotations;

namespace GAC.Web.Areas.Admin.Models;

public class UserRow
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string Role { get; set; } = "";
    public bool Disabled { get; set; }
}

public class CreateUserViewModel
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    [Required, MinLength(8)] public string Password { get; set; } = "";
    [Required] public string Role { get; set; } = "Editor";
}

public class EditUserViewModel
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    [Required] public string Role { get; set; } = "Editor";
    public bool Disabled { get; set; }
    [MinLength(8)] public string? NewPassword { get; set; }  // optional reset
}
```

- [ ] **Step 2: Controller**

`GAC.Web/Areas/Admin/Controllers/UsersController.cs`:

```csharp
using GAC.Core.Identity;
using GAC.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[AutoValidateAntiforgeryToken]
public class UsersController : Controller
{
    private readonly UserManager<ApplicationUser> _users;
    public UsersController(UserManager<ApplicationUser> users) => _users = users;

    public async Task<IActionResult> Index()
    {
        var list = await _users.Users.ToListAsync();
        var rows = new List<UserRow>();
        foreach (var u in list)
        {
            var roles = await _users.GetRolesAsync(u);
            rows.Add(new UserRow
            {
                Id = u.Id, Email = u.Email ?? "", DisplayName = u.DisplayName,
                Role = roles.FirstOrDefault() ?? "",
                Disabled = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            });
        }
        return View(rows);
    }

    public IActionResult Create()
    {
        ViewBag.Roles = RoleSelect("Editor");
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserViewModel m)
    {
        if (!ModelState.IsValid) { ViewBag.Roles = RoleSelect(m.Role); return View(m); }
        var user = new ApplicationUser { UserName = m.Email, Email = m.Email, EmailConfirmed = true, DisplayName = m.DisplayName };
        var res = await _users.CreateAsync(user, m.Password);
        if (!res.Succeeded)
        {
            foreach (var e in res.Errors) ModelState.AddModelError("", e.Description);
            ViewBag.Roles = RoleSelect(m.Role); return View(m);
        }
        await _users.AddToRoleAsync(user, m.Role);
        TempData["Flash"] = "User created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var u = await _users.FindByIdAsync(id);
        if (u is null) return NotFound();
        var roles = await _users.GetRolesAsync(u);
        ViewBag.Roles = RoleSelect(roles.FirstOrDefault() ?? "Editor");
        return View(new EditUserViewModel
        {
            Id = u.Id, Email = u.Email ?? "", DisplayName = u.DisplayName,
            Role = roles.FirstOrDefault() ?? "Editor",
            Disabled = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditUserViewModel m)
    {
        var u = await _users.FindByIdAsync(m.Id);
        if (u is null) return NotFound();
        if (!ModelState.IsValid) { ViewBag.Roles = RoleSelect(m.Role); return View(m); }

        u.DisplayName = m.DisplayName;
        await _users.UpdateAsync(u);

        var current = await _users.GetRolesAsync(u);
        await _users.RemoveFromRolesAsync(u, current);
        await _users.AddToRoleAsync(u, m.Role);

        await _users.SetLockoutEnabledAsync(u, true);
        await _users.SetLockoutEndDateAsync(u, m.Disabled ? DateTimeOffset.MaxValue : null);

        if (!string.IsNullOrWhiteSpace(m.NewPassword))
        {
            var token = await _users.GeneratePasswordResetTokenAsync(u);
            var reset = await _users.ResetPasswordAsync(u, token, m.NewPassword);
            if (!reset.Succeeded)
            {
                foreach (var e in reset.Errors) ModelState.AddModelError("", e.Description);
                ViewBag.Roles = RoleSelect(m.Role); return View(m);
            }
        }
        TempData["Flash"] = "User updated.";
        return RedirectToAction(nameof(Index));
    }

    private static List<SelectListItem> RoleSelect(string selected)
        => Roles.All.Select(r => new SelectListItem(r, r, r == selected)).ToList();
}
```

- [ ] **Step 3: Views**
  - `Users/Index.cshtml` (`@model IReadOnlyList<GAC.Web.Areas.Admin.Models.UserRow>`): table — Email, DisplayName, Role, Disabled?, Edit link; "New user" link.
  - `Users/Create.cshtml` (`@model CreateUserViewModel`): form `asp-action="Create"` with `asp-for` Email, DisplayName, Password (type=password), and `<select asp-for="Role" asp-items="ViewBag.Roles">`; validation summary; Create button.
  - `Users/Edit.cshtml` (`@model EditUserViewModel`): hidden `Id`; read-only Email; `asp-for` DisplayName; role select; checkbox `asp-for="Disabled"`; optional `asp-for="NewPassword"` (type=password, "leave blank to keep"); Save.

- [ ] **Step 4: Access test**

Append to `GAC.Tests/Admin/AdminAuthTests.cs`:

```csharp
[Theory]
[InlineData(Roles.Admin, HttpStatusCode.OK)]
[InlineData(Roles.Editor, HttpStatusCode.Found)]
[InlineData(Roles.Sales, HttpStatusCode.Found)]
public async Task Users_AccessByRole(string role, HttpStatusCode expected)
{
    var res = await _factory.ClientForRole(role).GetAsync("/Admin/Users");
    Assert.Equal(expected, res.StatusCode);
}
```

- [ ] **Step 5: Run the full suite**

Run: `dotnet test Solution/GAC.sln`
Expected: all green.

- [ ] **Step 6: Commit**

```bash
git add Solution/GAC.Web/Areas/Admin/Models/UserViewModels.cs Solution/GAC.Web/Areas/Admin/Controllers/UsersController.cs Solution/GAC.Web/Areas/Admin/Views/Users Solution/GAC.Tests/Admin/AdminAuthTests.cs
git commit -m "feat(admin): user management — create/edit, roles, disable, password reset (Admin only)"
```

---

## Task 11: Final pass — handoff + full verification

**Files:**
- Modify: `Solution/docs/HANDOFF.md`

- [ ] **Step 1: Update HANDOFF.md**

In `Solution/docs/HANDOFF.md`: set Phase 6a ✅ in §4 (note "Phase 6b — bodies — NEXT"); add a "## 5d. Phase 6a — Admin area (foundation + CRUD)" section summarizing what was built (area, policies, login, dashboard, leads, vehicles+images, menu, hero, news, offers, page metadata, settings, media+picker, users); add the admin routes to the §6 routing map (`/admin/login`, `/admin/logout`, `/admin/denied`, `/Admin`, `/Admin/{controller}/...`); record the new total test count; and note the storage root config key `Media:Root` (defaults to `wwwroot/uploads`, served by `UseStaticFiles`).

- [ ] **Step 2: Run the complete suite once more**

Run: `dotnet test Solution/GAC.sln`
Expected: all green.

- [ ] **Step 3: Secret scan before any push**

Run: `git grep -nE "Codex@123456|P@ssw0rd|sk_live|sk_test" -- ':!*.Development.json'`
Expected: no output. Confirm `appsettings.Development.json` is NOT staged: `git status --short` shows no `appsettings.Development.json`.

- [ ] **Step 4: Commit handoff**

```bash
git add Solution/docs/HANDOFF.md
git commit -m "docs: HANDOFF — Phase 6a admin foundation + CRUD complete"
```

- [ ] **Step 5:** Use **superpowers:finishing-a-development-branch** to complete (push to `main` per the established per-phase rhythm).

---

## Self-review (author check against the spec)

**Spec coverage:** Foundation/auth/policies/login (Task 1) ✓; Leads inbox — list/filter/detail/status/delete (Task 2) ✓; shared bilingual editor + media library + picker (Task 3) ✓; Vehicles meta+images, visibility/order/category → drives `/models`+mega-menu (Task 4) ✓; Menu CRUD → header nav (Task 5) ✓; Hero slides (Task 6) ✓; News+Offers (Task 7) ✓; Content+Form page metadata (Task 8) ✓; Site settings, Admin-only (Task 9) ✓; User management, Admin-only (Task 10) ✓; roles enforced everywhere with Admin as superset (every `*_AccessByRole` test) ✓; handoff (Task 11) ✓. **Deferred to 6b (per spec §6):** all `BodyHtml` editing and the generic-template migration — not in this plan. **Structured vehicle children (`SpecGroups` etc.):** intentionally unmanaged (spec §2) — no task, correct.

**Type consistency:** Service method names are stable across tasks (`ListAsync`/`GetAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync`/`MoveAsync`/`SlugExistsAsync`). `_LocalizedField` field-name convention (`Foo.En`/`Foo.Ar`) matches the `LocalizedText { En, Ar }` owned type for binder round-tripping. Policy constants (`AdminPolicies.ContentEditor/LeadsAccess/AdminOnly`) are referenced identically in `Program.cs` and every controller. `TestAuthHandler.RoleHeader` + `ClientForRole` are used uniformly in access tests.

**Placeholder scan:** No TBD/"handle later" steps; every code step shows complete code, and the repetitive CRUD views (Tasks 5–10) are specified with exact field lists + the shared partials they reuse (no "same as Task N" hand-waving on field content).

