using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class ProcessingActivityTests
{
    private static ProcessingActivity Build(string name = "Tratamiento de RRHH") =>
        ProcessingActivity.Create(Guid.NewGuid(), name, Guid.NewGuid(),
            description: "Gestión de nómina", controller: "RRHH", department: "Recursos Humanos");

    private static ProcessingActivity BuildReadyForReview()
    {
        var act = Build();
        act.SetPurpose(PurposeSection.Create("Gestión de nómina", LegalBasis.ContractExecution, "Contrato laboral"), Guid.NewGuid());
        act.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], Guid.NewGuid());
        act.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], Guid.NewGuid());
        return act;
    }

    private static ProcessingActivity BuildReadyForApproval()
    {
        var act = BuildReadyForReview();
        act.SetRetention(RetentionSection.Create("5 años"), Guid.NewGuid());
        return act;
    }

    // ── TC1: Create OK → Draft v1 ─────────────────────────────────────────────
    [Fact]
    public void Create_Valid_ReturnsDraftV1()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var act = ProcessingActivity.Create(tenantId, "Gestión de nómina", userId);

        Assert.Equal(ProcessingActivityStatus.Draft, act.Status);
        Assert.Equal(1, act.Version);
        Assert.Equal(tenantId, act.TenantId);
        Assert.Equal("Gestión de nómina", act.Name);
        Assert.True(act.IsEditable);
        Assert.NotEqual(Guid.Empty, act.Id);
    }

    // ── TC2: Nombre vacío → excepción ────────────────────────────────────────
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyName_Throws(string name)
    {
        Assert.Throws<ArgumentException>(() =>
            ProcessingActivity.Create(Guid.NewGuid(), name, Guid.NewGuid()));
    }

    // ── TC3: Nombre > 300 chars → excepción ──────────────────────────────────
    [Fact]
    public void Create_NameTooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ProcessingActivity.Create(Guid.NewGuid(), new string('x', 301), Guid.NewGuid()));
    }

    // ── TC4: Update en Draft → OK ────────────────────────────────────────────
    [Fact]
    public void Update_InDraft_UpdatesFields()
    {
        var act = Build();
        var userId = Guid.NewGuid();
        act.Update("Nuevo nombre", userId, description: "Nueva desc");

        Assert.Equal("Nuevo nombre", act.Name);
        Assert.Equal("Nueva desc", act.Description);
        Assert.Equal(userId, act.LastModifiedBy);
        Assert.NotNull(act.LastModifiedAt);
    }

    // ── TC5: FSM Draft → UnderReview → Approved ──────────────────────────────
    [Fact]
    public void FSM_DraftToApproved_OK()
    {
        var act = BuildReadyForApproval();
        var reviewer = Guid.NewGuid();
        var approver = Guid.NewGuid();

        act.SubmitForReview(reviewer);
        Assert.Equal(ProcessingActivityStatus.UnderReview, act.Status);

        act.Approve(approver);
        Assert.Equal(ProcessingActivityStatus.Approved, act.Status);
        Assert.Equal(approver, act.ApprovedBy);
        Assert.NotNull(act.ApprovedAt);
    }

    // ── TC6: Update en Approved → excepción ──────────────────────────────────
    [Fact]
    public void Update_Approved_Throws()
    {
        var act = BuildReadyForApproval();
        act.SubmitForReview(Guid.NewGuid());
        act.Approve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            act.Update("Otro nombre", Guid.NewGuid()));
    }

    // ── TC7: ReturnToDraft desde UnderReview ──────────────────────────────────
    [Fact]
    public void ReturnToDraft_FromUnderReview_OK()
    {
        var act = BuildReadyForReview();
        act.SubmitForReview(Guid.NewGuid());
        act.ReturnToDraft(Guid.NewGuid());

        Assert.Equal(ProcessingActivityStatus.Draft, act.Status);
        Assert.True(act.IsEditable);
    }

    // ── TC8: ReturnToDraft desde Draft → excepción ────────────────────────────
    [Fact]
    public void ReturnToDraft_FromDraft_Throws()
    {
        var act = Build();
        Assert.Throws<InvalidOperationException>(() => act.ReturnToDraft(Guid.NewGuid()));
    }

    // ── TC9: Archive desde cualquier estado no-archived → OK ─────────────────
    [Fact]
    public void Archive_FromDraft_OK()
    {
        var act = Build();
        act.Archive(Guid.NewGuid());

        Assert.Equal(ProcessingActivityStatus.Archived, act.Status);
        Assert.False(act.IsEditable);
    }

    // ── TC10: Archive dos veces → excepción ──────────────────────────────────
    [Fact]
    public void Archive_Twice_Throws()
    {
        var act = Build();
        act.Archive(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => act.Archive(Guid.NewGuid()));
    }

    // ── TC11: SubmitForReview desde UnderReview → excepción ──────────────────
    [Fact]
    public void SubmitForReview_FromUnderReview_Throws()
    {
        var act = BuildReadyForReview();
        act.SubmitForReview(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => act.SubmitForReview(Guid.NewGuid()));
    }

    // ── TC12: Name se trimmea ─────────────────────────────────────────────────
    [Fact]
    public void Create_NameTrimmed()
    {
        var act = ProcessingActivity.Create(Guid.NewGuid(), "  Mi tratamiento  ", Guid.NewGuid());
        Assert.Equal("Mi tratamiento", act.Name);
    }
}
