using Evidata.Modules.Documents.Domain;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Documents.Application.Queries;

public sealed record DocumentDto(
    Guid Id, Guid TenantId, string OriginalFileName, string ContentType,
    long? SizeBytes, string? BlobPath, string Status,
    string? Description, Guid CreatedBy, DateTimeOffset CreatedAt)
{
    public static DocumentDto From(Document d) => new(
        d.Id, d.TenantId, d.OriginalFileName, d.ContentType,
        d.SizeBytes, d.BlobPath, d.Status.ToString(),
        d.Description, d.CreatedBy, d.CreatedAt);
}

public sealed class ListDocumentsQueryHandler(DocumentDbContext db)
{
    public async Task<IReadOnlyList<DocumentDto>> HandleAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var items = await db.Documents
            .Where(d => d.TenantId == tenantId && d.DeletedAt == null)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
        return items.Select(DocumentDto.From).ToList();
    }
}
