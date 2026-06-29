namespace GAC.Core.Content;

public class PromoCampaign
{
    public int Id { get; set; }
    public int PromoSectionId { get; set; }
    public LocalizedText Text { get; set; } = new();
    public int SortOrder { get; set; }
}
