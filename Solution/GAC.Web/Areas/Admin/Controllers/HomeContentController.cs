using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class HomeContentController : Controller
{
    private readonly IAdminHomeService _svc;
    public HomeContentController(IAdminHomeService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListSlidesAsync());

    public IActionResult Create() => View("Edit", new HeroSlide());

    public async Task<IActionResult> Edit(int id)
    {
        var slide = await _svc.GetSlideAsync(id);
        return slide is null ? NotFound() : View(slide);
    }

    [HttpPost]
    public async Task<IActionResult> Save(HeroSlide slide)
    {
        if (string.IsNullOrWhiteSpace(slide.ImagePath))
            ModelState.AddModelError(nameof(slide.ImagePath), "Image is required.");
        if (string.IsNullOrWhiteSpace(slide.Heading?.En))
            ModelState.AddModelError("Heading.En", "Heading (English) is required.");
        if (!ModelState.IsValid)
            return View("Edit", slide);

        if (slide.Id == 0)
        {
            await _svc.CreateSlideAsync(slide);
            TempData["Flash"] = "Slide created.";
        }
        else
        {
            await _svc.UpdateSlideAsync(slide);
            TempData["Flash"] = "Slide saved.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost] public async Task<IActionResult> Delete(int id)
    { await _svc.DeleteSlideAsync(id); TempData["Flash"] = "Slide deleted."; return RedirectToAction(nameof(Index)); }

    [HttpPost] public async Task<IActionResult> Move(int id, int direction)
    { await _svc.MoveSlideAsync(id, direction); return RedirectToAction(nameof(Index)); }
}
