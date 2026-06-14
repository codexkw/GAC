namespace GAC.Web.Infrastructure;

/// <summary>301-redirects legacy "*.html" paths to their clean equivalents.</summary>
public class LegacyHtmlRedirectMiddleware
{
    private readonly RequestDelegate _next;
    public LegacyHtmlRedirectMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var path = ctx.Request.Path.Value ?? "";
        if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var clean = path.Equals("/live-empow-sport.html", StringComparison.OrdinalIgnoreCase)
                ? "/empow-sport"
                : UrlHelpers.NormalizeUrl(path.TrimStart('/'));
            ctx.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            ctx.Response.Headers.Location = clean + ctx.Request.QueryString;
            return;
        }
        await _next(ctx);
    }
}
