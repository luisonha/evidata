namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Política de retención de datos del tratamiento (doc 14, sec 5.8).
/// Value object embebido (owned type).
/// </summary>
public class RetentionSection
{
    private RetentionSection() { }

    /// <summary>Período de retención en meses (null = indefinido/no declarado).</summary>
    public int? RetentionMonths { get; private set; }

    /// <summary>Descripción textual del período (ej. "5 años desde extinción contrato").</summary>
    public string PeriodDescription { get; private set; } = default!;

    /// <summary>Justificación legal o regulatoria del período declarado.</summary>
    public string? LegalJustification { get; private set; }

    public static RetentionSection Create(
        string periodDescription,
        int? retentionMonths = null,
        string? legalJustification = null)
    {
        if (string.IsNullOrWhiteSpace(periodDescription))
            throw new ArgumentException("La descripción del período de retención es obligatoria.", nameof(periodDescription));

        if (retentionMonths.HasValue && retentionMonths.Value <= 0)
            throw new ArgumentException("Los meses de retención deben ser un valor positivo.", nameof(retentionMonths));

        return new RetentionSection
        {
            PeriodDescription = periodDescription.Trim(),
            RetentionMonths = retentionMonths,
            LegalJustification = legalJustification?.Trim()
        };
    }
}
