using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.Workflow.Application.Abstractions;

/// <summary>
/// Service abstraction for notifying downstream modules about Review state transitions.
/// Implementations encapsulate the messaging mechanism (Outbox, MediatR, etc.).
/// </summary>
public interface IReviewNotificationService
{
    /// <summary>
    /// Notifies when a review transitions to Approved status.
    /// Downstream modules should respond by marking reviewed entities.
    /// </summary>
    Task NotifyApprovedAsync(Review review, CancellationToken ct = default);

    /// <summary>
    /// Notifies when a review transitions to ChangesRequested status.
    /// Downstream modules may need to revert earlier processing.
    /// </summary>
    Task NotifyChangesRequestedAsync(Review review, CancellationToken ct = default);

    /// <summary>
    /// Notifies when a review transitions to Cancelled status.
    /// Downstream modules should undo any processing triggered by this review.
    /// </summary>
    Task NotifyCancelledAsync(Review review, CancellationToken ct = default);
}
