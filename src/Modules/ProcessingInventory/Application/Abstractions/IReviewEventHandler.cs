using Evidata.Modules.Workflow.Application.Notifications;

namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>
/// Handler para eventos de Review que afectan ProcessingActivity.
/// Se inyecta en Workflow para ser invocado cuando reviews transicionan.
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
