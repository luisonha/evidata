namespace Evidata.Modules.Mcp.Domain;

/// <summary>
/// Feedback del usuario sobre la calidad de la respuesta del asistente MCP.
/// Append-only — un usuario puede registrar un único feedback por interacción.
/// </summary>
public sealed class McpFeedback
{
    public Guid Id { get; private set; }
    public Guid InteractionId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }

    public McpFeedbackRating Rating { get; private set; }

    /// <summary>Comentario libre del usuario (opcional).</summary>
    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    private McpFeedback() { }

    public static McpFeedback Record(
        Guid interactionId,
        Guid tenantId,
        Guid userId,
        McpFeedbackRating rating,
        string? comment = null)
    {
        return new McpFeedback
        {
            Id = Guid.NewGuid(),
            InteractionId = interactionId,
            TenantId = tenantId,
            UserId = userId,
            Rating = rating,
            Comment = comment?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
