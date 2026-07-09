namespace Evidata.Modules.Mcp.Domain;

/// <summary>
/// Citación de fuente usada en una respuesta MCP.
/// Inmutable — se crea junto con la interacción y nunca se modifica.
/// </summary>
public sealed class McpCitation
{
    public Guid Id { get; private set; }
    public Guid InteractionId { get; private set; }

    public McpCitationSourceType SourceType { get; private set; }

    /// <summary>Identificador de la fuente (ej: id de documento, id de norma).</summary>
    public string SourceId { get; private set; } = default!;

    /// <summary>Versión de la fuente en el momento de la cita (nullable).</summary>
    public string? SourceVersion { get; private set; }

    /// <summary>Fragmento de texto citado o referencia directa.</summary>
    public string Fragment { get; private set; } = default!;

    public DateTimeOffset CreatedAt { get; private set; }

    private McpCitation() { }

    public static McpCitation Create(
        Guid interactionId,
        McpCitationSourceType sourceType,
        string sourceId,
        string fragment,
        string? sourceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(fragment);

        return new McpCitation
        {
            Id = Guid.NewGuid(),
            InteractionId = interactionId,
            SourceType = sourceType,
            SourceId = sourceId.Trim(),
            Fragment = fragment.Trim(),
            SourceVersion = sourceVersion?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
