using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Application.Queries;

public sealed class GetGapSummaryByProcessingActivityQueryHandler(GapManagementDbContext db)
    : IGapSummaryQueryService
{
    public async Task<ProcessingActivityGapSummaryDto> GetByProcessingActivityAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default)
    {
        // TODO: GapManagement todavía no modela relación por versionId; filtrar también por versión cuando ComplianceGap soporte ese vínculo.
        var gaps = await db.ComplianceGaps
            .AsNoTracking()
            .Where(g => g.TenantId == tenantId && g.SourceEntityId == processingActivityId)
            .ToListAsync(ct);

        var highestSeverity = gaps
            .Where(IsActiveGap)
            .Select(g => (GapSeverity?)g.Severity)
            .OrderByDescending(severity => severity)
            .FirstOrDefault();

        return new ProcessingActivityGapSummaryDto(
            processingActivityId,
            versionId,
            gaps.Count,
            gaps.Count(g => g.Status == GapStatus.Open),
            gaps.Count(g => g.Status == GapStatus.InCorrection),
            gaps.Count(g => g.Status == GapStatus.Resolved),
            gaps.Count(g => g.Status == GapStatus.AcceptedWithRisk),
            gaps.Count(g => g.Status == GapStatus.Dismissed),
            highestSeverity,
            highestSeverity is null ? null : ToSeverityLabelKey(highestSeverity.Value),
            gaps.Any(g => g.BlocksApproval));
    }

    private static bool IsActiveGap(ComplianceGap gap) =>
        gap.Status is not (GapStatus.Resolved or GapStatus.AcceptedWithRisk or GapStatus.Dismissed or GapStatus.Closed);

    private static string ToSeverityLabelKey(GapSeverity severity) =>
        $"gap.severity.{char.ToLowerInvariant(severity.ToString()[0])}{severity.ToString()[1..]}";
}
