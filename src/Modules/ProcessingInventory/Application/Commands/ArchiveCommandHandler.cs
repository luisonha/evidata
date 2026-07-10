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
/// Handler for ArchiveCommand.
/// 
/// P1-011c: Instruments audit logging for Archive action (AUD-ARC-001).
/// - Validates the ProcessingActivity exists
/// - Transitions to Archived state (can archive from any state except already archived)
/// - Logs audit event with correlationId, result (Success/Failure), and structured metadata
/// - No sensitive data in metadata
/// </summary>
public sealed class ArchiveCommandHandler(
    ProcessingInventoryDbContext db,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<ProcessingActivityDto> HandleAsync(
        ArchiveCommand cmd, CancellationToken ct = default)
    {
        var activity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Id == cmd.ProcessingActivityId, ct)
            ?? throw new InvalidOperationException(
                $"ProcessingActivity {cmd.ProcessingActivityId} not found in tenant {cmd.TenantId}");

        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();
        var metadata = new Dictionary<string, object?>
        {
            { "activityName", activity.Name },
            { "previousStatus", activity.Status.ToString() },
            { "version", activity.Version }
        };

        try
        {
            // Attempt to archive
            // This will throw if already archived
            activity.Archive(cmd.ArchivedBy);

            db.ProcessingActivities.Update(activity);
            await db.SaveChangesAsync(ct);

            // Audit: Archive (AUD-ARC-001) — Success
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ArchivedBy,
                AuditEventType.Archive.ToString(),
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
            // Archive failed (e.g., already archived)
            metadata["errorMessage"] = ex.Message;
            
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ArchivedBy,
                AuditEventType.Archive.ToString(),
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
