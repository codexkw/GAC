namespace GAC.Core.Content;

public class SliderGroup : IOrderable
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public LocalizedText Eyebrow { get; set; } = new();
    public LocalizedText Title { get; set; } = new();
    public int SortOrder { get; set; }
    public List<SliderSlide> Slides { get; set; } = new();
}
