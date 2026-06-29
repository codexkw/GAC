using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class HomeSectionsController : Controller
{
    private readonly IAdminHomeService _svc;
    public HomeSectionsController(IAdminHomeService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.GetHomeAggregateAsync());

    [HttpPost]
    public async Task<IActionResult> SavePromo(PromoSection promo)
    {
        await _svc.SavePromoAsync(promo);
        TempData["Flash"] = "Promo section saved.";
        // Pin the area so the redirect lands on the admin page, not a public route.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    [HttpPost]
    public async Task<IActionResult> SaveCard(DualCard card)
    {
        await _svc.SaveCardAsync(card);
        TempData["Flash"] = "Card saved.";
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
