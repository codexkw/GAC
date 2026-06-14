using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class HeaderViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public IReadOnlyList<MenuItem> Menu { get; set; } = new List<MenuItem>();
    public IReadOnlyList<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}

public class HeaderViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    private readonly IVehicleService _vehicles;
    public HeaderViewComponent(ISiteService site, IVehicleService vehicles)
    { _site = site; _vehicles = vehicles; }

    public async Task<IViewComponentResult> InvokeAsync() => View(new HeaderViewModel
    {
        Settings = await _site.GetSettingsAsync(),
        Menu = await _site.GetMenuAsync(),
        Vehicles = await _vehicles.GetVisibleAsync()
    });
}
