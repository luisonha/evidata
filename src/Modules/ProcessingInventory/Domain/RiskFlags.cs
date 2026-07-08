namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Flags de riesgo regulatorio de un tratamiento RAT (doc 14, sec 9).
///
/// Son calculados automáticamente por <see cref="ProcessingActivity.RecalculateFlags"/>
/// y persisten como columnas booleanas para facilitar consultas y filtros.
///
/// Algunos flags bloquean la aprobación si no se resuelven.
/// </summary>
public class RiskFlags
{
    private RiskFlags() { }

    // ── Flags de datos ─────────────────────────────────────────────────────────

    /// <summary>Tratamiento incluye categoría de dato sensible.</summary>
    public bool SensitiveData { get; private set; }

    /// <summary>Tratamiento incluye datos de niños, niñas o adolescentes (NNA).</summary>
    public bool ChildrenData { get; private set; }

    /// <summary>Tratamiento incluye datos biométricos.</summary>
    public bool BiometricData { get; private set; }

    // ── Flags operacionales ────────────────────────────────────────────────────

    /// <summary>Se declara transferencia internacional (destinatario fuera de Chile).</summary>
    public bool InternationalTransfer { get; private set; }

    /// <summary>Se declara decisión automatizada o perfilamiento.</summary>
    public bool AutomatedDecision { get; private set; }

    // ── Flags de completitud ───────────────────────────────────────────────────

    /// <summary>Falta evidencia para una base de licitud que requiere prueba (Consent, LegitimateInterest).</summary>
    public bool MissingLegalBasisEvidence { get; private set; }

    /// <summary>No se ha definido período de retención.</summary>
    public bool MissingRetention { get; private set; }

    /// <summary>No existen medidas de seguridad declaradas (bloqueante si hay datos sensibles).</summary>
    public bool MissingSecurityMeasures { get; private set; }

    /// <summary>Hay al menos una brecha crítica abierta sin aceptación formal.</summary>
    public bool CriticalGapOpen { get; private set; }

    // ── Derived ────────────────────────────────────────────────────────────────

    /// <summary>True si el tratamiento tiene algún flag de riesgo elevado que requiere revisión reforzada.</summary>
    public bool RequiresEnhancedReview => ChildrenData || BiometricData || AutomatedDecision;

    /// <summary>True si hay algún flag que bloquea la aprobación.</summary>
    public bool BlocksApproval =>
        (SensitiveData && MissingSecurityMeasures) ||
        MissingLegalBasisEvidence ||
        CriticalGapOpen;

    /// <summary>True si no hay ningún flag activo.</summary>
    public bool IsClean =>
        !SensitiveData && !ChildrenData && !BiometricData &&
        !InternationalTransfer && !AutomatedDecision &&
        !MissingLegalBasisEvidence && !MissingRetention &&
        !MissingSecurityMeasures && !CriticalGapOpen;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static RiskFlags Empty() => new();

    public static RiskFlags Calculate(
        IReadOnlyList<DataCategoryEntry> dataCategories,
        IReadOnlyList<SecurityMeasureEntry> securityMeasures,
        RetentionSection? retention,
        PurposeSection? purpose,
        bool hasInternationalTransfer,
        bool hasAutomatedDecision,
        bool hasCriticalGapOpen)
    {
        var hasSensitive = dataCategories.Any(d =>
            d.Sensitivity is DataSensitivityLevel.Sensitive or DataSensitivityLevel.SpecialCategory);

        var hasChildren = dataCategories.Any(d =>
            d.Sensitivity == DataSensitivityLevel.SpecialCategory);

        // Biometría: SpecialCategory con comentario que incluya "biom" (heurística hasta tener catálogo integrado)
        var hasBiometric = dataCategories.Any(d =>
            d.Sensitivity == DataSensitivityLevel.SpecialCategory &&
            d.Comment != null &&
            d.Comment.Contains("biom", StringComparison.OrdinalIgnoreCase));

        // Base de licitud que requiere prueba documental
        var needsEvidenceForBasis = purpose?.LegalBasis is LegalBasis.Consent or LegalBasis.LegitimateInterest;

        return new RiskFlags
        {
            SensitiveData = hasSensitive,
            ChildrenData = hasChildren,
            BiometricData = hasBiometric,
            InternationalTransfer = hasInternationalTransfer,
            AutomatedDecision = hasAutomatedDecision,
            MissingLegalBasisEvidence = needsEvidenceForBasis, // se limpia cuando se asocia evidencia (f5-rat-evidencias)
            MissingRetention = retention is null,
            MissingSecurityMeasures = hasSensitive && securityMeasures.Count == 0,
            CriticalGapOpen = hasCriticalGapOpen
        };
    }
}
