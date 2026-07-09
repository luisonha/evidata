namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Entrada de categoría de dato dentro de un tratamiento RAT (doc 14, sec 5.4).
/// Colección embebida — almacenada como JSONB en PG.
/// </summary>
public class DataCategoryEntry
{
    private DataCategoryEntry() { } // deserialización

    /// <summary>ID de referencia a catálogo global DataCategory (LegalKnowledge).</summary>
    public Guid DataCategoryId { get; private set; }

    /// <summary>Nombre local o alias del dato en este tratamiento (opcional).</summary>
    public string? LocalName { get; private set; }

    /// <summary>Sensibilidad declarada para este dato en este tratamiento.</summary>
    public DataSensitivityLevel Sensitivity { get; private set; }

    /// <summary>Comentario adicional.</summary>
    public string? Comment { get; private set; }

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

/// <summary>Nivel de sensibilidad de un dato en el contexto del tratamiento.</summary>
public enum DataSensitivityLevel
{
    /// <summary>Datos ordinarios (nombre, dirección, email).</summary>
    Ordinary,
    /// <summary>Datos financieros, laborales o comerciales con riesgo medio.</summary>
    Sensitive,
    /// <summary>Datos especialmente protegidos: salud, origen, religión, biometría, NNA.</summary>
    SpecialCategory
}
