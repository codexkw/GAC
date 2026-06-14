namespace GAC.Core.Content;

public class ColorOption
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Name { get; set; } = new();
    public string Hex { get; set; } = "#000000";
    public string? ImagePath { get; set; }
    public int SortOrder { get; set; }
}
