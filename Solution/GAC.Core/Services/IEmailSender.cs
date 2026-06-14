using GAC.Core.Content;

namespace GAC.Core.Services;

/// <summary>Best-effort notification of a new lead. Implementations must never throw to the caller.</summary>
public interface IEmailSender
{
    Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default);
}
