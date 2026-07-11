namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Command to activate an approved ProcessingActivity version as the active version.
/// 
/// Implements P1-012 (AUD-ACT-001 / SEC-ACT-001):
/// - Only TenantOwner or ComplianceAdmin can activate
/// - Version must be in Approved state
/// - Previous active version (if exists) transitions to Deprecated
/// - Logs ProcessingActivityActivated audit event
/// </summary>
public sealed record ActivateProcessingActivityCommand(
    Guid TenantId,
    Guid ProcessingActivityId,
    Guid ActivatedBy);
