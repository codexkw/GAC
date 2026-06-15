using System.Diagnostics;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class HomeController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    private readonly ISiteService _site;
    public HomeController(IContentService content, IVehicleService vehicles, ISiteService site)
    { _content = content; _vehicles = vehicles; _site = site; }

    public async Task<IActionResult> Index()
    {
        var settings = await _site.GetSettingsAsync();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var seo = SeoBuilder.ForListing(null, "/");
        seo.JsonLd.Add(SeoBuilder.AutoDealerJsonLd(settings, baseUrl));
        ViewData["Seo"] = seo;

        return View(new HomeViewModel
        {
            Home = await _content.GetHomePageAsync(),
            Vehicles = await _vehicles.GetVisibleAsync(),
            News = await _content.GetPublishedNewsAsync()
        });
    }

    [HttpGet("/not-found")]
    public IActionResult NotFoundPage()
    {
        ViewData["Seo"] = new SeoData { Title = "Page not found", CanonicalPath = "/not-found",
            Robots = "noindex,nofollow" };
        return View("NotFound");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
