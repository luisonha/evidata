using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class RatSectionsTests
{
    private static ProcessingActivity BuildDraft() =>
        ProcessingActivity.Create(Guid.NewGuid(), "Tratamiento Test", Guid.NewGuid());

    private static ProcessingActivity BuildApproved()
    {
        var act = BuildDraft();
        act.SetPurpose(PurposeSection.Create("Gestión de nómina", LegalBasis.ContractExecution, "Contrato laboral"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());
        act.SetRetention(RetentionSection.Create("5 años"), Guid.NewGuid());
        act.SubmitForReview(Guid.NewGuid());
        act.Approve(Guid.NewGuid());
        return act;
    }

    // ── PurposeSection ────────────────────────────────────────────────────────

    [Fact]
    public void SetPurpose_Valid_Assigned()
    {
        var act = BuildDraft();
        var purpose = PurposeSection.Create(
            "Gestión de nómina de empleados",
            LegalBasis.ContractExecution,
            "Necesario para ejecutar el contrato laboral vigente.");

        act.SetPurpose(purpose, Guid.NewGuid());

        Assert.NotNull(act.Purpose);
        Assert.Equal("Gestión de nómina de empleados", act.Purpose.Purpose);
        Assert.Equal(LegalBasis.ContractExecution, act.Purpose.LegalBasis);
    }

    [Fact]
    public void PurposeSection_EmptyPurpose_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PurposeSection.Create("", LegalBasis.Consent, "justificación"));

    [Fact]
    public void PurposeSection_EmptyJustification_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            PurposeSection.Create("finalidad", LegalBasis.Consent, ""));

    [Fact]
    public void SetPurpose_OnApproved_Throws()
    {
        var act = BuildApproved();
        var purpose = PurposeSection.Create("fin", LegalBasis.LegalObligation, "just");

        Assert.Throws<InvalidOperationException>(() => act.SetPurpose(purpose, Guid.NewGuid()));
    }

    // ── DataCategories ────────────────────────────────────────────────────────

    [Fact]
    public void SetDataCategories_Valid_AssignsList()
    {
        var act = BuildDraft();
        var entries = new[]
        {
            DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary),
            DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.SpecialCategory, comment: "Salud")
        };

        act.SetDataCategories(entries, Guid.NewGuid());

        Assert.Equal(2, act.DataCategories.Count);
    }

    [Fact]
    public void SetDataCategories_Empty_Throws()
    {
        var act = BuildDraft();
        Assert.Throws<ArgumentException>(() =>
            act.SetDataCategories([], Guid.NewGuid()));
    }

    [Fact]
    public void SetDataCategories_Replaces_Previous()
    {
        var act = BuildDraft();
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataCategories([
            DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Sensitive),
            DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Sensitive)
        ], Guid.NewGuid());

        Assert.Equal(2, act.DataCategories.Count);
    }

    [Fact]
    public void DataCategoryEntry_EmptyGuid_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DataCategoryEntry.Create(Guid.Empty, DataSensitivityLevel.Ordinary));

    // ── DataSubjects ──────────────────────────────────────────────────────────

    [Fact]
    public void SetDataSubjects_Valid_AssignsList()
    {
        var act = BuildDraft();
        var entries = new[]
        {
            DataSubjectEntry.Create(DataSubjectType.Employees),
            DataSubjectEntry.Create(DataSubjectType.Customers, estimatedCount: 1000)
        };

        act.SetDataSubjects(entries, Guid.NewGuid());

        Assert.Equal(2, act.DataSubjects.Count);
        Assert.Equal(1000, act.DataSubjects[1].EstimatedCount);
    }

    [Fact]
    public void SetDataSubjects_Empty_Throws()
    {
        var act = BuildDraft();
        Assert.Throws<ArgumentException>(() => act.SetDataSubjects([], Guid.NewGuid()));
    }

    [Fact]
    public void DataSubjectEntry_OtherWithoutDescription_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DataSubjectEntry.Create(DataSubjectType.Other, description: null));

    [Fact]
    public void DataSubjectEntry_OtherWithDescription_OK()
    {
        var entry = DataSubjectEntry.Create(DataSubjectType.Other, description: "Socios comerciales");
        Assert.Equal("Socios comerciales", entry.Description);
    }

    [Fact]
    public void DataSubjectEntry_NegativeCount_Throws() =>
        Assert.Throws<ArgumentException>(() =>
            DataSubjectEntry.Create(DataSubjectType.Employees, estimatedCount: -1));

    // ── Touch (LastModifiedAt) ────────────────────────────────────────────────

    [Fact]
    public void SetPurpose_UpdatesLastModified()
    {
        var act = BuildDraft();
        var userId = Guid.NewGuid();
        var purpose = PurposeSection.Create("finalidad", LegalBasis.Consent, "justificación");
        act.SetPurpose(purpose, userId);

        Assert.Equal(userId, act.LastModifiedBy);
        Assert.NotNull(act.LastModifiedAt);
    }
}
