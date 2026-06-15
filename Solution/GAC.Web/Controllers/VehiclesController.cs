using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class VehiclesController : Controller
{
    private readonly IVehicleService _vehicles;
    public VehiclesController(IVehicleService vehicles) => _vehicles = vehicles;

    [HttpGet("/models")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("Models", "/models");
        return View(await _vehicles.GetVisibleAsync());
    }
}
