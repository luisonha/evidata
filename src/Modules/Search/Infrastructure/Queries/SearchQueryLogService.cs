using Evidata.Modules.Search.Application.Abstractions;
using Evidata.Modules.Search.Domain;
using Evidata.Modules.Search.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Search.Infrastructure.Queries;

/// <summary>
/// Implementación de <see cref="ISearchQueryLogService"/> con EF Core.
/// Append-only: RecordAsync inserta, RecordSelectionAsync actualiza HadSelection.
/// </summary>
public sealed class SearchQueryLogService : ISearchQueryLogService
{
    private readonly SearchDbContext _db;

    public int MinQueryLength => 3;

    public SearchQueryLogService(SearchDbContext db)
    {
        _db = db;
    }

    public async Task<SearchQueryLog> RecordAsync(
        Guid tenantId, Guid userId, string query, string source,
        int resultCount, string? filtersJson = null, bool? hadSelection = null,
        CancellationToken ct = default)
    {
        var log = SearchQueryLog.Record(tenantId, userId, query, source,
            resultCount, filtersJson, hadSelection);

        _db.SearchQueryLogs.Add(log);
        await _db.SaveChangesAsync(ct);
        return log;
    }

    public async Task RecordSelectionAsync(Guid logId, CancellationToken ct = default)
    {
        var log = await _db.SearchQueryLogs.FirstOrDefaultAsync(l => l.Id == logId, ct)
            ?? throw new KeyNotFoundException($"SearchQueryLog {logId} no encontrado.");

        log.RecordSelection();
        await _db.SaveChangesAsync(ct);
    }

    public Task<SearchQueryLog?> GetByIdAsync(Guid logId, CancellationToken ct = default) =>
        _db.SearchQueryLogs.FirstOrDefaultAsync(l => l.Id == logId, ct);

    public async Task<IReadOnlyList<SearchQueryLog>> GetRecentByUserAsync(
        Guid tenantId, Guid userId, int limit = 20, CancellationToken ct = default)
    {
        return await _db.SearchQueryLogs
            .Where(l => l.TenantId == tenantId && l.UserId == userId)
            .OrderByDescending(l => l.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);
    }
}
