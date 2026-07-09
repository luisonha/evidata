using Evidata.Functions.Notifications.Email;
using Evidata.Modules.ProcessingInventory.Application.Notifications;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Evidata.Functions.Notifications.Handlers;

/// <summary>
/// Procesa eventos RAT (rat.*) y despacha notificaciones de email al actor
/// y/o al responsable del tratamiento.
/// </summary>
public sealed class RatNotificationHandler
{
    private readonly IEmailSender _email;
    private readonly ILogger<RatNotificationHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public RatNotificationHandler(IEmailSender email, ILogger<RatNotificationHandler> logger)
    {
        _email  = email;
        _logger = logger;
    }

    /// <returns><c>true</c> si el tipo fue reconocido y procesado.</returns>
    public async Task<bool> HandleAsync(string messageType, string payloadJson, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<RatEventPayload>(payloadJson, JsonOpts);
        if (payload is null)
        {
            _logger.LogWarning("⚠ Payload RatEvent nulo para tipo {Type} — descartando", messageType);
            return false;
        }

        return messageType switch
        {
            RatEventTypes.RatSubmittedForReview => await OnSubmittedForReview(payload, ct),
            RatEventTypes.RatApproved           => await OnApproved(payload, ct),
            RatEventTypes.RatRejected           => await OnRejected(payload, ct),
            RatEventTypes.RatArchived           => OnArchived(payload),
            _                                   => LogUnknown(messageType)
        };
    }

    // ── Handlers ─────────────────────────────────────────────────────────────

    private async Task<bool> OnSubmittedForReview(RatEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "🔍 RAT ENVIADO A REVISIÓN | RatId={Id} Tenant={Tenant} Nombre='{Name}' Actor={Actor}",
            p.ActivityId, p.TenantId, p.ActivityName, p.ActorId);

        await _email.SendAsync(new EmailMessage(
            ToAddress: p.ActorEmail,
            ToName:    p.ActorEmail,
            Subject:   $"[Evidata] RAT enviado para revisión: {p.ActivityName}",
            HtmlBody:  EmailTemplates.RatSubmittedForReview(p.ActivityName, p.TenantId.ToString(), p.ActorEmail)
        ), ct);

        return true;
    }

    private async Task<bool> OnApproved(RatEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "✅ RAT APROBADO | RatId={Id} Tenant={Tenant} Nombre='{Name}' Actor={Actor}",
            p.ActivityId, p.TenantId, p.ActivityName, p.ActorId);

        await _email.SendAsync(new EmailMessage(
            ToAddress: p.ActorEmail,
            ToName:    p.ActorEmail,
            Subject:   $"[Evidata] RAT aprobado: {p.ActivityName}",
            HtmlBody:  EmailTemplates.RatApproved(p.ActivityName, p.TenantId.ToString(), p.ActorEmail)
        ), ct);

        return true;
    }

    private async Task<bool> OnRejected(RatEventPayload p, CancellationToken ct)
    {
        _logger.LogWarning(
            "❌ RAT RECHAZADO | RatId={Id} Tenant={Tenant} Nombre='{Name}' Razón={Reason}",
            p.ActivityId, p.TenantId, p.ActivityName, p.Reason ?? "sin razón");

        await _email.SendAsync(new EmailMessage(
            ToAddress: p.ActorEmail,
            ToName:    p.ActorEmail,
            Subject:   $"[Evidata] RAT requiere correcciones: {p.ActivityName}",
            HtmlBody:  EmailTemplates.RatRejected(p.ActivityName, p.TenantId.ToString(), p.ActorEmail, p.Reason ?? "Sin detalle")
        ), ct);

        return true;
    }

    private bool OnArchived(RatEventPayload p)
    {
        _logger.LogInformation(
            "📦 RAT ARCHIVADO | RatId={Id} Tenant={Tenant} Nombre='{Name}'",
            p.ActivityId, p.TenantId, p.ActivityName);
        return true;
    }

    private bool LogUnknown(string messageType)
    {
        _logger.LogWarning("⚠ Tipo RAT no reconocido: {Type} — ignorando", messageType);
        return false;
    }
}
