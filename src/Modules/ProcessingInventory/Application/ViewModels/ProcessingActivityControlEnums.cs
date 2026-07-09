namespace Evidata.Modules.ProcessingInventory.Application.ViewModels;

/// <summary>
/// Projection enums for the cross-module /control read model.
/// TODO T-P1-03: decide whether these remain local projections or map to canonical enums
/// owned by Workflow, Audit, Reporting, GapManagement and future RBAC types.
/// </summary>
public enum ProcessingActivityVersionStatus
{
    Draft,
    InReview,
    ChangesRequested,
    Approved,
    Active,
    Deprecated,
    Archived
}

public enum RiskLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum ProcessingActivityNodeCode
{
    Purpose,
    LegalBasis,
    DataCategories,
    DataSubjects,
    Systems,
    Providers,
    Transfers,
    Retention,
    Security,
    Evidence,
    Gaps,
    Review,
    Export
}

public enum ProcessingActivityNodeStatus
{
    NotStarted,
    InProgress,
    Completed,
    Blocked
}

public enum GapSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum ReviewDecisionCode
{
    Approved,
    ChangesRequested,
    Rejected
}

public enum ReviewDomain
{
    Legal,
    Security
}

public enum AuditEventType
{
    CreateProcessingActivity,
    UpdateProcessingActivityNode,
    SubmitProcessingActivityForReview,
    ApproveProcessingActivity,
    ActivateProcessingActivity,
    ArchiveProcessingActivity,
    ValidateEvidence,
    RejectEvidence,
    AcceptGapWithRisk,
    GenerateOfficialExport
}

public enum AuditEventResult
{
    Success,
    Failure,
    Blocked
}

public enum ExportType
{
    ProcessingActivityPdfSummary,
    GlobalRatExcel,
    ApprovalHistory,
    InternalJson
}

public enum ExportVisibility
{
    UserVisible,
    TechnicalOnly
}

public enum RbacRoleCode
{
    TenantOwner,
    ComplianceAdmin,
    ProcessOwner,
    LegalReviewer,
    SecurityReviewer,
    Auditor,
    Viewer
}

public enum PermissionCode
{
    ApproveProcessingActivity,
    ActivateProcessingActivity,
    ValidateEvidence,
    AcceptGapWithRisk,
    GenerateOfficialExport,
    DownloadEvidence
}
