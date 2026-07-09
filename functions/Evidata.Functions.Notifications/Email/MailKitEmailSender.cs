using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Evidata.Functions.Notifications.Email;

/// <summary>
/// Implementación SMTP vía MailKit.
/// Configuración esperada (appsettings / env vars):
///   Email:Host     → SMTP host  (default: localhost)
///   Email:Port     → SMTP port  (default: 1025 — Mailpit local)
///   Email:UseSsl   → bool       (default: false)
///   Email:From     → dirección origen (default: noreply@evidata.local)
///   Email:FromName → nombre origen    (default: Evidata)
/// </summary>
public sealed class MailKitEmailSender : IEmailSender
{
    private readonly string _host;
    private readonly int    _port;
    private readonly bool   _useSsl;
    private readonly string _from;
    private readonly string _fromName;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IConfiguration config, ILogger<MailKitEmailSender> logger)
    {
        _host     = config["Email:Host"]     ?? "localhost";
        _port     = int.TryParse(config["Email:Port"], out var p) ? p : 1025;
        _useSsl   = bool.TryParse(config["Email:UseSsl"], out var s) && s;
        _from     = config["Email:From"]     ?? "noreply@evidata.local";
        _fromName = config["Email:FromName"] ?? "Evidata";
        _logger   = logger;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_fromName, _from));
        mime.To.Add(new MailboxAddress(message.ToName, message.ToAddress));
        mime.Subject = message.Subject;

        var builder = new BodyBuilder
        {
            HtmlBody      = message.HtmlBody,
            TextBody      = message.PlainTextBody ?? StripHtml(message.HtmlBody)
        };
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port,
            _useSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.None, ct);

        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(quit: true, ct);

        _logger.LogInformation(
            "📧 Email enviado. To={To} Subject={Subject}",
            message.ToAddress, message.Subject);
    }

    private static string StripHtml(string html) =>
        System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", string.Empty);
}
