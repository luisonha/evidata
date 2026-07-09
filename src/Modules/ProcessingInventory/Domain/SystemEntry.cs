using System.Text.Json.Serialization;

namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Sistema interno involucrado en el tratamiento (doc 14, sec 5.6).
/// Colección JSONB en processing_activities.
/// </summary>
public class SystemEntry
{
    [JsonConstructor]
    private SystemEntry() { }

    public string SystemName { get; init; } = default!;
    public string? Role { get; init; }
    public bool IsExternal { get; init; }

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
