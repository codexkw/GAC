using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class MenuController : Controller
{
    private readonly IAdminMenuService _svc;
    public MenuController(IAdminMenuService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAllAsync());

    public async Task<IActionResult> Create()
    {
        await SetParentsAsync();
        return View("Edit", new MenuItem());
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _svc.GetAsync(id);
        if (item is null) return NotFound();
        await SetParentsAsync(item.Id);
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Save(MenuItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Label?.En))
            ModelState.AddModelError("Label.En", "Label (English) is required.");
        if (!ModelState.IsValid)
        {
            await SetParentsAsync(item.Id == 0 ? null : item.Id);
            return View("Edit", item);
        }

        if (item.Id == 0)
        {
            await _svc.CreateAsync(item);
            TempData["Flash"] = "Menu item created.";
        }
        else
        {
            await _svc.UpdateAsync(item);
            TempData["Flash"] = "Menu item saved.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        TempData["Flash"] = "Menu item deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        await _svc.MoveAsync(id, direction);
        return RedirectToAction(nameof(Index));
    }

    private async Task SetParentsAsync(int? excludeId = null)
    {
        var topLevel = (await _svc.ListAllAsync()).Where(m => m.ParentId == null);
        if (excludeId is not null) topLevel = topLevel.Where(m => m.Id != excludeId);
        ViewBag.Parents = topLevel
            .Select(m => new SelectListItem(m.Label.Localize(), m.Id.ToString()))
            .ToList();
    }
}
