using Evidata.Modules.Mcp.Domain.RiskRouting;

namespace Evidata.Modules.Mcp.Application.RiskRouting;

/// <summary>
/// Servicio de clasificación y enrutamiento de riesgo para consultas MCP.
/// Desacoplado del clasificador para permitir sustitución (ML, reglas externas).
/// </summary>
public interface IMcpRiskRouter
{
    /// <summary>
    /// Clasifica la pregunta y retorna el nivel de riesgo con señales detectadas.
    /// </summary>
    RiskClassificationResult Classify(string question);

    /// <summary>
    /// Indica si la interacción requiere revisión humana según su nivel de riesgo.
    /// Regla: cualquier High (incluyendo abstención) dispara HITL.
    /// </summary>
    bool RequiresHumanReview(RiskClassificationResult result);
}
