using System.Text.Json.Serialization;

namespace Evidata.Modules.ProcessingInventory.Domain;

public enum SecurityMeasureType { Technical, Organizational, Physical }

/// <summary>
/// Medida de seguridad declarada para el tratamiento (doc 14, sec 5.9).
/// Colección JSONB en processing_activities.
/// </summary>
public class SecurityMeasureEntry
{
    [JsonConstructor]
    private SecurityMeasureEntry() { }

    public SecurityMeasureType MeasureType { get; init; }
    public string Description { get; init; } = default!;
    public Guid? SecurityMeasureCatalogId { get; init; }

    public static SecurityMeasureEntry Create(
        SecurityMeasureType measureType,
        string description,
        Guid? catalogId = null)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("La descripción de la medida es obligatoria.", nameof(description));

        return new SecurityMeasureEntry
        {
            MeasureType = measureType,
            Description = description.Trim(),
            SecurityMeasureCatalogId = catalogId
        };
    }
}
