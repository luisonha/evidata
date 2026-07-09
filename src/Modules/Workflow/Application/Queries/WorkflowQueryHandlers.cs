using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Workflow.Application.Queries;

public sealed record WorkflowTaskDto(
    Guid Id, Guid TenantId, string TargetModule, string TargetEntityType,
    Guid TargetEntityId, string TaskType, string Status, string Title,
    string? Description, Guid AssignedTo, DateTimeOffset? DueAt, DateTimeOffset CreatedAt)
{
    public static WorkflowTaskDto From(WorkflowTask t) => new(
        t.Id, t.TenantId, t.TargetModule, t.TargetEntityType, t.TargetEntityId,
        t.TaskType.ToString(), t.Status.ToString(), t.Title,
        t.Description, t.AssignedTo, t.DueAt, t.CreatedAt);
}

public sealed class ListWorkflowTasksQueryHandler(WorkflowDbContext db)
{
    public async Task<IReadOnlyList<WorkflowTaskDto>> HandleAsync(
        Guid tenantId, bool pendingOnly = false, CancellationToken ct = default)
    {
        var query = db.WorkflowTasks.Where(t => t.TenantId == tenantId);

        if (pendingOnly)
            query = query.Where(t =>
                t.Status == WorkflowTaskStatus.Open ||
                t.Status == WorkflowTaskStatus.InProgress);

        var items = await query
            .OrderBy(t => t.DueAt ?? DateTimeOffset.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return items.Select(WorkflowTaskDto.From).ToList();
    }
}

/// <summary>
/// DTO para la vista de resumen de revisión de una actividad de procesamiento.
/// Mapea desde Review más reciente hacia la estructura de ReviewSummaryViewModel.
/// </summary>
public sealed record ReviewSummaryDto(
    Guid ProcessingActivityId,
    Guid VersionId,
    string Status,
    string StatusLabelKey,
    string? LastDecisionCode,
    string? LastDecisionLabelKey,
    IReadOnlyList<object> RequiredDomains,
    IReadOnlyList<object> PendingDomains,
    DateTimeOffset? DecidedAt);

public sealed class GetReviewSummaryQueryHandler(WorkflowDbContext db)
{
    public async Task<ReviewSummaryDto?> HandleAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default)
    {
        // Búsqueda de la revisión más reciente para esta actividad de procesamiento
        var latestReview = await db.Reviews
            .Where(r =>
                r.TenantId == tenantId &&
                r.TargetModule == "ProcessingInventory" &&
                r.TargetEntityType == "ProcessingActivity" &&
                r.TargetEntityId == processingActivityId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(ct);

        // Mapeo de ReviewStatus a ReviewDecisionCode
        string? lastDecisionCode = null;
        DateTimeOffset? decidedAt = null;

        if (latestReview is not null)
        {
            if (latestReview.Status == ReviewStatus.Approved)
            {
                lastDecisionCode = "Approved";
                decidedAt = latestReview.CreatedAt;
            }
            else if (latestReview.Status == ReviewStatus.ChangesRequested)
            {
                lastDecisionCode = "ChangesRequested";
                decidedAt = latestReview.CreatedAt;
            }
            // Cancelled o sin revisión completada → lastDecisionCode es null
        }

        // TODO: Mapear a statusLabelKey desde i18n (requiere recurso UI)
        var statusLabelKey = "processing.activity.version.status.draft";

        // TODO: Mapear a lastDecisionLabelKey desde i18n (requiere recurso UI)
        var lastDecisionLabelKey = lastDecisionCode is not null
            ? $"review.decision.{lastDecisionCode.ToLowerInvariant()}"
            : null;

        // TODO: Rellenar requiredDomains/pendingDomains.
        // Review no tiene clasificación por dominio (Legal, Security).
        // Esto requiere decisión de producto: ¿se mapea desde processingActivityId en ProcessingInventory,
        // o se infiere desde roles de revisores? Por ahora, arrays vacíos.
        var requiredDomains = new List<object>();
        var pendingDomains = new List<object>();

        // TODO T-P1-03f: Campo `status` requiere acceso a ProcessingActivityVersion.
        // Por ahora, valor neutral "Draft".
        var status = "Draft";

        return new ReviewSummaryDto(
            processingActivityId,
            versionId,
            status,
            statusLabelKey,
            lastDecisionCode,
            lastDecisionLabelKey,
            requiredDomains,
            pendingDomains,
            decidedAt);
    }
}
