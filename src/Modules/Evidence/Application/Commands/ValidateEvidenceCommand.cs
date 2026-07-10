namespace Evidata.Modules.Evidence.Application.Commands;

/// <summary>
/// Comando para validar o rechazar evidencia.
/// 
/// P1-011a: Auditoría instrumentada con:
/// - AuditEventType.ValidateEvidence (AUD-EV-001) en aprobación exitosa
/// - AuditEventType.RejectEvidence (AUD-EV-002) en rechazo
/// - result: Success si fue autorizado y aplicado, Blocked si SEC-EV-001 bloqueó
/// - correlationId propagado desde X-Correlation-Id header
/// - metadata: evidenceRequirementId, reviewDomain, resultado de validación
/// </summary>
public sealed record ValidateEvidenceCommand(
    Guid TenantId,
    Guid EvidenceValidationId,
    string Action,  // "Validate", "Reject", "MarkInsufficient"
    string? Comment,
    Guid ValidatedBy);

/// <summary>
/// Comando para rechazar evidencia.
/// 
/// P1-011a: Auditoría instrumentada con AUD-EV-002.
/// </summary>
public sealed record RejectEvidenceCommand(
    Guid TenantId,
    Guid EvidenceValidationId,
    string? Comment,
    Guid RejectedBy);

/// <summary>
/// DTO con resultado de validación para respuesta de API.
/// </summary>
public sealed record EvidenceValidationResultDto(
    Guid Id,
    Guid EvidenceRequirementId,
    Guid? EvidenceId,
    string Status,
    string? ValidationComment,
    Guid? ValidatedBy,
    DateTimeOffset? ValidatedAt);
