namespace GAC.Core.Content;

// Singleton aggregate backing the structured, admin-editable /cost-of-service page.
// The price table is a matrix: Rows = service intervals, Models = car-model columns,
// each model's Cells are aligned by SortOrder/index to the page's ordered Rows.
public class CostOfServicePage
{
    public int Id { get; set; }
    public LocalizedText Title { get; set; } = new();
    public LocalizedText ButtonLabel { get; set; } = new();
    public string? ButtonUrl { get; set; }                  // PDF path or external link
    public LocalizedText TableHeadLabel { get; set; } = new(); // first-column header
    public LocalizedText FooterNote { get; set; } = new();  // multiline → lines
    public List<CostServiceRow> Rows { get; set; } = new();
    public List<CostServiceModel> Models { get; set; } = new();
}
