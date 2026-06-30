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
    public LocalizedText ExtendedTableHtml { get; set; } = new();
    public List<WarrantyCallout> Callouts { get; set; } = new();
}
