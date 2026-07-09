using System.Text.Json.Serialization;

namespace Evidata.Modules.ProcessingInventory.Domain;

public enum DataSubjectType
{
    Employees, Customers, Applicants, Suppliers, WebUsers,
    Children, Patients, Students, PublicOfficials, Other
}

/// <summary>
/// Entrada de categoría de titular dentro de un tratamiento RAT.
/// Colección embebida — almacenada como JSONB en PG.
/// </summary>
public class DataSubjectEntry
{
    [JsonConstructor]
    private DataSubjectEntry() { }

    public DataSubjectType SubjectType { get; init; }
    public string? Description { get; init; }
    public int? EstimatedCount { get; init; }

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
