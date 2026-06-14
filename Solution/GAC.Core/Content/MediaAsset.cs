namespace GAC.Core.Content;

public class MediaAsset
{
    public int Id { get; set; }
    public string Path { get; set; } = "";
    public string? OriginalFileName { get; set; }
    public LocalizedText Alt { get; set; } = new();
    public DateTimeOffset UploadedAt { get; set; }
}
