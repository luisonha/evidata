namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Proveedor/encargado de tratamiento involucrado (doc 14, sec 5.7).
/// Colección JSONB en processing_activities.
/// </summary>
public class SupplierEntry
{
    private SupplierEntry() { }

    /// <summary>Nombre del proveedor o encargado.</summary>
    public string SupplierName { get; private set; } = default!;

    /// <summary>País de domicilio del proveedor (relevante para transferencias).</summary>
    public string? Country { get; private set; }

    /// <summary>Servicio o función que presta.</summary>
    public string? ServiceDescription { get; private set; }

    /// <summary>¿Existe contrato de encargo de tratamiento firmado?</summary>
    public bool HasDataProcessingAgreement { get; private set; }

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
