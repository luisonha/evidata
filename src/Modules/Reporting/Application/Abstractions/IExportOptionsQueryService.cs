namespace Evidata.Modules.Reporting.Application.Abstractions;

/// <summary>
/// DTO para las opciones de exportación disponibles en una actividad de procesamiento.
/// </summary>
public sealed record ExportOptionViewModel(
    string ExportType,
    string ExportTypeLabelKey,
    string Visibility,
    string VisibilityLabelKey,
    bool IsAvailable,
    string? BlockedReasonCode,
    string? BlockedReasonLabelKey);

/// <summary>
/// Servicio de consulta para obtener las opciones de exportación disponibles.
/// </summary>
public interface IExportOptionsQueryService
{
    /// <summary>
    /// Obtiene las opciones de exportación disponibles para una actividad de procesamiento.
    /// </summary>
    /// <param name="processingActivityId">El identificador de la actividad de procesamiento.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>Una lista de opciones de exportación disponibles.</returns>
    Task<IReadOnlyList<ExportOptionViewModel>> GetExportOptionsAsync(
        Guid processingActivityId,
        CancellationToken ct = default);
}
