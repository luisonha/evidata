using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Workflow.Application.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Notifications;

/// <summary>
/// Implementación de <see cref="IReviewEventHandler"/> para ProcessingActivity.
/// Maneja eventos de revisión emitidos por el módulo Workflow.
///
/// Cuando una revisión de un ProcessingActivity es aprobada:
/// 1. Busca la ProcessingActivity por su ID (TargetEntityId)
/// 2. Invoca MarkAsReviewed() para setear ReviewedAt = UtcNow (idempotente)
/// 3. Persiste el cambio en la base de datos
/// 4. Registra un evento de auditoría consistente con el patrón de auditoría del proyecto
/// </summary>
public sealed class ReviewEventHandler : IReviewEventHandler
{
    private readonly ProcessingInventoryDbContext _db;
    private readonly IAuditService _auditService;
    private const string TargetEntityTypeProcessingActivity = "ProcessingActivity";
    private const string TargetModuleProcessingInventory = "ProcessingInventory";

    public ReviewEventHandler(ProcessingInventoryDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task HandleReviewApprovedAsync(
        ReviewApprovedEventPayload payload,
        CancellationToken ct = default)
    {
        // Only handle reviews for ProcessingActivity in ProcessingInventory module
        if (payload.TargetModule != TargetModuleProcessingInventory ||
            payload.TargetEntityType != TargetEntityTypeProcessingActivity)
        {
            // Not our concern — another module's entity
            return;
        }

        var activity = await _db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == payload.TenantId && a.Id == payload.TargetEntityId, ct)
            ?? throw new InvalidOperationException(
                $"ProcessingActivity {payload.TargetEntityId} not found in tenant {payload.TenantId}");

        // Mark as reviewed — sets ReviewedAt = UtcNow
        // This enables the VersionModifiedAfterReview blocker to function correctly
        // Idempotent: if already marked, MarkAsReviewed() will not overwrite the timestamp
        activity.MarkAsReviewed();

        _db.ProcessingActivities.Update(activity);
        await _db.SaveChangesAsync(ct);
        
        // Log audit event per project audit pattern (consistency with ApproveProcessingActivityCommandHandler)
        var metadata = new Dictionary<string, object?>
        {
            { "reviewId", payload.ReviewId },
            { "reviewerId", payload.ReviewerId },
            { "activityName", activity.Name },
            { "version", activity.Version },
            { "comments", payload.Comments }
        };

        await _auditService.LogAsync(
            payload.TenantId,
            payload.ReviewerId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            TargetEntityTypeProcessingActivity,
            payload.TargetEntityId,
            AuditEventResult.Success,
            payload.ReviewId.ToString(),
            metadata,
            ct: ct);
    }
}
