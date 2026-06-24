namespace GAC.Core.Content;

public class SafetyToggle : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Title { get; set; } = new();
    public string? ImagePath { get; set; }
    public LocalizedText Strap { get; set; } = new();
    public LocalizedText Content { get; set; } = new();
    public int SortOrder { get; set; }
}
