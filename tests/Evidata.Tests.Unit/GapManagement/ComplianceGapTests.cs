using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Tests.Unit.GapManagement;

public class ComplianceGapTests
{
    private static ComplianceGap BuildOpen(GapSeverity severity = GapSeverity.Medium) =>
        ComplianceGap.Create(
            Guid.NewGuid(), "ProcessingInventory", Guid.NewGuid(),
            "Falta medida de seguridad", "Las categorías sensibles carecen de cifrado.",
            severity, Guid.NewGuid());

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_Valid_ReturnsOpenGap()
    {
        var gap = BuildOpen();
        Assert.Equal(GapStatus.Open, gap.Status);
        Assert.NotEqual(Guid.Empty, gap.Id);
        Assert.False(gap.BlocksApproval); // Medium no bloquea
    }

    [Fact]
    public void Create_Critical_BlocksApproval()
    {
        var gap = BuildOpen(GapSeverity.Critical);
        Assert.True(gap.BlocksApproval);
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ComplianceGap.Create(Guid.NewGuid(), "Module", Guid.NewGuid(),
                "", "desc", GapSeverity.Low, Guid.NewGuid()));
    }

    [Fact]
    public void Create_EmptySourceModule_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ComplianceGap.Create(Guid.NewGuid(), "", Guid.NewGuid(),
                "titulo", "desc", GapSeverity.Low, Guid.NewGuid()));
    }

    [Fact]
    public void Create_TitleTooLong_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ComplianceGap.Create(Guid.NewGuid(), "Module", Guid.NewGuid(),
                new string('x', 301), "desc", GapSeverity.Low, Guid.NewGuid()));
    }

    // ── Assign ────────────────────────────────────────────────────────────────

    [Fact]
    public void Assign_FromOpen_SetsOwnerAndAssigned()
    {
        var gap = BuildOpen();
        var owner = Guid.NewGuid();
        var due = DateTimeOffset.UtcNow.AddDays(30);

        gap.Assign(owner, Guid.NewGuid(), due);

        Assert.Equal(GapStatus.Assigned, gap.Status);
        Assert.Equal(owner, gap.OwnerId);
        Assert.Equal(due, gap.DueAt);
    }

    [Fact]
    public void Assign_FromResolved_Throws()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            gap.Assign(Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── StartProgress ─────────────────────────────────────────────────────────

    [Fact]
    public void StartProgress_FromAssigned_SetsInProgress()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());

        Assert.Equal(GapStatus.InProgress, gap.Status);
    }

    [Fact]
    public void StartProgress_FromBlocked_SetsInProgress()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.Block(Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());

        Assert.Equal(GapStatus.InProgress, gap.Status);
    }

    [Fact]
    public void StartProgress_FromOpen_Throws()
    {
        var gap = BuildOpen();
        Assert.Throws<InvalidOperationException>(() => gap.StartProgress(Guid.NewGuid()));
    }

    // ── Block ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Block_FromInProgress_SetsBlocked()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Block(Guid.NewGuid());

        Assert.Equal(GapStatus.Blocked, gap.Status);
    }

    [Fact]
    public void Block_FromResolved_Throws()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => gap.Block(Guid.NewGuid()));
    }

    // ── Resolve ───────────────────────────────────────────────────────────────

    [Fact]
    public void Resolve_FromInProgress_SetsResolved()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());

        Assert.Equal(GapStatus.Resolved, gap.Status);
    }

    [Fact]
    public void Resolve_FromAssigned_Throws()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => gap.Resolve(Guid.NewGuid()));
    }

    // ── AcceptRisk ────────────────────────────────────────────────────────────

    [Fact]
    public void AcceptRisk_FromOpen_SetsAcceptedRisk()
    {
        var gap = BuildOpen();
        gap.AcceptRisk("Riesgo residual documentado y aceptado por DPO.", Guid.NewGuid());

        Assert.Equal(GapStatus.AcceptedRisk, gap.Status);
        Assert.Contains("DPO", gap.RiskAcceptanceJustification);
    }

    [Fact]
    public void AcceptRisk_EmptyJustification_Throws()
    {
        var gap = BuildOpen();
        Assert.Throws<ArgumentException>(() => gap.AcceptRisk("", Guid.NewGuid()));
    }

    [Fact]
    public void AcceptRisk_FromResolved_Throws()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            gap.AcceptRisk("justificación", Guid.NewGuid()));
    }

    // ── Close ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Close_FromResolved_SetsClosed()
    {
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());
        var closer = Guid.NewGuid();
        gap.Close(closer);

        Assert.Equal(GapStatus.Closed, gap.Status);
        Assert.NotNull(gap.ClosedAt);
        Assert.Equal(closer, gap.ClosedBy);
    }

    [Fact]
    public void Close_FromAcceptedRisk_SetsClosed()
    {
        var gap = BuildOpen();
        gap.AcceptRisk("Justificación formal", Guid.NewGuid());
        gap.Close(Guid.NewGuid());

        Assert.Equal(GapStatus.Closed, gap.Status);
    }

    [Fact]
    public void Close_FromOpen_Throws()
    {
        var gap = BuildOpen();
        Assert.Throws<InvalidOperationException>(() => gap.Close(Guid.NewGuid()));
    }

    // ── BlocksApproval ────────────────────────────────────────────────────────

    [Fact]
    public void Critical_Resolved_DoesNotBlockApproval()
    {
        var gap = BuildOpen(GapSeverity.Critical);
        gap.Assign(Guid.NewGuid(), Guid.NewGuid());
        gap.StartProgress(Guid.NewGuid());
        gap.Resolve(Guid.NewGuid());

        Assert.False(gap.BlocksApproval);
    }

    [Fact]
    public void Critical_AcceptedRisk_DoesNotBlockApproval()
    {
        var gap = BuildOpen(GapSeverity.Critical);
        gap.AcceptRisk("Aceptado formalmente", Guid.NewGuid());

        Assert.False(gap.BlocksApproval);
    }

    [Fact]
    public void Critical_Closed_DoesNotBlockApproval()
    {
        var gap = BuildOpen(GapSeverity.Critical);
        gap.AcceptRisk("Aceptado formalmente", Guid.NewGuid());
        gap.Close(Guid.NewGuid());

        Assert.False(gap.BlocksApproval);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Open_ChangesFields()
    {
        var gap = BuildOpen();
        var due = DateTimeOffset.UtcNow.AddDays(10);

        gap.Update("Nuevo título", "Nueva descripción", GapSeverity.High, Guid.NewGuid(), due);

        Assert.Equal("Nuevo título", gap.Title);
        Assert.Equal(GapSeverity.High, gap.Severity);
        Assert.Equal(due, gap.DueAt);
    }

    [Fact]
    public void Update_Closed_Throws()
    {
        var gap = BuildOpen();
        gap.AcceptRisk("Justificación", Guid.NewGuid());
        gap.Close(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            gap.Update("x", "y", GapSeverity.Low, Guid.NewGuid()));
    }
}
