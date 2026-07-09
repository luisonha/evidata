namespace Evidata.Modules.Mcp.Domain;

/// <summary>
/// Interacción con el asistente MCP (Model Context Protocol).
/// Registra la pregunta, respuesta generada, nivel de riesgo y flag HITL.
/// Append-only una vez completada — solo <see cref="RequestHumanReview"/> puede
/// modificar el estado tras la creación.
/// </summary>
public sealed class McpInteraction
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }

    public string Question { get; private set; } = default!;
    public string Answer { get; private set; } = default!;

    public McpRiskLevel RiskLevel { get; private set; }

    /// <summary>Indica si la respuesta usó contexto específico del tenant.</summary>
    public bool UsedTenantContext { get; private set; }

    /// <summary>Flag HITL — indica que la respuesta requiere revisión humana.</summary>
    public bool RequiresHumanReview { get; private set; }

    public McpInteractionStatus Status { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private readonly List<McpCitation> _citations = new();
    public IReadOnlyList<McpCitation> Citations => _citations.AsReadOnly();

    private McpInteraction() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static McpInteraction Record(
        Guid tenantId,
        Guid userId,
        string question,
        string answer,
        McpRiskLevel riskLevel,
        bool usedTenantContext,
        bool requiresHumanReview)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(answer);

        var status = requiresHumanReview
            ? McpInteractionStatus.ReviewRequested
            : McpInteractionStatus.Completed;

        return new McpInteraction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Question = question.Trim(),
            Answer = answer.Trim(),
            RiskLevel = riskLevel,
            UsedTenantContext = usedTenantContext,
            RequiresHumanReview = requiresHumanReview,
            Status = status,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Fabrica de error — cuando el modelo falló al generar la respuesta.
    /// </summary>
    public static McpInteraction RecordFailed(Guid tenantId, Guid userId, string question)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        return new McpInteraction
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Question = question.Trim(),
            Answer = string.Empty,
            RiskLevel = McpRiskLevel.High,
            UsedTenantContext = false,
            RequiresHumanReview = true,
            Status = McpInteractionStatus.Failed,
            OccurredAt = DateTimeOffset.UtcNow
        };
    }

    // ── Mutaciones ────────────────────────────────────────────────────────────

    /// <summary>
    /// Eleva el status a ReviewRequested si no estaba ya en ese estado o Failed.
    /// Se puede llamar desde cualquier estado Completed para escalar.
    /// </summary>
    public void RequestHumanReview()
    {
        if (Status == McpInteractionStatus.Failed)
            throw new InvalidOperationException(
                "No se puede solicitar revisión sobre una interacción fallida.");

        RequiresHumanReview = true;
        Status = McpInteractionStatus.ReviewRequested;
    }

    /// <summary>Agrega una citación a la interacción.</summary>
    public McpCitation AddCitation(
        McpCitationSourceType sourceType,
        string sourceId,
        string fragment,
        string? sourceVersion = null)
    {
        var citation = McpCitation.Create(Id, sourceType, sourceId, fragment, sourceVersion);
        _citations.Add(citation);
        return citation;
    }
}
