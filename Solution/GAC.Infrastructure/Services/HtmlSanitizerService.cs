using GAC.Core.Services;
using Ganss.Xss;

namespace GAC.Infrastructure.Services;

public class HtmlSanitizerService : IHtmlSanitizerService
{
    private readonly HtmlSanitizer _sanitizer;

    public HtmlSanitizerService()
    {
        _sanitizer = new HtmlSanitizer();
        _sanitizer.AllowedTags.Clear();
        foreach (var t in new[] { "p", "div", "br", "strong", "b", "em", "i", "u", "ul", "ol", "li", "a" })
            _sanitizer.AllowedTags.Add(t);

        _sanitizer.AllowedAttributes.Clear();
        _sanitizer.AllowedAttributes.Add("href");

        _sanitizer.AllowedCssProperties.Clear();
        _sanitizer.AllowDataAttributes = false;

        _sanitizer.AllowedSchemes.Clear();
        foreach (var s in new[] { "http", "https", "mailto", "tel" })
            _sanitizer.AllowedSchemes.Add(s);
    }

    public string Sanitize(string? html)
        => string.IsNullOrWhiteSpace(html) ? "" : _sanitizer.Sanitize(html);
}
