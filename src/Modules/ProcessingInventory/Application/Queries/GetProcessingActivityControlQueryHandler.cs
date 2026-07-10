using Evidata.Modules.ProcessingInventory.Application.ViewModels;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Queries;

/// <summary>
/// Composes the complete /control view for a ProcessingActivity.
/// This is a stub implementation that will be completed in a separate composition class in the API layer
/// to avoid circular dependencies between modules. 
/// 
/// P1-001: Endpoint implementation stub pending full composition with Evidence, Gap, Review, Timeline, Exports.
/// </summary>
public sealed class GetProcessingActivityControlQueryHandler
{
    private readonly ProcessingInventoryDbContext _db;

    public GetProcessingActivityControlQueryHandler(ProcessingInventoryDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Handles the GET /processing-activities/{id}/control request.
    /// Returns a basic ProcessingActivityControlViewModel with minimal data.
    /// Full composition will be implemented in the API layer to avoid module circular dependencies.
    /// </summary>
    public async Task<ProcessingActivityControlViewModel?> HandleAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid userId,
        CancellationToken ct = default)
    {
        // Fetch the processing activity
        var activity = await _db.ProcessingActivities
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == processingActivityId, ct);

        if (activity is null)
            return null;

        // TODO P1-001 full composition: This handler needs to be extended or wrapped by an API-layer composition
        // that has access to all the services (Evidence, Gap, Review, Timeline, Exports, Permissions).
        // For now, return a stub with basic activity data.

        var versionId = processingActivityId;

        // Build minimal control view with just activity data
        var processingActivityDetail = new ProcessingActivityDetailViewModel(
            activity.Id,
            activity.TenantId,
            activity.Name,
            activity.Description,
            null, // AreaId not in domain yet
            activity.CreatedBy, // Use CreatedBy as owner for now
            null, // Category not in domain yet
            activity.Status,
            MapStatusToLocalizationKey(activity.Status),
            activity.Version,
            activity.SupersedesId,
            activity.ApprovedBy,
            activity.ApprovedAt,
            null, // ActiveVersionId
            null, // CurrentDraftVersionId
            activity.CreatedAt,
            activity.LastModifiedAt,
            null); // ArchivedAt

        var version = new ProcessingActivityVersionViewModel(
            versionId,
            activity.Version,
            MapStatusToVersionStatus(activity.Status),
            MapStatusToVersionLocalizationKey(activity.Status),
            activity.SupersedesId,
            activity.ApprovedAt,
            activity.ApprovedBy,
            null,
            null);

        // Stub summaries and data - to be populated by composition in API layer
        var evidenceSummaryViewModel = new EvidenceSummaryViewModel(
            processingActivityId, versionId, 0, 0, 0, 0, 0, 0, 0, 0);

        var gapSummaryViewModel = new GapSummaryViewModel(
            processingActivityId, versionId, 0, 0, 0, 0, 0, 0, null, null, false);

        var reviewSummaryViewModel = new ReviewSummaryViewModel(
            processingActivityId, versionId,
            ProcessingActivityVersionStatus.Draft, "version.status.draft",
            null, null, [], [], null);

        var timeline = new List<TimelineEventViewModel>();
        var priorityActions = new List<AvailableActionViewModel>();
        var blockedActions = new List<BlockedActionViewModel>();

        var operationalMap = new OperationalMapViewModel(new List<ProcessingActivityNodeViewModel>());
        var exportViewModels = new List<ExportOptionViewModel>();

        var userRoles = new List<RbacRoleItemViewModel>();
        var resourcePermissionsViewModel = new ResourcePermissionsViewModel(
            userRoles, priorityActions, ReadOnly: false, blockedActions);

        var controlTower = new ControlTowerViewModel(
            0m, null, null, priorityActions, blockedActions);

        var controlViewModel = new ProcessingActivityControlViewModel(
            processingActivityDetail,
            version,
            controlTower,
            priorityActions,
            operationalMap,
            evidenceSummaryViewModel,
            gapSummaryViewModel,
            reviewSummaryViewModel,
            timeline,
            exportViewModels,
            resourcePermissionsViewModel,
            blockedActions,
            null!);

        return controlViewModel;
    }

    /// <summary>
    /// Maps ProcessingActivityStatus to ProcessingActivityVersionStatus enum for the ViewModel.
    /// </summary>
    private static ProcessingActivityVersionStatus MapStatusToVersionStatus(Domain.ProcessingActivityStatus status) =>
        status switch
        {
            Domain.ProcessingActivityStatus.Draft => ProcessingActivityVersionStatus.Draft,
            Domain.ProcessingActivityStatus.UnderReview => ProcessingActivityVersionStatus.InReview,
            Domain.ProcessingActivityStatus.Approved => ProcessingActivityVersionStatus.Approved,
            Domain.ProcessingActivityStatus.Archived => ProcessingActivityVersionStatus.Archived,
            _ => ProcessingActivityVersionStatus.Draft
        };

    /// <summary>
    /// Maps ProcessingActivityStatus to localization key for detail view status field.
    /// Format: "status.{lowercase-status}" (e.g., "status.draft", "status.underReview").
    /// </summary>
    private static string MapStatusToLocalizationKey(Domain.ProcessingActivityStatus status)
    {
        var statusString = status.ToString();
        return $"status.{char.ToLowerInvariant(statusString[0])}{statusString[1..]}";
    }

    /// <summary>
    /// Maps ProcessingActivityStatus to localization key for version status field.
    /// Format: "version.status.{lowercase-status}" (e.g., "version.status.draft").
    /// </summary>
    private static string MapStatusToVersionLocalizationKey(Domain.ProcessingActivityStatus status)
    {
        var statusString = status.ToString();
        return $"version.status.{char.ToLowerInvariant(statusString[0])}{statusString[1..]}";
    }
}
