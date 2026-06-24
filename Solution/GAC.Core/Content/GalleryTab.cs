namespace GAC.Core.Content;

public class GalleryTab : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public int SortOrder { get; set; }
    public List<GalleryImage> Images { get; set; } = new();
}
