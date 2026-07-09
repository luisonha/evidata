using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Modules.ProcessingInventory.Application.ViewModels;

public sealed record ProcessingActivityControlViewModel(
    ProcessingActivityDetailViewModel ProcessingActivity,
    ProcessingActivityVersionViewModel Version,
    ControlTowerViewModel ControlTower,
    IReadOnlyList<AvailableActionViewModel> PriorityActions,
    OperationalMapViewModel OperationalMap,
    EvidenceSummaryViewModel EvidenceSummary,
    GapSummaryViewModel GapSummary,
    ReviewSummaryViewModel ReviewSummary,
    IReadOnlyList<TimelineEventViewModel> Timeline,
    IReadOnlyList<ExportOptionViewModel> Exports,
    ResourcePermissionsViewModel Permissions,
    IReadOnlyList<BlockedActionViewModel> BlockedActions,
    object Localization);

public sealed record ProcessingActivityListViewModel(
    Guid Id,
    string Name,
    string? Description,
    ProcessingActivityStatus Status,
    string StatusLabelKey,
    int Version,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record ProcessingActivityDetailViewModel(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    Guid? AreaId,
    Guid? OwnerUserId,
    string? Category,
    ProcessingActivityStatus Status,
    string StatusLabelKey,
    int Version,
    Guid? SupersedesId,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    Guid? ActiveVersionId,
    Guid? CurrentDraftVersionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ArchivedAt);

public sealed record ProcessingActivityNodeViewModel(
    Guid ProcessingActivityId,
    Guid VersionId,
    ProcessingActivityNodeCode NodeCode,
    string NodeCodeLabelKey,
    ProcessingActivityNodeStatus Status,
    string StatusLabelKey,
    decimal CompletionPercentage,
    RiskLevel? RiskLevel,
    string? RiskLevelLabelKey,
    object Fields,
    object SuggestedFields,
    object ConfirmedFields,
    DateTimeOffset UpdatedAt);

public sealed record EvidenceSummaryViewModel(
    Guid ProcessingActivityId,
    Guid VersionId,
    int TotalRequirements,
    int PendingCount,
    int AttachedCount,
    int ValidatedCount,
    int InsufficientCount,
    int RejectedCount,
    int BlockingRequirementsCount,
    decimal CompletionPercentage);

public sealed record GapSummaryViewModel(
    Guid ProcessingActivityId,
    Guid VersionId,
    int TotalCount,
    int OpenCount,
    int InCorrectionCount,
    int ResolvedCount,
    int AcceptedWithRiskCount,
    int DismissedCount,
    GapSeverity? HighestSeverity,
    string? HighestSeverityLabelKey,
    bool ApprovalBlocked);

public sealed record ReviewSummaryViewModel(
    Guid ProcessingActivityId,
    Guid VersionId,
    ProcessingActivityVersionStatus Status,
    string StatusLabelKey,
    ReviewDecisionCode? LastDecisionCode,
    string? LastDecisionLabelKey,
    IReadOnlyList<ReviewDomainItemViewModel> RequiredDomains,
    IReadOnlyList<ReviewDomainItemViewModel> PendingDomains,
    DateTimeOffset? DecidedAt);

public sealed record TimelineEventViewModel(
    Guid Id,
    AuditEventType EventType,
    string EventTypeLabelKey,
    string ResourceType,
    Guid ResourceId,
    Guid ActorUserId,
    DateTimeOffset OccurredAt,
    AuditEventResult Result,
    string ResultLabelKey,
    string? CorrelationId,
    object? Metadata);

public sealed record ExportOptionViewModel(
    ExportType ExportType,
    string ExportTypeLabelKey,
    ExportVisibility Visibility,
    string VisibilityLabelKey,
    bool IsAvailable,
    string? BlockedReasonCode,
    string? BlockedReasonLabelKey);

public sealed record ResourcePermissionsViewModel(
    IReadOnlyList<RbacRoleItemViewModel> RoleCodes,
    IReadOnlyList<AvailableActionViewModel> AvailableActions,
    bool ReadOnly,
    IReadOnlyList<BlockedActionViewModel> BlockedActions);

public sealed record AvailableActionViewModel(
    PermissionCode Action,
    string LabelKey);

public sealed record BlockedActionViewModel(
    PermissionCode Action,
    string ActionLabelKey,
    string ReasonCode,
    string LabelKey,
    GapSeverity Severity,
    string SeverityLabelKey,
    ProcessingActivityNodeCode? RelatedNode,
    string? RelatedNodeLabelKey);

public sealed record ProcessingActivityVersionViewModel(
    Guid? Id,
    int Version,
    ProcessingActivityVersionStatus Status,
    string StatusLabelKey,
    Guid? SupersedesId,
    DateTimeOffset? ApprovedAt,
    Guid? ApprovedBy,
    Guid? ActiveVersionId,
    Guid? CurrentDraftVersionId);

public sealed record ControlTowerViewModel(
    decimal CompletionPercentage,
    RiskLevel? RiskLevel,
    string? RiskLevelLabelKey,
    IReadOnlyList<AvailableActionViewModel> AvailableActions,
    IReadOnlyList<BlockedActionViewModel> BlockedActions);

public sealed record OperationalMapViewModel(
    IReadOnlyList<ProcessingActivityNodeViewModel> Nodes);

public sealed record ReviewDomainItemViewModel(
    ReviewDomain Code,
    string LabelKey);

public sealed record RbacRoleItemViewModel(
    RbacRoleCode Code,
    string LabelKey);
