using Evidata.Modules.Audit.Application.DTOs;

namespace Evidata.Modules.Audit.Application.Abstractions;

/// <summary>
/// Query interface for timeline events (read model only).
/// Encapsulates the contract for fetching audit logs as timeline events.
/// </summary>
public interface ITimelineQueryService
{
    /// <summary>
    /// Get paginated timeline events for a specific resource.
    /// </summary>
    /// <param name="tenantId">Tenant identifier</param>
    /// <param name="resourceId">Resource identifier (e.g., processingActivityId)</param>
    /// <param name="resource">Resource type filter (e.g., "ProcessingActivity"). If null, no filter.</param>
    /// <param name="skip">Number of events to skip (0-based; default: 0)</param>
    /// <param name="take">Number of events to take (default: 50)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Paged list of timeline events ordered by OccurredAt descending</returns>
    Task<IReadOnlyList<TimelineEventViewModel>> GetTimelineAsync(
        Guid tenantId,
        Guid resourceId,
        string? resource = "ProcessingActivity",
        int skip = 0,
        int take = 50,
        CancellationToken ct = default);
}
