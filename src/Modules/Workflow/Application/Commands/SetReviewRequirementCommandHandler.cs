using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;

namespace Evidata.Modules.Workflow.Application.Commands;

/// <summary>
/// Handler para SetReviewRequirementCommand.
/// </summary>
public sealed class SetReviewRequirementCommandHandler(
    IReviewRequirementPolicyService policyService)
{
    public async Task<ReviewRequirementDto> HandleAsync(
        SetReviewRequirementCommand cmd,
        Guid modifiedBy,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(cmd.EntityType))
            throw new ArgumentException("EntityType cannot be null or empty.", nameof(cmd.EntityType));

        if (modifiedBy == Guid.Empty)
            throw new ArgumentException("modifiedBy cannot be empty.", nameof(modifiedBy));

        var reviewType = (ReviewType)cmd.ReviewType;
        
        var requirement = await policyService.SetRequirementAsync(
            cmd.TenantId,
            reviewType,
            cmd.EntityType,
            cmd.IsRequired,
            modifiedBy,
            ct);

        return ReviewRequirementDto.From(requirement);
    }
}

/// <summary>
/// DTO para retornar configuración de requerimiento de revisión.
/// </summary>
public sealed record ReviewRequirementDto(
    Guid Id,
    Guid TenantId,
    int ReviewType,
    string EntityType,
    bool IsRequired,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid CreatedBy,
    Guid ModifiedBy)
{
    public static ReviewRequirementDto From(ReviewRequirement requirement) =>
        new(
            requirement.Id,
            requirement.TenantId,
            (int)requirement.ReviewType,
            requirement.EntityType,
            requirement.IsRequired,
            requirement.CreatedAt,
            requirement.ModifiedAt,
            requirement.CreatedBy,
            requirement.ModifiedBy);
}
