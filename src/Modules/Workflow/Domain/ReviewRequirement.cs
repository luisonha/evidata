namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Política de configuración por tenant que especifica si un tipo de revisión
/// es requerido (bloqueante) para un tipo de entidad específico.
/// 
/// Esta entidad permite que cada tenant configure su propia política de revisiones:
/// - Si no existe configuración explícita para un tenant/entity-type/review-type,
///   el comportamiento es conservador (requerido = true) — mantiene la compatibilidad
///   con el MVP actual que bloquea la aprobación si cualquier review está pendiente.
/// 
/// Ejemplo: Tenant A puede configurar que las revisiones de Security sean opcionales
/// para ProcessingActivity, mientras que Legal sigue siendo obligatoria.
/// </summary>
public sealed class ReviewRequirement
{
    public Guid Id { get; private set; }
    
    /// <summary>Tenant propietario de esta política.</summary>
    public Guid TenantId { get; private set; }
    
    /// <summary>Tipo de revisión (Legal, Security).</summary>
    public ReviewType ReviewType { get; private set; }
    
    /// <summary>Tipo de entidad destino (ej: "ProcessingActivity").</summary>
    public string EntityType { get; private set; } = default!;
    
    /// <summary>¿Este tipo de revisión es requerido (bloqueante) para este tipo de entidad?</summary>
    public bool IsRequired { get; private set; }
    
    /// <summary>Creado en este momento.</summary>
    public DateTimeOffset CreatedAt { get; private set; }
    
    /// <summary>Última modificación.</summary>
    public DateTimeOffset ModifiedAt { get; private set; }
    
    /// <summary>Usuario que creó esta configuración.</summary>
    public Guid CreatedBy { get; private set; }
    
    /// <summary>Usuario que modificó esta configuración por última vez.</summary>
    public Guid ModifiedBy { get; private set; }

    private ReviewRequirement() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ReviewRequirement Create(
        Guid tenantId,
        ReviewType reviewType,
        string entityType,
        bool isRequired,
        Guid createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(entityType);
        
        if (createdBy == Guid.Empty)
            throw new ArgumentException("createdBy cannot be empty.", nameof(createdBy));

        return new ReviewRequirement
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReviewType = reviewType,
            EntityType = entityType.Trim(),
            IsRequired = isRequired,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            ModifiedBy = createdBy
        };
    }

    // ── Modificación ──────────────────────────────────────────────────────────

    /// <summary>Actualiza si esta configuración es requerida o no.</summary>
    public void SetRequired(bool isRequired, Guid modifiedBy)
    {
        if (modifiedBy == Guid.Empty)
            throw new ArgumentException("modifiedBy cannot be empty.", nameof(modifiedBy));

        IsRequired = isRequired;
        ModifiedAt = DateTimeOffset.UtcNow;
        ModifiedBy = modifiedBy;
    }
}
