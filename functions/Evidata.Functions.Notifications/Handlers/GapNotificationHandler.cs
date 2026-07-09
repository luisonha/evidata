using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Evidata.Functions.Notifications.Handlers;

/// <summary>
/// Procesa eventos de brecha de cumplimiento y despacha la notificación adecuada.
///
/// Entrega actual: log estructurado con todos los campos relevantes.
/// Extensión futura: inyectar IEmailSender / IPushNotifier (SendGrid, ACS, etc.)
/// </summary>
public class GapNotificationHandler
{
    private readonly ILogger<GapNotificationHandler> _logger;

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    public GapNotificationHandler(ILogger<GapNotificationHandler> logger)
    {
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

    private Task<bool> OnGapCreated(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "📋 GAP CREADO | GapId={GapId} Tenant={TenantId} Título='{Title}' Severidad={Severity} Actor={ActorId}",
            p.GapId, p.TenantId, p.GapTitle, p.Severity, p.ActorId);

        // Notifica al propietario asignado si existe
        if (p.OwnerId.HasValue)
            _logger.LogInformation(
                "→ Notificando propietario {OwnerId} sobre nueva brecha '{Title}'",
                p.OwnerId.Value, p.GapTitle);

        return Task.FromResult(true);
    }

    private Task<bool> OnGapAssigned(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "👤 GAP ASIGNADO | GapId={GapId} Tenant={TenantId} NuevoOwner={OwnerId} Actor={ActorId}",
            p.GapId, p.TenantId, p.OwnerId, p.ActorId);

        if (p.OwnerId.HasValue)
            _logger.LogInformation(
                "→ Notificando nuevo propietario {OwnerId}: asignado a brecha '{Title}'",
                p.OwnerId.Value, p.GapTitle);

        return Task.FromResult(true);
    }

    private Task<bool> OnGapProgressed(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "⏩ GAP PROGRESADO | GapId={GapId} Tenant={TenantId} NuevoEstado={Status}",
            p.GapId, p.TenantId, p.Status);
        return Task.FromResult(true);
    }

    private Task<bool> OnGapBlocked(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogWarning(
            "🚧 GAP BLOQUEADO | GapId={GapId} Tenant={TenantId} Título='{Title}' Razón={Extra}",
            p.GapId, p.TenantId, p.GapTitle, p.Extra ?? "sin detalle");

        // Brecha bloqueada: severidad Critical/High notifica a responsables del tenant
        if (p.Severity is GapSeverity.Critical or GapSeverity.High)
            _logger.LogWarning(
                "→ Alerta de alta severidad enviada para brecha bloqueada '{Title}'", p.GapTitle);

        return Task.FromResult(true);
    }

    private Task<bool> OnGapResolved(GapEventPayload p, CancellationToken ct)
    {
        _logger.LogInformation(
            "✅ GAP RESUELTO | GapId={GapId} Tenant={TenantId} Título='{Title}'",
            p.GapId, p.TenantId, p.GapTitle);
        return Task.FromResult(true);
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
}
