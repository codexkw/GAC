using System.Diagnostics;
using GAC.Core.Services;
using GAC.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class HomeController : Controller
{
    private readonly IContentService _content;
    private readonly IVehicleService _vehicles;
    public HomeController(IContentService content, IVehicleService vehicles)
    { _content = content; _vehicles = vehicles; }

    public async Task<IActionResult> Index() => View(new HomeViewModel
    {
        Home = await _content.GetHomePageAsync(),
        Vehicles = await _vehicles.GetVisibleAsync(),
        News = await _content.GetPublishedNewsAsync()
    });

    [HttpGet("/not-found")]
    public IActionResult NotFoundPage() => View("NotFound");

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
