namespace GAC.Core.Content;

// One brand's row in the Extended Warranty table. Columns are fixed; the
// brand name is plain text (proper noun) and the four attribute cells are
// bilingual. PolicyUrl is a link or an uploaded-PDF path.
public class WarrantyBrandRow
{
    public int Id { get; set; }
    public int WarrantyPageId { get; set; }
    public string Brand { get; set; } = "";
    public LocalizedText ManufacturerWarranty { get; set; } = new();
    public LocalizedText ManufacturerRoadside { get; set; } = new();
    public LocalizedText ExtendedWarranty { get; set; } = new();
    public LocalizedText ExtendedRoadside { get; set; } = new();
    public string? PolicyUrl { get; set; }
    public int SortOrder { get; set; }
}
