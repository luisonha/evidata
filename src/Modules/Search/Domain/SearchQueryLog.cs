namespace Evidata.Modules.Search.Domain;

/// <summary>
/// Log inmutable de búsqueda auditada.
/// Registra quién buscó, qué buscó, qué fuentes consultó y cuántos resultados obtuvo.
/// Append-only: nunca se edita después de creado.
/// </summary>
public sealed class SearchQueryLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Término o expresión de búsqueda ingresada por el usuario.</summary>
    public string Query { get; private set; } = default!;

    /// <summary>Fuente consultada: "legal", "tenant", "combined".</summary>
    public string Source { get; private set; } = default!;

    /// <summary>Filtros aplicados serializados en JSON (nullable).</summary>
    public string? FiltersJson { get; private set; }

    public int ResultCount { get; private set; }

    /// <summary>Indica si el usuario seleccionó algún resultado (null = no rastreado).</summary>
    public bool? HadSelection { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private SearchQueryLog() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static SearchQueryLog Record(
        Guid tenantId,
        Guid userId,
        string query,
        string source,
        int resultCount,
        string? filtersJson = null,
        bool? hadSelection = null)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("La consulta de búsqueda no puede estar vacía.", nameof(query));
        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("La fuente de búsqueda no puede estar vacía.", nameof(source));
        if (resultCount < 0)
            throw new ArgumentOutOfRangeException(nameof(resultCount), "El conteo de resultados no puede ser negativo.");

        return new SearchQueryLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Query = query.Trim(),
            Source = source.Trim().ToLowerInvariant(),
            FiltersJson = filtersJson?.Trim(),
            ResultCount = resultCount,
            HadSelection = hadSelection,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>Registra que el usuario seleccionó un resultado. Solo puede llamarse una vez.</summary>
    public void RecordSelection()
    {
        if (HadSelection.HasValue)
            throw new InvalidOperationException("La selección ya fue registrada.");
        HadSelection = true;
    }
}
