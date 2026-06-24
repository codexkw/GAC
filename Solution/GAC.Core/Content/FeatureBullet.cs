namespace GAC.Core.Content;

public class FeatureBullet : IOrderable
{
    public int Id { get; set; }
    public int FeatureSectionId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
