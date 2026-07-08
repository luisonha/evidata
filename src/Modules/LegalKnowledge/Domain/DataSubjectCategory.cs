using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Categoría de titulares de datos (interesados) según Ley 21.719.
/// Catálogo global base extendible por tenant.
///
/// Ejemplos: "Clientes", "Empleados", "Proveedores", "Menores de edad".
/// </summary>
public class DataSubjectCategory : ITenantScoped
{
    private DataSubjectCategory() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Null = global. Populated = personalizado por tenant.</summary>
    public Guid TenantId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    /// <summary>Indica si esta categoría requiere salvaguardas especiales (ej. menores).</summary>
    public bool RequiresSpecialSafeguards { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static readonly Guid GlobalTenantId = Guid.Empty;

    public static DataSubjectCategory CreateGlobal(
        string code, string name, bool requiresSpecialSafeguards, Guid createdBy, string? description = null) =>
        Create(GlobalTenantId, code, name, requiresSpecialSafeguards, createdBy, description);

    public static DataSubjectCategory CreateForTenant(
        Guid tenantId, string code, string name, bool requiresSpecialSafeguards, Guid createdBy, string? description = null) =>
        Create(tenantId, code, name, requiresSpecialSafeguards, createdBy, description);

    private static DataSubjectCategory Create(
        Guid tenantId, string code, string name, bool requiresSpecialSafeguards, Guid createdBy, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new DataSubjectCategory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            RequiresSpecialSafeguards = requiresSpecialSafeguards,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate(Guid updatedBy) { IsActive = false; SetUpdated(updatedBy); }

    private void SetUpdated(Guid userId) { UpdatedBy = userId; UpdatedAt = DateTimeOffset.UtcNow; }
}
