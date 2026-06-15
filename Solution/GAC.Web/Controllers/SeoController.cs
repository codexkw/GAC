using System.Xml.Linq;
using GAC.Core.Services;
using GAC.Web.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class SeoController : Controller
{
    private readonly IVehicleService _vehicles;
    private readonly IContentService _content;
    public SeoController(IVehicleService vehicles, IContentService content)
    { _vehicles = vehicles; _content = content; }

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> Sitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>();

        void Add(string path, DateOnly? lastmod = null)
        {
            var el = new XElement(ns + "url", new XElement(ns + "loc", SeoBuilder.Abs(baseUrl, path)));
            if (lastmod.HasValue)
                el.Add(new XElement(ns + "lastmod", lastmod.Value.ToString("yyyy-MM-dd")));
            urls.Add(el);
        }

        Add("/");
        Add("/models");
        Add("/news");
        Add("/offers");
        foreach (var v in await _vehicles.GetVisibleAsync()) Add("/" + v.Slug);
        foreach (var p in await _content.GetAllContentPagesAsync()) Add("/" + p.Slug);
        foreach (var f in await _content.GetAllFormPagesAsync()) Add("/" + f.Slug);
        foreach (var n in await _content.GetPublishedNewsAsync()) Add("/news/" + n.Slug, n.PublishedOn);

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(ns + "urlset", urls));
        var xml = doc.Declaration + Environment.NewLine + doc;
        return Content(xml, "application/xml; charset=utf-8");
    }

    [HttpGet("/robots.txt")]
    public IActionResult Robots()
    {
        var sitemap = SeoBuilder.Abs($"{Request.Scheme}://{Request.Host}", "/sitemap.xml");
        var body = "User-agent: *\n" +
                   "Disallow: /admin\n" +
                   "Disallow: /admin/\n\n" +
                   $"Sitemap: {sitemap}\n";
        return Content(body, "text/plain; charset=utf-8");
    }
}
