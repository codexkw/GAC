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
        // Pin the area so the redirect lands on the admin page, not a public route.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
