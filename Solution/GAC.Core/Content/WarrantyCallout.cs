namespace GAC.Core.Content;

public class WarrantyCallout
{
    public int Id { get; set; }
    public int WarrantyPageId { get; set; }
    public LocalizedText Lead { get; set; } = new();
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
