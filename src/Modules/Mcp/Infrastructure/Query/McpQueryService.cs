using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Query;
using Evidata.Modules.Mcp.Application.RatContext;
using Evidata.Modules.Mcp.Application.RiskRouting;
using Evidata.Modules.Mcp.Domain;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Mcp.Infrastructure.Query;

/// <summary>
/// Implementación del servicio de consulta MCP.
///
/// Flujo:
///   1. Clasificar riesgo de la pregunta (señales de texto)
///   2. Abstención inmediata si la consulta requiere asesoría legal
///   3. Obtener contexto RAT del tenant (RATs aprobados)
///   4. Construir respuesta enriquecida con los RATs relevantes
///   5. Registrar la interacción con flags correctos
///   6. Devolver respuesta con referencias y nivel de riesgo
///
/// <strong>Nota:</strong> La respuesta textual actual es generada por un
/// motor de reglas determinista. El módulo está diseñado para sustituirlo
/// por un LLM (Azure OpenAI) sin cambiar los contratos.
/// </summary>
public sealed class McpQueryService : IMcpQueryService
{
    private readonly IMcpRiskRouter _riskRouter;
    private readonly IRatContextProvider _ratContextProvider;
    private readonly IMcpInteractionService _interactions;
    private readonly ILogger<McpQueryService> _logger;

    public McpQueryService(
        IMcpRiskRouter riskRouter,
        IRatContextProvider ratContextProvider,
        IMcpInteractionService interactions,
        ILogger<McpQueryService> logger)
    {
        _riskRouter          = riskRouter;
        _ratContextProvider  = ratContextProvider;
        _interactions        = interactions;
        _logger              = logger;
    }

    public async Task<McpQueryResponse> QueryAsync(McpQueryRequest request, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Question);

        // ── 1. Clasificar riesgo ──────────────────────────────────────────────
        var classification = _riskRouter.Classify(request.Question);

        _logger.LogInformation(
            "MCP query | Tenant={Tenant} User={User} Risk={Risk} Abstain={Abstain} Signals={Signals}",
            request.TenantId, request.UserId,
            classification.RiskLevel, classification.ShouldAbstain,
            string.Join(", ", classification.MatchedSignals));

        // ── 2. Abstención ─────────────────────────────────────────────────────
        if (classification.ShouldAbstain)
        {
            var failedInteraction = await _interactions.RecordFailedAsync(
                request.TenantId, request.UserId, request.Question, ct);

            _logger.LogWarning(
                "MCP abstención | InteractionId={Id} Razón={Reason}",
                failedInteraction.Id, classification.AbstentionReason);

            return new McpQueryResponse(
                InteractionId: failedInteraction.Id,
                Answer: BuildAbstentionAnswer(classification.AbstentionReason!),
                RiskLevel: McpRiskLevel.High,
                RequiresHumanReview: false,
                UsedTenantContext: false,
                RatReferences: [],
                AbstentionReason: classification.AbstentionReason);
        }

        // ── 3. Contexto RAT del tenant ────────────────────────────────────────
        var ratSnapshot = await _ratContextProvider.GetSnapshotAsync(request.TenantId, ct);
        var usedContext = !ratSnapshot.IsEmpty;

        _logger.LogInformation(
            "MCP contexto RAT | Tenant={Tenant} ApprovedRATs={Count} HasSensitiveData={Sensitive}",
            request.TenantId, ratSnapshot.ApprovedActivities.Count, ratSnapshot.HasAnySensitiveData);

        // ── 4. Construir respuesta con contexto ───────────────────────────────
        var relevantRats = SelectRelevantRats(request.Question, ratSnapshot);
        var answer = BuildAnswer(request.Question, classification.RiskLevel, ratSnapshot, relevantRats);
        var requiresHumanReview = _riskRouter.RequiresHumanReview(classification);

        // ── 5. Registrar interacción ──────────────────────────────────────────
        var interaction = await _interactions.RecordAsync(
            tenantId: request.TenantId,
            userId: request.UserId,
            question: request.Question,
            answer: answer,
            riskLevel: classification.RiskLevel,
            usedTenantContext: usedContext,
            requiresHumanReview: requiresHumanReview,
            ct: ct);

        // Agregar citaciones a los RATs usados
        foreach (var rat in relevantRats)
        {
            await _interactions.AddCitationAsync(
                interaction.Id,
                McpCitationSourceType.ProcessingActivity,
                rat.ActivityId.ToString(),
                $"Actividad de tratamiento: {rat.Name}",
                ct: ct);
        }

        _logger.LogInformation(
            "MCP interacción registrada | Id={Id} Risk={Risk} HITL={Hitl} UsedContext={Context} Citations={Citations}",
            interaction.Id, classification.RiskLevel, requiresHumanReview, usedContext, relevantRats.Count);

        return new McpQueryResponse(
            InteractionId: interaction.Id,
            Answer: answer,
            RiskLevel: classification.RiskLevel,
            RequiresHumanReview: requiresHumanReview,
            UsedTenantContext: usedContext,
            RatReferences: relevantRats.Select(r => new RatContextReference(r.ActivityId, r.Name, r.Department)).ToList());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Selecciona los RATs más relevantes para la consulta por coincidencia de términos.
    /// Sin ML — coincidencia simple de palabras en nombre/departamento.
    /// Máximo 3 para no saturar la respuesta.
    /// </summary>
    private static List<RatActivitySummary> SelectRelevantRats(
        string question, RatContextSnapshot snapshot)
    {
        if (snapshot.IsEmpty) return [];

        var words = question.ToLowerInvariant()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return snapshot.ApprovedActivities
            .OrderByDescending(a =>
            {
                var text = $"{a.Name} {a.Department}".ToLowerInvariant();
                return words.Count(w => w.Length > 3 && text.Contains(w));
            })
            .Take(3)
            .ToList();
    }

    private static string BuildAnswer(
        string question,
        McpRiskLevel riskLevel,
        RatContextSnapshot snapshot,
        List<RatActivitySummary> relevantRats)
    {
        var sb = new System.Text.StringBuilder();

        // Advertencia de riesgo alto
        if (riskLevel == McpRiskLevel.High)
        {
            sb.AppendLine("⚠️ **Esta consulta ha sido marcada para revisión humana** antes de aplicar la recomendación.");
            sb.AppendLine();
        }
        else if (riskLevel == McpRiskLevel.Medium)
        {
            sb.AppendLine("ℹ️ Esta respuesta es orientativa. Consulta con tu DPO antes de tomar decisiones.");
            sb.AppendLine();
        }

        // Contexto RAT
        if (!snapshot.IsEmpty)
        {
            sb.AppendLine($"**Contexto del tenant:** {snapshot.ApprovedActivities.Count} actividad(es) de tratamiento aprobada(s).");

            if (snapshot.HasAnySensitiveData)
                sb.AppendLine("🔴 Tu organización trata **datos sensibles** — aplican obligaciones reforzadas (Art. 16 Ley 21.719).");
            if (snapshot.HasAnyInternationalTransfer)
                sb.AppendLine("🌍 Detectadas **transferencias internacionales** — verificar adecuación o garantías apropiadas.");

            if (relevantRats.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("**Actividades de tratamiento relevantes para tu consulta:**");
                foreach (var rat in relevantRats)
                {
                    sb.Append($"• **{rat.Name}**");
                    if (rat.Department != null) sb.Append($" ({rat.Department})");
                    if (rat.HasSensitiveData) sb.Append(" — datos sensibles");
                    if (rat.LegalBasis != null) sb.Append($" | Base: {rat.LegalBasis}");
                    sb.AppendLine();
                }
            }

            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("ℹ️ No se encontraron actividades de tratamiento aprobadas para este tenant.");
            sb.AppendLine("Completa y aprueba tus RATs para obtener respuestas contextualizadas.");
            sb.AppendLine();
        }

        // Respuesta base según nivel de riesgo
        sb.AppendLine(riskLevel == McpRiskLevel.High
            ? "La consulta contiene señales de alta complejidad legal. Se ha escalado para revisión por tu DPO."
            : "Basado en el contexto de tus tratamientos registrados, asegúrate de que las actividades relevantes cuenten con base de licitud válida y estén correctamente documentadas conforme a la Ley 21.719.");

        return sb.ToString().Trim();
    }

    private static string BuildAbstentionAnswer(string reason) =>
        $"⛔ **No puedo responder esta consulta.**\n\n{reason}\n\n" +
        "Por favor, consulta directamente con un abogado especializado en protección de datos o con tu Delegado de Protección de Datos (DPO).";
}
