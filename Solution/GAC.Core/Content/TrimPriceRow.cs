namespace GAC.Core.Content;

public class TrimPriceRow : IOrderable
{
    public int Id { get; set; }
    public int TrimId { get; set; }
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
