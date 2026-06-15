using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class NewsController : Controller
{
    private readonly IContentService _content;
    public NewsController(IContentService content) => _content = content;

    [HttpGet("/news")]
    public async Task<IActionResult> Index()
    {
        ViewData["Seo"] = SeoBuilder.ForListing("News", "/news");
        return View(await _content.GetPublishedNewsAsync());
    }

    [HttpGet("/news/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var article = await _content.GetNewsBySlugAsync(slug);
        if (article == null) return NotFound();
        ViewData["Seo"] = SeoBuilder.ForNews(article, $"{Request.Scheme}://{Request.Host}");
        return View(article);
    }
}
