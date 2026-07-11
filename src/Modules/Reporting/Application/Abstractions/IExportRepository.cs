using Evidata.Modules.Reporting.Domain;

namespace Evidata.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Repository abstraction for Export aggregate.
/// </summary>
public interface IExportRepository
{
    /// <summary>Adds a new export to the repository.</summary>
    Task AddAsync(Export export, CancellationToken ct = default);

    /// <summary>Updates an existing export.</summary>
    Task UpdateAsync(Export export, CancellationToken ct = default);

    /// <summary>Gets an export by its id.</summary>
    Task<Export?> GetByIdAsync(Guid exportId, CancellationToken ct = default);

    /// <summary>Lists exports for a specific processing activity.</summary>
    Task<IReadOnlyList<Export>> GetByProcessingActivityAsync(
        Guid processingActivityId, CancellationToken ct = default);

    /// <summary>Gets the latest version of a specific export type for an activity.</summary>
    Task<Export?> GetLatestByActivityAndTypeAsync(
        Guid processingActivityId, ExportType exportType, CancellationToken ct = default);

    /// <summary>Gets an export by correlation id.</summary>
    Task<Export?> GetByCorrelationIdAsync(string correlationId, CancellationToken ct = default);

    /// <summary>Calculates the next version number for a given activity + type combination.</summary>
    Task<int> GetNextVersionAsync(Guid processingActivityId, ExportType exportType, CancellationToken ct = default);

    /// <summary>Persists all pending changes.</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
