using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>
/// Servicio de versionado del RAT.
///
/// Responsabilidades:
/// - Aprobar un tratamiento y persistir su snapshot inmutable.
/// - Crear una nueva versión Draft a partir de un tratamiento aprobado.
/// - Consultar el historial de snapshots de un tratamiento.
/// </summary>
public interface IProcessingActivityVersionService
{
    /// <summary>
    /// Aprueba un tratamiento y guarda el snapshot.
    /// Retorna el snapshot creado.
    /// </summary>
    Task<ProcessingActivitySnapshot> ApproveAndSnapshotAsync(
        Guid activityId,
        Guid approvedBy,
        bool retentionRequired = false,
        CancellationToken ct = default);

    /// <summary>
    /// Crea una nueva versión Draft a partir de un tratamiento aprobado.
    /// El original queda Approved e inmutable.
    /// </summary>
    Task<ProcessingActivity> CreateNewVersionAsync(
        Guid approvedActivityId,
        Guid createdBy,
        CancellationToken ct = default);

    /// <summary>
    /// Lista todos los snapshots de un tratamiento, ordenados por versión descendente.
    /// </summary>
    Task<IReadOnlyList<ProcessingActivitySnapshot>> GetSnapshotsAsync(
        Guid activityId,
        Guid tenantId,
        CancellationToken ct = default);
}
