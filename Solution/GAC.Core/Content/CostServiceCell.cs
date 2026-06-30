namespace GAC.Core.Content;

// One price cell in a car-model column, aligned by SortOrder/index to the page's
// ordered Rows. Value is plain text to preserve formatting ("1,005", "—", etc.).
public class CostServiceCell
{
    public int Id { get; set; }
    public int CostServiceModelId { get; set; }
    public int SortOrder { get; set; }
    public string? Value { get; set; }
}
