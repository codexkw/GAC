using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace GAC.Web.Controllers;

public class CultureController : Controller
{
    private static readonly string[] Supported = { "en", "ar" };

    [HttpPost]
    [HttpGet] // allow simple <a> links from the header switch too
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(culture) || !Supported.Contains(culture))
            culture = "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(new CultureInfo(culture))),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl)
            && (Url?.IsLocalUrl(returnUrl) ?? IsLocalUrl(returnUrl)))
            return LocalRedirect(returnUrl);

        return LocalRedirect("/");
    }

    // Fallback local-URL check when no IUrlHelper is available (e.g. unit tests).
    private static bool IsLocalUrl(string url) =>
        url.StartsWith('/') && !url.StartsWith("//") && !url.StartsWith("/\\");
}
