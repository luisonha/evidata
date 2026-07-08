using Evidata.Modules.Evidence.Domain;

namespace Evidata.Modules.Evidence.Application.Abstractions;

/// <summary>Resultado de agregar un vínculo.</summary>
/// <param name="LinkId">ID del nuevo EvidenceLink</param>
public sealed record AddLinkResult(Guid LinkId);

/// <summary>Resultado de listar vínculos de una evidencia.</summary>
/// <param name="LinkId">ID del vínculo</param>
/// <param name="LinkedEntityType">Tipo de entidad vinculada</param>
/// <param name="LinkedEntityId">ID de la entidad vinculada</param>
/// <param name="Note">Nota opcional</param>
/// <param name="CreatedBy">Creador del vínculo</param>
/// <param name="CreatedAt">Fecha de creación</param>
public sealed record EvidenceLinkDto(
    Guid LinkId,
    LinkedEntityType LinkedEntityType,
    Guid LinkedEntityId,
    string? Note,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

/// <summary>
/// Servicio para gestionar vínculos entre evidencias y otras entidades del sistema.
///
/// Reglas:
/// - Previene duplicados: no puede existir dos links activos para la misma
///   (evidenceId, entityType, entityId) dentro del mismo tenant.
/// - Solo links del mismo tenant son visibles.
/// - La eliminación es lógica (soft delete).
/// </summary>
public interface IEvidenceLinkService
{
    /// <summary>
    /// Crea un vínculo entre una evidencia y una entidad.
    /// Lanza excepción si ya existe un link activo idéntico.
    /// </summary>
    Task<AddLinkResult> AddLinkAsync(
        Guid tenantId,
        Guid evidenceId,
        LinkedEntityType linkedEntityType,
        Guid linkedEntityId,
        Guid createdBy,
        string? note = null,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina lógicamente un vínculo existente.
    /// Lanza excepción si el link no pertenece al tenant o ya está eliminado.
    /// </summary>
    Task RemoveLinkAsync(
        Guid tenantId,
        Guid linkId,
        Guid deletedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna todos los vínculos activos de una evidencia dentro del tenant.
    /// </summary>
    Task<IReadOnlyList<EvidenceLinkDto>> GetLinksAsync(
        Guid tenantId,
        Guid evidenceId,
        CancellationToken ct = default);
}
