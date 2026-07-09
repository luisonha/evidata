namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Resultado de una validación de completitud del RAT.
/// Inmutable — construido por <see cref="ProcessingActivityValidator"/>.
/// </summary>
public sealed class ValidationResult
{
    private ValidationResult() { }

    public bool IsValid { get; private init; }

    /// <summary>Lista de errores que impiden la transición solicitada.</summary>
    public IReadOnlyList<string> Errors { get; private init; } = [];

    /// <summary>Advertencias no bloqueantes (recomendaciones).</summary>
    public IReadOnlyList<string> Warnings { get; private init; } = [];

    public static ValidationResult Ok(IEnumerable<string>? warnings = null) =>
        new() { IsValid = true, Warnings = (warnings ?? []).ToList().AsReadOnly() };

    public static ValidationResult Fail(
        IEnumerable<string> errors,
        IEnumerable<string>? warnings = null) =>
        new()
        {
            IsValid = false,
            Errors = errors.ToList().AsReadOnly(),
            Warnings = (warnings ?? []).ToList().AsReadOnly()
        };
}

/// <summary>
/// Valida la completitud de un <see cref="ProcessingActivity"/> antes de transiciones clave.
///
/// Reglas según doc 14, sec 8:
///
/// Para enviar a revisión (SubmitForReview):
///   - Nombre (siempre presente).
///   - Finalidad y base de licitud.
///   - Al menos una categoría de datos.
///   - Al menos un tipo de titular.
///
/// Para aprobar (Approve):
///   - Todo lo anterior más:
///   - Justificación de base de licitud (incluida en PurposeSection).
///   - No hay flags que bloqueen aprobación (BlocksApproval == false).
///   - Medidas de seguridad si hay datos sensibles.
///   - Retención declarada (warning si no, error configurable).
/// </summary>
public static class ProcessingActivityValidator
{
    // ── Validación para SubmitForReview ────────────────────────────────────────

    public static ValidationResult ValidateForReview(ProcessingActivity activity)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Nombre siempre obligatorio (ya validado en Create/Update pero lo confirmamos)
        if (string.IsNullOrWhiteSpace(activity.Name))
            errors.Add("El nombre del tratamiento es obligatorio.");

        // Finalidad y base de licitud
        if (activity.Purpose is null)
            errors.Add("La sección de finalidad y base de licitud es obligatoria.");

        // Al menos una categoría de datos
        if (activity.DataCategories.Count == 0)
            errors.Add("Debe declararse al menos una categoría de datos.");

        // Al menos un tipo de titular
        if (activity.DataSubjects.Count == 0)
            errors.Add("Debe declararse al menos un tipo de titular.");

        // Advertencias no bloqueantes
        if (activity.Retention is null)
            warnings.Add("Se recomienda declarar la política de retención antes de enviar a revisión.");

        if (activity.SecurityMeasures.Count == 0)
            warnings.Add("Se recomienda declarar medidas de seguridad antes de enviar a revisión.");

        return errors.Count > 0
            ? ValidationResult.Fail(errors, warnings)
            : ValidationResult.Ok(warnings);
    }

    // ── Validación para Approve ────────────────────────────────────────────────

    public static ValidationResult ValidateForApproval(
        ProcessingActivity activity,
        bool retentionRequired = false)
    {
        var errors = new List<string>();
        var warnings = new List<string>();

        // Incluye todas las validaciones de revisión
        var reviewResult = ValidateForReview(activity);
        errors.AddRange(reviewResult.Errors);

        // Flags que bloquean aprobación
        if (activity.Flags.MissingSecurityMeasures)
            errors.Add("Faltan medidas de seguridad: obligatorias para datos sensibles o de categoría especial.");

        if (activity.Flags.MissingLegalBasisEvidence)
            errors.Add("Falta evidencia de base de licitud: obligatoria para Consentimiento e Interés Legítimo.");

        if (activity.Flags.CriticalGapOpen)
            errors.Add("Existen brechas críticas abiertas sin aceptación formal: deben cerrarse antes de aprobar.");

        // Retención — configurable como error o warning
        if (activity.Retention is null)
        {
            if (retentionRequired)
                errors.Add("La política de retención es obligatoria para aprobar este tratamiento.");
            else
                warnings.Add("La retención no está declarada. Se recomienda documentarla antes de aprobar.");
        }

        // Advertencia si hay flags de riesgo elevado sin revisión de seguridad declarada
        if (activity.Flags.RequiresEnhancedReview && activity.SecurityMeasures.Count == 0)
            warnings.Add("El tratamiento requiere revisión reforzada (NNA/biometría/automatización). Declare medidas específicas.");

        return errors.Count > 0
            ? ValidationResult.Fail(errors, warnings)
            : ValidationResult.Ok(warnings);
    }
}
