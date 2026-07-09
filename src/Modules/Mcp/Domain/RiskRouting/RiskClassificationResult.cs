namespace Evidata.Modules.Mcp.Domain.RiskRouting;

/// <summary>
/// Resultado de la clasificación de riesgo de una consulta MCP.
/// </summary>
public sealed record RiskClassificationResult(
    McpRiskLevel RiskLevel,
    bool ShouldAbstain,
    string? AbstentionReason,
    IReadOnlyList<string> MatchedSignals)
{
    public static RiskClassificationResult Low() =>
        new(McpRiskLevel.Low, false, null, Array.Empty<string>());

    public static RiskClassificationResult Medium(IReadOnlyList<string> signals) =>
        new(McpRiskLevel.Medium, false, null, signals);

    public static RiskClassificationResult High(IReadOnlyList<string> signals) =>
        new(McpRiskLevel.High, false, null, signals);

    public static RiskClassificationResult Abstain(string reason, IReadOnlyList<string> signals) =>
        new(McpRiskLevel.High, true, reason, signals);
}
