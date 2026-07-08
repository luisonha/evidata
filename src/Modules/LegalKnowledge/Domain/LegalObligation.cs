namespace Evidata.Modules.LegalKnowledge.Domain;

/// <summary>
/// Frecuencia de cumplimiento de una obligación legal.
/// </summary>
public enum ObligationFrequency
{
    OneTime,
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annually,
    OnEvent,
    Continuous
}

/// <summary>
/// Estado de vigencia de la obligación.
/// </summary>
public enum ObligationStatus
{
    Active,
    Superseded,
    Repealed
}

/// <summary>
/// Una obligación legal computable derivada de la Ley 21.719 (u otra fuente legal).
/// Representa el QUÉ debe cumplirse: texto normativo, plazos, frecuencia, artículo origen.
///
/// Estas entidades son de catálogo global (no tienen tenant_id) — son compartidas
/// entre todos los tenants. Los tenants mapean sus procesos a obligaciones via GapManagement.
/// </summary>
public class LegalObligation
{
    private LegalObligation() { } // EF Core

    public Guid Id { get; private set; }

    /// <summary>Código de referencia único: ej. "LEY21719-ART6-OBL1".</summary>
    public string Code { get; private set; } = default!;

    /// <summary>Título breve de la obligación (para UI y reportes).</summary>
    public string Title { get; private set; } = default!;

    /// <summary>Texto completo de la obligación tal como aparece en la ley.</summary>
    public string LegalText { get; private set; } = default!;

    /// <summary>Artículo de la ley que origina esta obligación (ej. "Art. 6°").</summary>
    public string? SourceArticle { get; private set; }

    /// <summary>Nombre de la ley o norma fuente (ej. "Ley 21.719").</summary>
    public string LegalSourceName { get; private set; } = default!;

    /// <summary>Plazo máximo de cumplimiento en días (null = sin plazo fijo).</summary>
    public int? DeadlineDays { get; private set; }

    /// <summary>Frecuencia con que debe cumplirse.</summary>
    public ObligationFrequency Frequency { get; private set; }

    /// <summary>Estado de vigencia.</summary>
    public ObligationStatus Status { get; private set; }

    /// <summary>Notas interpretativas o guía de implementación (opcional).</summary>
    public string? Notes { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    // ── Factory ────────────────────────────────────────────────────────────────

    public static LegalObligation Create(
        string code,
        string title,
        string legalText,
        string legalSourceName,
        ObligationFrequency frequency,
        Guid createdBy,
        string? sourceArticle = null,
        int? deadlineDays = null,
        string? notes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalText);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalSourceName);

        if (deadlineDays is < 0)
            throw new ArgumentOutOfRangeException(nameof(deadlineDays), "El plazo no puede ser negativo.");

        return new LegalObligation
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Title = title.Trim(),
            LegalText = legalText.Trim(),
            LegalSourceName = legalSourceName.Trim(),
            Frequency = frequency,
            Status = ObligationStatus.Active,
            SourceArticle = sourceArticle?.Trim(),
            DeadlineDays = deadlineDays,
            Notes = notes?.Trim(),
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutations ──────────────────────────────────────────────────────────────

    public void Supersede(Guid updatedBy)
    {
        Status = ObligationStatus.Superseded;
        SetUpdated(updatedBy);
    }

    public void Repeal(Guid updatedBy)
    {
        Status = ObligationStatus.Repealed;
        SetUpdated(updatedBy);
    }

    public void UpdateNotes(string? notes, Guid updatedBy)
    {
        Notes = notes?.Trim();
        SetUpdated(updatedBy);
    }

    private void SetUpdated(Guid userId)
    {
        UpdatedBy = userId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
