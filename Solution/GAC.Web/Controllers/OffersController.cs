using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class OffersController : Controller
{
    private readonly IContentService _content;
    public OffersController(IContentService content) => _content = content;

    [HttpGet("/offers")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("Offers", "/offers");
        return View(await _content.GetActiveOffersAsync());
    }
}
