namespace GAC.Core.Content;

public class SpecGroup
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public int SortOrder { get; set; }
    public List<SpecRow> Rows { get; set; } = new();
}
