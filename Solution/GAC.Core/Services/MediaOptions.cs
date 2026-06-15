namespace GAC.Core.Services;

public class MediaOptions
{
    // Absolute filesystem folder where uploads are written. Defaults to wwwroot/uploads (set in Program.cs).
    public string Root { get; set; } = "";
    // Public URL prefix that maps to Root (default "/uploads").
    public string PublicPrefix { get; set; } = "/uploads";
    public long MaxBytes { get; set; } = 5 * 1024 * 1024;
}
