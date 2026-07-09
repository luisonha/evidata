using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Application.Queries;

/// <summary>
/// Implementation of timeline query service.
/// Delegates to GetProcessingActivityTimelineQueryHandler for business logic.
/// </summary>
public sealed class TimelineQueryService(IAuditLogRepository repository) : ITimelineQueryService
{
    private readonly GetProcessingActivityTimelineQueryHandler _handler =
        new GetProcessingActivityTimelineQueryHandler(repository);

    public Task<IReadOnlyList<TimelineEventViewModel>> GetTimelineAsync(
        Guid tenantId,
        Guid resourceId,
        string? resource = "ProcessingActivity",
        int skip = 0,
        int take = 50,
        CancellationToken ct = default)
    {
        return _handler.HandleAsync(tenantId, resourceId, resource, skip, take, ct);
    }
}
