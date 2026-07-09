namespace Evidata.Modules.Mcp.Application.RatContext;

/// <summary>
/// Proyección mínima de un tratamiento RAT aprobado para contexto MCP.
/// Solo incluye los campos necesarios para enriquecer una respuesta del asistente.
/// </summary>
public sealed record RatActivitySummary(
    Guid ActivityId,
    string Name,
    string? Department,
    string? LegalBasis,
    IReadOnlyList<string> DataCategoryNames,
    bool HasSensitiveData,
    bool HasInternationalTransfer,
    bool HasAutomatedDecision,
    bool RequiresEnhancedReview);

/// <summary>
/// Snapshot del contexto RAT de un tenant para uso del asistente MCP.
/// </summary>
public sealed record RatContextSnapshot(
    Guid TenantId,
    IReadOnlyList<RatActivitySummary> ApprovedActivities)
{
    public bool IsEmpty => ApprovedActivities.Count == 0;

    public bool HasAnySensitiveData =>
        ApprovedActivities.Any(a => a.HasSensitiveData);

    public bool HasAnyInternationalTransfer =>
        ApprovedActivities.Any(a => a.HasInternationalTransfer);
}
