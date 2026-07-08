using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Categoría de datos personales según Ley 21.719 (art. 4°).
/// Catálogo global base: puede ser extendido por tenants con entradas propias (TenantId != null).
///
/// Ejemplos: "Datos de identificación", "Datos sensibles", "Datos financieros".
/// </summary>
public class DataCategory : ITenantScoped
{
    private DataCategory() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>
    /// Null = entrada del catálogo global (visible a todos los tenants).
    /// Populated = entrada personalizada por ese tenant.
    /// Implementa ITenantScoped para que el global EF filter aplique aislamiento
    /// cuando el tenant quiere ver solo sus entradas (override disponible).
    /// </summary>
    public Guid TenantId { get; private set; }

    /// <summary>Código único dentro del scope (global o tenant): ej. "DATOS-SENSIBLES".</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Nombre legible: ej. "Datos sensibles (art. 4° inc. 2)".</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Descripción y ejemplos de qué datos pertenecen a esta categoría.</summary>
    public string? Description { get; private set; }

    /// <summary>Indica si esta categoría requiere tratamiento especial (datos sensibles).</summary>
    public bool IsSensitive { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // Sentinel para entradas globales (tenant_id = 00000000-...)
    public static readonly Guid GlobalTenantId = Guid.Empty;

    public static DataCategory CreateGlobal(
        string code, string name, bool isSensitive, Guid createdBy, string? description = null) =>
        Create(GlobalTenantId, code, name, isSensitive, createdBy, description);

    public static DataCategory CreateForTenant(
        Guid tenantId, string code, string name, bool isSensitive, Guid createdBy, string? description = null) =>
        Create(tenantId, code, name, isSensitive, createdBy, description);

    private static DataCategory Create(
        Guid tenantId, string code, string name, bool isSensitive, Guid createdBy, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new DataCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            IsSensitive = isSensitive,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate(Guid updatedBy) { IsActive = false; SetUpdated(updatedBy); }

    public void UpdateDescription(string? description, Guid updatedBy)
    {
        Description = description?.Trim();
        SetUpdated(updatedBy);
    }

    private void SetUpdated(Guid userId) { UpdatedBy = userId; UpdatedAt = DateTimeOffset.UtcNow; }
}
