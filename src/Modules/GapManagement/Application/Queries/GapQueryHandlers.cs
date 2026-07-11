using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Application.Queries;

public sealed record ComplianceGapDto(
    Guid Id, Guid TenantId, string SourceModule, Guid SourceEntityId,
    string Title, string Description, string Severity, string Status,
    bool BlocksApproval, Guid? OwnerId, DateTimeOffset? DueAt,
    Guid CreatedBy, DateTimeOffset CreatedAt, DateTimeOffset? LastModifiedAt)
{
    public static ComplianceGapDto From(ComplianceGap g) => new(
        g.Id, g.TenantId, g.SourceModule, g.SourceEntityId,
        g.Title, g.Description, g.Severity.ToString(), g.Status.ToString(),
        g.BlocksApproval, g.OwnerId, g.DueAt,
        g.CreatedBy, g.CreatedAt, g.LastModifiedAt);
}

public sealed record GapSummaryDto(
    int Total, int Open, int InCorrection, int Resolved, int Critical, int High, int Medium, int Low);

public sealed class ListGapsQueryHandler(GapManagementDbContext db)
{
    public async Task<IReadOnlyList<ComplianceGapDto>> HandleAsync(
        Guid tenantId, string? severity = null, string? status = null, CancellationToken ct = default)
    {
        var query = db.ComplianceGaps.Where(g => g.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(severity) &&
            Enum.TryParse<GapSeverity>(severity, ignoreCase: true, out var sev))
            query = query.Where(g => g.Severity == sev);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<GapStatus>(status, ignoreCase: true, out var st))
            query = query.Where(g => g.Status == st);

        var items = await query
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(ct);
        return items.Select(ComplianceGapDto.From).ToList();
    }
}

public sealed class GetGapsSummaryQueryHandler(GapManagementDbContext db)
{
    public async Task<GapSummaryDto> HandleAsync(Guid tenantId, CancellationToken ct = default)
    {
        var gaps = await db.ComplianceGaps
            .Where(g => g.TenantId == tenantId)
            .ToListAsync(ct);

        return new GapSummaryDto(
            gaps.Count,
            gaps.Count(g => g.Status == GapStatus.Open),
            gaps.Count(g => g.Status == GapStatus.InCorrection),
            gaps.Count(g => g.Status == GapStatus.Resolved),
            gaps.Count(g => g.Severity == GapSeverity.Critical),
            gaps.Count(g => g.Severity == GapSeverity.High),
            gaps.Count(g => g.Severity == GapSeverity.Medium),
            gaps.Count(g => g.Severity == GapSeverity.Low));
    }
}
