namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>
/// Deprecated: Use <see cref="Evidata.Modules.Contracts.Notifications.IReviewEventHandler"/> instead.
/// 
/// P1-019: IReviewEventHandler is now defined in Evidata.Modules.Contracts
/// to eliminate circular dependencies between Workflow and ProcessingInventory.
/// This namespace alias is kept for backward compatibility.
/// </summary>
using IReviewEventHandler = Evidata.Modules.Contracts.Notifications.IReviewEventHandler;

