using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class RatValidacionesTests
{
    private static ProcessingActivity BuildMinimal() =>
        ProcessingActivity.Create(Guid.NewGuid(), "Tratamiento", Guid.NewGuid());

    private static ProcessingActivity BuildReadyForReview()
    {
        var act = BuildMinimal();
        act.SetPurpose(
            PurposeSection.Create("Gestión de nómina", LegalBasis.ContractExecution, "Contrato laboral"),
            Guid.NewGuid());
        act.SetDataCategories(
            [DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)],
            Guid.NewGuid());
        act.SetDataSubjects(
            [DataSubjectEntry.Create(DataSubjectType.Employees)],
            Guid.NewGuid());
        return act;
    }

    private static ProcessingActivity BuildReadyForApproval()
    {
        var act = BuildReadyForReview();
        act.SetRetention(RetentionSection.Create("5 años"), Guid.NewGuid());
        return act;
    }

    // ── ValidateForReview ─────────────────────────────────────────────────────

    [Fact]
    public void ValidateForReview_Complete_IsValid()
    {
        var result = ProcessingActivityValidator.ValidateForReview(BuildReadyForReview());
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateForReview_NoPurpose_Fails()
    {
        var act = BuildMinimal();
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());

        var result = ProcessingActivityValidator.ValidateForReview(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("finalidad"));
    }

    [Fact]
    public void ValidateForReview_NoDataCategories_Fails()
    {
        var act = BuildMinimal();
        act.SetPurpose(PurposeSection.Create("fin", LegalBasis.LegalObligation, "just"), Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());

        var result = ProcessingActivityValidator.ValidateForReview(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("categoría"));
    }

    [Fact]
    public void ValidateForReview_NoDataSubjects_Fails()
    {
        var act = BuildMinimal();
        act.SetPurpose(PurposeSection.Create("fin", LegalBasis.LegalObligation, "just"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());

        var result = ProcessingActivityValidator.ValidateForReview(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("titular"));
    }

    [Fact]
    public void ValidateForReview_NoRetention_HasWarning()
    {
        var result = ProcessingActivityValidator.ValidateForReview(BuildReadyForReview());
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("retención"));
    }

    // ── ValidateForApproval ───────────────────────────────────────────────────

    [Fact]
    public void ValidateForApproval_Complete_IsValid()
    {
        var result = ProcessingActivityValidator.ValidateForApproval(BuildReadyForApproval());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateForApproval_SensitiveWithoutMeasures_Fails()
    {
        var act = BuildReadyForReview();
        act.SetDataCategories(
            [DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Sensitive)],
            Guid.NewGuid());
        // No medidas de seguridad

        var result = ProcessingActivityValidator.ValidateForApproval(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("medidas de seguridad"));
    }

    [Fact]
    public void ValidateForApproval_Consent_MissingEvidenceFlag_Fails()
    {
        var act = BuildMinimal();
        act.SetPurpose(PurposeSection.Create("fin", LegalBasis.Consent, "just"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Customers)], Guid.NewGuid());

        var result = ProcessingActivityValidator.ValidateForApproval(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("evidencia de base"));
    }

    [Fact]
    public void ValidateForApproval_CriticalGapOpen_Fails()
    {
        var act = BuildReadyForApproval();
        act.SetCriticalGapFlag(hasCriticalGap: true, Guid.NewGuid());

        var result = ProcessingActivityValidator.ValidateForApproval(act);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("brechas críticas"));
    }

    [Fact]
    public void ValidateForApproval_RetentionRequired_NoRetention_Fails()
    {
        var result = ProcessingActivityValidator.ValidateForApproval(
            BuildReadyForReview(), retentionRequired: true);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("retención"));
    }

    [Fact]
    public void ValidateForApproval_RetentionNotRequired_NoRetention_Warning()
    {
        var result = ProcessingActivityValidator.ValidateForApproval(
            BuildReadyForReview(), retentionRequired: false);
        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("retención"));
    }

    // ── SubmitForReview integrado con validación ──────────────────────────────

    [Fact]
    public void SubmitForReview_Incomplete_Throws()
    {
        var act = BuildMinimal(); // sin secciones mínimas
        Assert.Throws<InvalidOperationException>(() => act.SubmitForReview(Guid.NewGuid()));
    }

    [Fact]
    public void SubmitForReview_Complete_Succeeds()
    {
        var act = BuildReadyForReview();
        act.SubmitForReview(Guid.NewGuid());
        Assert.Equal(ProcessingActivityStatus.UnderReview, act.Status);
    }

    // ── Approve integrado con validación ──────────────────────────────────────

    [Fact]
    public void Approve_WithBlockingFlag_Throws()
    {
        var act = BuildReadyForReview();
        // Sensitive sin medidas → BlocksApproval
        act.SetDataCategories(
            [DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Sensitive)],
            Guid.NewGuid());
        act.SubmitForReview(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => act.Approve(Guid.NewGuid()));
    }
}
