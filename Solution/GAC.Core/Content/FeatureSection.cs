namespace GAC.Core.Content;

public class FeatureSection : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Heading { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
    public string? ImagePath { get; set; }
    public FeatureLayout Layout { get; set; } = FeatureLayout.ImageLeft;
    public int SortOrder { get; set; }
}
