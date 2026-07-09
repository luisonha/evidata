namespace Evidata.Modules.Security.Application.Abstractions;

/// <summary>
/// Calculates available and blocked actions for a user on a resource within a tenant.
/// Returns a generic result that can be mapped to UI-specific view models.
/// </summary>
public interface IResourcePermissionsQueryService
{
    /// <summary>
    /// Get available and blocked actions for a user on a resource in a tenant.
    /// </summary>
    /// <param name="userId">User to check permissions for.</param>
    /// <param name="tenantId">Tenant context.</param>
    /// <param name="resourceType">Type of resource (e.g., "processingActivity").</param>
    /// <param name="resourceId">ID of the specific resource instance.</param>
    /// <param name="context">Optional context data (e.g., owner userId, activity status, gap flags).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Permissions result with available and blocked actions (string-based for portability).</returns>
    Task<ResourcePermissionsResult> GetResourcePermissionsAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        Guid resourceId,
        ResourceContextData? context = null,
        CancellationToken ct = default);
}

/// <summary>
/// Context data for permission evaluation (e.g., ProcessingActivity owner, status, evidence flags).
/// </summary>
public record ResourceContextData(
    Guid? ResourceOwnerId = null,
    object? Status = null,
    IReadOnlyDictionary<string, object>? CustomFlags = null);

/// <summary>
/// Generic result containing available and blocked actions.
/// </summary>
public record ResourcePermissionsResult(
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<AvailableActionResult> AvailableActions,
    bool ReadOnly,
    IReadOnlyList<BlockedActionResult> BlockedActions);

/// <summary>
/// Available action result.
/// </summary>
public record AvailableActionResult(
    string ActionCode,
    string LabelKey);

/// <summary>
/// Blocked action result with reason.
/// </summary>
public record BlockedActionResult(
    string ActionCode,
    string ActionLabelKey,
    string ReasonCode,
    string ReasonLabelKey,
    string Severity,
    string SeverityLabelKey,
    string? RelatedNode = null,
    string? RelatedNodeLabelKey = null);

