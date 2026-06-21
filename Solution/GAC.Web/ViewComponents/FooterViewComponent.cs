using GAC.Core.Content;
using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class FooterViewModel
{
    public SiteSettings Settings { get; set; } = new();
    public IReadOnlyList<DockItem> DockItems { get; set; } = new List<DockItem>();
    public string? CurrentVehicleBrochure { get; set; }
}

public class FooterViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    public FooterViewComponent(ISiteService site) => _site = site;

    public async Task<IViewComponentResult> InvokeAsync() => View(new FooterViewModel
    {
        Settings = await _site.GetSettingsAsync(),
        DockItems = await _site.GetDockItemsAsync(),
        CurrentVehicleBrochure = HttpContext.Items["CurrentVehicleBrochure"] as string
    });
}
