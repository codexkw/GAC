namespace GAC.Core.Content;

public class StatItem : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Value { get; set; } = new();
    public int SortOrder { get; set; }
}
