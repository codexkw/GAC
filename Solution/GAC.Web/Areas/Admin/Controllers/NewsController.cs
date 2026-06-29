using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Areas.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AdminPolicies.ContentEditor)]
[AutoValidateAntiforgeryToken]
public class NewsController : Controller
{
    private readonly IAdminNewsService _svc;
    public NewsController(IAdminNewsService svc) => _svc = svc;

    public async Task<IActionResult> Index() => View(await _svc.ListAsync());

    public IActionResult Create() =>
        View("Edit", new NewsArticle { IsPublished = true, PublishedOn = DateOnly.FromDateTime(DateTime.UtcNow) });

    public async Task<IActionResult> Edit(int id)
    {
        var a = await _svc.GetAsync(id);
        return a is null ? NotFound() : View(a);
    }

    [HttpPost]
    public async Task<IActionResult> Save(NewsArticle a)
    {
        // Auto-derive the slug from the English title when left blank; otherwise
        // normalize whatever was typed so the stored slug is always URL-safe.
        a.Slug = string.IsNullOrWhiteSpace(a.Slug) ? Slug.From(a.Title.En) : Slug.From(a.Slug);

        if (string.IsNullOrWhiteSpace(a.Title.En))
            ModelState.AddModelError("Title.En", "An English title is required.");
        if (string.IsNullOrWhiteSpace(a.Slug))
            ModelState.AddModelError(nameof(a.Slug), "Enter a slug, or an English title to generate one from.");
        else if (await _svc.SlugExistsAsync(a.Slug, a.Id == 0 ? null : a.Id))
            ModelState.AddModelError(nameof(a.Slug), "That slug is already in use.");

        if (!ModelState.IsValid) return View("Edit", a);

        if (a.Id == 0)
        {
            await _svc.CreateAsync(a);
            TempData["Flash"] = "Article created.";
        }
        else
        {
            await _svc.UpdateAsync(a);
            TempData["Flash"] = "Article saved.";
        }
        // Pin the area: a public NewsController ([HttpGet("/news")]) shares this
        // controller name, so without an explicit area the redirect can resolve
        // to the public /news page instead of the admin list.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        await _svc.DeleteAsync(id);
        TempData["Flash"] = "Article deleted.";
        // Pin the area: a public NewsController ([HttpGet("/news")]) shares this
        // controller name, so without an explicit area the redirect can resolve
        // to the public /news page instead of the admin list.
        return RedirectToAction(nameof(Index), new { area = "Admin" });
    }
}
