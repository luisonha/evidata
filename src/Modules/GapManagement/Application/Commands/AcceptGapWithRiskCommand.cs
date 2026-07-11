namespace Evidata.Modules.GapManagement.Application.Commands;

/// <summary>
/// Comando para aceptar una brecha con riesgo.
/// 
/// P1-011b: Auditoría instrumentada con:
/// - AuditEventType.AcceptGapWithRisk (AUD-GAP-001)
/// - result: Success si fue autorizado y aplicado, Blocked si SEC-GAP-001 bloqueó
/// - correlationId propagado desde X-Correlation-Id header
/// - metadata: complianceGapId, justificación (o referencia a ella), rol del actor
/// </summary>
public sealed record AcceptGapWithRiskCommand(
    Guid TenantId,
    Guid ComplianceGapId,
    string Justification,
    Guid AcceptedBy);

/// <summary>
/// DTO con resultado de aceptación de brecha con riesgo.
/// </summary>
public sealed record AcceptGapWithRiskResultDto(
    Guid Id,
    string Status,
    string? RiskAcceptanceJustification,
    Guid? LastModifiedBy,
    DateTimeOffset? LastModifiedAt);
