namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Command to approve a ProcessingActivity version.
/// 
/// P1-013: Implements ApproveProcessingActivity authorization + blockers + audit (SEC-APP-001).
/// 
/// Approval means: "The organization considers the reviewed version ready to be approved,
/// subject to later activation if needed."
/// 
/// Rules:
/// - User must belong to same tenant
/// - User must have ApproveProcessingActivity permission
/// - ProcessOwner cannot approve their own activity (SEC-APP-001)
/// - Version must be in UnderReview state
/// - No active blockers (blocking evidence, critical gap, required review pending, etc.)
/// 
/// Result on Success:
/// - ProcessingActivityVersion.status = Approved
/// - ProcessingActivity.status = Approved
/// - AuditEvent = ProcessingActivityApproved
/// 
/// Result on Blocked (HTTP 422):
/// - Code = BlockingEvidenceMissing | CriticalGapOpen | RequiredReviewPending | VersionModifiedAfterReview
/// - AuditEvent = ApprovalBlocked
/// 
/// Result on Denied (HTTP 403):
/// - Code = InsufficientPermissions
/// - AuditEvent = AccessDenied or ApprovalDenied
/// </summary>
public sealed class ApproveProcessingActivityCommand
{
    public Guid TenantId { get; }
    public Guid ProcessingActivityId { get; }
    public Guid ApprovedBy { get; }

    public ApproveProcessingActivityCommand(
        Guid tenantId,
        Guid processingActivityId,
        Guid approvedBy)
    {
        TenantId = tenantId;
        ProcessingActivityId = processingActivityId;
        ApprovedBy = approvedBy;
    }
}
