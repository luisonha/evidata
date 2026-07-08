namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Sistema interno involucrado en el tratamiento (doc 14, sec 5.6).
/// Colección JSONB en processing_activities.
/// </summary>
public class SystemEntry
{
    private SystemEntry() { }

    /// <summary>Nombre del sistema o aplicación.</summary>
    public string SystemName { get; private set; } = default!;

    /// <summary>Descripción del rol que cumple en el tratamiento.</summary>
    public string? Role { get; private set; }

    /// <summary>¿Es un sistema de terceros (SaaS externo)?</summary>
    public bool IsExternal { get; private set; }

    public static SystemEntry Create(string systemName, string? role = null, bool isExternal = false)
    {
        if (string.IsNullOrWhiteSpace(systemName))
            throw new ArgumentException("El nombre del sistema no puede ser vacío.", nameof(systemName));

        return new SystemEntry
        {
            SystemName = systemName.Trim(),
            Role = role?.Trim(),
            IsExternal = isExternal
        };
    }
}
