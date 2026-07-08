using Evidata.Modules.Mcp.Application.RiskRouting;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Domain.RiskRouting;

namespace Evidata.Modules.Mcp.Infrastructure.RiskRouting;

/// <summary>
/// Implementación de <see cref="IMcpRiskRouter"/> basada en el clasificador de señales.
/// </summary>
public sealed class McpRiskRouter : IMcpRiskRouter
{
    private readonly McpRiskClassifier _classifier = new();

    public RiskClassificationResult Classify(string question) =>
        _classifier.Classify(question);

    public bool RequiresHumanReview(RiskClassificationResult result) =>
        result.RiskLevel == McpRiskLevel.High;
}
