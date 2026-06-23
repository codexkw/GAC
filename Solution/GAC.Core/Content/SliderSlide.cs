namespace GAC.Core.Content;

public class SliderSlide : IOrderable
{
    public int Id { get; set; }
    public int SliderGroupId { get; set; }
    public string? ImagePath { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public int SortOrder { get; set; }
}
