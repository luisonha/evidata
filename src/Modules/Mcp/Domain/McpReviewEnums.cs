namespace Evidata.Modules.Mcp.Domain;

/// <summary>Estado de la tarea de revisión humana (HITL).</summary>
public enum McpReviewTaskStatus
{
    Open,
    InProgress,
    Approved,
    Rejected
}

/// <summary>Valoración del usuario sobre la respuesta del asistente.</summary>
public enum McpFeedbackRating
{
    Helpful,
    NotHelpful
}
