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

        // TODO: GapManagement aún no soporta un estado Dismissed explícito; mapear dismissedCount cuando la FSM lo incorpore.
        const int dismissedCount = 0;

        return new ProcessingActivityGapSummaryDto(
            processingActivityId,
            versionId,
            gaps.Count,
            gaps.Count(g => g.Status is GapStatus.Open or GapStatus.Assigned),
            gaps.Count(g => g.Status is GapStatus.InProgress or GapStatus.Blocked),
            gaps.Count(g => g.Status == GapStatus.Resolved || IsClosedFromResolved(g)),
            gaps.Count(g => g.Status == GapStatus.AcceptedRisk || IsAcceptedWithRiskClosed(g)),
            dismissedCount,
            highestSeverity,
            highestSeverity is null ? null : ToSeverityLabelKey(highestSeverity.Value),
            gaps.Any(g => g.BlocksApproval));
    }

    private static bool IsActiveGap(ComplianceGap gap) =>
        gap.Status is not (GapStatus.Resolved or GapStatus.AcceptedRisk or GapStatus.Closed);

    private static bool IsClosedFromResolved(ComplianceGap gap) =>
        gap.Status == GapStatus.Closed && gap.RiskAcceptanceJustification is null && gap.ClosedAt.HasValue;

    private static bool IsAcceptedWithRiskClosed(ComplianceGap gap) =>
        gap.Status == GapStatus.Closed && gap.RiskAcceptanceJustification is not null;

    private static string ToSeverityLabelKey(GapSeverity severity) =>
        $"gap.severity.{char.ToLowerInvariant(severity.ToString()[0])}{severity.ToString()[1..]}";
}
