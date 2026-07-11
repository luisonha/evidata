namespace Evidata.Modules.Contracts.RiskAssessment;

/// <summary>
/// Generic DTOs for cross-module risk assessment queries.
/// Used by ProcessingInventory risk assessment service to avoid circular dependencies.
/// </summary>

/// <summary>Represents severity level of a gap.</summary>
public enum GapSeverityLevel
{
    /// <summary>Not specified.</summary>
    None = 0,
    /// <summary>Low severity gap.</summary>
    Low = 1,
    /// <summary>Medium severity gap.</summary>
    Medium = 2,
    /// <summary>High/Critical severity gap.</summary>
    Critical = 3
}

/// <summary>Summary of gaps affecting a processing activity (GapManagement module).</summary>
public record GapSummaryDto(
    /// <summary>Number of open (unresolved) gaps.</summary>
    int OpenCount,
    /// <summary>Highest severity among open gaps (if any).</summary>
    GapSeverityLevel? HighestSeverity,
    /// <summary>List of reason strings for open gaps.</summary>
    IReadOnlyList<string> OpenGapReasons
);

/// <summary>Summary of evidence requirements affecting a processing activity (Evidence module).</summary>
public record EvidenceSummaryDto(
    /// <summary>Number of requirements that must be met (blocking).</summary>
    int BlockingRequirementsCount,
    /// <summary>Number of requirements with pending validation.</summary>
    int PendingCount,
    /// <summary>List of reason strings for pending evidence.</summary>
    IReadOnlyList<string> PendingReasons
);

/// <summary>Represents the type of a review (e.g., "Compliance", "RiskMitigation").</summary>
public enum ReviewTypeEnum
{
    /// <summary>Unknown review type.</summary>
    Unknown = 0,
    /// <summary>Compliance-related review.</summary>
    Compliance = 1,
    /// <summary>Risk mitigation review.</summary>
    RiskMitigation = 2,
    /// <summary>Security review.</summary>
    Security = 3,
    /// <summary>Data privacy review.</summary>
    DataPrivacy = 4,
    /// <summary>Operational review.</summary>
    Operational = 5
}

/// <summary>Represents a review requirement configuration.</summary>
public record ReviewRequirement(
    /// <summary>ID of the review requirement.</summary>
    Guid Id,
    /// <summary>Type of review (e.g., "Compliance").</summary>
    ReviewTypeEnum ReviewType,
    /// <summary>Whether this requirement is mandatory.</summary>
    bool IsMandatory,
    /// <summary>Display name for the review.</summary>
    string DisplayName
);

/// <summary>Summary of reviews affecting a processing activity (Workflow module).</summary>
public record ReviewSummaryDto(
    /// <summary>List of domain codes with pending (unapproved) reviews.</summary>
    IReadOnlyList<string> PendingDomains,
    /// <summary>List of reason strings for pending reviews.</summary>
    IReadOnlyList<string> PendingReasons
);

/// <summary>
/// Query service for gap summaries (exposed by GapManagement module).
/// ProcessingInventory injects this via DI to query cross-module risk data.
/// </summary>
public interface IGapSummaryQueryService
{
    /// <summary>Get summary of open gaps for a processing activity.</summary>
    /// <param name="processingActivityId">Activity ID to check.</param>
    /// <param name="versionId">Version/snapshot ID of the activity (typically same as activityId for current version).</param>
    /// <returns>Summary of open gaps or null if none found.</returns>
    Task<GapSummaryDto?> GetGapSummaryAsync(Guid processingActivityId, Guid versionId);
}

/// <summary>
/// Query service for evidence summaries (exposed by Evidence module).
/// ProcessingInventory injects this via DI to query cross-module risk data.
/// </summary>
public interface IEvidenceSummaryQueryService
{
    /// <summary>Get summary of evidence requirements and validation status for a processing activity.</summary>
    /// <param name="processingActivityId">Activity ID to check.</param>
    /// <param name="versionId">Version/snapshot ID of the activity (typically same as activityId for current version).</param>
    /// <returns>Summary of evidence status or null if none found.</returns>
    Task<EvidenceSummaryDto?> GetEvidenceSummaryAsync(Guid processingActivityId, Guid versionId);
}

/// <summary>
/// Query service for review summaries (exposed by Workflow module).
/// ProcessingInventory injects this via DI to query cross-module risk data.
/// </summary>
public interface IReviewSummaryQueryService
{
    /// <summary>Get summary of pending reviews for a processing activity.</summary>
    /// <param name="processingActivityId">Activity ID to check.</param>
    /// <param name="versionId">Version/snapshot ID of the activity (typically same as activityId for current version).</param>
    /// <returns>Summary of pending reviews or null if none found.</returns>
    Task<ReviewSummaryDto?> GetReviewSummaryAsync(Guid processingActivityId, Guid versionId);
}

/// <summary>
/// Policy service for review requirements (exposed by Workflow module).
/// ProcessingInventory uses this to determine which reviews are mandatory by tenant.
/// </summary>
public interface IReviewRequirementPolicyService
{
    /// <summary>Get review requirements for a tenant.</summary>
    /// <param name="tenantId">Tenant ID.</param>
    /// <returns>List of review requirements applicable to the tenant.</returns>
    Task<IReadOnlyList<ReviewRequirement>> GetReviewRequirementsAsync(Guid tenantId);
}
