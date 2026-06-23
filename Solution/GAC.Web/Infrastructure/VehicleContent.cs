using System.Collections.Generic;
using System.Linq;
using GAC.Core.Content;

namespace GAC.Web.Infrastructure;

/// <summary>Pure helpers for rendering the vehicle detail page.</summary>
public static class VehicleContent
{
    /// <summary>A vehicle uses structured sections when any typed collection is populated.</summary>
    public static bool HasStructuredContent(Vehicle v)
        => v.Features.Count > 0
        || v.SpecGroups.Count > 0
        || v.Colors.Count > 0
        || v.Trims.Count > 0;

    public static string FeatureLayoutCss(FeatureLayout layout) => layout switch
    {
        FeatureLayout.ImageRight => "mp-feature mp-feature--reverse",
        FeatureLayout.Banner => "mp-feature mp-feature--banner",
        FeatureLayout.TextOnly => "mp-feature mp-feature--text",
        _ => "mp-feature"
    };

    public static bool ShowsImage(FeatureLayout layout) => layout != FeatureLayout.TextOnly;

    public static IEnumerable<FeatureSection> DesignFeatures(Vehicle v)
        => v.Features.Where(f => f.GroupKey == FeatureGroup.Design).OrderBy(f => f.SortOrder);

    public static IEnumerable<FeatureSection> PerformanceFeatures(Vehicle v)
        => v.Features.Where(f => f.GroupKey == FeatureGroup.Performance).OrderBy(f => f.SortOrder);

    public static string TabKey(string prefix, int index) => $"{prefix}{index + 1}";

    public static string StateActive(bool isFirst) => isFirst ? " is-active" : "";

    public static string StateOpen(bool isFirst) => isFirst ? " is-open" : "";

    public static string AriaExpanded(bool isFirst) => isFirst ? "true" : "false";
}
