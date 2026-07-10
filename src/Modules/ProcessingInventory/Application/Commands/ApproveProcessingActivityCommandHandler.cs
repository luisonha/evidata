using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Evidata.Modules.Security.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Handler for ApproveProcessingActivityCommand.
/// 
/// P1-013: Orchestrates full approval authorization (SEC-APP-001), business blockers, and audit
/// for ProcessingActivity version approval.
/// 
/// Autorización (RBAC):
/// - Valida que usuario pertenece al mismo tenant
/// - Valida que usuario tiene permiso "ApproveProcessingActivity"
/// - Valida que ProcessOwner NO puede aprobar su propio tratamiento (SEC-APP-001)
/// - Retorna 403 InsufficientPermissions si denegada
/// 
/// Lógica de negocio (Blockers):
/// - Valida que versión está en estado UnderReview
/// - Verifica RiskFlags.BlocksApproval (incluye CriticalGapOpen, MissingLegalBasisEvidence, etc.)
/// - Retorna 422 BlockingEvidenceMissing | CriticalGapOpen | VersionModifiedAfterReview si bloqueada
/// 
/// Auditoría:
/// - Genera AuditEvent ProcessingActivityApproved (success)
/// - Genera AuditEvent ApprovalBlocked (blocker)
/// - Genera AuditEvent AccessDenied o ApprovalDenied (authorization failure)
/// 
/// Nota sobre blockers implementados vs pendientes:
/// ✓ RiskFlags.BlocksApproval (CriticalGapOpen, MissingLegalBasisEvidence)
/// ✓ ProcessOwner cannot approve self (via ResourcePermissionsQueryService.IsBlocked_ApproveOwnActivity)
/// ✓ Status must be UnderReview
/// ⚠ BlockingEvidence: parcialmente verificable via RiskFlags.MissingLegalBasisEvidence; 
///   evidencia bloqueante específica sin modelo dominio (p.ej. "must have X evidence for security review")
/// ⚠ RequiredReviewPending: no hay modelo Review con estado "requerida" en Workflow.Review aún
/// ⚠ VersionModifiedAfterReview: no hay timestamp de "reviewedAt" en dominio para comparar vs LastModifiedAt
/// 
/// Fail-closed: si hay error en autorización o negocio, retorna 422 o 403; auditoría registra resultado.
/// </summary>
public sealed class ApproveProcessingActivityCommandHandler(
    ProcessingInventoryDbContext db,
    SecurityDbContext securityDb,
    IResourcePermissionsQueryService permissionsService,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<ProcessingActivityDto> HandleAsync(
        ApproveProcessingActivityCommand cmd,
        CancellationToken ct = default)
    {
        var activity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Id == cmd.ProcessingActivityId, ct)
            ?? throw new InvalidOperationException(
                $"ProcessingActivity {cmd.ProcessingActivityId} not found in tenant {cmd.TenantId}");

        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();
        var metadata = new Dictionary<string, object?>
        {
            { "activityName", activity.Name },
            { "currentStatus", activity.Status.ToString() },
            { "version", activity.Version },
            { "approvedBy", cmd.ApprovedBy }
        };

        try
        {
            // ── Autorización (RBAC) ─────────────────────────────────────────────────
            
            // SEC-APP-001: ProcessOwner cannot approve their own activity
            var resourceContext = new ResourceContextData
            {
                ResourceOwnerId = activity.CreatedBy, // The creator is considered the ProcessOwner
                ReviewDomain = null
            };

            var permissions = await permissionsService.GetResourcePermissionsAsync(
                cmd.ApprovedBy,
                cmd.TenantId,
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                resourceContext,
                ct);

            // Check if ApproveProcessingActivity is available (not blocked)
            var approveAction = permissions.AvailableActions.FirstOrDefault(a => a.ActionCode == "ApproveProcessingActivity");
            var isBlocked = permissions.BlockedActions.FirstOrDefault(b => b.ActionCode == "ApproveProcessingActivity");

            if (isBlocked != null || approveAction == null)
            {
                // Authorization denied
                metadata["denialReason"] = isBlocked?.ReasonLabelKey ?? "InsufficientPermissions";
                metadata["denialCode"] = isBlocked?.ReasonCode ?? "SEC-APP-001";

                await auditService.LogAsync(
                    cmd.TenantId,
                    cmd.ApprovedBy,
                    AuditEventType.ProcessingActivityApproved.ToString(),
                    "ProcessingActivity",
                    cmd.ProcessingActivityId,
                    AuditEventResult.Blocked,
                    correlationId,
                    metadata,
                    ct: ct);

                throw new UnauthorizedAccessException(
                    $"User cannot approve this ProcessingActivity: {isBlocked?.ReasonLabelKey}");
            }

            // ── Validación de estado ────────────────────────────────────────────────

            if (activity.Status != ProcessingActivityStatus.UnderReview)
            {
                metadata["error"] = $"Cannot approve from {activity.Status} state. Only UnderReview versions can be approved.";

                await auditService.LogAsync(
                    cmd.TenantId,
                    cmd.ApprovedBy,
                    AuditEventType.ProcessingActivityApproved.ToString(),
                    "ProcessingActivity",
                    cmd.ProcessingActivityId,
                    AuditEventResult.Blocked,
                    correlationId,
                    metadata,
                    ct: ct);

                throw new InvalidOperationException(
                    $"VersionNotApprovalReady: Cannot approve ProcessingActivity in {activity.Status} state.");
            }

            // ── Validación de blockers de negocio ───────────────────────────────────
            
            // Check RiskFlags.BlocksApproval — verifica CriticalGapOpen, MissingLegalBasisEvidence, etc.
            if (activity.Flags.BlocksApproval)
            {
                // Determine which blocker is active
                var blockerCode = "BlockingEvidenceMissing"; // default
                var blockerDetail = "Unknown blocking condition";

                if (activity.Flags.CriticalGapOpen)
                {
                    blockerCode = "CriticalGapOpen";
                    blockerDetail = "Critical compliance gap remains open";
                }
                else if (activity.Flags.MissingLegalBasisEvidence)
                {
                    blockerCode = "BlockingEvidenceMissing";
                    blockerDetail = "Legal basis evidence is missing for this treatment";
                }

                metadata["blockerCode"] = blockerCode;
                metadata["blockerDetail"] = blockerDetail;
                metadata["flags"] = new
                {
                    CriticalGapOpen = activity.Flags.CriticalGapOpen,
                    MissingLegalBasisEvidence = activity.Flags.MissingLegalBasisEvidence,
                    MissingSecurityMeasures = activity.Flags.MissingSecurityMeasures,
                    SensitiveData = activity.Flags.SensitiveData
                };

                await auditService.LogAsync(
                    cmd.TenantId,
                    cmd.ApprovedBy,
                    AuditEventType.ProcessingActivityApproved.ToString(),
                    "ProcessingActivity",
                    cmd.ProcessingActivityId,
                    AuditEventResult.Blocked,
                    correlationId,
                    metadata,
                    ct: ct);

                throw new InvalidOperationException(
                    $"ApprovalBlocked ({blockerCode}): {blockerDetail}");
            }

            // ── Approbar (dominio) ──────────────────────────────────────────────────

            activity.Approve(cmd.ApprovedBy);

            db.ProcessingActivities.Update(activity);
            await db.SaveChangesAsync(ct);

            metadata["approvalSuccess"] = true;
            metadata["approvedAt"] = activity.ApprovedAt?.ToString("O");

            // ── Auditoría: Success ──────────────────────────────────────────────────

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ApprovedBy,
                AuditEventType.ProcessingActivityApproved.ToString(),
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                ct: ct);

            return ProcessingActivityDto.From(activity);
        }
        catch (UnauthorizedAccessException ex)
        {
            // Authorization already logged above
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("ApprovalBlocked") || ex.Message.Contains("VersionNotApprovalReady"))
        {
            // Business blockers already logged above
            throw;
        }
        catch (Exception ex)
        {
            // Unexpected error — log as Failure
            metadata["errorMessage"] = ex.Message;
            metadata["errorType"] = ex.GetType().Name;

            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ApprovedBy,
                AuditEventType.ProcessingActivityApproved.ToString(),
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
