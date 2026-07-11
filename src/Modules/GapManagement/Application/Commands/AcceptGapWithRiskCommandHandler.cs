using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Evidata.Modules.GapManagement.Application.Commands;

/// <summary>
/// Handler para AcceptGapWithRiskCommand.
/// 
/// P1-011b: Instrumenta auditoría para aceptación de brecha con riesgo.
/// - Autentica que el usuario tiene el rol correcto (SEC-GAP-001: TenantOwner/ComplianceAdmin)
/// - Valida que la justificación no está vacía (fail-closed)
/// - Ejecuta la transición de estado en ComplianceGap
/// - Registra AUD-GAP-001 (AcceptGapWithRisk)
/// - Propaga correlationId desde X-Correlation-Id header
/// - result: Success si fue autorizado, Blocked si no tiene permiso o falta justificación
/// </summary>
public sealed class AcceptGapWithRiskCommandHandler(
    GapManagementDbContext db,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<AcceptGapWithRiskResultDto> HandleAsync(
        AcceptGapWithRiskCommand cmd,
        CancellationToken ct = default)
    {
        // 1. Obtener el gap
        var gap = await db.ComplianceGaps
            .FirstOrDefaultAsync(g => g.Id == cmd.ComplianceGapId && g.TenantId == cmd.TenantId, ct)
            ?? throw new InvalidOperationException($"ComplianceGap {cmd.ComplianceGapId} no encontrada.");

        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();

        // 2. SEC-GAP-001 (fail-closed): Validar rol (solo TenantOwner/ComplianceAdmin)
        var hasPermission = CheckAcceptGapPermission();
        
        if (!hasPermission)
        {
            // Registrar evento bloqueado por permisos
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.AcceptedBy,
                AuditEventType.AcceptGapWithRisk.ToString(),
                "ComplianceGap",
                cmd.ComplianceGapId,
                AuditEventResult.Blocked,
                correlationId,
                new Dictionary<string, object?>
                {
                    { "complianceGapId", gap.Id },
                    { "blockedReason", "Insufficient permissions - only TenantOwner/ComplianceAdmin can accept gap with risk" }
                },
                severity: AuditSeverity.Warning,
                ct: ct);

            throw new UnauthorizedAccessException(
                "Solo TenantOwner o ComplianceAdmin pueden aceptar una brecha con riesgo.");
        }

        // 3. SEC-GAP-001 (fail-closed): Validar justificación no vacía
        if (string.IsNullOrWhiteSpace(cmd.Justification))
        {
            // Registrar evento bloqueado por falta de justificación
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.AcceptedBy,
                AuditEventType.AcceptGapWithRisk.ToString(),
                "ComplianceGap",
                cmd.ComplianceGapId,
                AuditEventResult.Blocked,
                correlationId,
                new Dictionary<string, object?>
                {
                    { "complianceGapId", gap.Id },
                    { "blockedReason", "Missing required justification for risk acceptance" }
                },
                severity: AuditSeverity.Warning,
                ct: ct);

            throw new ArgumentException(
                "La justificación para aceptación de riesgo es obligatoria.",
                nameof(cmd.Justification));
        }

        // 4. Ejecutar transición de estado
        try
        {
            gap.AcceptRisk(cmd.Justification, cmd.AcceptedBy);
            db.ComplianceGaps.Update(gap);
            await db.SaveChangesAsync(ct);

            // 5. Registrar evento de auditoría exitoso
            var metadata = new Dictionary<string, object?>
            {
                { "complianceGapId", gap.Id },
                { "gapSeverity", gap.Severity.ToString() },
                { "justificationLength", cmd.Justification.Length },
                { "newStatus", gap.Status.ToString() }
            };

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.AcceptedBy,
                AuditEventType.AcceptGapWithRisk.ToString(),
                "ComplianceGap",
                cmd.ComplianceGapId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                severity: AuditSeverity.Critical,
                ct: ct);

            return new AcceptGapWithRiskResultDto(
                gap.Id,
                gap.Status.ToString(),
                gap.RiskAcceptanceJustification,
                gap.LastModifiedBy,
                gap.LastModifiedAt);
        }
        catch (InvalidOperationException ex)
        {
            // Error técnico
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.AcceptedBy,
                AuditEventType.AcceptGapWithRisk.ToString(),
                "ComplianceGap",
                cmd.ComplianceGapId,
                AuditEventResult.Failure,
                correlationId,
                new Dictionary<string, object?>
                {
                    { "complianceGapId", gap.Id },
                    { "error", ex.Message }
                },
                severity: AuditSeverity.Critical,
                ct: ct);

            throw;
        }
    }

    /// <summary>
    /// SEC-GAP-001: Valida que el usuario es TenantOwner o ComplianceAdmin.
    /// Fail-closed: si el rol no es exacto, retorna false.
    /// </summary>
    private bool CheckAcceptGapPermission()
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

        return userClaims.Contains("TenantOwner") || userClaims.Contains("tenant-owner") ||
               userClaims.Contains("ComplianceAdmin") || userClaims.Contains("compliance-admin");
    }
}
