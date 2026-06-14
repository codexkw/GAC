using GAC.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class NewsController : Controller
{
    private readonly IContentService _content;
    public NewsController(IContentService content) => _content = content;

    [HttpGet("/news")]
    public async Task<IActionResult> Index() => View(await _content.GetPublishedNewsAsync());

    [HttpGet("/news/{slug}")]
    public async Task<IActionResult> Detail(string slug)
    {
        var article = await _content.GetNewsBySlugAsync(slug);
        return article == null ? NotFound() : View(article);
    }
}
