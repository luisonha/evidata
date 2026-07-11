using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Evidata.Modules.Evidence.Application.Commands;

/// <summary>
/// Handler para ValidateEvidenceCommand.
/// 
/// P1-011a: Instrumenta auditoría para validación/rechazo de evidencia.
/// - Autentica que el usuario tiene el rol correcto (SEC-EV-001)
/// - Ejecuta la transición de estado en EvidenceValidation
/// - Registra AUD-EV-001 (ValidateEvidence) o AUD-EV-002 (RejectEvidence)
/// - Propaga correlationId desde X-Correlation-Id header
/// - result: Success si fue autorizado, Blocked si no tiene permiso
/// </summary>
public sealed class ValidateEvidenceCommandHandler(
    EvidenceDbContext db,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<EvidenceValidationResultDto> HandleAsync(
        ValidateEvidenceCommand cmd, 
        CancellationToken ct = default)
    {
        // 1. Obtener la validación y el requirement
        var validation = await db.EvidenceValidations
            .Include(v => v.EvidenceRequirement)
            .FirstOrDefaultAsync(v => v.Id == cmd.EvidenceValidationId && v.TenantId == cmd.TenantId, ct)
            ?? throw new InvalidOperationException($"EvidenceValidation {cmd.EvidenceValidationId} no encontrada.");

        var requirement = validation.EvidenceRequirement;
        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();

        // 2. SEC-EV-001: Validar rol según reviewDomain (fail-closed)
        var hasPermission = CheckValidationPermission(requirement.ReviewDomain);
        
        if (!hasPermission)
        {
            // Registrar evento bloqueado
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ValidatedBy,
                AuditEventType.ValidateEvidence.ToString(),
                "EvidenceValidation",
                cmd.EvidenceValidationId,
                AuditEventResult.Blocked,
                correlationId,
                new Dictionary<string, object?>
                {
                    { "evidenceRequirementId", requirement.Id },
                    { "reviewDomain", requirement.ReviewDomain.ToString() },
                    { "rejectedReason", "Insufficient permissions for review domain" }
                },
                ct: ct);

            throw new UnauthorizedAccessException(
                $"Usuario no tiene permiso para validar evidencia de dominio {requirement.ReviewDomain}.");
        }

        // 3. Ejecutar transición según acción
        var metadata = new Dictionary<string, object?>
        {
            { "evidenceRequirementId", requirement.Id },
            { "reviewDomain", requirement.ReviewDomain.ToString() },
            { "action", cmd.Action },
            { "commentLength", (cmd.Comment?.Length ?? 0) }
        };

        AuditEventType auditEventType = AuditEventType.ValidateEvidence; // Default
        
        try
        {
            switch (cmd.Action.ToLower())
            {
                case "validate":
                    validation.Validate(cmd.Comment, cmd.ValidatedBy);
                    auditEventType = AuditEventType.ValidateEvidence;
                    metadata["outcome"] = "Approved";
                    break;

                case "reject":
                    validation.Reject(cmd.Comment, cmd.ValidatedBy);
                    auditEventType = AuditEventType.RejectEvidence;
                    metadata["outcome"] = "Rejected";
                    break;

                case "markinsufficient":
                    validation.MarkInsufficient(cmd.Comment, cmd.ValidatedBy);
                    auditEventType = AuditEventType.RejectEvidence;
                    metadata["outcome"] = "Insufficient";
                    break;

                default:
                    throw new ArgumentException($"Acción inválida: {cmd.Action}", nameof(cmd.Action));
            }

            db.EvidenceValidations.Update(validation);
            await db.SaveChangesAsync(ct);

            // 4. Registrar evento de auditoría exitoso
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ValidatedBy,
                auditEventType.ToString(),
                "EvidenceValidation",
                cmd.EvidenceValidationId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                ct: ct);

            return new EvidenceValidationResultDto(
                validation.Id,
                validation.EvidenceRequirementId,
                validation.EvidenceId,
                validation.Status.ToString(),
                validation.ValidationComment,
                validation.ValidatedBy,
                validation.ValidatedAt);
        }
        catch (InvalidOperationException ex)
        {
            // Error técnico (p.ej., estado inválido)
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ValidatedBy,
                auditEventType.ToString(),
                "EvidenceValidation",
                cmd.EvidenceValidationId,
                AuditEventResult.Failure,
                correlationId,
                new Dictionary<string, object?>
                {
                    { "evidenceRequirementId", requirement.Id },
                    { "error", ex.Message }
                },
                ct: ct);

            throw;
        }
    }

    /// <summary>
    /// SEC-EV-001: Valida que el usuario tiene el rol requerido según el reviewDomain.
    /// Fail-closed: si el rol no es exacto, retorna false.
    /// </summary>
    private bool CheckValidationPermission(ReviewDomain reviewDomain)
    {
        // En contexto real, se consultaría IPermissionService o similar.
        // Para MVP, se usa una simple comprobación de claims.
        // Este patrón se refina en P2 con RBAC service centralizado.

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            return false;

        var userClaims = httpContext.User.Claims
            .Where(c => c.Type == "roles" || c.Type == "role")
            .Select(c => c.Value)
            .ToList();

        return reviewDomain switch
        {
            ReviewDomain.Legal => userClaims.Contains("LegalReviewer") || userClaims.Contains("legal-reviewer"),
            ReviewDomain.Security => userClaims.Contains("SecurityReviewer") || userClaims.Contains("security-reviewer"),
            _ => false
        };
    }
}
