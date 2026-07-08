namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Versión específica de una fuente legal.
/// Cada modificación, enmienda o actualización genera una nueva versión.
/// El versionado garantiza trazabilidad regulatoria: qué texto estaba vigente en qué fecha.
/// </summary>
public class LegalSourceVersion
{
    private LegalSourceVersion() { } // EF Core

    public Guid Id { get; private set; }
    public Guid LegalSourceId { get; private set; }

    /// <summary>Número de versión autoincremental dentro de la fuente (1, 2, 3...).</summary>
    public int VersionNumber { get; private set; }

    /// <summary>Etiqueta semántica de la versión: ej. "v1.0", "2024-ENE-MOD1".</summary>
    public string VersionTag { get; private set; } = default!;

    /// <summary>Fecha desde la cual esta versión entró en vigor.</summary>
    public DateOnly EffectiveDate { get; private set; }

    /// <summary>Resumen de los cambios introducidos en esta versión.</summary>
    public string Summary { get; private set; } = default!;

    /// <summary>URL al diario oficial o documento de modificación (opcional).</summary>
    public string? ChangelogUrl { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation
    public LegalSource LegalSource { get; private set; } = default!;

    internal static LegalSourceVersion Create(
        Guid legalSourceId,
        string versionTag,
        DateOnly effectiveDate,
        string summary,
        int versionNumber,
        Guid createdBy,
        string? changelogUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(versionTag);
        ArgumentException.ThrowIfNullOrWhiteSpace(summary);
        if (versionNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(versionNumber), "El número de versión debe ser >= 1.");

        return new LegalSourceVersion
        {
            Id = Guid.NewGuid(),
            LegalSourceId = legalSourceId,
            VersionTag = versionTag.Trim(),
            EffectiveDate = effectiveDate,
            Summary = summary.Trim(),
            VersionNumber = versionNumber,
            ChangelogUrl = changelogUrl?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
