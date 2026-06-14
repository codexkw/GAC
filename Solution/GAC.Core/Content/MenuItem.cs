namespace GAC.Core.Content;

public class MenuItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public MenuItem? Parent { get; set; }
    public List<MenuItem> Children { get; set; } = new();
    public LocalizedText Label { get; set; } = new();
    public string? Url { get; set; }
    public int SortOrder { get; set; }
}
