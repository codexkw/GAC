namespace GAC.Core.Content;

public class WarrantyPage
{
    public int Id { get; set; }
    public string BannerImagePath { get; set; } = "";
    public LocalizedText BannerLabel { get; set; } = new();
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Intro { get; set; } = new();
    public string TermsImagePath { get; set; } = "";
    public LocalizedText TermsNote { get; set; } = new();
    public LocalizedText ExtendedHeading { get; set; } = new();
    public LocalizedText ExtendedIntro { get; set; } = new();
    public LocalizedText ExtendedTableHtml { get; set; } = new();   // legacy, unused after the structured table ships
    public List<WarrantyCallout> Callouts { get; set; } = new();

    // Structured Extended-Warranty brand table.
    public LocalizedText TableBrandHeader { get; set; } = new();
    public LocalizedText TableMfrWarrantyHeader { get; set; } = new();
    public LocalizedText TableMfrRoadsideHeader { get; set; } = new();
    public LocalizedText TableExtWarrantyHeader { get; set; } = new();
    public LocalizedText TableExtRoadsideHeader { get; set; } = new();
    public LocalizedText TablePolicyHeader { get; set; } = new();
    public List<WarrantyBrandRow> BrandRows { get; set; } = new();
}
