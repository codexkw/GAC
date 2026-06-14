using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.ViewComponents;

public class FooterViewComponent : ViewComponent
{
    private readonly ISiteService _site;
    public FooterViewComponent(ISiteService site) => _site = site;
    public async Task<IViewComponentResult> InvokeAsync() => View(await _site.GetSettingsAsync());
}
