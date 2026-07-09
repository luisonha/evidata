namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>Tipo de medida de seguridad declarada.</summary>
public enum SecurityMeasureType
{
    Technical,
    Organizational,
    Physical
}

/// <summary>
/// Medida de seguridad declarada para el tratamiento (doc 14, sec 5.9).
/// Colección JSONB en processing_activities.
/// Obligatoria si hay datos sensibles o SpecialCategory.
/// </summary>
public class SecurityMeasureEntry
{
    private SecurityMeasureEntry() { }

    public SecurityMeasureType MeasureType { get; private set; }

    /// <summary>Descripción de la medida implementada.</summary>
    public string Description { get; private set; } = default!;

    /// <summary>ID de referencia al catálogo global SecurityMeasure (LegalKnowledge), si aplica.</summary>
    public Guid? SecurityMeasureCatalogId { get; private set; }

    public static SecurityMeasureEntry Create(
        SecurityMeasureType measureType,
        string description,
        Guid? catalogId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción de la medida de seguridad es obligatoria.", nameof(description));

        return new SecurityMeasureEntry
        {
            MeasureType = measureType,
            Description = description.Trim(),
            SecurityMeasureCatalogId = catalogId
        };
    }
}
