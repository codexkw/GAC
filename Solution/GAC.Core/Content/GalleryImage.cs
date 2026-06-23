namespace GAC.Core.Content;

public class GalleryImage : IOrderable
{
    public int Id { get; set; }
    public int GalleryTabId { get; set; }
    public string? ImagePath { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
