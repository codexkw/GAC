namespace GAC.Core.Content;

public class NewsArticle
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public DateOnly PublishedOn { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Excerpt { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
