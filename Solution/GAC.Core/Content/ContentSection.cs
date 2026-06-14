namespace GAC.Core.Content;

public class ContentSection
{
    public int Id { get; set; }
    public int ContentPageId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
