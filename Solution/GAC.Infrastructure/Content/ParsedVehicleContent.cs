using GAC.Core.Content;

namespace GAC.Infrastructure.Content;

/// <summary>Everything parsed out of ONE language of a vehicle's BodyHtml.
/// Entities carry no VehicleId/SortOrder yet — the migrator assigns those.</summary>
public sealed class ParsedVehicleContent
{
    public string? HeroImage { get; set; }
    public string? HeroTitle { get; set; }
    public string? HeroSub { get; set; }

    public List<SectionHeading> Headings { get; set; } = new();
    public List<StatItem> Stats { get; set; } = new();
    public string? StatsNote { get; set; }
    public List<SliderGroup> Sliders { get; set; } = new();
    public List<FeatureSection> Features { get; set; } = new();
    public List<GalleryTab> GalleryTabs { get; set; } = new();
    public QualityBlock? Quality { get; set; }
    public string? TechBanner { get; set; }
    public List<CardItem> Cards { get; set; } = new();
    public List<SafetyToggle> Safety { get; set; } = new();
    public List<Trim> Trims { get; set; } = new();
    public List<WarrantyLink> Warranty { get; set; } = new();

    public string? EnquiryBg { get; set; }
    public string? EnquiryTitle { get; set; }
    public string? EnquirySub { get; set; }
    public string? EnquiryLead { get; set; }
}
