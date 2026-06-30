namespace GAC.Core.Content;

// One service-interval row (the left-hand column label, e.g. "5,000 KM/6 Month").
public class CostServiceRow
{
    public int Id { get; set; }
    public int CostOfServicePageId { get; set; }
    public LocalizedText Label { get; set; } = new();
    public int SortOrder { get; set; }
}
