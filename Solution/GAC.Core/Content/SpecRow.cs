namespace GAC.Core.Content;

public class SpecRow
{
    public int Id { get; set; }
    public int SpecGroupId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public LocalizedText Value { get; set; } = new();
    public int SortOrder { get; set; }
}
