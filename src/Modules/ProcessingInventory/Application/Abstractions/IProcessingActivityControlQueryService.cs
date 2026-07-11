using Evidata.Modules.ProcessingInventory.Application.ViewModels;

namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>
/// Abstraction for the /control endpoint query handler.
/// Allows composition handlers in the API layer to implement this contract
/// without creating circular dependencies.
/// P1-FULL-COMPOSITION: Bridges module and API composition layer.
/// </summary>
public interface IProcessingActivityControlQueryService
{
    /// <summary>
    /// Handles the GET /processing-activities/{id}/control request.
    /// </summary>
    Task<ProcessingActivityControlViewModel?> HandleAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid userId,
        CancellationToken ct = default);
}
