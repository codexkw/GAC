namespace GAC.Core.Content;

public class Lead
{
    public int Id { get; set; }
    public FormType FormType { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.New;
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }
    public int? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }
    public DateOnly? PreferredDate { get; set; }
    public string? SourcePage { get; set; }
    public string? Branch { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
