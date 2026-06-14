namespace GAC.Core.Content;

public class ContentPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsVisible { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText MetaTitle { get; set; } = new();
    public LocalizedText MetaDescription { get; set; } = new();
    public List<ContentSection> Sections { get; set; } = new();
}
