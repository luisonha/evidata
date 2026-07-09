using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Tests.Unit.ProcessingInventory;

/// <summary>
/// Tests de dominio para versionado del RAT:
/// - Approve devuelve snapshot inmutable
/// - CreateNewVersion genera Draft con version+1 y SupersedesId
/// - Inmutabilidad del aprobado después de crear nueva versión
/// </summary>
public class RatVersionadoTests
{
    private static ProcessingActivity BuildReady()
    {
        var act = ProcessingActivity.Create(Guid.NewGuid(), "Tratamiento listo", Guid.NewGuid());
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

    private static ProcessingActivity BuildApproved()
    {
        var act = BuildReady();
        act.SubmitForReview(Guid.NewGuid());
        act.Approve(Guid.NewGuid());
        return act;
    }

    // ── Approve + Snapshot ────────────────────────────────────────────────────

    [Fact]
    public void Approve_ReturnsSnapshot_WithCorrectFields()
    {
        var act = BuildReady();
        act.SubmitForReview(Guid.NewGuid());
        var approver = Guid.NewGuid();

        var snapshot = act.Approve(approver);

        Assert.NotNull(snapshot);
        Assert.Equal(act.Id, snapshot.ActivityId);
        Assert.Equal(act.TenantId, snapshot.TenantId);
        Assert.Equal(1, snapshot.Version);
        Assert.Equal(approver, snapshot.ApprovedBy);
        Assert.True(snapshot.ApprovedAt > DateTimeOffset.UtcNow.AddSeconds(-5));
        Assert.False(string.IsNullOrWhiteSpace(snapshot.Payload));
    }

    [Fact]
    public void Approve_Snapshot_PayloadContainsActivityId()
    {
        var act = BuildReady();
        act.SubmitForReview(Guid.NewGuid());
        var snapshot = act.Approve(Guid.NewGuid());

        Assert.Contains(act.Id.ToString(), snapshot.Payload);
    }

    [Fact]
    public void Approve_Snapshot_IsImmutable_UniqueId()
    {
        var act1 = BuildReady();
        act1.SubmitForReview(Guid.NewGuid());
        var s1 = act1.Approve(Guid.NewGuid());

        var act2 = BuildReady();
        act2.SubmitForReview(Guid.NewGuid());
        var s2 = act2.Approve(Guid.NewGuid());

        Assert.NotEqual(s1.Id, s2.Id);
    }

    [Fact]
    public void TakeFrom_NotApproved_Throws()
    {
        var act = BuildReady();
        // No está Approved todavía
        Assert.Throws<InvalidOperationException>(() =>
            ProcessingActivitySnapshot.TakeFrom(act));
    }

    // ── CreateNewVersion ──────────────────────────────────────────────────────

    [Fact]
    public void CreateNewVersion_ReturnsDraftVersionPlusOne()
    {
        var approved = BuildApproved();
        var creator = Guid.NewGuid();

        var next = approved.CreateNewVersion(creator);

        Assert.Equal(ProcessingActivityStatus.Draft, next.Status);
        Assert.Equal(2, next.Version);
        Assert.Equal(approved.Id, next.SupersedesId);
        Assert.Equal(creator, next.CreatedBy);
    }

    [Fact]
    public void CreateNewVersion_CopiesAllSections()
    {
        var approved = BuildApproved();

        var next = approved.CreateNewVersion(Guid.NewGuid());

        Assert.Equal(approved.Name, next.Name);
        Assert.Equal(approved.TenantId, next.TenantId);
        Assert.Equal(approved.DataCategories.Count, next.DataCategories.Count);
        Assert.Equal(approved.DataSubjects.Count, next.DataSubjects.Count);
        Assert.NotNull(next.Purpose);
    }

    [Fact]
    public void CreateNewVersion_NewIdGenerated()
    {
        var approved = BuildApproved();
        var next = approved.CreateNewVersion(Guid.NewGuid());

        Assert.NotEqual(approved.Id, next.Id);
    }

    [Fact]
    public void CreateNewVersion_FromDraft_Throws()
    {
        var draft = BuildReady();
        Assert.Throws<InvalidOperationException>(() => draft.CreateNewVersion(Guid.NewGuid()));
    }

    [Fact]
    public void CreateNewVersion_FromUnderReview_Throws()
    {
        var act = BuildReady();
        act.SubmitForReview(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => act.CreateNewVersion(Guid.NewGuid()));
    }

    // ── Inmutabilidad del aprobado ────────────────────────────────────────────

    [Fact]
    public void Approved_OriginalRemainsApproved_AfterNewVersion()
    {
        var approved = BuildApproved();
        approved.CreateNewVersion(Guid.NewGuid());

        // El original sigue Approved
        Assert.Equal(ProcessingActivityStatus.Approved, approved.Status);
    }

    [Fact]
    public void Approved_CannotBeModified_AfterNewVersion()
    {
        var approved = BuildApproved();
        approved.CreateNewVersion(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            approved.Update("Nuevo nombre", Guid.NewGuid()));
    }

    // ── Versionado encadenado ─────────────────────────────────────────────────

    [Fact]
    public void Chain_V1_V2_V3_VersionsIncrement()
    {
        var v1 = BuildApproved();
        Assert.Equal(1, v1.Version);

        var v2Draft = v1.CreateNewVersion(Guid.NewGuid());
        Assert.Equal(2, v2Draft.Version);
        Assert.Equal(v1.Id, v2Draft.SupersedesId);

        // Aprobar v2
        v2Draft.SubmitForReview(Guid.NewGuid());
        v2Draft.Approve(Guid.NewGuid());

        var v3Draft = v2Draft.CreateNewVersion(Guid.NewGuid());
        Assert.Equal(3, v3Draft.Version);
        Assert.Equal(v2Draft.Id, v3Draft.SupersedesId);
    }
}
