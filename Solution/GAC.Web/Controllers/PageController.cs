using GAC.Core.Content;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class PageController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public PageController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    [HttpGet("/{slug:regex(^(?!(?i:admin)$).*$)}")]
    public async Task<IActionResult> Show(string slug)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var content = await _content.GetContentPageBySlugAsync(slug);
        if (content != null)
        {
            ViewData["Seo"] = SeoBuilder.ForContentPage(content, baseUrl);
            // The warranty page has a dedicated structured editor + a cars grid
            // pulled live from the visible vehicles; its body is not the ContentPage HTML.
            if (content.Slug == "warranty")
            {
                var warranty = await _content.GetWarrantyPageAsync() ?? new GAC.Core.Content.WarrantyPage();
                var vehicles = await _vehicles.GetVisibleAsync();
                return View("~/Views/Content/Warranty.cshtml",
                    new GAC.Web.Models.WarrantyPageViewModel { Warranty = warranty, Vehicles = vehicles });
            }
            // The road-assistance page has a dedicated structured editor; its body is
            // not the ContentPage HTML.
            if (content.Slug == "road-assistance")
            {
                var road = await _content.GetRoadAssistancePageAsync() ?? new GAC.Core.Content.RoadAssistancePage();
                return View("~/Views/Content/RoadAssistance.cshtml", road);
            }
            // The cost-of-service page is a structured price matrix, not the ContentPage HTML.
            if (content.Slug == "cost-of-service")
            {
                var cos = await _content.GetCostOfServicePageAsync() ?? new GAC.Core.Content.CostOfServicePage();
                return View("~/Views/Content/CostOfService.cshtml", cos);
            }
            return View("~/Views/Content/Page.cshtml", content);
        }

        var form = await _content.GetFormPageBySlugAsync(slug);
        if (form != null)
        {
            ViewData["Seo"] = SeoBuilder.ForFormPage(form, baseUrl);
            return View("~/Views/Forms/Page.cshtml", new GAC.Web.Models.FormPageViewModel { Page = form });
        }

        var vehicle = await _vehicles.GetBySlugAsync(slug);
        if (vehicle != null)
        {
            HttpContext.Items["CurrentVehicleBrochure"] = vehicle.BrochurePdf;
            ViewData["Seo"] = SeoBuilder.ForVehicle(vehicle, baseUrl);
            return View("~/Views/Vehicles/Detail.cshtml", vehicle);
        }

        return NotFound();
    }
}
