using Evidata.Modules.LegalKnowledge.Domain;
using Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.LegalKnowledge.Application.Queries;

public sealed record LegalSourceDto(
    Guid Id, string Code, string Name, string Jurisdiction,
    string Type, DateOnly PublishedAt, string? OfficialUrl, bool IsActive,
    DateTimeOffset CreatedAt, int VersionCount)
{
    public static LegalSourceDto From(LegalSource s) => new(
        s.Id, s.Code, s.Name, s.Jurisdiction, s.Type.ToString(),
        s.PublishedAt, s.OfficialUrl, s.IsActive, s.CreatedAt, s.Versions.Count);
}

public sealed record LegalObligationDto(
    Guid Id, string Code, string Title, string LegalText,
    string? SourceArticle, string LegalSourceName, int? DeadlineDays,
    string Frequency, string Status, string? Notes, DateTimeOffset CreatedAt)
{
    public static LegalObligationDto From(LegalObligation o) => new(
        o.Id, o.Code, o.Title, o.LegalText, o.SourceArticle, o.LegalSourceName,
        o.DeadlineDays, o.Frequency.ToString(), o.Status.ToString(), o.Notes, o.CreatedAt);
}

public sealed class ListLegalSourcesQueryHandler(LegalKnowledgeDbContext db)
{
    public async Task<IReadOnlyList<LegalSourceDto>> HandleAsync(
        bool? activeOnly = true, CancellationToken ct = default)
    {
        var query = db.LegalSources.Include(s => s.Versions).AsQueryable();
        if (activeOnly == true) query = query.Where(s => s.IsActive);
        var items = await query.OrderBy(s => s.Code).ToListAsync(ct);
        return items.Select(LegalSourceDto.From).ToList();
    }
}

public sealed class ListLegalObligationsQueryHandler(LegalKnowledgeDbContext db)
{
    public async Task<IReadOnlyList<LegalObligationDto>> HandleAsync(
        string? status = null, CancellationToken ct = default)
    {
        var query = db.LegalObligations.AsQueryable();
        if (!string.IsNullOrWhiteSpace(status) &&
            Enum.TryParse<ObligationStatus>(status, ignoreCase: true, out var s))
            query = query.Where(o => o.Status == s);
        else
            query = query.Where(o => o.Status == ObligationStatus.Active);

        var items = await query.OrderBy(o => o.Code).ToListAsync(ct);
        return items.Select(LegalObligationDto.From).ToList();
    }
}
