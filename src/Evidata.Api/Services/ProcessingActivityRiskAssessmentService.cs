using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Evidata.Api.Services;

/// <summary>
/// Implementation of ProcessingActivityRiskAssessmentService.
/// Aggregates risk data from multiple modules for export warning auto-detection.
/// 
/// Implements P1-014-P2: Auto-detection of ExportWarning.
/// This service coordinates queries from GapManagement, Evidence, and Workflow modules
/// to identify risks that should trigger export warnings.
/// 
/// Key Design Decisions:
/// 1. Lives in API layer to avoid circular module dependencies (GapManagement → ProcessingInventory already exists)
/// 2. Tenant isolation: Always resolved from ICurrentUserContext, never from client.
/// 3. Uses REAL interfaces from each module (Abstractions), not duplicates
/// 4. Query-only: All injected services are read-only interfaces (no state modification).
/// 5. Graceful degradation: If one module's risk assessment fails, others continue.
/// </summary>
public sealed class ProcessingActivityRiskAssessmentService(
    IGapSummaryQueryService gapSummaryQueryService,
    IEvidenceSummaryQueryService evidenceSummaryQueryService,
    IReviewSummaryQueryService reviewSummaryQueryService,
    ICurrentUserContext currentUserContext,
    ILogger<ProcessingActivityRiskAssessmentService> logger) 
    : IProcessingActivityRiskAssessmentService
{
    public async Task<ProcessingActivityRiskAssessment> AssessRisksAsync(
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default)
    {
        // Resolve tenant from current user context (never from client parameters).
        var tenantId = currentUserContext.TenantId;
        if (tenantId == Guid.Empty)
        {
            logger.LogError("Unable to resolve TenantId from ICurrentUserContext for risk assessment");
            throw new InvalidOperationException("TenantId not available in current context (P1-014-P2)");
        }

        logger.LogDebug(
            "Starting risk assessment for ProcessingActivity {ActivityId} (version {VersionId}) in tenant {TenantId}",
            processingActivityId, versionId, tenantId);

        var hasCriticalGapsOpen = false;
        var criticalGapDetails = new List<string>();

        var hasPendingEvidence = false;
        var pendingEvidenceDetails = new List<string>();

        var hasPendingReviews = false;
        var pendingReviewDetails = new List<string>();

        // ─── Risk Check #1: Critical Gaps Open ───────────────────────────────
        try
        {
            var gapSummary = await gapSummaryQueryService.GetByProcessingActivityAsync(
                tenantId, processingActivityId, versionId, ct);

            if (gapSummary != null && gapSummary.OpenCount > 0 && gapSummary.HighestSeverity.HasValue)
            {
                // A gap is "critical" if it is open AND has severity assigned
                hasCriticalGapsOpen = true;
                var severityLabel = gapSummary.HighestSeverityLabelKey ?? gapSummary.HighestSeverity.ToString();
                criticalGapDetails.Add(
                    $"Open gaps detected: {gapSummary.OpenCount} open (highest severity: {severityLabel})");
                
                logger.LogDebug(
                    "Risk detected: Critical gaps open for activity {ActivityId}: {OpenCount} open gaps",
                    processingActivityId, gapSummary.OpenCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to assess gap risks for activity {ActivityId}: {ErrorMessage}",
                processingActivityId, ex.Message);
            // Do not throw; continue with other risk assessments (graceful degradation)
        }

        // ─── Risk Check #2: Pending Evidence ────────────────────────────────
        try
        {
            var evidenceSummary = await evidenceSummaryQueryService.GetSummaryAsync(
                tenantId, processingActivityId, versionId, ct);

            if (evidenceSummary != null && 
                (evidenceSummary.BlockingRequirementsCount > 0 || evidenceSummary.PendingCount > 0))
            {
                // Evidence is "pending" if:
                // 1. There are blocking requirements not yet validated, OR
                // 2. There are pending requirements
                hasPendingEvidence = true;

                if (evidenceSummary.BlockingRequirementsCount > 0)
                {
                    pendingEvidenceDetails.Add(
                        $"Blocking evidence requirements not met: {evidenceSummary.BlockingRequirementsCount}");
                }

                if (evidenceSummary.PendingCount > 0)
                {
                    pendingEvidenceDetails.Add(
                        $"Evidence pending attachment/validation: {evidenceSummary.PendingCount}");
                }

                logger.LogDebug(
                    "Risk detected: Pending evidence for activity {ActivityId}: blocking={Blocking}, pending={Pending}",
                    processingActivityId,
                    evidenceSummary.BlockingRequirementsCount,
                    evidenceSummary.PendingCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to assess evidence risks for activity {ActivityId}: {ErrorMessage}",
                processingActivityId, ex.Message);
            // Do not throw; continue with other risk assessments (graceful degradation)
        }

        // ─── Risk Check #3: Pending Reviews ────────────────────────────────
        try
        {
            var reviewSummary = await reviewSummaryQueryService.GetReviewSummaryAsync(
                tenantId, processingActivityId, versionId, ct);

            if (reviewSummary != null && reviewSummary.PendingDomains.Count > 0)
            {
                // If there are ANY pending review domains, mark as pending reviews
                // (conservative approach for P1-014-P2: better to warn and let operator decide)
                hasPendingReviews = true;
                pendingReviewDetails.Add(
                    $"Reviews pending approval: {reviewSummary.PendingDomains.Count} domain(s) awaiting decision");

                logger.LogDebug(
                    "Risk detected: Pending reviews for activity {ActivityId}: {PendingCount} pending domain(s)",
                    processingActivityId, reviewSummary.PendingDomains.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to assess review risks for activity {ActivityId}: {ErrorMessage}",
                processingActivityId, ex.Message);
            // Do not throw; continue with other risk assessments (graceful degradation)
        }

        // ─── Build Result ────────────────────────────────────────────────────
        var assessment = new ProcessingActivityRiskAssessment
        {
            HasCriticalGapsOpen = hasCriticalGapsOpen,
            CriticalGapDetails = criticalGapDetails.AsReadOnly(),
            HasPendingEvidence = hasPendingEvidence,
            PendingEvidenceDetails = pendingEvidenceDetails.AsReadOnly(),
            HasPendingReviews = hasPendingReviews,
            PendingReviewDetails = pendingReviewDetails.AsReadOnly()
        };

        logger.LogDebug(
            "Risk assessment complete for activity {ActivityId}: hasGaps={HasGaps}, hasEvidence={HasEvidence}, hasReviews={HasReviews}",
            processingActivityId,
            assessment.HasCriticalGapsOpen,
            assessment.HasPendingEvidence,
            assessment.HasPendingReviews);

        return assessment;
    }
}
