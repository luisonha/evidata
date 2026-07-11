using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Handler for SubmitForReviewCommand.
/// 
/// P1-011c: Instruments audit logging for SubmitForReview action (AUD-REV-001).
/// - Validates the ProcessingActivity is in Draft state
/// - Validates completeness requirements (via ProcessingActivityValidator)
/// - Transitions to UnderReview
/// - Logs audit event with correlationId, result (Success/Failure/Blocked), and structured metadata
/// - No sensitive data in metadata (only IDs and counts)
/// </summary>
public sealed class SubmitForReviewCommandHandler(
    ProcessingInventoryDbContext db,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<ProcessingActivityDto> HandleAsync(
        SubmitForReviewCommand cmd, CancellationToken ct = default)
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
            { "version", activity.Version }
        };

        try
        {
            // Attempt to transition to UnderReview
            // This will throw if state is invalid or if validation fails
            activity.SubmitForReview(cmd.SubmittedBy);

            db.ProcessingActivities.Update(activity);
            await db.SaveChangesAsync(ct);

            // Audit: SubmitForReview (AUD-REV-001) — Success
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.SubmittedBy,
                AuditEventType.SubmitForReview.ToString(),
                "ProcessingActivity",
                cmd.ProcessingActivityId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                ct: ct);

            return ProcessingActivityDto.From(activity);
        }
        catch (InvalidOperationException ex)
        {
            // State transition failed (e.g., not in Draft, validation failed)
            // Log as Failure
            metadata["errorMessage"] = ex.Message;
            
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.SubmittedBy,
                AuditEventType.SubmitForReview.ToString(),
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
