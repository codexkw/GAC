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
