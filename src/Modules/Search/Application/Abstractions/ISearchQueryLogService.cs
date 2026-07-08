using Evidata.Modules.Search.Domain;

namespace Evidata.Modules.Search.Application.Abstractions;

/// <summary>
/// Servicio de registro auditado de búsquedas.
/// Append-only: solo se registran y consultan, nunca se modifican.
/// </summary>
public interface ISearchQueryLogService
{
    /// <summary>Registra una búsqueda realizada.</summary>
    Task<SearchQueryLog> RecordAsync(
        Guid tenantId,
        Guid userId,
        string query,
        string source,
        int resultCount,
        string? filtersJson = null,
        bool? hadSelection = null,
        CancellationToken ct = default);

    /// <summary>Registra que el usuario seleccionó un resultado de la búsqueda.</summary>
    Task RecordSelectionAsync(Guid logId, CancellationToken ct = default);

    /// <summary>Obtiene un log por Id.</summary>
    Task<SearchQueryLog?> GetByIdAsync(Guid logId, CancellationToken ct = default);

    /// <summary>Lista búsquedas recientes de un usuario en un tenant.</summary>
    Task<IReadOnlyList<SearchQueryLog>> GetRecentByUserAsync(
        Guid tenantId, Guid userId, int limit = 20, CancellationToken ct = default);

    /// <summary>
    /// Mínimo de caracteres requeridos para ejecutar una búsqueda.
    /// Aplicar siempre antes de llamar a RecordAsync para evitar logs triviales.
    /// </summary>
    int MinQueryLength { get; }
}
