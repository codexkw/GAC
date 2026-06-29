using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class OffersController : Controller
{
    private readonly IAdminOfferService _svc;
    public OffersController(IAdminOfferService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAsync());

    public IActionResult Create() => View("Edit", new Offer { IsActive = true });

    public async Task<IActionResult> Edit(int id)
    {
        var o = await _svc.GetAsync(id);
        return o is null ? NotFound() : View(o);
    }

    [HttpPost]
    public async Task<IActionResult> Save(Offer a)
    {
        if (string.IsNullOrWhiteSpace(a.Slug))
            ModelState.AddModelError(nameof(a.Slug), "Slug is required.");
        else if (await _svc.SlugExistsAsync(a.Slug, a.Id == 0 ? null : a.Id))
            ModelState.AddModelError(nameof(a.Slug), "That slug is already in use.");
        if (!ModelState.IsValid) return View("Edit", a);

        if (a.Id == 0)
        {
            await _svc.CreateAsync(a);
            TempData["Flash"] = "Offer created.";
        }
        else
        {
            await _svc.UpdateAsync(a);
            TempData["Flash"] = "Offer saved.";
        }
        // Pin the area: a public OffersController ([HttpGet("/offers")]) shares
        // this controller name, so without an explicit area the redirect can
        // resolve to the public /offers page instead of the admin list.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        TempData["Flash"] = "Offer deleted.";
        // Pin the area: a public OffersController ([HttpGet("/offers")]) shares
        // this controller name, so without an explicit area the redirect can
        // resolve to the public /offers page instead of the admin list.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
