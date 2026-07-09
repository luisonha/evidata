using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Queries;

namespace Evidata.Modules.Workflow.Infrastructure;

/// <summary>
/// Implementación del servicio de consulta de resumen de revisión.
/// Delega al query handler GetReviewSummaryQueryHandler.
/// </summary>
public sealed class ReviewSummaryQueryService(GetReviewSummaryQueryHandler handler) : IReviewSummaryQueryService
{
    public Task<ReviewSummaryDto?> GetReviewSummaryAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default) =>
        handler.HandleAsync(tenantId, processingActivityId, versionId, ct);
}
