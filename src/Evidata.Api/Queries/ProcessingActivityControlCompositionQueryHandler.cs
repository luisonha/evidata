using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Application.ViewModels;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Queries;
using Microsoft.Extensions.Logging;

namespace Evidata.Api.Queries;

/// <summary>
/// Composition Query Handler for the /control endpoint.
/// Orchestrates data from all modules (Evidence, Gap, Workflow, Audit, Reporting, Security)
/// without creating circular dependencies. This handler lives in the API layer, which is the only
/// place allowed to depend on all modules.
/// 
/// P1-FULL-COMPOSITION: Strategy from Gandalf, approved by team.
/// Implements Option 3: Shared Abstractions + API-layer composition.
/// </summary>
public sealed class ProcessingActivityControlCompositionQueryHandler : IProcessingActivityControlQueryService
{
    private readonly GetProcessingActivityControlQueryHandler _stubHandler;
    private readonly IEvidenceSummaryQueryService _evidenceService;
    private readonly IGapSummaryQueryService _gapService;
    private readonly IReviewSummaryQueryService _reviewService;
    private readonly ITimelineQueryService _timelineService;
    private readonly IExportOptionsQueryService _exportService;
    private readonly IResourcePermissionsQueryService _permissionsService;
    private readonly ILogger<ProcessingActivityControlCompositionQueryHandler> _logger;

    public ProcessingActivityControlCompositionQueryHandler(
        GetProcessingActivityControlQueryHandler stubHandler,
        IEvidenceSummaryQueryService evidenceService,
        IGapSummaryQueryService gapService,
        IReviewSummaryQueryService reviewService,
        ITimelineQueryService timelineService,
        IExportOptionsQueryService exportService,
        IResourcePermissionsQueryService permissionsService,
        ILogger<ProcessingActivityControlCompositionQueryHandler> logger)
    {
        _stubHandler = stubHandler ?? throw new ArgumentNullException(nameof(stubHandler));
        _evidenceService = evidenceService ?? throw new ArgumentNullException(nameof(evidenceService));
        _gapService = gapService ?? throw new ArgumentNullException(nameof(gapService));
        _reviewService = reviewService ?? throw new ArgumentNullException(nameof(reviewService));
        _timelineService = timelineService ?? throw new ArgumentNullException(nameof(timelineService));
        _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        _permissionsService = permissionsService ?? throw new ArgumentNullException(nameof(permissionsService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles the complete /control endpoint by orchestrating all module services in parallel.
    /// Tenant isolation is enforced at each service call.
    /// 
    /// P1-DEGRADATION Strategy:
    /// - CRITICAL services (permissions): fail-closed. If they fail, the entire endpoint fails.
    /// - OPTIONAL services (evidence, gap, review, timeline, exports): fail-open with graceful degradation.
    ///   If they fail, a warning is logged and a safe default value is used for composition.
    /// </summary>
    public async Task<ProcessingActivityControlViewModel?> HandleAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid userId,
        CancellationToken ct = default)
    {
        // Step 1: Get the base activity using the stub handler (ProcessingInventory module)
        var baseControl = await _stubHandler.HandleAsync(tenantId, processingActivityId, userId, ct);
        if (baseControl is null)
            return null;

        var versionId = baseControl.Version.Id ?? processingActivityId;

        // Step 2: Fetch all summaries in parallel
        // Each service must pass tenantId for tenant isolation.
        var evidenceTask = _evidenceService.GetSummaryAsync(tenantId, processingActivityId, versionId, ct);
        var gapTask = _gapService.GetByProcessingActivityAsync(tenantId, processingActivityId, versionId, ct);
        var reviewTask = _reviewService.GetReviewSummaryAsync(tenantId, processingActivityId, versionId, ct);
        var timelineTask = _timelineService.GetTimelineAsync(tenantId, processingActivityId, "ProcessingActivity", 0, 100, ct);
        var exportTask = _exportService.GetExportOptionsAsync(processingActivityId, ct);

        // Permissions service requires context about the resource for better evaluation
        var permissionContextData = new ResourceContextData(
            ResourceOwnerId: baseControl.ProcessingActivity.OwnerUserId,
            Status: baseControl.ProcessingActivity.Status,
            CustomFlags: null);
        var permissionsTask = _permissionsService.GetResourcePermissionsAsync(
            userId, tenantId, "processingActivity", processingActivityId, permissionContextData, ct);

        // Step 3: Handle each task individually to support graceful degradation
        // CRITICAL: permissionsTask must succeed; if it fails, propagate the exception (fail-closed)
        EvidenceSummaryDto? evidenceSummaryDto = null;
        ProcessingActivityGapSummaryDto? gapSummaryDto = null;
        ReviewSummaryDto? reviewSummaryDto = null;
        IReadOnlyList<Modules.Audit.Application.DTOs.TimelineEventViewModel>? timelineEvents = null;
        IReadOnlyList<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>? exportOptions = null;

        try
        {
            evidenceSummaryDto = await evidenceTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Evidence service failed for processingActivityId={processingActivityId}, tenantId={tenantId}. " +
                "Degrading to empty evidence summary. Exception: {exceptionMessage}",
                processingActivityId, tenantId, ex.Message);
            // Degrade to empty: will be handled by MapEvidenceSummary
            evidenceSummaryDto = null;
        }

        try
        {
            gapSummaryDto = await gapTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Gap Management service failed for processingActivityId={processingActivityId}, tenantId={tenantId}. " +
                "Degrading to empty gap summary. Exception: {exceptionMessage}",
                processingActivityId, tenantId, ex.Message);
            // Degrade to empty
            gapSummaryDto = null;
        }

        try
        {
            reviewSummaryDto = await reviewTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Review service failed for processingActivityId={processingActivityId}, tenantId={tenantId}. " +
                "Degrading to empty review summary. Exception: {exceptionMessage}",
                processingActivityId, tenantId, ex.Message);
            // Degrade to null: MapReviewSummary already handles null safely
            reviewSummaryDto = null;
        }

        try
        {
            timelineEvents = await timelineTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Timeline service failed for processingActivityId={processingActivityId}, tenantId={tenantId}. " +
                "Degrading to empty timeline. Exception: {exceptionMessage}",
                processingActivityId, tenantId, ex.Message);
            // Degrade to empty list
            timelineEvents = new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly();
        }

        try
        {
            exportOptions = await exportTask;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Export service failed for processingActivityId={processingActivityId}. " +
                "Degrading to empty export options. Exception: {exceptionMessage}",
                processingActivityId, ex.Message);
            // Degrade to empty list
            exportOptions = new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly();
        }

        // CRITICAL (fail-closed): permissionsTask must succeed
        // If it throws, the exception propagates and the entire endpoint fails
        var permissionsResult = await permissionsTask;

        // Step 4: Map DTOs to ViewModels
        var evidenceSummary = MapEvidenceSummary(evidenceSummaryDto);
        var gapSummary = MapGapSummary(gapSummaryDto);
        var reviewSummary = MapReviewSummary(reviewSummaryDto, baseControl.Version);
        var timeline = MapTimeline(timelineEvents ?? new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());
        var exports = MapExports(exportOptions ?? new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());
        var permissions = MapPermissions(permissionsResult, baseControl.ProcessingActivity.OwnerUserId);

        // Step 5: Compose the complete view model
        var controlViewModel = new ProcessingActivityControlViewModel(
            ProcessingActivity: baseControl.ProcessingActivity,
            Version: baseControl.Version,
            ControlTower: new ControlTowerViewModel(
                CompletionPercentage: CalculateCompletionPercentage(evidenceSummary, gapSummary),
                RiskLevel: CalculateRiskLevel(gapSummary),
                RiskLevelLabelKey: GetRiskLevelLabelKey(CalculateRiskLevel(gapSummary)),
                AvailableActions: permissions.AvailableActions,
                BlockedActions: permissions.BlockedActions),
            PriorityActions: permissions.AvailableActions,
            OperationalMap: baseControl.OperationalMap,
            EvidenceSummary: evidenceSummary,
            GapSummary: gapSummary,
            ReviewSummary: reviewSummary,
            Timeline: timeline,
            Exports: exports,
            Permissions: permissions,
            BlockedActions: permissions.BlockedActions,
            Localization: null!); // Localization handled by frontend

        return controlViewModel;
    }

    /// <summary>
    /// Maps Evidence module DTO to ProcessingInventory ViewModel.
    /// Handles null gracefully by returning an empty summary (0 requirements, 0% completion).
    /// </summary>
    private static EvidenceSummaryViewModel MapEvidenceSummary(EvidenceSummaryDto? dto)
    {
        if (dto is null)
        {
            // Return empty evidence summary if service failed or returned null
            return new EvidenceSummaryViewModel(
                ProcessingActivityId: Guid.Empty,
                VersionId: Guid.Empty,
                TotalRequirements: 0,
                PendingCount: 0,
                AttachedCount: 0,
                ValidatedCount: 0,
                InsufficientCount: 0,
                RejectedCount: 0,
                BlockingRequirementsCount: 0,
                CompletionPercentage: 0m);
        }

        return new EvidenceSummaryViewModel(
            ProcessingActivityId: dto.ProcessingActivityId,
            VersionId: dto.VersionId,
            TotalRequirements: dto.TotalRequirements,
            PendingCount: dto.PendingCount,
            AttachedCount: dto.AttachedCount,
            ValidatedCount: dto.ValidatedCount,
            InsufficientCount: dto.InsufficientCount,
            RejectedCount: dto.RejectedCount,
            BlockingRequirementsCount: dto.BlockingRequirementsCount,
            CompletionPercentage: dto.CompletionPercentage);
    }

    /// <summary>
    /// Maps GapManagement module DTO to ProcessingInventory ViewModel.
    /// Handles null gracefully by returning an empty summary (0 gaps).
    /// </summary>
    private static Modules.ProcessingInventory.Application.ViewModels.GapSummaryViewModel MapGapSummary(ProcessingActivityGapSummaryDto? dto)
    {
        if (dto is null)
        {
            // Return empty gap summary if service failed or returned null
            return new Modules.ProcessingInventory.Application.ViewModels.GapSummaryViewModel(
                ProcessingActivityId: Guid.Empty,
                VersionId: Guid.Empty,
                TotalCount: 0,
                OpenCount: 0,
                InCorrectionCount: 0,
                ResolvedCount: 0,
                AcceptedWithRiskCount: 0,
                DismissedCount: 0,
                HighestSeverity: null,
                HighestSeverityLabelKey: "",
                ApprovalBlocked: false);
        }

        return new Modules.ProcessingInventory.Application.ViewModels.GapSummaryViewModel(
            ProcessingActivityId: dto.ProcessingActivityId,
            VersionId: dto.VersionId,
            TotalCount: dto.TotalCount,
            OpenCount: dto.OpenCount,
            InCorrectionCount: dto.InCorrectionCount,
            ResolvedCount: dto.ResolvedCount,
            AcceptedWithRiskCount: dto.AcceptedWithRiskCount,
            DismissedCount: dto.DismissedCount,
            HighestSeverity: ConvertGapSeverity(dto.HighestSeverity),
            HighestSeverityLabelKey: dto.HighestSeverityLabelKey,
            ApprovalBlocked: dto.ApprovalBlocked);
    }

    /// <summary>
    /// Converts GapManagement domain enum to ProcessingInventory view model enum.
    /// </summary>
    private static Modules.ProcessingInventory.Application.ViewModels.GapSeverity? ConvertGapSeverity(
        Modules.GapManagement.Domain.GapSeverity? severity) =>
        severity switch
        {
            Modules.GapManagement.Domain.GapSeverity.Low => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Low,
            Modules.GapManagement.Domain.GapSeverity.Medium => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Medium,
            Modules.GapManagement.Domain.GapSeverity.High => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.High,
            Modules.GapManagement.Domain.GapSeverity.Critical => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Critical,
            _ => null
        };

    /// <summary>
    /// Maps Workflow module DTO to ProcessingInventory ViewModel.
    /// Converts string Status/DecisionCode to enums expected by the ViewModel.
    /// </summary>
    private static ReviewSummaryViewModel MapReviewSummary(
        ReviewSummaryDto? dto,
        ProcessingActivityVersionViewModel version)
    {
        if (dto is null)
        {
            // Return empty review summary if service returns null
            return new ReviewSummaryViewModel(
                ProcessingActivityId: version.Id ?? Guid.Empty,
                VersionId: version.Id ?? Guid.Empty,
                Status: ProcessingActivityVersionStatus.Draft,
                StatusLabelKey: "version.status.draft",
                LastDecisionCode: null,
                LastDecisionLabelKey: null,
                RequiredDomains: [],
                PendingDomains: [],
                DecidedAt: null);
        }

        var status = ParseVersionStatus(dto.Status);
        return new ReviewSummaryViewModel(
            ProcessingActivityId: dto.ProcessingActivityId,
            VersionId: dto.VersionId,
            Status: status,
            StatusLabelKey: dto.StatusLabelKey,
            LastDecisionCode: ParseDecisionCode(dto.LastDecisionCode),
            LastDecisionLabelKey: dto.LastDecisionLabelKey,
            RequiredDomains: [],
            PendingDomains: [],
            DecidedAt: dto.DecidedAt);
    }

    /// <summary>
    /// Maps Audit module Timeline events to ProcessingInventory ViewModels.
    /// Converts string fields to appropriate types.
    /// </summary>
    private static IReadOnlyList<Modules.ProcessingInventory.Application.ViewModels.TimelineEventViewModel> MapTimeline(
        IReadOnlyList<Modules.Audit.Application.DTOs.TimelineEventViewModel> events)
    {
        return events
            .Select(e => new Modules.ProcessingInventory.Application.ViewModels.TimelineEventViewModel(
                Id: Guid.TryParse(e.Id, out var id) ? id : Guid.Empty,
                EventType: ParseAuditEventType(e.EventType),
                EventTypeLabelKey: e.EventTypeLabelKey,
                ResourceType: e.ResourceType,
                ResourceId: Guid.TryParse(e.ResourceId, out var rid) ? rid : Guid.Empty,
                ActorUserId: Guid.TryParse(e.ActorUserId, out var uid) ? uid : Guid.Empty,
                OccurredAt: DateTimeOffset.TryParse(e.OccurredAt, out var dt) ? dt : DateTimeOffset.UtcNow,
                Result: ParseAuditEventResult(e.Result),
                ResultLabelKey: e.ResultLabelKey,
                CorrelationId: e.CorrelationId,
                Metadata: e.Metadata))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Maps Reporting module ExportOptions to ProcessingInventory ViewModels.
    /// Converts string enums to ProcessingInventory enums.
    /// </summary>
    private static IReadOnlyList<Modules.ProcessingInventory.Application.ViewModels.ExportOptionViewModel> MapExports(
        IReadOnlyList<Modules.Reporting.Application.Abstractions.ExportOptionViewModel> exports)
    {
        return exports
            .Select(e => new Modules.ProcessingInventory.Application.ViewModels.ExportOptionViewModel(
                ExportType: ParseExportType(e.ExportType),
                ExportTypeLabelKey: e.ExportTypeLabelKey,
                Visibility: ParseExportVisibility(e.Visibility),
                VisibilityLabelKey: e.VisibilityLabelKey,
                IsAvailable: e.IsAvailable,
                BlockedReasonCode: e.BlockedReasonCode,
                BlockedReasonLabelKey: e.BlockedReasonLabelKey))
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Maps Security module ResourcePermissionsResult to ProcessingInventory ViewModels.
    /// Converts string-based action codes to PermissionCode enums.
    /// P1-004: This is critical — blocked actions must be real and come from Security module.
    /// </summary>
    private static ResourcePermissionsViewModel MapPermissions(
        ResourcePermissionsResult result,
        Guid? resourceOwnerId)
    {
        var roleCodes = result.RoleCodes
            .Select(code => ParseRbacRoleCode(code))
            .Select(code => new RbacRoleItemViewModel(code, GetRoleCodeLabelKey(code)))
            .ToList()
            .AsReadOnly();

        var availableActions = result.AvailableActions
            .Select(action => new AvailableActionViewModel(
                Action: ParsePermissionCode(action.ActionCode),
                LabelKey: action.LabelKey))
            .ToList()
            .AsReadOnly();

        var blockedActions = result.BlockedActions
            .Select(action => new BlockedActionViewModel(
                Action: ParsePermissionCode(action.ActionCode),
                ActionLabelKey: action.ActionLabelKey,
                ReasonCode: action.ReasonCode,
                LabelKey: action.ReasonLabelKey,
                Severity: ParseGapSeverity(action.Severity),
                SeverityLabelKey: action.SeverityLabelKey,
                RelatedNode: ParseProcessingActivityNodeCode(action.RelatedNode),
                RelatedNodeLabelKey: action.RelatedNodeLabelKey))
            .ToList()
            .AsReadOnly();

        return new ResourcePermissionsViewModel(
            RoleCodes: roleCodes,
            AvailableActions: availableActions,
            ReadOnly: result.ReadOnly,
            BlockedActions: blockedActions);
    }

    /// <summary>
    /// Calculate overall completion percentage from evidence and gap summaries.
    /// </summary>
    private static decimal CalculateCompletionPercentage(
        EvidenceSummaryViewModel evidence,
        GapSummaryViewModel gaps)
    {
        // Simple weighted average: 60% evidence, 40% gaps resolved
        var evidencePercentage = evidence.CompletionPercentage;
        var gapPercentage = gaps.TotalCount > 0
            ? (decimal)(gaps.ResolvedCount + gaps.DismissedCount + gaps.AcceptedWithRiskCount) / gaps.TotalCount * 100
            : 100;

        return (evidencePercentage * 0.6m + gapPercentage * 0.4m);
    }

    /// <summary>
    /// Determine overall risk level from gap summary.
    /// </summary>
    private static RiskLevel? CalculateRiskLevel(GapSummaryViewModel gaps)
    {
        return gaps.HighestSeverity switch
        {
            GapSeverity.Critical => RiskLevel.High,
            GapSeverity.High => RiskLevel.High,
            GapSeverity.Medium => RiskLevel.Medium,
            GapSeverity.Low => RiskLevel.Low,
            _ => null
        };
    }

    private static string GetRiskLevelLabelKey(RiskLevel? level) =>
        level switch
        {
            RiskLevel.High => "risk.high",
            RiskLevel.Medium => "risk.medium",
            RiskLevel.Low => "risk.low",
            _ => "risk.none"
        };

    // Parsing helpers for enum conversions

    private static ProcessingActivityVersionStatus ParseVersionStatus(string status) =>
        status switch
        {
            "Draft" => ProcessingActivityVersionStatus.Draft,
            "InReview" or "UnderReview" => ProcessingActivityVersionStatus.InReview,
            "Approved" => ProcessingActivityVersionStatus.Approved,
            "Archived" => ProcessingActivityVersionStatus.Archived,
            _ => ProcessingActivityVersionStatus.Draft
        };

    private static ReviewDecisionCode? ParseDecisionCode(string? code) =>
        code switch
        {
            "Approved" => ReviewDecisionCode.Approved,
            "Rejected" => ReviewDecisionCode.Rejected,
            "ChangesRequested" => ReviewDecisionCode.ChangesRequested,
            _ => null
        };

    private static AuditEventType ParseAuditEventType(string eventType) =>
        Enum.TryParse<AuditEventType>(eventType, ignoreCase: true, out var parsed)
            ? parsed
            : AuditEventType.CreateProcessingActivity;

    private static AuditEventResult ParseAuditEventResult(string result) =>
        Enum.TryParse<AuditEventResult>(result, ignoreCase: true, out var parsed)
            ? parsed
            : AuditEventResult.Success;

    private static ExportType ParseExportType(string exportType) =>
        Enum.TryParse<ExportType>(exportType, ignoreCase: true, out var parsed)
            ? parsed
            : ExportType.ProcessingActivityPdfSummary;

    private static ExportVisibility ParseExportVisibility(string visibility) =>
        Enum.TryParse<ExportVisibility>(visibility, ignoreCase: true, out var parsed)
            ? parsed
            : ExportVisibility.TechnicalOnly;

    private static PermissionCode ParsePermissionCode(string code) =>
        Enum.TryParse<PermissionCode>(code, ignoreCase: true, out var parsed)
            ? parsed
            : PermissionCode.ApproveProcessingActivity;

    private static RbacRoleCode ParseRbacRoleCode(string code) =>
        Enum.TryParse<RbacRoleCode>(code, ignoreCase: true, out var parsed)
            ? parsed
            : RbacRoleCode.Viewer;

    private static string GetRoleCodeLabelKey(RbacRoleCode code) =>
        code switch
        {
            RbacRoleCode.TenantOwner => "role.tenantOwner",
            RbacRoleCode.ComplianceAdmin => "role.complianceAdmin",
            RbacRoleCode.ProcessOwner => "role.processOwner",
            RbacRoleCode.LegalReviewer => "role.legalReviewer",
            RbacRoleCode.SecurityReviewer => "role.securityReviewer",
            RbacRoleCode.Auditor => "role.auditor",
            RbacRoleCode.Viewer => "role.viewer",
            _ => "role.viewer"
        };

    private static Modules.ProcessingInventory.Application.ViewModels.GapSeverity ParseGapSeverity(string severity) =>
        severity switch
        {
            "Low" => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Low,
            "Medium" => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Medium,
            "High" => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.High,
            "Critical" => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Critical,
            _ => Modules.ProcessingInventory.Application.ViewModels.GapSeverity.Low
        };

    private static ProcessingActivityNodeCode? ParseProcessingActivityNodeCode(string? code) =>
        string.IsNullOrEmpty(code)
            ? null
            : Enum.TryParse<ProcessingActivityNodeCode>(code, ignoreCase: true, out var parsed)
                ? parsed
                : null;
}
