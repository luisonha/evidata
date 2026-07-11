namespace Evidata.Modules.ProcessingInventory.Application.Abstractions;

/// <summary>
/// Service for assessing processing activity risks from multiple modules.
/// Used for auto-detecting export warnings based on cross-module riskiness.
/// Aggregates data from GapManagement, Evidence, and Workflow modules.
/// 
/// Implements P1-014-P2: Auto-detection of ExportWarning when generating official exports.
/// </summary>
public interface IProcessingActivityRiskAssessmentService
{
    /// <summary>
    /// Assesses all active risks for a processing activity in a given version.
    /// Tenant is resolved from ICurrentUserContext (never from client parameters).
    /// </summary>
    /// <param name="processingActivityId">ID of the processing activity to assess.</param>
    /// <param name="versionId">ID of the version to assess.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Risk assessment result with detailed findings.</returns>
    Task<ProcessingActivityRiskAssessment> AssessRisksAsync(
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a risk assessment.
/// Contains boolean flags for each risk category and detailed reasons for each.
/// </summary>
public sealed class ProcessingActivityRiskAssessment
{
    /// <summary>True if there are critical gaps open (severity unresolved).</summary>
    public bool HasCriticalGapsOpen { get; init; }

    /// <summary>List of details about critical gaps (if HasCriticalGapsOpen is true).</summary>
    public IReadOnlyList<string> CriticalGapDetails { get; init; } = [];

    /// <summary>True if there is evidence pending validation or insufficient.</summary>
    public bool HasPendingEvidence { get; init; }

    /// <summary>List of details about pending evidence (if HasPendingEvidence is true).</summary>
    public IReadOnlyList<string> PendingEvidenceDetails { get; init; } = [];

    /// <summary>True if there are required reviews not yet approved.</summary>
    public bool HasPendingReviews { get; init; }

    /// <summary>List of details about pending reviews (if HasPendingReviews is true).</summary>
    public IReadOnlyList<string> PendingReviewDetails { get; init; } = [];

    /// <summary>True if any risk flag is set (convenience property for decision logic).</summary>
    public bool HasAnyRisk => HasCriticalGapsOpen || HasPendingEvidence || HasPendingReviews;

    /// <summary>
    /// Generates a concise warning message summarizing all detected risks.
    /// If no risks, returns empty string.
    /// </summary>
    public string GenerateWarningMessage()
    {
        var risks = new List<string>();

        if (HasCriticalGapsOpen)
            risks.Add("Critical gaps remain open");

        if (HasPendingEvidence)
            risks.Add("Evidence pending validation");

        if (HasPendingReviews)
            risks.Add("Required reviews not approved");

        return risks.Count > 0
            ? string.Join("; ", risks)
            : string.Empty;
    }
}
