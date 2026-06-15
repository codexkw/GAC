using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.AdminOnly)]
[AutoValidateAntiforgeryToken]
public class SettingsController : Controller
{
    private readonly IAdminSettingsService _svc;
    public SettingsController(IAdminSettingsService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.GetAsync());

    [HttpPost]
    public async Task<IActionResult> Save(SiteSettings settings)
    {
        await _svc.UpdateAsync(settings);
        TempData["Flash"] = "Settings saved.";
        return RedirectToAction(nameof(Index));
    }
}
