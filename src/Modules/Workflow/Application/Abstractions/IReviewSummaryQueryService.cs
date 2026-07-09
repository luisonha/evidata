using Evidata.Modules.Workflow.Application.Queries;

namespace Evidata.Modules.Workflow.Application.Abstractions;

/// <summary>
/// Servicio de consulta para obtener el resumen de revisión de una actividad de procesamiento.
/// Proporciona una vista uniforme del estado de revisión, decisiones y dominios pendientes.
/// </summary>
public interface IReviewSummaryQueryService
{
    /// <summary>
    /// Obtiene el resumen de revisión para una actividad de procesamiento específica.
    /// </summary>
    /// <param name="tenantId">ID del tenant.</param>
    /// <param name="processingActivityId">ID de la actividad de procesamiento.</param>
    /// <param name="versionId">ID de la versión.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>DTO de resumen de revisión, o null si no existe.</returns>
    Task<ReviewSummaryDto?> GetReviewSummaryAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default);
}
