# Phase 5 — Forms & Leads Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the 5 static lead-capture forms functional — POST with anti-forgery, bilingual server-side validation, persist submissions to the `Lead` entity, and send an SMTP notification email — without altering the ported visual markup/classes that `main.js` and `styles.css` depend on.

**Architecture:** Each form does a real `<form method="post">` to `FormsController.Submit("/forms/{slug}")`. A single `LeadFormInput` view model (DataAnnotations, bilingual via the Phase-4 `SharedResource` resx) covers all five forms; per-form-type fields (Model, Branch) are validated conditionally in the controller. Valid submissions map to a `Lead`, persist via `ILeadService`, fire a best-effort `IEmailSender` (MailKit) notification, then **Post-Redirect-Get** back to the same page where a `TempData`-driven success banner renders. Validation failures re-render the form with errors + preserved input. `main.js` keeps its client-side validation but its success branch now does a native `form.submit()` instead of faking success — so it also works with JS disabled.

**Tech Stack:** ASP.NET Core 9 MVC, EF Core 9.0.6 (the `Leads` table already exists from Phase 2 — **no new migration**), MailKit (SMTP), `IStringLocalizer`/`IHtmlLocalizer<SharedResource>` + `.resx`, xUnit (unit + WebApplicationFactory integration).

---

## Background facts (already verified — do not re-investigate)

- **Forms with real input fields (5):** `book-a-service`, `book-a-test-drive`, `request-a-quote`, `fleet`, `recall-enquiry`. Each is a partial under `GAC.Web/Views/Forms/Forms/_{slug}.cshtml` with a `<form data-form novalidate>`.
- **`contact-us` has NO form** — it is a "Locate Us" directory. It only needs its `@model` line changed (Task 4) because the container view's model type changes.
- **Field names currently in the markup** (kept as-is — model binding is case-insensitive and the empty-prefix fallback binds them to PascalCase properties): `title`, `firstName`, `lastName`, `email`, `phone`, `model`, `branch`, `mileage`, `dueDate`, `message`, `marketing`.
- **Per-form field matrix** (which fields each form has / which are `required` in the markup):
  | slug | FormType | title | first/last | email | phone | model | branch | mileage | dueDate | message |
  |---|---|---|---|---|---|---|---|---|---|---|
  | book-a-service | ServiceBooking | req | req | req | req | **req** | **req** | opt | opt | opt |
  | book-a-test-drive | TestDrive | req | req | req | req | **req** | – | – | – | – |
  | request-a-quote | Quote | req | req | req | req | **req** | – | – | – | – |
  | fleet | Fleet | req | req | req | req | – | **req** | – | – | opt |
  | recall-enquiry | RecallEnquiry | req | req | req | req | – | – | – | – | opt |
- **`Lead` entity** (`GAC.Core/Content/Lead.cs`, table exists): `Id, FormType, Status(=New), Name, Phone?, Email?, Message?, VehicleId?, Vehicle?, PreferredDate(DateOnly?), SourcePage?, Branch?, CreatedAt(DateTimeOffset)`. `DbSet<Lead> Leads` exists in `ApplicationDbContext`.
- **`main.js` form block** is at lines ~121–139; the success branch (lines ~132–134) replaces `form.innerHTML` with a fake success message.
- **GET routing:** `PageController.Show("/{slug}")` resolves ContentPage → FormPage → Vehicle → 404, and for a FormPage returns `View("~/Views/Forms/Page.cshtml", form)`. `Page.cshtml` renders `_{slug}` partial.
- **Secrets (repo is PUBLIC):** committed `appsettings.json` holds placeholders only; real secrets live ONLY in the gitignored `appsettings.Development.json`. **Never `git add appsettings.Development.json`; always scope `git add`, never `git add -A`/`.`.**
- **Localization pattern (Phase 4):** `IHtmlLocalizer<SharedResource> @L` injected in `_ViewImports`; keys ARE the English source text; only `Resources/SharedResource.ar.resx` exists (missing key → English). `SharedResource` lives in root namespace `GAC.Web`.
- **Gotcha:** Razor HTML-encodes Arabic to numeric char refs — integration tests must `WebUtility.HtmlDecode` before asserting Arabic substrings.

---

## File structure

**Create:**
- `GAC.Core/Services/ILeadService.cs` — lead persistence abstraction.
- `GAC.Core/Services/IEmailSender.cs` — email abstraction (no MailKit dependency in Core).
- `GAC.Infrastructure/Services/LeadService.cs` — EF impl.
- `GAC.Infrastructure/Services/SmtpOptions.cs` — bound config POCO.
- `GAC.Infrastructure/Services/SmtpEmailSender.cs` — MailKit impl (best-effort, never throws to caller).
- `GAC.Web/Models/LeadFormInput.cs` — bound form view model + DataAnnotations.
- `GAC.Web/Models/FormPageViewModel.cs` — composite `{ FormPage Page; LeadFormInput Input; }`.
- `GAC.Web/Infrastructure/ViewExtensions.cs` — `FieldErrorClass(this IHtmlHelper, key)` helper.
- `GAC.Web/Controllers/FormsController.cs` — `[HttpPost("/forms/{slug}")] Submit`.
- `GAC.Tests/LeadServiceTests.cs`, `GAC.Tests/LeadFormInputValidationTests.cs`, `GAC.Tests/FormsControllerTests.cs`, `GAC.Tests/FormSubmissionTests.cs` (integration GET wiring).

**Modify:**
- `GAC.Infrastructure/GAC.Infrastructure.csproj` — add MailKit.
- `GAC.Web/appsettings.json` — flesh out `Smtp` section (placeholders for secrets only).
- `GAC.Web/appsettings.Development.json` — add real SMTP creds (LOCAL ONLY, gitignored).
- `GAC.Web/Program.cs` — DI: options, `ILeadService`, `IEmailSender`, `.AddDataAnnotationsLocalization`.
- `GAC.Web/Controllers/PageController.cs` — pass `FormPageViewModel` for form pages.
- `GAC.Web/Views/Forms/Page.cshtml` — model swap + TempData success banner.
- `GAC.Web/Views/Forms/Forms/_*.cshtml` (all 6) — model swap; the 5 real forms also get POST wiring + value/error binding.
- `GAC.Web/wwwroot/assets/js/main.js` — success branch → `form.submit()`.
- `GAC.Web/Resources/SharedResource.ar.resx` — Arabic for validation + success strings.

---

## Task 1: SMTP config, email sender, lead service (Infrastructure + Core)

**Files:**
- Create: `GAC.Core/Services/ILeadService.cs`, `GAC.Core/Services/IEmailSender.cs`
- Create: `GAC.Infrastructure/Services/SmtpOptions.cs`, `LeadService.cs`, `SmtpEmailSender.cs`
- Modify: `GAC.Infrastructure/GAC.Infrastructure.csproj`, `GAC.Web/appsettings.json`, `GAC.Web/appsettings.Development.json`, `GAC.Web/Program.cs`
- Test: `GAC.Tests/LeadServiceTests.cs`

- [ ] **Step 1: Write the failing test** — `GAC.Tests/LeadServiceTests.cs`

```csharp
using GAC.Core.Content;
using GAC.Infrastructure.Data;
using GAC.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAC.Tests;

public class LeadServiceTests
{
    private static ApplicationDbContext NewDb(string name)
    {
        var sp = new ServiceCollection()
            .AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name))
            .BuildServiceProvider();
        return sp.GetRequiredService<ApplicationDbContext>();
    }

    [Fact]
    public async Task CreateAsync_PersistsLead()
    {
        var db = NewDb("lead-create");
        var svc = new LeadService(db);

        await svc.CreateAsync(new Lead
        {
            FormType = FormType.TestDrive,
            Name = "Mr Ada Lovelace",
            Email = "ada@example.com",
            Phone = "12345678"
        });

        var lead = await db.Leads.SingleAsync();
        Assert.Equal("Mr Ada Lovelace", lead.Name);
        Assert.Equal(FormType.TestDrive, lead.FormType);
        Assert.Equal(LeadStatus.New, lead.Status);
    }
}
```

- [ ] **Step 2: Run it — expect FAIL** (LeadService does not exist)

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~LeadServiceTests`
Expected: build error / FAIL — `LeadService` not found.

- [ ] **Step 3: Add the MailKit package** — `GAC.Infrastructure/GAC.Infrastructure.csproj`

Add inside the existing `<ItemGroup>` that holds PackageReferences:
```xml
    <PackageReference Include="MailKit" Version="4.8.0" />
```
Run: `dotnet restore Solution/GAC.sln`
(MailKit is not a `Microsoft.*` package, so it will not float to net10 — pin it anyway as above.)

- [ ] **Step 4: Create the Core abstractions**

`GAC.Core/Services/ILeadService.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

public interface ILeadService
{
    Task CreateAsync(Lead lead, CancellationToken ct = default);
}
```

`GAC.Core/Services/IEmailSender.cs`:
```csharp
using GAC.Core.Content;

namespace GAC.Core.Services;

/// <summary>Best-effort notification of a new lead. Implementations must never throw to the caller.</summary>
public interface IEmailSender
{
    Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default);
}
```

- [ ] **Step 5: Create `SmtpOptions`** — `GAC.Infrastructure/Services/SmtpOptions.cs`
```csharp
namespace GAC.Infrastructure.Services;

/// <summary>Bound from the "Smtp" config section. Secrets live ONLY in appsettings.Development.json (gitignored).</summary>
public class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true; // STARTTLS on 587
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "GAC Motors";
    public string AdminNotifyEmail { get; set; } = "";
}
```

- [ ] **Step 6: Create `LeadService`** — `GAC.Infrastructure/Services/LeadService.cs`
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Infrastructure.Data;

namespace GAC.Infrastructure.Services;

public class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    public LeadService(ApplicationDbContext db) => _db = db;

    public async Task CreateAsync(Lead lead, CancellationToken ct = default)
    {
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
    }
}
```

- [ ] **Step 7: Create `SmtpEmailSender`** — `GAC.Infrastructure/Services/SmtpEmailSender.cs`
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GAC.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<SmtpOptions> opt, ILogger<SmtpEmailSender> log)
    { _opt = opt.Value; _log = log; }

    public async Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default)
    {
        var to = string.IsNullOrWhiteSpace(_opt.AdminNotifyEmail) ? _opt.FromEmail : _opt.AdminNotifyEmail;
        if (!_opt.Enabled || string.IsNullOrWhiteSpace(_opt.Host) || string.IsNullOrWhiteSpace(to))
        {
            _log.LogInformation("SMTP disabled or unconfigured — skipping lead notification for {FormType}.", lead.FormType);
            return;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
            msg.To.Add(MailboxAddress.Parse(to));
            if (!string.IsNullOrWhiteSpace(lead.Email))
                msg.ReplyTo.Add(MailboxAddress.Parse(lead.Email));
            msg.Subject = $"New {formTitle} enquiry — {lead.Name}";

            var body = new System.Text.StringBuilder();
            body.AppendLine($"Form: {formTitle} ({lead.FormType})");
            body.AppendLine($"Name: {lead.Name}");
            body.AppendLine($"Phone: {lead.Phone}");
            body.AppendLine($"Email: {lead.Email}");
            if (!string.IsNullOrWhiteSpace(lead.Branch)) body.AppendLine($"Branch: {lead.Branch}");
            if (lead.PreferredDate is not null) body.AppendLine($"Preferred date: {lead.PreferredDate}");
            if (!string.IsNullOrWhiteSpace(lead.SourcePage)) body.AppendLine($"Source page: {lead.SourcePage}");
            if (!string.IsNullOrWhiteSpace(lead.Message)) { body.AppendLine(); body.AppendLine(lead.Message); }
            msg.Body = new TextPart("plain") { Text = body.ToString() };

            using var client = new SmtpClient();
            var secure = _opt.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
            await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);
            if (!string.IsNullOrWhiteSpace(_opt.Username))
                await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, ct);
            _log.LogInformation("Lead notification sent for {FormType} to {To}.", lead.FormType, to);
        }
        catch (Exception ex)
        {
            // Never break the submission because email failed.
            _log.LogError(ex, "Failed to send lead notification for {FormType}.", lead.FormType);
        }
    }
}
```

- [ ] **Step 8: Flesh out the committed `Smtp` section** — `GAC.Web/appsettings.json`

Replace the existing `"Smtp": { ... }` block with (note: only `Username`/`Password` are placeholders; addresses are not secret; `Enabled:false` so the public/placeholder config never attempts a send):
```json
  "Smtp": {
    "Enabled": false,
    "Host": "smtp.mailgun.org",
    "Port": 587,
    "UseSsl": true,
    "Username": "__SET_LOCALLY__",
    "Password": "__SET_LOCALLY__",
    "FromEmail": "postmaster@mg.codexkw.co",
    "FromName": "GAC Motors",
    "AdminNotifyEmail": "postmaster@mg.codexkw.co"
  }
```

- [ ] **Step 9: Add the REAL SMTP creds LOCALLY** — `GAC.Web/appsettings.Development.json` (GITIGNORED — never `git add` this file)

Add a top-level `"Smtp"` key alongside the existing `ConnectionStrings` (config merges, so only the overridden keys are needed):
```json
  "Smtp": {
    "Enabled": true,
    "Username": "postmaster@mg.codexkw.co",
    "Password": "<REAL_SMTP_PASSWORD — provided privately, set locally only>"
  }
```
The real Mailgun SMTP password was supplied by the user out-of-band; put the literal value here in your LOCAL gitignored file only. NEVER write it into any committed file or doc.
Verify it is ignored: `git check-ignore Solution/GAC.Web/appsettings.Development.json` must print the path.

- [ ] **Step 10: Register DI** — `GAC.Web/Program.cs`

After the existing `builder.Services.AddScoped<IContentService, ContentService>();` line, add:
```csharp
builder.Services.Configure<GAC.Infrastructure.Services.SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<ILeadService, LeadService>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
```
(`GAC.Core.Services` and `GAC.Infrastructure.Services` usings already exist at the top of Program.cs; add `using GAC.Infrastructure.Services;` only if not present.)

- [ ] **Step 11: Run the test — expect PASS**

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~LeadServiceTests`
Expected: PASS (1 test).

- [ ] **Step 12: Build the whole solution**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: build succeeds, no warnings introduced.

- [ ] **Step 13: Commit** (scoped — do NOT add appsettings.Development.json)
```bash
git add Solution/GAC.Core/Services/ILeadService.cs Solution/GAC.Core/Services/IEmailSender.cs \
        Solution/GAC.Infrastructure/Services/SmtpOptions.cs Solution/GAC.Infrastructure/Services/LeadService.cs \
        Solution/GAC.Infrastructure/Services/SmtpEmailSender.cs Solution/GAC.Infrastructure/GAC.Infrastructure.csproj \
        Solution/GAC.Web/appsettings.json Solution/GAC.Web/Program.cs Solution/GAC.Tests/LeadServiceTests.cs
git commit -m "feat(phase5): SMTP email sender + lead service + DI"
```

---

## Task 2: `LeadFormInput` view model, composite VM, and bilingual DataAnnotations

**Files:**
- Create: `GAC.Web/Models/LeadFormInput.cs`, `GAC.Web/Models/FormPageViewModel.cs`
- Modify: `GAC.Web/Program.cs`, `GAC.Web/Resources/SharedResource.ar.resx`
- Test: `GAC.Tests/LeadFormInputValidationTests.cs`

- [ ] **Step 1: Write the failing test** — `GAC.Tests/LeadFormInputValidationTests.cs`
```csharp
using System.ComponentModel.DataAnnotations;
using GAC.Web.Models;
using Xunit;

namespace GAC.Tests;

public class LeadFormInputValidationTests
{
    private static IList<ValidationResult> Validate(LeadFormInput input)
    {
        var ctx = new ValidationContext(input);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(input, ctx, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Empty_Input_FailsRequiredCoreFields()
    {
        var results = Validate(new LeadFormInput());
        var members = results.SelectMany(r => r.MemberNames).ToHashSet();
        Assert.Contains(nameof(LeadFormInput.Title), members);
        Assert.Contains(nameof(LeadFormInput.FirstName), members);
        Assert.Contains(nameof(LeadFormInput.LastName), members);
        Assert.Contains(nameof(LeadFormInput.Email), members);
        Assert.Contains(nameof(LeadFormInput.Phone), members);
    }

    [Fact]
    public void Invalid_Email_Fails()
    {
        var input = new LeadFormInput
        {
            Title = "Mr", FirstName = "A", LastName = "B", Phone = "123", Email = "not-an-email"
        };
        var results = Validate(input);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(LeadFormInput.Email)));
    }

    [Fact]
    public void Valid_Core_Passes()
    {
        var input = new LeadFormInput
        {
            Title = "Mr", FirstName = "Ada", LastName = "Lovelace",
            Email = "ada@example.com", Phone = "12345678"
        };
        Assert.Empty(Validate(input));
    }
}
```

- [ ] **Step 2: Run it — expect FAIL** (LeadFormInput does not exist)

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~LeadFormInputValidationTests`
Expected: build error — `LeadFormInput` not found.

- [ ] **Step 3: Create `LeadFormInput`** — `GAC.Web/Models/LeadFormInput.cs`

The five universally-required fields use DataAnnotations (ErrorMessage strings ARE the resx keys, matching the Phase-4 "key = English text" pattern). `Model` and `Branch` are validated conditionally in the controller (Task 3) because they are only required for certain form types.
```csharp
using System.ComponentModel.DataAnnotations;

namespace GAC.Web.Models;

/// <summary>
/// Posted by all five lead-capture forms. Field names match the existing markup (case-insensitive
/// binding). Model/Branch are conditionally required per FormType in FormsController.
/// </summary>
public class LeadFormInput
{
    [Required(ErrorMessage = "Please select a title.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please enter your first name.")]
    public string? FirstName { get; set; }

    [Required(ErrorMessage = "Please enter your last name.")]
    public string? LastName { get; set; }

    [Required(ErrorMessage = "Please enter a valid email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your contact number.")]
    public string? Phone { get; set; }

    public string? Model { get; set; }
    public string? Branch { get; set; }
    public string? Mileage { get; set; }
    public string? DueDate { get; set; }
    public string? Message { get; set; }
    public bool Marketing { get; set; }
}
```

- [ ] **Step 4: Create `FormPageViewModel`** — `GAC.Web/Models/FormPageViewModel.cs`
```csharp
using GAC.Core.Content;

namespace GAC.Web.Models;

public class FormPageViewModel
{
    public FormPage Page { get; set; } = new();
    public LeadFormInput Input { get; set; } = new();
}
```

- [ ] **Step 5: Run the test — expect PASS**

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~LeadFormInputValidationTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Enable bilingual DataAnnotations** — `GAC.Web/Program.cs`

Change the MVC registration block from:
```csharp
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization();
```
to:
```csharp
builder.Services
    .AddControllersWithViews()
    .AddViewLocalization()
    .AddDataAnnotationsLocalization(options =>
        options.DataAnnotationLocalizerProvider = (type, factory) =>
            factory.Create(typeof(SharedResource)));
```
(`SharedResource` is in the `GAC.Web` namespace = the Program.cs root namespace, so no extra using is needed.)

- [ ] **Step 7: Add Arabic for the validation + success strings** — `GAC.Web/Resources/SharedResource.ar.resx`

Insert these `<data>` entries before the closing `</root>` (do not duplicate any existing key):
```xml
  <data name="Please select a title." xml:space="preserve"><value>الرجاء اختيار اللقب.</value></data>
  <data name="Please enter your first name." xml:space="preserve"><value>الرجاء إدخال الاسم الأول.</value></data>
  <data name="Please enter your last name." xml:space="preserve"><value>الرجاء إدخال اسم العائلة.</value></data>
  <data name="Please enter a valid email." xml:space="preserve"><value>الرجاء إدخال بريد إلكتروني صحيح.</value></data>
  <data name="Please enter your contact number." xml:space="preserve"><value>الرجاء إدخال رقم التواصل.</value></data>
  <data name="Please select your model." xml:space="preserve"><value>الرجاء اختيار الموديل.</value></data>
  <data name="Please select a branch." xml:space="preserve"><value>الرجاء اختيار الفرع.</value></data>
  <data name="Thanks — we received your request." xml:space="preserve"><value>شكراً — تم استلام طلبك.</value></data>
  <data name="A representative will contact you within one business day." xml:space="preserve"><value>سيتواصل معك أحد ممثلينا خلال يوم عمل واحد.</value></data>
  <data name="Submit" xml:space="preserve"><value>إرسال</value></data>
```

- [ ] **Step 8: Add a localizer-resolution test** — append to `GAC.Tests/SharedLocalizerTests.cs`

Open `GAC.Tests/SharedLocalizerTests.cs`, and inside the existing test class add this fact (reuse the file's existing localizer-building helper / pattern — match how `Resolves_ArabicValue_WhenCultureIsArabic` constructs its localizer):
```csharp
    [Fact]
    public void Resolves_ValidationStrings_InArabic()
    {
        // Arrange the SharedResource Arabic localizer exactly as the existing arabic test does,
        // then assert these Phase-5 keys resolve to Arabic (not the English key).
        // Keys to check: "Please enter your first name.", "Please select a branch.",
        // "Thanks — we received your request."
        // (Use the same localizer-construction helper this file already uses.)
    }
```
Then replace that placeholder body with real assertions following the existing test's construction (build `IStringLocalizer<SharedResource>` under `CultureInfo("ar")`, assert each value differs from the key and is non-empty). Keep it consistent with the file's existing style.

- [ ] **Step 9: Run tests — expect PASS**

Run: `dotnet test Solution/GAC.sln --filter "FullyQualifiedName~LeadFormInputValidationTests|FullyQualifiedName~SharedLocalizerTests"`
Expected: PASS.

- [ ] **Step 10: Commit**
```bash
git add Solution/GAC.Web/Models/LeadFormInput.cs Solution/GAC.Web/Models/FormPageViewModel.cs \
        Solution/GAC.Web/Program.cs Solution/GAC.Web/Resources/SharedResource.ar.resx \
        Solution/GAC.Tests/LeadFormInputValidationTests.cs Solution/GAC.Tests/SharedLocalizerTests.cs
git commit -m "feat(phase5): LeadFormInput VM + bilingual DataAnnotations + resx strings"
```

---

## Task 3: `FormsController.Submit`, lead mapping, PRG, and the container view

**Files:**
- Create: `GAC.Web/Controllers/FormsController.cs`, `GAC.Web/Infrastructure/ViewExtensions.cs`
- Modify: `GAC.Web/Controllers/PageController.cs`, `GAC.Web/Views/Forms/Page.cshtml`
- Test: `GAC.Tests/FormsControllerTests.cs`

- [ ] **Step 1: Write the failing test** — `GAC.Tests/FormsControllerTests.cs`

Uses hand-rolled fakes (no Moq dependency).
```csharp
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Controllers;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GAC.Tests;

public class FormsControllerTests
{
    // --- fakes ---
    private sealed class FakeContent : IContentService
    {
        public FormPage? Form;
        public Task<FormPage?> GetFormPageBySlugAsync(string slug) => Task.FromResult(Form);
        public Task<HomePage?> GetHomePageAsync() => Task.FromResult<HomePage?>(null);
        public Task<ContentPage?> GetContentPageBySlugAsync(string slug) => Task.FromResult<ContentPage?>(null);
        public Task<IReadOnlyList<NewsArticle>> GetPublishedNewsAsync() => Task.FromResult<IReadOnlyList<NewsArticle>>(new List<NewsArticle>());
        public Task<NewsArticle?> GetNewsBySlugAsync(string slug) => Task.FromResult<NewsArticle?>(null);
        public Task<IReadOnlyList<Offer>> GetActiveOffersAsync() => Task.FromResult<IReadOnlyList<Offer>>(new List<Offer>());
    }
    private sealed class FakeVehicles : IVehicleService
    {
        public Task<IReadOnlyList<Vehicle>> GetVisibleAsync() => Task.FromResult<IReadOnlyList<Vehicle>>(new List<Vehicle>());
        public Task<Vehicle?> GetBySlugAsync(string slug) => Task.FromResult<Vehicle?>(null);
    }
    private sealed class FakeLeads : ILeadService
    {
        public Lead? Created;
        public Task CreateAsync(Lead lead, CancellationToken ct = default) { Created = lead; return Task.CompletedTask; }
    }
    private sealed class ThrowingEmail : IEmailSender
    {
        public bool Called;
        public Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default)
        { Called = true; throw new InvalidOperationException("smtp down"); }
    }
    private sealed class PassThroughLoc : IStringLocalizer<SharedResource>
    {
        public LocalizedString this[string name] => new(name, name, false);
        public LocalizedString this[string name, params object[] arguments] => new(name, name, false);
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => System.Array.Empty<LocalizedString>();
    }

    private static FormsController Build(FakeContent content, FakeLeads leads, IEmailSender email)
    {
        var c = new FormsController(content, new FakeVehicles(), leads, email, new PassThroughLoc(), NullLogger<FormsController>.Instance);
        c.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            new Microsoft.AspNetCore.Http.DefaultHttpContext(),
            new FakeTempDataProvider());
        return c;
    }
    private sealed class FakeTempDataProvider : Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(Microsoft.AspNetCore.Http.HttpContext c) => new Dictionary<string, object>();
        public void SaveTempData(Microsoft.AspNetCore.Http.HttpContext c, IDictionary<string, object> v) { }
    }

    private static FormPage Page(FormType t) => new() { Slug = "x", FormType = t, IsVisible = true, Title = new LocalizedText { En = "X" } };
    private static LeadFormInput ValidCore() => new() { Title = "Mr", FirstName = "Ada", LastName = "Lovelace", Email = "ada@example.com", Phone = "12345678" };

    [Fact]
    public async Task Submit_UnknownSlug_ReturnsNotFound()
    {
        var ctrl = Build(new FakeContent { Form = null }, new FakeLeads(), new ThrowingEmail());
        var result = await ctrl.Submit("nope", ValidCore());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Submit_Invalid_ReturnsViewWithModel()
    {
        var ctrl = Build(new FakeContent { Form = Page(FormType.Contact) }, new FakeLeads(), new ThrowingEmail());
        ctrl.ModelState.AddModelError("FirstName", "required");
        var result = await ctrl.Submit("x", new LeadFormInput());
        var view = Assert.IsType<ViewResult>(result);
        Assert.IsType<FormPageViewModel>(view.Model);
    }

    [Fact]
    public async Task Submit_ServiceBooking_MissingModelAndBranch_IsInvalid()
    {
        var ctrl = Build(new FakeContent { Form = Page(FormType.ServiceBooking) }, new FakeLeads(), new ThrowingEmail());
        var result = await ctrl.Submit("x", ValidCore()); // no Model, no Branch
        Assert.IsType<ViewResult>(result);
        Assert.False(ctrl.ModelState.IsValid);
        Assert.True(ctrl.ModelState.ContainsKey("Model"));
        Assert.True(ctrl.ModelState.ContainsKey("Branch"));
    }

    [Fact]
    public async Task Submit_Valid_PersistsLead_AndRedirects_EvenIfEmailThrows()
    {
        var leads = new FakeLeads();
        var email = new ThrowingEmail();
        var ctrl = Build(new FakeContent { Form = Page(FormType.TestDrive) }, leads, email);
        var input = ValidCore(); input.Model = "GS8";
        var result = await ctrl.Submit("x", input);
        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/x", redirect.Url);
        Assert.NotNull(leads.Created);
        Assert.Equal("Mr Ada Lovelace", leads.Created!.Name);
        Assert.Equal(FormType.TestDrive, leads.Created.FormType);
        Assert.True(email.Called); // email attempted but its exception did not bubble
    }
}
```

- [ ] **Step 2: Run it — expect FAIL** (FormsController does not exist)

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~FormsControllerTests`
Expected: build error — `FormsController` not found.

- [ ] **Step 3: Create the view helper** — `GAC.Web/Infrastructure/ViewExtensions.cs`
```csharp
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GAC.Web.Infrastructure;

public static class ViewExtensions
{
    /// <summary>Returns " error" (note leading space) when the given ModelState key is invalid, else "".
    /// Used to add the .error class to a .field wrapper so the existing .err span shows.</summary>
    public static string FieldErrorClass(this IHtmlHelper html, string key)
        => html.ViewData.ModelState.GetFieldValidationState(key) == ModelValidationState.Invalid ? " error" : "";
}
```

- [ ] **Step 4: Create `FormsController`** — `GAC.Web/Controllers/FormsController.cs`
```csharp
using System.Globalization;
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace GAC.Web.Controllers;

public class FormsController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    private readonly ILeadService _leads;
    private readonly IEmailSender _email;
    private readonly IStringLocalizer<SharedResource> _loc;
    private readonly ILogger<FormsController> _log;

    public FormsController(IContentService content, IVehicleService vehicles, ILeadService leads,
        IEmailSender email, IStringLocalizer<SharedResource> loc, ILogger<FormsController> log)
    { _content = content; _vehicles = vehicles; _leads = leads; _email = email; _loc = loc; _log = log; }

    [HttpPost("/forms/{slug}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(string slug, [Bind(Prefix = "")] LeadFormInput input)
    {
        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form is null) return NotFound();

        if (RequiresModel(form.FormType) && string.IsNullOrWhiteSpace(input.Model))
            ModelState.AddModelError("Model", _loc["Please select your model."]);
        if (RequiresBranch(form.FormType) && string.IsNullOrWhiteSpace(input.Branch))
            ModelState.AddModelError("Branch", _loc["Please select a branch."]);

        if (!ModelState.IsValid)
        {
            ViewData["Title"] = form.Title.Localize();
            return View("~/Views/Forms/Page.cshtml", new FormPageViewModel { Page = form, Input = input });
        }

        var lead = await BuildLeadAsync(form, input);
        await _leads.CreateAsync(lead);
        await _email.SendLeadNotificationAsync(lead, form.Title.Localize()); // impl is best-effort/never throws

        TempData["FormSubmitted"] = "1";
        return Redirect($"/{slug}");
    }

    private static bool RequiresModel(FormType t) =>
        t is FormType.ServiceBooking or FormType.TestDrive or FormType.Quote;

    private static bool RequiresBranch(FormType t) =>
        t is FormType.ServiceBooking or FormType.Fleet;

    private async Task<Lead> BuildLeadAsync(FormPage form, LeadFormInput input)
    {
        var name = string.Join(" ", new[] { input.Title, input.FirstName, input.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        // Resolve a vehicle by display name (case-insensitive) where possible; keep raw model text otherwise.
        int? vehicleId = null;
        if (!string.IsNullOrWhiteSpace(input.Model))
        {
            var vehicles = await _vehicles.GetVisibleAsync();
            var match = vehicles.FirstOrDefault(v =>
                string.Equals(v.Name.En, input.Model, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Name.Ar, input.Model, StringComparison.OrdinalIgnoreCase));
            vehicleId = match?.Id;
        }

        // The Lead schema is lean — fold extra captured fields into the message so nothing is lost.
        var notes = new List<string>();
        if (!string.IsNullOrWhiteSpace(input.Model) && vehicleId is null) notes.Add($"Model: {input.Model}");
        if (!string.IsNullOrWhiteSpace(input.Mileage)) notes.Add($"Mileage: {input.Mileage}");
        if (input.Marketing) notes.Add("Marketing opt-in: Yes");
        var message = string.Join("\n", new[]
            {
                string.IsNullOrWhiteSpace(input.Message) ? null : input.Message.Trim(),
                notes.Count > 0 ? string.Join("\n", notes) : null
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

        DateOnly? preferred = null;
        if (!string.IsNullOrWhiteSpace(input.DueDate) &&
            DateOnly.TryParseExact(input.DueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var d))
            preferred = d;

        return new Lead
        {
            FormType = form.FormType,
            Status = LeadStatus.New,
            Name = name,
            Phone = input.Phone,
            Email = input.Email,
            Message = string.IsNullOrWhiteSpace(message) ? null : message,
            VehicleId = vehicleId,
            PreferredDate = preferred,
            Branch = string.IsNullOrWhiteSpace(input.Branch) ? null : input.Branch,
            SourcePage = "/" + form.Slug,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
```

- [ ] **Step 5: Run the controller tests — expect PASS**

Run: `dotnet test Solution/GAC.sln --filter FullyQualifiedName~FormsControllerTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Update `PageController` to pass the composite VM** — `GAC.Web/Controllers/PageController.cs`

Change the FormPage branch (line ~22-23) from:
```csharp
        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null) { ViewData["Title"] = form.Title.Localize(); return View("~/Views/Forms/Page.cshtml", form); }
```
to:
```csharp
        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null)
        {
            ViewData["Title"] = form.Title.Localize();
            return View("~/Views/Forms/Page.cshtml", new GAC.Web.Models.FormPageViewModel { Page = form });
        }
```

- [ ] **Step 7: Update the container view + success banner** — `GAC.Web/Views/Forms/Page.cshtml`

Replace the whole file with:
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
else
{
  <partial name="~/Views/Forms/Forms/_@(Model.Page.Slug).cshtml" model="Model" />
}
```

- [ ] **Step 8: Build — expect success**

Run: `dotnet build Solution/GAC.sln -c Debug`
Expected: build succeeds. (The 6 form partials still declare `@model GAC.Core.Content.FormPage` — they are updated in Task 4. The build still succeeds because Razor views compile at runtime by default in this project; if the project uses Razor compile-on-build and this errors, proceed directly to Task 4 then rebuild. Note this in the commit if so.)

- [ ] **Step 9: Commit**
```bash
git add Solution/GAC.Web/Controllers/FormsController.cs Solution/GAC.Web/Infrastructure/ViewExtensions.cs \
        Solution/GAC.Web/Controllers/PageController.cs Solution/GAC.Web/Views/Forms/Page.cshtml \
        Solution/GAC.Tests/FormsControllerTests.cs
git commit -m "feat(phase5): FormsController.Submit, lead mapping, PRG + success banner"
```

---

## Task 4: Wire the 5 form partials + `main.js`, update `contact-us` model

**Files:**
- Modify: all 6 `GAC.Web/Views/Forms/Forms/_*.cshtml`
- Modify: `GAC.Web/wwwroot/assets/js/main.js`
- Test: `GAC.Tests/FormSubmissionTests.cs`

**Shared wiring rules** (apply to each of the 5 real-form partials):
1. Change line 1 `@model GAC.Core.Content.FormPage` → `@model GAC.Web.Models.FormPageViewModel`.
2. Any `@Model.Title.Localize()` → `@Model.Page.Title.Localize()`.
3. Change the opening form tag `<form data-form novalidate>` →
   `<form data-form novalidate method="post" asp-controller="Forms" asp-action="Submit" asp-route-slug="@Model.Page.Slug">`
   (the `asp-*` tag helper auto-injects the anti-forgery hidden field; `[ValidateAntiForgeryToken]` validates it).
4. For each **text/email/tel input** `<input id="x" name="fieldName" ... />`, add `value="@Model.Input.FieldName"` (PascalCase property). Keep `id`, `name`, `type`, `required` unchanged.
5. For each `<div class="field">` that wraps a field with a `.err` span, change it to
   `<div class="field@(Html.FieldErrorClass("PropertyName"))">` where `PropertyName` is the matching `LeadFormInput` property (Title, FirstName, LastName, Email, Phone, Model, Branch).
6. Localize each `.err` span text via `@L["..."]` using the exact English string already present (which is a resx key from Task 2). Mapping:
   - "Please select your model." , "Please select a branch." , "Please select a title." ,
     "Please enter your first name." , "Please enter your last name." , "Please enter a valid email." ,
     "Please enter your contact number."
7. For each `<select>`, preserve the chosen value by marking the matching option `selected`. Use this pattern (example for the title select):
   ```cshtml
   <select id="title" name="title" required>
     <option value="">Please select ...</option>
     @foreach (var t in new[] { "Mr", "Ms", "Mrs", "Miss" })
     {
       <option selected="@(Model.Input.Title == t)">@t</option>
     }
   </select>
   ```
   Apply the same loop pattern to the `model` and `branch` selects, using each form's existing option list verbatim (copy the exact `<option>` text values into the array).
8. For the `marketing` checkbox `<input id="mk" name="marketing" type="checkbox" />`, add `value="true"` and preserve state: `<input id="mk" name="marketing" type="checkbox" value="true" checked="@Model.Input.Marketing" />`.
9. Localize the submit button text: `<button ...>@L["Submit"]</button>`.
10. **Do not** change any `id`, class, `data-*`, SVG, layout, or the privacy-disclaimer prose.

- [ ] **Step 1: Update `_book-a-service.cshtml`** per the shared rules.
  - Fields present: model(req)→`Model`, mileage→`Mileage`, dueDate→`DueDate`, branch(req)→`Branch`, title(req)→`Title`, firstName(req)→`FirstName`, lastName(req)→`LastName`, email(req)→`Email`, phone(req)→`Phone`, message→`Message`, marketing.
  - `model` select option list: `GN8, GS8, GS4, GS3, GA8, GA4, EMPOW, EMPOW R, EMZOOM, EMKOO, M8, traveler`.
  - `branch` select option list: `GAC service Center, GAC Motors Jeddah, Kilo 3 Branch, Jeddah, Al Amal Branch, Dammam Branch – Service, GAC Motors Al-Madinah Al-Munawarrah Branch, GAC Motors Jazan Branch`.
  - `mileage`, `dueDate`, `message` have no `.err` span (optional) — just add `value=`/preserve; no error class needed.

- [ ] **Step 2: Update `_book-a-test-drive.cshtml`** per the shared rules.
  - Fields: title(req), firstName(req), lastName(req), email(req), phone(req), model(req)→`Model`.
  - `model` select list: `EMZOOM, EMPOW, EMPOW R, EMKOO, Traveler, GS8, M8, GS4 MAX, HYPTECH HT`.

- [ ] **Step 3: Update `_request-a-quote.cshtml`** per the shared rules.
  - Same fields + same `model` list as test-drive.

- [ ] **Step 4: Update `_fleet.cshtml`** per the shared rules.
  - Fields: branch(req)→`Branch`, title(req), firstName(req), lastName(req), email(req), phone(req), message(opt).
  - `branch` select list: `Riyadh Branch, GAC Motors Jeddah, Malibari Sq Showroom, GAC Motors Jeddah, Kilo 3 Branch, Dammam Branch, GAC Motors Al-Madinah Al-Munawarrah Branch`.
  - The form tag here is `<form data-form class="flt-form" novalidate>` — preserve the `class="flt-form"`: `<form data-form class="flt-form" novalidate method="post" asp-controller="Forms" asp-action="Submit" asp-route-slug="@Model.Page.Slug">`.
  - `@Model.Title.Localize()` (line ~12) → `@Model.Page.Title.Localize()`.

- [ ] **Step 5: Update `_recall-enquiry.cshtml`** per the shared rules.
  - Fields: title(req), firstName(req), lastName(req), email(req), phone(req), message(opt).

- [ ] **Step 6: Update `_contact-us.cshtml`** (no form — model swap only).
  - Change line 1 `@model GAC.Core.Content.FormPage` → `@model GAC.Web.Models.FormPageViewModel`. No other change (the body references no `@Model` members).

- [ ] **Step 7: Update `main.js`** — `GAC.Web/wwwroot/assets/js/main.js`

Replace the success branch (the `if (ok) { ... }` block, ~lines 132-134) so a valid client-side form does a real native submit instead of faking success:
```javascript
      if (ok) {
        form.submit();
      }
```
Leave the rest of the `[data-form]` handler (the `e.preventDefault()`, the required-field loop, the per-input `input` listener) unchanged. `form.submit()` does not re-trigger the submit event, so there is no recursion; with JS disabled the browser submits natively and the server validates.

- [ ] **Step 8: Write the integration wiring test** — `GAC.Tests/FormSubmissionTests.cs`

Read-only GETs (no DB writes): assert the rendered form is now a real POST form with an anti-forgery token, and that Arabic validation text renders under the AR cookie.
```csharp
using System.Net;
using Xunit;

namespace GAC.Tests;

public class FormSubmissionTests : IClassFixture<DevWebApplicationFactory>
{
    private readonly DevWebApplicationFactory _factory;
    public FormSubmissionTests(DevWebApplicationFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/book-a-service")]
    [InlineData("/book-a-test-drive")]
    [InlineData("/request-a-quote")]
    [InlineData("/fleet")]
    [InlineData("/recall-enquiry")]
    public async Task FormPage_RendersPostForm_WithAntiForgeryToken(string url)
    {
        var html = await (await _factory.CreateClient().GetAsync(url)).Content.ReadAsStringAsync();
        Assert.Contains("method=\"post\"", html);
        Assert.Contains("action=\"/forms/", html);
        Assert.Contains("__RequestVerificationToken", html);
    }

    [Fact]
    public async Task FormPage_Arabic_ShowsLocalizedErrorText()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", ".AspNetCore.Culture=c%3Dar%7Cuic%3Dar");
        var raw = await (await client.GetAsync("/recall-enquiry")).Content.ReadAsStringAsync();
        var html = WebUtility.HtmlDecode(raw); // Razor encodes Arabic to numeric refs
        Assert.Contains("الرجاء إدخال الاسم الأول.", html); // "Please enter your first name." in AR
    }

    [Fact]
    public async Task ContactUs_StillRenders200()
    {
        var res = await _factory.CreateClient().GetAsync("/contact-us");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}
```

- [ ] **Step 9: Build + run the full test suite — expect PASS**

Run: `dotnet build Solution/GAC.sln -c Debug` then `dotnet test Solution/GAC.sln`
Expected: build clean; all tests pass (67 prior + the new Phase-5 tests). Integration tests need the DB reachable.

- [ ] **Step 10: Commit**
```bash
git add Solution/GAC.Web/Views/Forms/Forms/_book-a-service.cshtml \
        Solution/GAC.Web/Views/Forms/Forms/_book-a-test-drive.cshtml \
        Solution/GAC.Web/Views/Forms/Forms/_request-a-quote.cshtml \
        Solution/GAC.Web/Views/Forms/Forms/_fleet.cshtml \
        Solution/GAC.Web/Views/Forms/Forms/_recall-enquiry.cshtml \
        Solution/GAC.Web/Views/Forms/Forms/_contact-us.cshtml \
        Solution/GAC.Web/wwwroot/assets/js/main.js \
        Solution/GAC.Tests/FormSubmissionTests.cs
git commit -m "feat(phase5): wire 5 lead forms to POST + bilingual validation + native submit"
```

---

## Task 5: Visual verification, HANDOFF, memory, finish

**Files:** Modify `Solution/docs/HANDOFF.md`; update memory files.

- [ ] **Step 1: Manual run + visual check (EN + AR)**

Run: `dotnet run --project Solution/GAC.Web` (serves http://localhost:5058).
Verify on `/book-a-test-drive` (and one other):
- Submitting with empty required fields shows inline errors (client-side); with JS disabled the server re-renders with the same errors.
- A valid submit redirects back to the page and shows the success banner ("Thanks — we received your request.").
- Switch language to Arabic: the page is RTL, validation messages render in Arabic, the success banner is Arabic.
- Confirm a `Lead` row was inserted (query `SELECT TOP 5 * FROM Leads ORDER BY Id DESC`).
- Confirm the SMTP send was attempted (log line) — actual delivery depends on the local Development.json creds; an SMTP failure must NOT break the submission.

Use the chrome-devtools MCP for screenshots if helpful. Stop the app when done.

- [ ] **Step 2: Update `Solution/docs/HANDOFF.md`**
  - Header date → end of Phase 5.
  - Phase 5 status → ✅ (with test count); Phase 6 → ⏭️ NEXT.
  - Add a "## 5c. Phase 5 — Forms & leads — what was built" section: the POST/PRG flow, `LeadFormInput` + conditional Model/Branch validation, bilingual DataAnnotations via `SharedResource`, `Lead` mapping (name composition, vehicle match, message-folding of model/mileage/marketing, DueDate→PreferredDate), MailKit `SmtpEmailSender` (best-effort), the `main.js` `form.submit()` change, and that `contact-us` has no form.
  - Note SMTP secret handling: placeholders in `appsettings.json`, real creds only in gitignored `appsettings.Development.json`.
  - Add USER ACTIONS for deploy: set real `Smtp:Username`/`Smtp:Password` (and confirm `Smtp:AdminNotifyEmail` = real sales inbox, currently `postmaster@mg.codexkw.co`) on the server; set `Smtp:Enabled=true`; ensure Mailgun domain `mg.codexkw.co` SPF/DKIM are valid so notifications aren't spam-filtered.

- [ ] **Step 3: Update memory** — `gac_cms_pivot.md` (add a PHASE 5 DONE paragraph) and `MEMORY.md` (GAC line → "PHASE 1-5 DONE + PUSHED ...; NEXT: Phase 6 (Admin area)").

- [ ] **Step 4: Finish the branch** — use superpowers:finishing-a-development-branch.
  - Verify full suite green first.
  - **Before any push: scan the staged diff** for the real SMTP password, `P@ssw0rd`, `83.229.86.221`, `Password=` (non-placeholder), and confirm `appsettings.Development.json` is NOT tracked (`git status` + `git ls-files | grep Development.json` returns nothing).
  - Per the established project pattern, the intended finish is **push to public `origin/main`**.

---

## Self-review notes (addressed in this plan)

- **Spec coverage:** anti-forgery (`[ValidateAntiForgeryToken]` + tag-helper token), bilingual server validation (DataAnnotations + `AddDataAnnotationsLocalization` over `SharedResource`, conditional Model/Branch), persist to `Lead` (`ILeadService`), SMTP email (`IEmailSender`/MailKit, best-effort), keep markup/classes (names/ids/classes unchanged; only attributes + value/error bindings added). Admin Leads inbox is intentionally deferred to Phase 6 (per HANDOFF §9 "or defer the inbox to Phase 6").
- **No new migration:** the `Leads` table already exists from Phase 2 (`AddContentModel`).
- **Type consistency:** `LeadFormInput` property names (Title/FirstName/LastName/Email/Phone/Model/Branch/Mileage/DueDate/Message/Marketing) are used identically in the VM, controller mapping, `Html.FieldErrorClass` keys, and partial bindings. `FormPageViewModel.Page`/`.Input` used consistently across PageController, Page.cshtml, partials, and FormsController.
- **Secrets:** real SMTP creds only in gitignored `appsettings.Development.json`; committed `appsettings.json` keeps `__SET_LOCALLY__` for Username/Password and `Enabled:false`. Scoped `git add` everywhere; explicit pre-push secret scan.
