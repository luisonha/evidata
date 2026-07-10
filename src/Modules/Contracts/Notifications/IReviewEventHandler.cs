namespace Evidata.Modules.Contracts.Notifications;

/// <summary>
/// Handler para eventos de Review que afectan múltiples módulos.
/// Se inyecta en Workflow para ser invocado cuando reviews transicionan.
/// 
/// P1-019: Moved from ProcessingInventory.Application.Abstractions to Contracts
/// to eliminate circular dependencies and enable direct DI injection instead of
/// reflection-based service locator pattern.
/// </summary>
public interface IReviewEventHandler
{
    /// <summary>
    /// Maneja el evento ReviewApproved emitido por Workflow.
    /// Si el review target es un ProcessingActivity, marca la actividad como revisada.
    /// </summary>
    Task HandleReviewApprovedAsync(
        ReviewApprovedEventPayload payload,
        CancellationToken ct = default);
}
