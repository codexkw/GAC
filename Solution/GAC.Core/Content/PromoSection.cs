namespace GAC.Core.Content;

public class PromoSection
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Description { get; set; } = new();
    public LocalizedText CtaText { get; set; } = new();
    public string? CtaLink { get; set; }
    public List<PromoCampaign> Campaigns { get; set; } = new();
}
