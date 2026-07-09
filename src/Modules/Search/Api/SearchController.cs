using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Search.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Search.Api;

[ApiController]
[Route("api/search")]
public class SearchController(
    SearchQueryHandler searchHandler,
    ICurrentUserContext currentUser) : ControllerBase
{
    /// <summary>
    /// Registra una búsqueda y devuelve el historial reciente del usuario.
    /// Mínimo 2 caracteres.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SearchHistoryDto>> Search(
        [FromQuery] string q,
        [FromQuery] string? source = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest("El término de búsqueda debe tener al menos 2 caracteres.");

        var result = await searchHandler.SearchAsync(
            currentUser.TenantId, currentUser.UserId,
            q.Trim(), source ?? "combined", ct);

        return Ok(result);
    }
}

public sealed record SearchHistoryDto(
    string Query, string Source, IReadOnlyList<SearchLogEntryDto> RecentSearches);

public sealed record SearchLogEntryDto(
    Guid Id, string Query, string Source, int ResultCount, DateTimeOffset OccurredAt);
