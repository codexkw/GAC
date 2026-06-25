using System.ComponentModel.DataAnnotations;
using System.Reflection;
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

public static class UrlHelpers
{
    /// <summary>Friendly enum label from its [Display(Name)] (falls back to the member name).
    /// Keeps admin list labels in sync with the GetEnumSelectList dropdown.</summary>
    public static string DisplayName(this Enum value)
    {
        var member = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
    }

    /// <summary>Defensively normalize a stored link to a clean app path (handles legacy ".html").</summary>
    public static string NormalizeUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "#";
        if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith('#')) return url;
        var u = url.Trim();
        if (u.Equals("index.html", StringComparison.OrdinalIgnoreCase) || u == "/") return "/";
        if (u.Equals("contact.html", StringComparison.OrdinalIgnoreCase)) return "/contact-us";
        if (u.Equals("model-detail.html", StringComparison.OrdinalIgnoreCase)) return "/models";
        if (u.EndsWith(".html", StringComparison.OrdinalIgnoreCase)) u = u[..^5];
        if (!u.StartsWith('/')) u = "/" + u;
        return u;
    }

    /// <summary>Megamenu/listing filter classes from category flags, e.g. Suv|Ev → "suv ev".</summary>
    public static string CategoryCss(VehicleCategory c)
    {
        var parts = new List<string>();
        if (c.HasFlag(VehicleCategory.Sedan)) parts.Add("sedan");
        if (c.HasFlag(VehicleCategory.Suv)) parts.Add("suv");
        if (c.HasFlag(VehicleCategory.Ev)) parts.Add("ev");
        return string.Join(' ', parts);
    }

    /// <summary>Listing/menu thumbnail: first Gallery image, else the Hero image, else empty.</summary>
    public static string ThumbPath(Vehicle v)
    {
        var thumb = v.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Gallery)
                    ?? v.Images.FirstOrDefault(i => i.Kind == VehicleImageKind.Hero);
        return thumb?.Path ?? "";
    }
}
