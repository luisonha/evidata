namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Command to submit a ProcessingActivity for review (Draft → UnderReview).
/// Implements AUD-REV-001: audit logging of review submission.
/// </summary>
public sealed record SubmitForReviewCommand(
    Guid TenantId,
    Guid ProcessingActivityId,
    Guid SubmittedBy);
