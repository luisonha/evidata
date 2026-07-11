namespace Evidata.Modules.GapManagement.Domain;

/// <summary>
/// Catálogo de reglas automáticas de detección de brechas (gaps).
/// Cada regla tiene un RuleCode único, define su Severity (Critical/High),
/// y si BlocksApproval (bool) cuando está activa en estado Open.
///
/// Registro auditable: las reglas son configuración del dominio, no mutable en producción.
/// </summary>
public class GapRule
{
    private GapRule() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Código único de la regla (e.g., "TRANSFER_WITHOUT_DESTINATION_COUNTRY").</summary>
    public string RuleCode { get; private set; } = default!;

    /// <summary>Descripción legible de la regla.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>
    /// Severidad: Critical o High.
    /// Crítica = debe ser resuelta antes de activar/aprobar RAT.
    /// Alta = urgente pero no bloquea directamente.
    /// </summary>
    public GapSeverity Severity { get; private set; }

    /// <summary>True si una brecha abierta con esta regla bloquea aprobación del RAT origen.</summary>
    public bool BlocksApproval { get; private set; }

    /// <summary>
    /// Nombre del fixture usado en tests para verificar que la regla se evalúa correctamente.
    /// Ejemplo: "pa-transfer-no-country" para TRANSFER_WITHOUT_DESTINATION_COUNTRY.
    /// </summary>
    public string TestFixtureName { get; private set; } = default!;

    /// <summary>
    /// Indicador de implementación: True si la regla está completamente evaluable en el motor.
    /// False si la regla está formalizada pero requiere datos que el dominio aún no captura,
    /// en cuyo caso debe documentarse en decisiones.
    /// </summary>
    public bool IsFullyImplemented { get; private set; }

    /// <summary>Notas de implementación, documentación de requisitos no disponibles, etc.</summary>
    public string? ImplementationNotes { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? LastModifiedBy { get; private set; }
    public DateTimeOffset? LastModifiedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    /// <summary>Crea una nueva regla de detección de brechas.</summary>
    public static GapRule Create(
        Guid tenantId,
        string ruleCode,
        string description,
        GapSeverity severity,
        bool blocksApproval,
        string testFixtureName,
        Guid createdBy,
        bool isFullyImplemented = true,
        string? implementationNotes = null)
    {
        if (string.IsNullOrWhiteSpace(ruleCode))
            throw new ArgumentException("RuleCode no puede ser vacío.", nameof(ruleCode));
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description no puede ser vacía.", nameof(description));
        if (string.IsNullOrWhiteSpace(testFixtureName))
            throw new ArgumentException("TestFixtureName no puede ser vacío.", nameof(testFixtureName));

        return new GapRule
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            RuleCode = ruleCode.Trim(),
            Description = description.Trim(),
            Severity = severity,
            BlocksApproval = blocksApproval,
            TestFixtureName = testFixtureName.Trim(),
            IsFullyImplemented = isFullyImplemented,
            ImplementationNotes = implementationNotes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Update ────────────────────────────────────────────────────────────────

    /// <summary>Actualiza campos editables de la regla (raras veces, para documentación de implementación).</summary>
    public void Update(
        string description,
        GapSeverity severity,
        bool blocksApproval,
        Guid modifiedBy,
        bool? isFullyImplemented = null,
        string? implementationNotes = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description no puede ser vacía.", nameof(description));

        Description = description.Trim();
        Severity = severity;
        BlocksApproval = blocksApproval;
        if (isFullyImplemented.HasValue)
            IsFullyImplemented = isFullyImplemented.Value;
        ImplementationNotes = implementationNotes?.Trim();
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }
}
