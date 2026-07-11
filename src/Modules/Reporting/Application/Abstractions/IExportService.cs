using Evidata.Modules.Reporting.Domain;

namespace Evidata.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Service for managing Export requests and operations.
/// Handles SEC-EXP-001 authorization, version management, and audit logging.
/// </summary>
public interface IExportService
{
    /// <summary>
    /// Requests a new export for a processing activity.
    /// Implements SEC-EXP-001: Validates authorization and activity state.
    /// </summary>
    Task<Export> RequestExportAsync(
        Guid tenantId,
        Guid processingActivityId,
        ExportType exportType,
        string contentType,
        Guid requestedByUserId,
        string correlationId,
        CancellationToken ct = default);

    /// <summary>Marks an export as starting generation.</summary>
    Task StartGenerationAsync(Guid exportId, CancellationToken ct = default);

    /// <summary>Completes an export with the generated artifact.</summary>
    Task CompleteAsync(
        Guid exportId, Guid artifactDocumentId, 
        CancellationToken ct = default);

    /// <summary>Fails an export with an error message.</summary>
    Task FailAsync(Guid exportId, string errorMessage, CancellationToken ct = default);

    /// <summary>Adds a warning to an export.</summary>
    Task AddWarningAsync(Guid exportId, string warning, CancellationToken ct = default);

    /// <summary>Gets an export by id.</summary>
    Task<Export?> GetExportAsync(Guid exportId, CancellationToken ct = default);

    /// <summary>Lists exports for a processing activity.</summary>
    Task<IReadOnlyList<Export>> ListExportsByActivityAsync(
        Guid processingActivityId, CancellationToken ct = default);

    /// <summary>Gets the export content by id for download.</summary>
    Task<ExportDownload?> GetExportDownloadAsync(
        Guid exportId, Guid tenantId, Guid userId,
        CancellationToken ct = default);
}

/// <summary>Result of an export download request.</summary>
public sealed record ExportDownload(
    Guid ExportId,
    string ContentType,
    Guid ArtifactDocumentId);
