namespace GAC.Core.Content;

public class Trim : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Name { get; set; } = new();
    public decimal? Price { get; set; }
    public LocalizedText Highlights { get; set; } = new();
    public string? SpecPdf { get; set; }
    public int SortOrder { get; set; }
}
