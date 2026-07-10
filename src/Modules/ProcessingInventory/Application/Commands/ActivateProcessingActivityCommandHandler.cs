using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Handler for ActivateProcessingActivityCommand.
/// 
/// P1-012: Formaliza la transición Activate() en ProcessingActivity (AUD-ACT-001 / SEC-ACT-001).
/// 
/// Autorización:
/// - Valida que solo TenantOwner o ComplianceAdmin pueden activar (SEC-ACT-001)
/// - Retorna 403 InsufficientPermissions si el usuario carece del rol
/// 
/// Lógica de negocio:
/// - Valida que la versión esté en estado Approved
/// - Busca la versión activa anterior (si existe) y la marca como Deprecated
/// - Actualiza activeVersionId del procesamiento principal
/// - Marca la nueva versión como Active
/// - Genera AuditEvent ProcessingActivityActivated
/// 
/// Fail-closed: Si hay error en autorización o estado, retorna 422 InvalidStatusTransition
/// o 403 InsufficientPermissions. Auditoría registra el resultado.
/// </summary>
public sealed class ActivateProcessingActivityCommandHandler(
    ProcessingInventoryDbContext db,
    SecurityDbContext securityDb,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<ProcessingActivityDto> HandleAsync(
        ActivateProcessingActivityCommand cmd,
        CancellationToken ct = default)
    {
        var activity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Id == cmd.ProcessingActivityId, ct)
            ?? throw new InvalidOperationException(
                $"ProcessingActivity {cmd.ProcessingActivityId} not found in tenant {cmd.TenantId}");

        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();

        // ── Autorización: SEC-ACT-001 ──────────────────────────────────────────────
        
        // Check if user has TenantOwner or ComplianceAdmin role
        var userRoles = await securityDb.UserRoleAssignments
            .Where(a => a.UserId == cmd.ActivatedBy && a.TenantId == cmd.TenantId)
            .Join(securityDb.Roles, a => a.RoleId, r => r.Id, (a, r) => r.Name)
            .Distinct()
            .ToListAsync(ct);

        var hasAdminRole = userRoles.Contains("TenantOwner") || userRoles.Contains("ComplianceAdmin");

        if (!hasAdminRole)
        {
            // Authorization denied — log audit event with Denied result
            var denyMetadata = new Dictionary<string, object?>
            {
                { "activityName", activity.Name },
                { "currentStatus", activity.Status.ToString() },
                { "version", activity.Version },
                { "userRoles", string.Join(", ", userRoles) }
            };

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ActivatedBy,
                AuditEventType.Activate.ToString(),
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                AuditEventResult.Blocked,  // Denied = Blocked in audit
                correlationId,
                denyMetadata,
                ct: ct);

            throw new UnauthorizedAccessException(
                "Only TenantOwner or ComplianceAdmin can activate a ProcessingActivity.");
        }

        // ── Validación de estado ────────────────────────────────────────────────────

        var metadata = new Dictionary<string, object?>
        {
            { "activityName", activity.Name },
            { "currentStatus", activity.Status.ToString() },
            { "version", activity.Version }
        };

        try
        {
            // Validate that activity is in Approved state
            if (activity.Status != ProcessingActivityStatus.Approved)
            {
                metadata["error"] = $"Cannot activate from {activity.Status} state. Only Approved versions can be activated.";

                await auditService.LogAsync(
                    cmd.TenantId,
                    cmd.ActivatedBy,
                    AuditEventType.Activate.ToString(),
                    "ProcessingActivity",
                    cmd.ProcessingActivityId,
                    AuditEventResult.Blocked,  // Invalid state transition = Blocked
                    correlationId,
                    metadata,
                    ct: ct);

                throw new InvalidOperationException(
                    $"InvalidStatusTransition: Cannot activate ProcessingActivity in {activity.Status} state.");
            }

            // ── Deprecar versión anterior ───────────────────────────────────────────
            
            // Find the previous active version in this root treatment group
            // Search for any version that currently has Status = Active and version < current version
            ProcessingActivity? previousActive = null;

            if (activity.SupersedesId.HasValue)
            {
                // This is not the first version; look for the root activity and then find the previous active
                previousActive = await db.ProcessingActivities
                    .Where(a => a.TenantId == cmd.TenantId 
                        && a.Status == ProcessingActivityStatus.Active
                        && a.Version < activity.Version
                        && (a.Id == activity.SupersedesId || 
                            db.ProcessingActivities.Any(x => x.Id == a.SupersedesId && x.SupersedesId == activity.SupersedesId)))
                    .OrderByDescending(a => a.Version)
                    .FirstOrDefaultAsync(ct);
            }
            else
            {
                // This might be a first version or root; check if any version of this lineage is active
                previousActive = await db.ProcessingActivities
                    .Where(a => a.TenantId == cmd.TenantId
                        && a.Status == ProcessingActivityStatus.Active
                        && (a.Id == activity.Id || 
                            db.ProcessingActivities.Any(x => x.SupersedesId == activity.Id && x.Status == ProcessingActivityStatus.Active)))
                    .FirstOrDefaultAsync(ct);
            }

            // Activate the new version
            activity.Activate(cmd.ActivatedBy);

            // If there's a previous active version, deprecate it
            if (previousActive != null)
            {
                previousActive.SetAsDeprecated(cmd.ActivatedBy);
                db.ProcessingActivities.Update(previousActive);
                metadata["deprecatedVersionId"] = previousActive.Id;
                metadata["deprecatedVersion"] = previousActive.Version;
            }

            // Update the root activity's activeVersionId (should be the first version without SupersedesId)
            var rootActivity = activity;
            while (rootActivity.SupersedesId.HasValue)
            {
                var parent = await db.ProcessingActivities
                    .FirstOrDefaultAsync(a => a.Id == rootActivity.SupersedesId, ct);
                if (parent == null) break;
                rootActivity = parent;
            }

            rootActivity.ActiveVersionId = activity.Id;

            db.ProcessingActivities.Update(activity);
            db.ProcessingActivities.Update(rootActivity);
            await db.SaveChangesAsync(ct);

            metadata["activeVersionId"] = activity.Id;
            metadata["activatedVersion"] = activity.Version;

            // ── Auditoría: Success ──────────────────────────────────────────────────

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ActivatedBy,
                AuditEventType.Activate.ToString(),
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                ct: ct);

            return ProcessingActivityDto.From(activity);
        }
        catch (Exception ex) when (!(ex is UnauthorizedAccessException))
        {
            // Unexpected error — log as Failure
            metadata["errorMessage"] = ex.Message;

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ActivatedBy,
                AuditEventType.Activate.ToString(),
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                AuditEventResult.Failure,
                correlationId,
                metadata,
                ct: ct);

            throw;
        }
    }
}
