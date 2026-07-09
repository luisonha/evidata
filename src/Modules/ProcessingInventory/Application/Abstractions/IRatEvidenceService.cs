using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;

namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>DTO de evidencia vinculada a un tratamiento RAT.</summary>
public sealed record RatEvidenceLinkDto(
    Guid LinkId,
    Guid EvidenceId,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

/// <summary>
/// Servicio para asociar/desasociar evidencias a tratamientos RAT.
///
/// Delega en <see cref="IEvidenceLinkService"/> usando
/// <see cref="LinkedEntityType.ProcessingActivity"/> como tipo de entidad.
///
/// Reglas:
/// - No permite duplicados: misma evidencia + tratamiento → excepción.
/// - Solo evidencias del mismo tenant son visibles.
/// - La eliminación es lógica.
/// </summary>
public interface IRatEvidenceService
{
    /// <summary>Vincula una evidencia a un tratamiento RAT.</summary>
    Task<Guid> AddEvidenceToRatAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid evidenceId,
        Guid createdBy,
        string? note = null,
        CancellationToken ct = default);

    /// <summary>Desvincula lógicamente una evidencia de un tratamiento RAT.</summary>
    Task RemoveEvidenceFromRatAsync(
        Guid tenantId,
        Guid linkId,
        Guid deletedBy,
        CancellationToken ct = default);

    /// <summary>Lista todas las evidencias activas vinculadas a un tratamiento RAT.</summary>
    Task<IReadOnlyList<RatEvidenceLinkDto>> GetEvidencesForRatAsync(
        Guid tenantId,
        Guid processingActivityId,
        CancellationToken ct = default);
}
