namespace GAC.Core.Content;

public class Offer
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public LocalizedText ButtonLabel { get; set; } = new();
    public string? ImagePath { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public int SortOrder { get; set; }
}
