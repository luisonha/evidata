namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Tipo de titular de datos afectado por el tratamiento (doc 14, sec 5.5).
/// </summary>
public enum DataSubjectType
{
    Employees,
    Customers,
    Applicants,
    Suppliers,
    WebUsers,
    Children,        // NNA — activa flag de riesgo
    Patients,
    Students,
    PublicOfficials,
    Other
}

/// <summary>
/// Entrada de categoría de titular dentro de un tratamiento RAT.
/// Colección embebida — almacenada como JSONB en PG.
/// </summary>
public class DataSubjectEntry
{
    private DataSubjectEntry() { } // deserialización

    public DataSubjectType SubjectType { get; private set; }

    /// <summary>Descripción libre cuando SubjectType == Other.</summary>
    public string? Description { get; private set; }

    /// <summary>Estimado de cantidad de titulares afectados (opcional).</summary>
    public int? EstimatedCount { get; private set; }

    public static DataSubjectEntry Create(
        DataSubjectType subjectType,
        string? description = null,
        int? estimatedCount = null)
    {
        if (subjectType == DataSubjectType.Other && string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "Se requiere descripción cuando el tipo de titular es 'Other'.", nameof(description));

        if (estimatedCount.HasValue && estimatedCount.Value < 0)
            throw new ArgumentException("EstimatedCount no puede ser negativo.", nameof(estimatedCount));

        return new DataSubjectEntry
        {
            SubjectType = subjectType,
            Description = description?.Trim(),
            EstimatedCount = estimatedCount
        };
    }
}
