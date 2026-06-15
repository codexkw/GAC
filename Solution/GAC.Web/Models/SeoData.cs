namespace GAC.Web.Models;

/// <summary>Per-page SEO data carried via ViewData["Seo"] and rendered by _SeoHead.cshtml.</summary>
public sealed class SeoData
{
    public string? Title { get; set; }          // page title, BEFORE the " - GAC Mutawa Alkadi" suffix
    public string? Description { get; set; }     // meta description
    public string? CanonicalPath { get; set; }   // root-relative clean path, e.g. "/gs8"
    public string? OgImage { get; set; }         // root-relative or absolute image path
    public string OgType { get; set; } = "website";   // website | article | product
    public string? Robots { get; set; }          // e.g. "noindex,nofollow"; null => omit
    public List<string> JsonLd { get; set; } = new();  // each entry is a complete JSON-LD object string
}
