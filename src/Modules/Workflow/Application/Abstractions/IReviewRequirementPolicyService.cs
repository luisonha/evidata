using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.Workflow.Application.Abstractions;

/// <summary>
/// Servicio de política de requerimientos de revisión configurables por tenant.
/// 
/// Determina si un tipo de revisión es requerido (bloqueante) para un tipo de entidad específico.
/// Implementa comportamiento por defecto conservador: si no hay configuración explícita,
/// la revisión es requerida (mantiene compatibilidad con MVP actual).
/// </summary>
public interface IReviewRequirementPolicyService
{
    /// <summary>
    /// Determina si un tipo de revisión es requerido (bloqueante) para una entidad en un tenant.
    /// 
    /// Si no existe configuración explícita, retorna true (conservador).
    /// </summary>
    Task<bool> IsReviewRequiredAsync(
        Guid tenantId,
        string entityType,
        ReviewType reviewType,
        CancellationToken ct = default);

    /// <summary>
    /// Obtiene todas las configuraciones de requerimientos de revisión para un tenant.
    /// </summary>
    Task<IReadOnlyList<ReviewRequirement>> GetAllForTenantAsync(
        Guid tenantId,
        CancellationToken ct = default);

    /// <summary>
    /// Crea o actualiza la configuración de requerimiento para un tipo de revisión/entidad.
    /// </summary>
    Task<ReviewRequirement> SetRequirementAsync(
        Guid tenantId,
        ReviewType reviewType,
        string entityType,
        bool isRequired,
        Guid modifiedBy,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina una configuración de requerimiento (revierte al comportamiento por defecto: requerido).
    /// </summary>
    Task DeleteRequirementAsync(
        Guid tenantId,
        ReviewType reviewType,
        string entityType,
        CancellationToken ct = default);
}
