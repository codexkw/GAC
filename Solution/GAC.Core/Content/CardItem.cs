namespace GAC.Core.Content;

public class CardItem : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
