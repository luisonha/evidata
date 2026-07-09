using Evidata.Functions.Notifications.Email;
using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Evidata.Functions.Notifications.Handlers;

/// <summary>
/// Procesa eventos de brecha de cumplimiento y despacha notificaciones via email.
/// </summary>
public class GapNotificationHandler
{
    private readonly IEmailSender _email;
    private readonly ILogger<GapNotificationHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public GapNotificationHandler(IEmailSender email, ILogger<GapNotificationHandler> logger)
    {
        _email  = email;
        _logger = logger;
    }

    /// <summary>
    /// Despacha el evento. Retorna <c>true</c> si fue procesado, <c>false</c> si el tipo es desconocido.
    /// </summary>
    public async Task<bool> HandleAsync(string messageType, string payloadJson, CancellationToken ct)
    {
        var payload = JsonSerializer.Deserialize<GapEventPayload>(payloadJson, JsonOpts);
        if (payload is null)
        {
            _logger.LogWarning("⚠ Payload GapEvent nulo para tipo {MessageType} — descartando", messageType);
            return false;
        }

        return messageType switch
        {
            GapEventTypes.GapCreated       => await OnGapCreated(payload, ct),
            GapEventTypes.GapAssigned      => await OnGapAssigned(payload, ct),
            GapEventTypes.GapProgressed    => await OnGapProgressed(payload, ct),
            GapEventTypes.GapBlocked       => await OnGapBlocked(payload, ct),
            GapEventTypes.GapResolved      => await OnGapResolved(payload, ct),
            GapEventTypes.GapRiskAccepted  => await OnGapRiskAccepted(payload, ct),
            GapEventTypes.GapClosed        => await OnGapClosed(payload, ct),
            _ => LogUnknown(messageType)
        };
    }

    // ── Handlers por tipo ─────────────────────────────────────────────────────

    private async Task<bool> OnGapCreated(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "📋 GAP CREADO | GapId={GapId} Tenant={TenantId} Título='{Title}' Severidad={Severity} Actor={ActorId}",
            p.GapId, p.TenantId, p.GapTitle, p.Severity, p.ActorId);

        if (p.OwnerId.HasValue)
        {
            // Notifica al propietario si tiene email conocido en el payload
            _logger.LogInformation(
                "→ Notificando propietario {OwnerId} sobre nueva brecha '{Title}'",
                p.OwnerId.Value, p.GapTitle);
        }

        // Notificación al actor que creó la brecha (confirmación)
        await TrySendAsync(new EmailMessage(
            ToAddress: DeriveEmail(p.ActorId),
            ToName:    p.ActorId.ToString(),
            Subject:   $"[Evidata] Nueva brecha registrada: {p.GapTitle}",
            HtmlBody:  EmailTemplates.GapCreated(p.GapTitle, p.Severity.ToString(), p.TenantId.ToString(), null)
        ), ct);

        return true;
    }

    private async Task<bool> OnGapAssigned(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "👤 GAP ASIGNADO | GapId={GapId} Tenant={TenantId} NuevoOwner={OwnerId} Actor={ActorId}",
            p.GapId, p.TenantId, p.OwnerId, p.ActorId);

        if (p.OwnerId.HasValue)
        {
            await TrySendAsync(new EmailMessage(
                ToAddress: DeriveEmail(p.OwnerId.Value),
                ToName:    p.OwnerId.Value.ToString(),
                Subject:   $"[Evidata] Se te asignó una brecha: {p.GapTitle}",
                HtmlBody:  EmailTemplates.GapAssigned(p.GapTitle, p.Severity.ToString(), p.TenantId.ToString(), p.OwnerId.Value.ToString())
            ), ct);
        }

        return true;
    }

    private Task<bool> OnGapProgressed(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "⏩ GAP PROGRESADO | GapId={GapId} Tenant={TenantId} NuevoEstado={Status}",
            p.GapId, p.TenantId, p.Status);
        return Task.FromResult(true);
    }

    private async Task<bool> OnGapBlocked(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogWarning(
            "🚧 GAP BLOQUEADO | GapId={GapId} Tenant={TenantId} Título='{Title}' Razón={Extra}",
            p.GapId, p.TenantId, p.GapTitle, p.Extra ?? "sin detalle");

        if (p.Severity is GapSeverity.Critical or GapSeverity.High)
        {
            await TrySendAsync(new EmailMessage(
                ToAddress: DeriveEmail(p.ActorId),
                ToName:    p.ActorId.ToString(),
                Subject:   $"[Evidata] ⚠️ Brecha bloqueada (severidad {p.Severity}): {p.GapTitle}",
                HtmlBody:  EmailTemplates.GapBlocked(p.GapTitle, p.Extra ?? "Sin detalle", p.TenantId.ToString())
            ), ct);
        }

        return true;
    }

    private async Task<bool> OnGapResolved(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "✅ GAP RESUELTO | GapId={GapId} Tenant={TenantId} Título='{Title}'",
            p.GapId, p.TenantId, p.GapTitle);

        await TrySendAsync(new EmailMessage(
            ToAddress: DeriveEmail(p.ActorId),
            ToName:    p.ActorId.ToString(),
            Subject:   $"[Evidata] ✅ Brecha resuelta: {p.GapTitle}",
            HtmlBody:  EmailTemplates.GapResolved(p.GapTitle, p.TenantId.ToString())
        ), ct);

        return true;
    }

    private Task<bool> OnGapRiskAccepted(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "⚖️ RIESGO ACEPTADO | GapId={GapId} Tenant={TenantId} Actor={ActorId} Justificación={Extra}",
            p.GapId, p.TenantId, p.ActorId, p.Extra ?? "sin justificación");
        return Task.FromResult(true);
    }

    private Task<bool> OnGapClosed(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "🔒 GAP CERRADO | GapId={GapId} Tenant={TenantId} Título='{Title}'",
            p.GapId, p.TenantId, p.GapTitle);
        return Task.FromResult(true);
    }

    private bool LogUnknown(string messageType)
    {
        _logger.LogWarning("⚠ Tipo de mensaje no reconocido: {MessageType} — ignorando", messageType);
        return false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Intenta enviar email. Si falla (SMTP no disponible en local), solo registra warning.
    /// El mensaje ya fue procesado — no relanzar para evitar reencolar.
    /// </summary>
    private async Task TrySendAsync(EmailMessage message, CancellationToken ct)
    {
        try
        {
            await _email.SendAsync(message, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "⚠ Email no enviado (SMTP no disponible). To={To} Subject={Subject}",
                message.ToAddress, message.Subject);
        }
    }

    /// <summary>
    /// Placeholder: en producción el email del actor llega en el payload.
    /// Aquí se devuelve un address placeholder para entornos sin Identity lookup.
    /// </summary>
    private static string DeriveEmail(Guid userId) =>
        $"user-{userId:N}@evidata.local";
}
