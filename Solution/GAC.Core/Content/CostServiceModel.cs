namespace GAC.Core.Content;

// One car-model column. Its Cells hold the price per interval, aligned by
// SortOrder/index to the page's ordered Rows.
public class CostServiceModel
{
    public int Id { get; set; }
    public int CostOfServicePageId { get; set; }
    public string Name { get; set; } = "";
    public int SortOrder { get; set; }
    public List<CostServiceCell> Cells { get; set; } = new();
}
