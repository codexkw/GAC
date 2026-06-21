using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class DockController : Controller
{
    private readonly IAdminDockService _svc;
    public DockController(IAdminDockService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAllAsync());

    public IActionResult Create() => View("Edit", new DockItem());

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _svc.GetAsync(id);
        if (item is null) return NotFound();
        return View(item);
    }

    [HttpPost]
    public async Task<IActionResult> Save(DockItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Label?.En))
            ModelState.AddModelError("Label.En", "Label (English) is required.");
        if (!ModelState.IsValid) return View("Edit", item);

        if (item.Id == 0)
        {
            await _svc.CreateAsync(item);
            TempData["Flash"] = "Dock item created.";
        }
        else
        {
            await _svc.UpdateAsync(item);
            TempData["Flash"] = "Dock item saved.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        TempData["Flash"] = "Dock item deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Move(int id, int direction)
    {
        await _svc.MoveAsync(id, direction);
        return RedirectToAction(nameof(Index));
    }
}
