using System.Text.Json.Serialization;

namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Entrada de categoría de dato dentro de un tratamiento RAT (doc 14, sec 5.4).
/// Colección embebida — almacenada como JSONB en PG.
/// </summary>
public class DataCategoryEntry
{
    [JsonConstructor]
    public DataCategoryEntry() { }

    public Guid DataCategoryId { get; init; }
    public string? LocalName { get; init; }
    public DataSensitivityLevel Sensitivity { get; init; }
    public string? Comment { get; init; }

    public static DataCategoryEntry Create(
        Guid dataCategoryId,
        DataSensitivityLevel sensitivity,
        string? localName = null,
        string? comment = null)
    {
        if (dataCategoryId == Guid.Empty)
            throw new ArgumentException("DataCategoryId no puede ser Guid vacío.", nameof(dataCategoryId));

        return new DataCategoryEntry
        {
            DataCategoryId = dataCategoryId,
            Sensitivity = sensitivity,
            LocalName = localName?.Trim(),
            Comment = comment?.Trim()
        };
    }
}

public enum DataSensitivityLevel
{
    Ordinary,
    Sensitive,
    SpecialCategory
}
