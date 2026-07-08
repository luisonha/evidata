namespace Evidata.Modules.GapManagement.Application.Abstractions;

/// <summary>
/// Proyección de los flags de riesgo de un tratamiento RAT.
/// Usada por el detector de brechas automáticas para desacoplar la lectura
/// del dominio ProcessingInventory de la lógica de detección.
/// </summary>
public sealed record RatFlagsSnapshot(
    Guid ActivityId,
    Guid TenantId,
    string ActivityName,
    bool MissingSecurityMeasures,
    bool MissingLegalBasisEvidence,
    bool MissingRetention,
    bool ChildrenData,
    bool BiometricData,
    bool InternationalTransfer,
    bool AutomatedDecision);

/// <summary>
/// Proveedor de flags de riesgo de tratamientos RAT.
/// Abstrae el acceso al módulo ProcessingInventory.
/// </summary>
public interface IRatFlagsProvider
{
    /// <summary>
    /// Retorna los flags de riesgo del tratamiento indicado.
    /// Lanza <see cref="InvalidOperationException"/> si no se encuentra o no pertenece al tenant.
    /// </summary>
    Task<RatFlagsSnapshot> GetFlagsAsync(
        Guid tenantId,
        Guid processingActivityId,
        CancellationToken ct = default);
}
