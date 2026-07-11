using Evidata.Modules.Reporting.Domain;

namespace Evidata.Modules.Reporting.Api;

/// <summary>
/// Export resource for v1 API contract.
/// </summary>
public sealed record ExportDto(
    Guid Id,
    Guid ProcessingActivityId,
    string ExportType,
    string Status,
    int Version,
    IReadOnlyList<string> Warnings,
    string ContentType,
    Guid? ArtifactDocumentId,
    Guid RequestedByUserId,
    DateTimeOffset? GeneratedAt,
    string CorrelationId)
{
    public static ExportDto From(Export export) => new(
        Id: export.Id,
        ProcessingActivityId: export.ProcessingActivityId,
        ExportType: export.ExportType.ToString(),
        Status: export.Status.ToString(),
        Version: export.Version,
        Warnings: export.Warnings,
        ContentType: export.ContentType,
        ArtifactDocumentId: export.ArtifactDocumentId,
        RequestedByUserId: export.RequestedByUserId,
        GeneratedAt: export.GeneratedAt,
        CorrelationId: export.CorrelationId);
}

/// <summary>
/// Request to create a new export.
/// </summary>
public sealed record CreateExportRequest(
    string ExportType,
    string ContentType = "application/pdf");

/// <summary>
/// Response for a successful export download.
/// </summary>
public sealed record ExportDownloadResponse(
    Guid ExportId,
    string ContentType,
    Guid ArtifactDocumentId);

/// <summary>
/// Error response for export operations.
/// </summary>
public sealed record ExportErrorResponse(
    string Code,
    string Message,
    string? Details = null);
