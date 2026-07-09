using Evidata.Modules.Search.Application.Abstractions;

namespace Evidata.Modules.Search.Application.Queries;

/// <summary>
/// Ejecuta una búsqueda: registra el log y devuelve el historial reciente del usuario.
/// La indexación cross-módulo se implementará en una fase futura mediante events/indexer.
/// </summary>
public sealed class SearchQueryHandler(ISearchQueryLogService logService)
{
    public async Task<SearchHistoryDto> SearchAsync(
        Guid tenantId, Guid userId, string q, string source, CancellationToken ct)
    {
        await logService.RecordAsync(tenantId, userId, q, source, resultCount: 0, ct: ct);

        var recent = await logService.GetRecentByUserAsync(tenantId, userId, limit: 20, ct: ct);

        return new SearchHistoryDto(
            q, source,
            recent.Select(r => new SearchLogEntryDto(
                r.Id, r.Query, r.Source, r.ResultCount, r.OccurredAt)).ToList());
    }
}

public sealed record SearchHistoryDto(
    string Query, string Source, IReadOnlyList<SearchLogEntryDto> RecentSearches);

public sealed record SearchLogEntryDto(
    Guid Id, string Query, string Source, int ResultCount, DateTimeOffset OccurredAt);
