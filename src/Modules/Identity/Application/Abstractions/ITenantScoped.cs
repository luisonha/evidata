namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Marker interface para entidades que pertenecen a un tenant específico.
/// Los DbContext deben aplicar un global query filter basado en TenantId
/// para garantizar aislamiento de datos.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}
