using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Tipo de medida de seguridad para clasificar controles técnicos y organizativos.
/// </summary>
public enum SecurityMeasureType
{
    Technical,
    Organizational,
    Physical,
    Legal
}

/// <summary>
/// Medida de seguridad requerida o recomendada por la normativa de protección de datos.
/// Catálogo global base extendible por tenant.
///
/// Ejemplos: "Cifrado en tránsito", "Control de acceso basado en roles",
/// "Política de retención de datos", "Registro de actividades de tratamiento".
/// </summary>
public class SecurityMeasure : ITenantScoped
{
    private SecurityMeasure() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Null = global. Populated = personalizado por tenant.</summary>
    public Guid TenantId { get; private set; }

    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }

    public SecurityMeasureType MeasureType { get; private set; }

    /// <summary>Indica si esta medida es obligatoria por ley (vs recomendada).</summary>
    public bool IsMandatory { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public static readonly Guid GlobalTenantId = Guid.Empty;

    public static SecurityMeasure CreateGlobal(
        string code, string name, SecurityMeasureType type, bool isMandatory,
        Guid createdBy, string? description = null) =>
        Create(GlobalTenantId, code, name, type, isMandatory, createdBy, description);

    public static SecurityMeasure CreateForTenant(
        Guid tenantId, string code, string name, SecurityMeasureType type, bool isMandatory,
        Guid createdBy, string? description = null) =>
        Create(tenantId, code, name, type, isMandatory, createdBy, description);

    private static SecurityMeasure Create(
        Guid tenantId, string code, string name, SecurityMeasureType type, bool isMandatory,
        Guid createdBy, string? description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new SecurityMeasure
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Description = description?.Trim(),
            MeasureType = type,
            IsMandatory = isMandatory,
            IsActive = true,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Deactivate(Guid updatedBy) { IsActive = false; SetUpdated(updatedBy); }

    private void SetUpdated(Guid userId) { UpdatedBy = userId; UpdatedAt = DateTimeOffset.UtcNow; }
}
