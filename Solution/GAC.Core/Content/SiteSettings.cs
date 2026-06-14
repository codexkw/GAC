namespace GAC.Core.Content;

public class SiteSettings
{
    public int Id { get; set; }
    public string? Phone { get; set; }
    public string? WhatsApp { get; set; }
    public string? Email { get; set; }
    public string? InstagramUrl { get; set; }
    public string? FacebookUrl { get; set; }
    public string? TiktokUrl { get; set; }
    public string? SnapchatUrl { get; set; }
    public string? XUrl { get; set; }
    public LocalizedText FooterTagline { get; set; } = new();
}
