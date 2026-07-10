using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Application.Notifications;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Notifications;

/// <summary>
/// Implementación de <see cref="IReviewEventHandler"/> para ProcessingActivity.
/// Maneja eventos de revisión emitidos por el módulo Workflow.
///
/// Cuando una revisión de un ProcessingActivity es aprobada:
/// 1. Busca la ProcessingActivity por su ID (TargetEntityId)
/// 2. Invoca MarkAsReviewed() para setear ReviewedAt = UtcNow
/// 3. Persiste el cambio en la base de datos
/// </summary>
public sealed class ReviewEventHandler : IReviewEventHandler
{
    private readonly ProcessingInventoryDbContext _db;
    private const string TargetEntityTypeProcessingActivity = "ProcessingActivity";
    private const string TargetModuleProcessingInventory = "ProcessingInventory";

    public ReviewEventHandler(ProcessingInventoryDbContext db)
    {
        _db = db;
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
    }
}
