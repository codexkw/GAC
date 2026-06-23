namespace GAC.Core.Content;

public class WarrantyLink : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public string Url { get; set; } = "";
    public int SortOrder { get; set; }
}
