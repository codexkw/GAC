namespace GAC.Core.Content;

public class Vehicle
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public VehicleCategory Category { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public decimal? PriceFrom { get; set; }

    public LocalizedText Name { get; set; } = new();
    public LocalizedText Tagline { get; set; } = new();
    public LocalizedText IntroText { get; set; } = new();
    public LocalizedText BodyHtml { get; set; } = new();

    public string? BrochurePdf { get; set; }
    public string? SpecPdf { get; set; }
    public string? WarrantyBookletPdf { get; set; }

    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();

    // Rich-section scalar/localized fields
    public string? TechBannerImage { get; set; }
    public LocalizedText StatsNote { get; set; } = new();
    public string? EnquiryBgImage { get; set; }
    public LocalizedText EnquiryTitle { get; set; } = new();
    public LocalizedText EnquirySub { get; set; } = new();
    public LocalizedText EnquiryLead { get; set; } = new();

    public List<VehicleImage> Images { get; set; } = new();
    public List<Trim> Trims { get; set; } = new();
    public List<SpecGroup> SpecGroups { get; set; } = new();
    public List<ColorOption> Colors { get; set; } = new();
    public List<FeatureSection> Features { get; set; } = new();

    // Rich-section collections
    public List<SectionHeading> Headings { get; set; } = new();
    public List<StatItem> Stats { get; set; } = new();
    public List<SliderGroup> Sliders { get; set; } = new();
    public List<GalleryTab> GalleryTabs { get; set; } = new();
    public List<CardItem> Cards { get; set; } = new();
    public List<SafetyToggle> SafetyToggles { get; set; } = new();
    public List<WarrantyLink> WarrantyLinks { get; set; } = new();
    public QualityBlock? Quality { get; set; }
}
