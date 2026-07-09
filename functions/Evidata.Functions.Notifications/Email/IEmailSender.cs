namespace Evidata.Functions.Notifications.Email;

/// <summary>
/// Abstracción de entrega de email. Permite intercambiar SMTP por ACS u otro proveedor.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
