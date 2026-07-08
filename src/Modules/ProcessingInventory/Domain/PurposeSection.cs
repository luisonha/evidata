namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Bases de licitud conforme a Ley 21.719 (Chile) + compatibilidad GDPR Art. 6.
/// </summary>
public enum LegalBasis
{
    /// <summary>Consentimiento expreso e informado del titular.</summary>
    Consent,
    /// <summary>Ejecución de contrato en que el titular es parte.</summary>
    ContractExecution,
    /// <summary>Obligación legal del responsable.</summary>
    LegalObligation,
    /// <summary>Interés vital del titular u otra persona.</summary>
    VitalInterests,
    /// <summary>Interés legítimo del responsable (requiere ponderación).</summary>
    LegitimateInterest,
    /// <summary>Función pública o misión de interés general.</summary>
    PublicTask,
    /// <summary>Otra base — requiere descripción en Justification.</summary>
    Other
}

/// <summary>
/// Sección "Finalidad y Base de Licitud" del tratamiento RAT (doc 14, sec 5.3).
/// Value object embebido — almacenado en columnas de la misma tabla.
/// </summary>
public class PurposeSection
{
    private PurposeSection() { } // EF owned type

    /// <summary>Finalidad explícita y específica del tratamiento.</summary>
    public string Purpose { get; private set; } = default!;

    /// <summary>Base de licitud seleccionada del catálogo.</summary>
    public LegalBasis LegalBasis { get; private set; }

    /// <summary>Justificación de por qué aplica la base declarada.</summary>
    public string LegalBasisJustification { get; private set; } = default!;

    public static PurposeSection Create(
        string purpose,
        LegalBasis legalBasis,
        string legalBasisJustification)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("La finalidad no puede ser vacía.", nameof(purpose));
        if (string.IsNullOrWhiteSpace(legalBasisJustification))
            throw new ArgumentException("La justificación de base de licitud es obligatoria.", nameof(legalBasisJustification));

        return new PurposeSection
        {
            Purpose = purpose.Trim(),
            LegalBasis = legalBasis,
            LegalBasisJustification = legalBasisJustification.Trim()
        };
    }
}
