using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Application.Queries;

public sealed record EvidenceDto(
    Guid Id, Guid TenantId, string Type, string Status, string Sensitivity,
    string Title, string? Description, string? BlobPath, string? ContentType,
    long? SizeBytes, string? Tags, Guid CreatedBy, DateTimeOffset CreatedAt)
{
    public static EvidenceDto From(Domain.Evidence e) => new(
        e.Id, e.TenantId, e.Type.ToString(), e.Status.ToString(), e.Sensitivity.ToString(),
        e.Title, e.Description, e.BlobPath, e.ContentType, e.SizeBytes,
        e.Tags, e.CreatedBy, e.CreatedAt);
}

public sealed class ListEvidenceQueryHandler(EvidenceDbContext db)
{
    public async Task<IReadOnlyList<EvidenceDto>> HandleAsync(
        Guid tenantId, string? status = null, CancellationToken ct = default)
    {
        var query = db.Evidences.Where(e =>
            e.TenantId == tenantId &&
            e.Status != Domain.EvidenceStatus.Deleted);

        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<Domain.EvidenceStatus>(status, ignoreCase: true, out var s))
            query = query.Where(e => e.Status == s);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
        return items.Select(EvidenceDto.From).ToList();
    }
}

public sealed class GetEvidenceQueryHandler(EvidenceDbContext db)
{
    public async Task<EvidenceDto?> HandleAsync(Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var item = await db.Evidences
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == id, ct);
        return item is null ? null : EvidenceDto.From(item);
    }
}
