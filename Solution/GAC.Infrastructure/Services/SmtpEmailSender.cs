using GAC.Core.Content;
using GAC.Core.Services;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GAC.Infrastructure.Services;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpOptions _opt;
    private readonly ILogger<SmtpEmailSender> _log;

    public SmtpEmailSender(IOptions<SmtpOptions> opt, ILogger<SmtpEmailSender> log)
    { _opt = opt.Value; _log = log; }

    /// <summary>
    /// Splits the configured notify list (comma- or semicolon-separated) into individual
    /// recipient addresses, trimming whitespace and dropping blanks. Falls back to FromEmail
    /// when no notify address is configured.
    /// </summary>
    public static IReadOnlyList<string> ParseRecipients(string? adminNotify, string? fromEmail)
    {
        var source = string.IsNullOrWhiteSpace(adminNotify) ? fromEmail : adminNotify;
        if (string.IsNullOrWhiteSpace(source)) return Array.Empty<string>();
        return source
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToList();
    }

    public async Task SendLeadNotificationAsync(Lead lead, string formTitle, CancellationToken ct = default)
    {
        var recipients = ParseRecipients(_opt.AdminNotifyEmail, _opt.FromEmail);
        if (!_opt.Enabled || string.IsNullOrWhiteSpace(_opt.Host) || recipients.Count == 0)
        {
            _log.LogInformation("SMTP disabled or unconfigured — skipping lead notification for {FormType}.", lead.FormType);
            return;
        }

        try
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(_opt.FromName, _opt.FromEmail));
            foreach (var r in recipients) msg.To.Add(MailboxAddress.Parse(r));
            if (!string.IsNullOrWhiteSpace(lead.Email))
                msg.ReplyTo.Add(new MailboxAddress("", lead.Email));
            msg.Subject = $"New {formTitle} enquiry — {lead.Name}";

            var body = new System.Text.StringBuilder();
            body.AppendLine($"Form: {formTitle} ({lead.FormType})");
            body.AppendLine($"Name: {lead.Name}");
            body.AppendLine($"Phone: {lead.Phone}");
            body.AppendLine($"Email: {lead.Email}");
            if (!string.IsNullOrWhiteSpace(lead.Branch)) body.AppendLine($"Branch: {lead.Branch}");
            if (lead.PreferredDate is not null) body.AppendLine($"Preferred date: {lead.PreferredDate}");
            if (!string.IsNullOrWhiteSpace(lead.SourcePage)) body.AppendLine($"Source page: {lead.SourcePage}");
            if (!string.IsNullOrWhiteSpace(lead.Message)) { body.AppendLine(); body.AppendLine(lead.Message); }
            msg.Body = new TextPart("plain") { Text = body.ToString() };

            using var client = new SmtpClient();
            var secure = _opt.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
            await client.ConnectAsync(_opt.Host, _opt.Port, secure, ct);
            if (!string.IsNullOrWhiteSpace(_opt.Username))
                await client.AuthenticateAsync(_opt.Username, _opt.Password, ct);
            await client.SendAsync(msg, ct);
            await client.DisconnectAsync(true, CancellationToken.None);
            _log.LogInformation("Lead notification sent for {FormType} to {To}.", lead.FormType, string.Join(", ", recipients));
        }
        catch (Exception ex)
        {
            // Never break the submission because email failed.
            _log.LogError(ex, "Failed to send lead notification for {FormType}.", lead.FormType);
        }
    }
}
