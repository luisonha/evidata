namespace Evidata.Modules.Evidence.Application.Abstractions;

/// <summary>
/// Contrato público para obtener el resumen de evidencia de un tratamiento.
/// Replica el shape del EvidenceSummaryViewModel del módulo ProcessingInventory
/// sin introducir una referencia de proyecto cruzada entre módulos.
/// </summary>
public sealed record EvidenceSummaryDto(
    Guid ProcessingActivityId,
    Guid VersionId,
    int TotalRequirements,
    int PendingCount,
    int AttachedCount,
    int ValidatedCount,
    int InsufficientCount,
    int RejectedCount,
    int BlockingRequirementsCount,
    decimal CompletionPercentage);

public interface IEvidenceSummaryQueryService
{
    Task<EvidenceSummaryDto> GetSummaryAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default);
}
