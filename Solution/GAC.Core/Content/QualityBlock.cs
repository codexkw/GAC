namespace GAC.Core.Content;

public class QualityBlock
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string? MainImage { get; set; }
    public string? ThumbImage { get; set; }
    public LocalizedText Strapline { get; set; } = new();
    public LocalizedText Content { get; set; } = new();
}
