using System.Text.Json.Serialization;

namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Proveedor/encargado de tratamiento involucrado (doc 14, sec 5.7).
/// Colección JSONB en processing_activities.
/// </summary>
public class SupplierEntry
{
    [JsonConstructor]
    public SupplierEntry() { }

    public string SupplierName { get; init; } = default!;
    public string? Country { get; init; }
    public string? ServiceDescription { get; init; }
    public bool HasDataProcessingAgreement { get; init; }

    public static SupplierEntry Create(
        string supplierName,
        string? country = null,
        string? serviceDescription = null,
        bool hasDataProcessingAgreement = false)
    {
        if (string.IsNullOrWhiteSpace(supplierName))
            throw new ArgumentException("El nombre del proveedor no puede ser vacío.", nameof(supplierName));

        return new SupplierEntry
        {
            SupplierName = supplierName.Trim(),
            Country = country?.Trim(),
            ServiceDescription = serviceDescription?.Trim(),
            HasDataProcessingAgreement = hasDataProcessingAgreement
        };
    }
}
