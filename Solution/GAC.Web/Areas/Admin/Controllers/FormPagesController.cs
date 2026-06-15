using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class FormPagesController : Controller
{
    private readonly IAdminPageService _svc;
    public FormPagesController(IAdminPageService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListFormsAsync());

    public async Task<IActionResult> Edit(int id)
    {
        var page = await _svc.GetFormAsync(id);
        return page is null ? NotFound() : View(page);
    }

    [HttpPost]
    public async Task<IActionResult> Save(FormPage page)
    {
        if (!await _svc.UpdateFormAsync(page)) return NotFound();
        TempData["Flash"] = "Page saved.";
        return RedirectToAction(nameof(Edit), new { id = page.Id });
    }
}
