using Evidata.Modules.Reporting.Application.Abstractions;

namespace Evidata.Modules.Reporting.Application.Queries;

/// <summary>
/// Handler de consulta para obtener las opciones de exportación disponibles.
/// </summary>
public sealed class GetExportOptionsQueryHandler : IExportOptionsQueryService
{
    /// <summary>
    /// Obtiene las opciones de exportación disponibles para una actividad de procesamiento.
    /// </summary>
    /// <remarks>
    /// TODO: ExportType público (ProcessingActivityPdfSummary/GlobalRatExcel/ApprovalHistory/InternalJson)
    /// no existe aún en el dominio — solo existe ReportType interno con valores distintos (RAT/Gaps/EvidencePack/ExecutiveSummary).
    /// Ver T-P1-EXPORT-adr para el diseño del recurso Export completo.
    /// NO se debe inventar un mapeo entre ambos enums sin decisión de arquitectura.
    /// 
    /// Por ahora, devolvemos una lista vacía de forma intencional como stub que será implementado
    /// una vez se resuelva la brecha de tipos de exportación a nivel de arquitectura.
    /// </remarks>
    public Task<IReadOnlyList<ExportOptionViewModel>> GetExportOptionsAsync(
        Guid processingActivityId,
        CancellationToken ct = default)
    {
        // Retorna una lista vacía de forma intencional hasta que se defina la especificación
        // completa del recurso Export y se mapee ExportType con ReportType.
        return Task.FromResult<IReadOnlyList<ExportOptionViewModel>>(
            Array.Empty<ExportOptionViewModel>());
    }
}
