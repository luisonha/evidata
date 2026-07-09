namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Tipo de fuente legal.
/// </summary>
public enum LegalSourceType
{
    Law,
    Decree,
    Regulation,
    CircularLetter,
    Resolution,
    Treaty,
    Standard
}

/// <summary>
/// Fuente legal: la norma jurídica madre (ej. "Ley 21.719").
/// Es un registro raíz — cada modificación genera una nueva <see cref="LegalSourceVersion"/>.
/// No tiene tenant_id: es catálogo global compartido entre todos los tenants.
/// </summary>
public class LegalSource
{
    private LegalSource() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Código único de la fuente: ej. "CL-LEY-21719", "CL-DEC-13".</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Nombre oficial completo: ej. "Ley 21.719 — Protección de Datos Personales".</summary>
    public string Name { get; private set; } = default!;

    /// <summary>País o jurisdicción (ISO 3166-1 alpha-2): "CL", "AR", "MX".</summary>
    public string Jurisdiction { get; private set; } = default!;

    public LegalSourceType Type { get; private set; }

    /// <summary>Fecha de publicación original en el Diario Oficial (u órgano equivalente).</summary>
    public DateOnly PublishedAt { get; private set; }

    /// <summary>URL oficial de la norma (BCN, DOF, BO, etc.).</summary>
    public string? OfficialUrl { get; private set; }

    public bool IsActive { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    public IReadOnlyCollection<LegalSourceVersion> Versions => _versions.AsReadOnly();
    private readonly List<LegalSourceVersion> _versions = new();

    // ── Factory ────────────────────────────────────────────────────────────────

    public static LegalSource Create(
        string code,
        string name,
        string jurisdiction,
        LegalSourceType type,
        DateOnly publishedAt,
        Guid createdBy,
        string? officialUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(jurisdiction);

        return new LegalSource
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Jurisdiction = jurisdiction.Trim().ToUpperInvariant(),
            Type = type,
            PublishedAt = publishedAt,
            IsActive = true,
            OfficialUrl = officialUrl?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public LegalSourceVersion AddVersion(
        string versionTag,
        DateOnly effectiveDate,
        string summary,
        Guid createdBy,
        string? changelogUrl = null)
    {
        var nextNumber = _versions.Count == 0 ? 1 : _versions.Max(v => v.VersionNumber) + 1;
        var version = LegalSourceVersion.Create(Id, versionTag, effectiveDate, summary, nextNumber, createdBy, changelogUrl);
        _versions.Add(version);
        SetUpdated(createdBy);
        return version;
    }

    public void Deactivate(Guid updatedBy)
    {
        IsActive = false;
        SetUpdated(updatedBy);
    }

    private void SetUpdated(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
