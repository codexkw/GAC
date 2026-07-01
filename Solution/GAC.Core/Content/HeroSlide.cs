namespace GAC.Core.Content;

public class HeroSlide
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public string? LogoImagePath { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Subheading { get; set; } = new();
    public LocalizedText CtaText { get; set; } = new();
    public string? CtaLink { get; set; }
    public int SortOrder { get; set; }
}
