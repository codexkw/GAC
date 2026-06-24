using System.Globalization;
using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
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
            ViewData["Seo"] = SeoBuilder.ForFormPage(form, $"{Request.Scheme}://{Request.Host}");
            return View("~/Views/Forms/Page.cshtml", new FormPageViewModel { Page = form, Input = input });
        }

        var lead = await BuildLeadAsync(form, input);
        await _leads.CreateAsync(lead);

        // Best-effort notification — a failure here must never break a valid submission.
        try
        {
            await _email.SendLeadNotificationAsync(lead, form.Title.Localize());
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Lead notification send failed for {FormType}.", lead.FormType);
        }

        TempData["FormSubmitted"] = "1";
        return Redirect($"/{slug}");
    }

    [HttpPost("/models/{slug}/enquiry")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitEnquiry(string slug, [Bind(Prefix = "")] LeadFormInput input)
    {
        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle is null) return NotFound();

        if (!ModelState.IsValid)
        {
            TempData["FormError"] = "1";
            return Redirect($"/{slug}#enquiry");
        }

        var name = string.Join(" ", new[] { input.Title, input.FirstName, input.LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

        var lead = new Lead
        {
            FormType = FormType.Quote,
            Status = LeadStatus.New,
            Name = name,
            Phone = input.Phone,
            Email = input.Email,
            Message = string.IsNullOrWhiteSpace(input.Message) ? null : input.Message.Trim(),
            VehicleId = vehicle.Id,
            Branch = string.IsNullOrWhiteSpace(input.Branch) ? null : input.Branch,
            SourcePage = "/" + slug,
            CreatedAt = DateTimeOffset.UtcNow
        };
        await _leads.CreateAsync(lead);

        // Best-effort notification — a failure here must never break a valid submission.
        try
        {
            await _email.SendLeadNotificationAsync(lead, $"{vehicle.Name.Localize()} enquiry");
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Enquiry lead notification failed for {Slug}.", slug);
        }

        TempData["FormSubmitted"] = "1";
        return Redirect($"/{slug}#enquiry");
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
