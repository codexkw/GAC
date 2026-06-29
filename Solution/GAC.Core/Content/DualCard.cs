namespace GAC.Core.Content;

public class DualCard
{
    public int Id { get; set; }
    public int HomePageId { get; set; }
    public string ImagePath { get; set; } = "";
    public string? Link { get; set; }
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Description { get; set; } = new();
    public LocalizedText ButtonText { get; set; } = new();
    public int SortOrder { get; set; }
}
