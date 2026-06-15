using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class PageController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public PageController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    // Catch-all single-segment slug. A literal attribute route like "/models" or "/news"
    // is more specific and wins over this parameter route, so dedicated controllers are safe.
    // Exclude "admin" so the Admin area's conventional route (not an attribute route) can win;
    // attribute routes otherwise take precedence over conventional routes.
    [HttpGet("/{slug:regex(^(?!(?i:admin)$).*$)}")]
    public async Task<IActionResult> Show(string slug)
    {
        var content = await _content.GetContentPageBySlugAsync(slug);
        if (content != null) { ViewData["Title"] = content.Title.Localize(); return View("~/Views/Content/Page.cshtml", content); }

        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null)
        {
            ViewData["Title"] = form.Title.Localize();
            return View("~/Views/Forms/Page.cshtml", new GAC.Web.Models.FormPageViewModel { Page = form });
        }

        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle != null) { ViewData["Title"] = vehicle.Name.Localize(); return View("~/Views/Vehicles/Detail.cshtml", vehicle); }

        return NotFound();
    }
}
