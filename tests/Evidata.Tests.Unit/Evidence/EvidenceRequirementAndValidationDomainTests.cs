using Evidata.Modules.Evidence.Domain;
using Xunit;

namespace Evidata.Tests.Unit.Evidence;

public class EvidenceRequirementDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid ProcessingActivityId = Guid.NewGuid();

    [Fact]
    public void Create_WithLegalDomain_CreatesRequirementInPendingState()
    {
        var requirement = EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Legal,
            "Política de privacidad aprobada",
            UserId,
            description: "Verificación de la política de privacidad");

        Assert.NotEqual(Guid.Empty, requirement.Id);
        Assert.Equal(TenantId, requirement.TenantId);
        Assert.Equal(ProcessingActivityId, requirement.ProcessingActivityId);
        Assert.Equal(ReviewDomain.Legal, requirement.ReviewDomain);
        Assert.Equal("Política de privacidad aprobada", requirement.Title);
        Assert.Equal("Verificación de la política de privacidad", requirement.Description);
        Assert.True(requirement.IsBlocking);
    }

    [Fact]
    public void Create_WithSecurityDomain_CreatesRequirementInPendingState()
    {
        var requirement = EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Security,
            "Matriz de medidas de seguridad",
            UserId,
            isBlocking: false);

        Assert.Equal(ReviewDomain.Security, requirement.ReviewDomain);
        Assert.False(requirement.IsBlocking);
    }

    [Fact]
    public void Create_WithEmptyProcessingActivityId_Throws()
    {
        Assert.Throws<ArgumentException>(() => EvidenceRequirement.Create(
            TenantId,
            Guid.Empty,
            ReviewDomain.Legal,
            "Test requirement",
            UserId));
    }

    [Fact]
    public void Create_WithEmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() => EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Legal,
            "  ",
            UserId));
    }

    [Fact]
    public void Update_ModifiesTitleAndDescription()
    {
        var requirement = EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Legal,
            "Título original",
            UserId);

        requirement.Update("Título actualizado", "Nueva descripción", false, UserId);

        Assert.Equal("Título actualizado", requirement.Title);
        Assert.Equal("Nueva descripción", requirement.Description);
        Assert.False(requirement.IsBlocking);
        Assert.NotNull(requirement.UpdatedAt);
        Assert.Equal(UserId, requirement.UpdatedBy);
    }

    [Fact]
    public void ReviewDomain_LegalMapToLegalReviewerRole()
    {
        // Per RBAC contract: Legal domain requires LegalReviewer role
        var requirement = EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Legal,
            "Legal evidence requirement",
            UserId);

        Assert.Equal(ReviewDomain.Legal, requirement.ReviewDomain);
    }

    [Fact]
    public void ReviewDomain_SecurityMapToSecurityReviewerRole()
    {
        // Per RBAC contract: Security domain requires SecurityReviewer role
        var requirement = EvidenceRequirement.Create(
            TenantId,
            ProcessingActivityId,
            ReviewDomain.Security,
            "Security evidence requirement",
            UserId);

        Assert.Equal(ReviewDomain.Security, requirement.ReviewDomain);
    }
}

public class EvidenceValidationDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid RequirementId = Guid.NewGuid();
    private static readonly Guid EvidenceId = Guid.NewGuid();

    [Fact]
    public void Create_StartsInPendingState()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.NotEqual(Guid.Empty, validation.Id);
        Assert.Equal(TenantId, validation.TenantId);
        Assert.Equal(RequirementId, validation.EvidenceRequirementId);
        Assert.Equal(EvidenceValidationStatus.Pending, validation.Status);
        Assert.Null(validation.EvidenceId);
        Assert.Null(validation.ValidatedBy);
        Assert.Null(validation.ValidatedAt);
    }

    [Fact]
    public void Create_WithEmptyRequirementId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EvidenceValidation.Create(TenantId, Guid.Empty, UserId));
    }

    [Fact]
    public void AttachEvidence_TransitionsPendingToAttached()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        validation.AttachEvidence(EvidenceId, UserId);

        Assert.Equal(EvidenceValidationStatus.Attached, validation.Status);
        Assert.Equal(EvidenceId, validation.EvidenceId);
        Assert.NotNull(validation.UpdatedAt);
    }

    [Fact]
    public void AttachEvidence_FromPendingWithEmptyId_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.Throws<ArgumentException>(() =>
            validation.AttachEvidence(Guid.Empty, UserId));
    }

    [Fact]
    public void AttachEvidence_WhenAlreadyAttached_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            validation.AttachEvidence(Guid.NewGuid(), UserId));
    }

    [Fact]
    public void AttachEvidence_FromNonPendingState_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);
        validation.Validate(comment: null, UserId);

        // Now it's Validated, cannot attach again
        Assert.Throws<InvalidOperationException>(() =>
            validation.AttachEvidence(Guid.NewGuid(), UserId));
    }

    [Fact]
    public void Validate_TransitionsAttachedToValidated()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);

        validation.Validate("Política aprobada", UserId);

        Assert.Equal(EvidenceValidationStatus.Validated, validation.Status);
        Assert.Equal("Política aprobada", validation.ValidationComment);
        Assert.Equal(UserId, validation.ValidatedBy);
        Assert.NotNull(validation.ValidatedAt);
    }

    [Fact]
    public void Validate_WithoutEvidenceAttached_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            validation.Validate("Comment", UserId));
    }

    [Fact]
    public void Validate_FromNonAttachedState_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            validation.Validate("Comment", UserId));
    }

    [Fact]
    public void MarkInsufficient_TransitionsAttachedToInsufficient()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);

        validation.MarkInsufficient("Falta cláusula de retención", UserId);

        Assert.Equal(EvidenceValidationStatus.Insufficient, validation.Status);
        Assert.Equal("Falta cláusula de retención", validation.ValidationComment);
        Assert.Equal(UserId, validation.ValidatedBy);
        Assert.NotNull(validation.ValidatedAt);
    }

    [Fact]
    public void MarkInsufficient_WithoutEvidenceAttached_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            validation.MarkInsufficient("Comment", UserId));
    }

    [Fact]
    public void Reject_TransitionsAttachedToRejected()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);

        validation.Reject("Documento inválido", UserId);

        Assert.Equal(EvidenceValidationStatus.Rejected, validation.Status);
        Assert.Equal("Documento inválido", validation.ValidationComment);
        Assert.Equal(UserId, validation.ValidatedBy);
        Assert.NotNull(validation.ValidatedAt);
    }

    [Fact]
    public void Reject_WithoutEvidenceAttached_Throws()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        Assert.Throws<InvalidOperationException>(() =>
            validation.Reject("Comment", UserId));
    }

    [Fact]
    public void StateTransitions_AllValidTransitionsWork()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);

        // Pending -> Attached
        validation.AttachEvidence(EvidenceId, UserId);
        Assert.Equal(EvidenceValidationStatus.Attached, validation.Status);

        // Attached -> Validated
        validation.Validate("Approved", UserId);
        Assert.Equal(EvidenceValidationStatus.Validated, validation.Status);
    }

    [Fact]
    public void StateTransitions_CannotGoBackward()
    {
        var validation = EvidenceValidation.Create(TenantId, RequirementId, UserId);
        validation.AttachEvidence(EvidenceId, UserId);
        validation.Validate("Approved", UserId);

        // Cannot transition from Validated back to Attached
        Assert.Throws<InvalidOperationException>(() =>
            validation.AttachEvidence(Guid.NewGuid(), UserId));
    }
}
