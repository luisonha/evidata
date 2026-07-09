using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Modules.GapManagement.Application.Abstractions;

public sealed record ProcessingActivityGapSummaryDto(
    Guid ProcessingActivityId,
    Guid VersionId,
    int TotalCount,
    int OpenCount,
    int InCorrectionCount,
    int ResolvedCount,
    int AcceptedWithRiskCount,
    int DismissedCount,
    GapSeverity? HighestSeverity,
    string? HighestSeverityLabelKey,
    bool ApprovalBlocked);

public interface IGapSummaryQueryService
{
    Task<ProcessingActivityGapSummaryDto> GetByProcessingActivityAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default);
}
