namespace Evidata.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Query service for reading ProcessingActivity data from external module.
/// Used for SEC-EXP-001: Validating that ProcessingActivity is in approved state.
/// </summary>
public interface IProcessingActivityReadOnlyQueryService
{
    /// <summary>
    /// Get ProcessingActivity status (Draft, UnderReview, Approved, Archived).
    /// Returns null if activity not found or not accessible in tenant.
    /// </summary>
    Task<string?> GetStatusAsync(Guid tenantId, Guid processingActivityId, CancellationToken ct = default);
}
