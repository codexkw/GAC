namespace GAC.Infrastructure.Services;

/// <summary>Bound from the "Smtp" config section. Secrets live ONLY in appsettings.Development.json (gitignored).</summary>
public class SmtpOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true; // STARTTLS on 587
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "GAC Motors";
    public string AdminNotifyEmail { get; set; } = "";
}
