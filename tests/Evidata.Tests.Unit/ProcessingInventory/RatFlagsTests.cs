using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class RatFlagsTests
{
    private static ProcessingActivity BuildDraft() =>
        ProcessingActivity.Create(Guid.NewGuid(), "Test", Guid.NewGuid());

    private static DataCategoryEntry Ordinary() =>
        DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary);

    private static DataCategoryEntry Special(string? comment = null) =>
        DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.SpecialCategory, comment: comment);

    private static DataCategoryEntry Sensitive() =>
        DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Sensitive);

    // ── TC1: Sin secciones → flags vacíos ────────────────────────────────────
    [Fact]
    public void NewActivity_FlagsAreEmpty()
    {
        var act = BuildDraft();
        Assert.True(act.Flags.IsClean);
        Assert.False(act.Flags.BlocksApproval);
    }

    // ── TC2: SpecialCategory → SensitiveData + ChildrenData ──────────────────
    [Fact]
    public void SetDataCategories_SpecialCategory_RaisesSensitiveAndChildren()
    {
        var act = BuildDraft();
        act.SetDataCategories([Special()], Guid.NewGuid());

        Assert.True(act.Flags.SensitiveData);
        Assert.True(act.Flags.ChildrenData);
    }

    // ── TC3: Sensitive → SensitiveData sola ──────────────────────────────────
    [Fact]
    public void SetDataCategories_Sensitive_RaisesSensitiveOnly()
    {
        var act = BuildDraft();
        act.SetDataCategories([Sensitive()], Guid.NewGuid());

        Assert.True(act.Flags.SensitiveData);
        Assert.False(act.Flags.ChildrenData);
    }

    // ── TC4: Ordinary → sin flags de dato ────────────────────────────────────
    [Fact]
    public void SetDataCategories_Ordinary_NoDataFlags()
    {
        var act = BuildDraft();
        act.SetDataCategories([Ordinary()], Guid.NewGuid());

        Assert.False(act.Flags.SensitiveData);
        Assert.False(act.Flags.ChildrenData);
        Assert.False(act.Flags.BiometricData);
    }

    // ── TC5: SpecialCategory + comment "biometría" → BiometricData ───────────
    [Fact]
    public void SetDataCategories_Biometric_RaisesBiometricFlag()
    {
        var act = BuildDraft();
        act.SetDataCategories([Special(comment: "datos biométricos")], Guid.NewGuid());

        Assert.True(act.Flags.BiometricData);
        Assert.True(act.Flags.RequiresEnhancedReview);
    }

    // ── TC6: SetRiskInputs transferencia + decisión automatizada ─────────────
    [Fact]
    public void SetRiskInputs_SetsTransferAndAutomatedFlags()
    {
        var act = BuildDraft();
        act.SetRiskInputs(hasInternationalTransfer: true, hasAutomatedDecision: true, Guid.NewGuid());

        Assert.True(act.Flags.InternationalTransfer);
        Assert.True(act.Flags.AutomatedDecision);
        Assert.True(act.Flags.RequiresEnhancedReview);
    }

    // ── TC7: Consent → MissingLegalBasisEvidence ─────────────────────────────
    [Fact]
    public void SetPurpose_Consent_RaisesMissingEvidenceFlag()
    {
        var act = BuildDraft();
        act.SetDataCategories([Ordinary()], Guid.NewGuid());
        act.SetPurpose(
            PurposeSection.Create("finalidad", LegalBasis.Consent, "por consentimiento"),
            Guid.NewGuid());

        Assert.True(act.Flags.MissingLegalBasisEvidence);
    }

    // ── TC8: LegalObligation → NO MissingLegalBasisEvidence ──────────────────
    [Fact]
    public void SetPurpose_LegalObligation_NoMissingEvidenceFlag()
    {
        var act = BuildDraft();
        act.SetPurpose(
            PurposeSection.Create("finalidad", LegalBasis.LegalObligation, "obligación legal"),
            Guid.NewGuid());

        Assert.False(act.Flags.MissingLegalBasisEvidence);
    }

    // ── TC9: Sin retención → MissingRetention ────────────────────────────────
    [Fact]
    public void NoRetention_MissingRetentionFlagSet()
    {
        var act = BuildDraft();
        act.SetDataCategories([Ordinary()], Guid.NewGuid());

        Assert.True(act.Flags.MissingRetention);
    }

    // ── TC10: Con retención → MissingRetention limpio ────────────────────────
    [Fact]
    public void SetRetention_ClearsMissingRetentionFlag()
    {
        var act = BuildDraft();
        act.SetRetention(RetentionSection.Create("5 años"), Guid.NewGuid());

        Assert.False(act.Flags.MissingRetention);
    }

    // ── TC11: Sensitive sin medidas → MissingSecurityMeasures + BlocksApproval
    [Fact]
    public void SensitiveDataWithoutMeasures_BlocksApproval()
    {
        var act = BuildDraft();
        act.SetDataCategories([Sensitive()], Guid.NewGuid());

        Assert.True(act.Flags.MissingSecurityMeasures);
        Assert.True(act.Flags.BlocksApproval);
    }

    // ── TC12: Sensitive + medidas → MissingSecurityMeasures limpio ───────────
    [Fact]
    public void SensitiveDataWithMeasures_ClearsMissingMeasuresFlag()
    {
        var act = BuildDraft();
        act.SetDataCategories([Sensitive()], Guid.NewGuid());
        act.SetSecurityMeasures(
            [SecurityMeasureEntry.Create(SecurityMeasureType.Technical, "Cifrado AES")],
            Guid.NewGuid());

        Assert.False(act.Flags.MissingSecurityMeasures);
    }

    // ── TC13: SetCriticalGapFlag → BlocksApproval ────────────────────────────
    [Fact]
    public void SetCriticalGapFlag_BlocksApproval()
    {
        var act = BuildDraft();
        act.SetCriticalGapFlag(hasCriticalGap: true, Guid.NewGuid());

        Assert.True(act.Flags.CriticalGapOpen);
        Assert.True(act.Flags.BlocksApproval);
    }

    // ── TC14: Flags se recalculan al cambiar categorías ───────────────────────
    [Fact]
    public void Flags_RecalculateOnCategoryChange()
    {
        var act = BuildDraft();
        act.SetDataCategories([Sensitive()], Guid.NewGuid());
        Assert.True(act.Flags.SensitiveData);

        // Cambiar a solo Ordinary
        act.SetDataCategories([Ordinary()], Guid.NewGuid());
        Assert.False(act.Flags.SensitiveData);
        Assert.False(act.Flags.MissingSecurityMeasures);
    }

    // ── TC15: SetRiskInputs en Approved → excepción ───────────────────────────
    [Fact]
    public void SetRiskInputs_OnApproved_Throws()
    {
        var act = ProcessingActivity.Create(Guid.NewGuid(), "Tratamiento Aprobado", Guid.NewGuid());
        act.SetPurpose(PurposeSection.Create("fin", LegalBasis.LegalObligation, "just"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());
        act.SubmitForReview(Guid.NewGuid());
        act.Approve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            act.SetRiskInputs(true, false, Guid.NewGuid()));
    }
}
