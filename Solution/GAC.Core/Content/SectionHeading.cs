namespace GAC.Core.Content;

public class SectionHeading
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public SectionKey Key { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText Sub { get; set; } = new();
    public LocalizedText Body { get; set; } = new();
}
