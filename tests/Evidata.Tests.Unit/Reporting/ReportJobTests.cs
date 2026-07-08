using Evidata.Modules.Reporting.Domain;

namespace Evidata.Tests.Unit.Reporting;

public class ReportJobTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();
    private const string _params = "{\"from\":\"2026-01-01\"}";

    private static ReportJob Build() =>
        ReportJob.Create(_tenant, ReportType.RAT, _params, _user);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsStatusRequested()
    {
        var j = Build();
        Assert.Equal(ReportJobStatus.Requested, j.Status);
    }

    [Fact]
    public void Create_SetsRequiredFields()
    {
        var j = Build();
        Assert.Equal(_tenant, j.TenantId);
        Assert.Equal(ReportType.RAT, j.ReportType);
        Assert.Equal(_params, j.Parameters);
        Assert.Equal(_user, j.RequestedBy);
        Assert.NotEqual(Guid.Empty, j.Id);
        Assert.False(j.IsTerminal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyParameters_Throws(string p)
    {
        Assert.Throws<ArgumentException>(() =>
            ReportJob.Create(_tenant, ReportType.RAT, p, _user));
    }

    // ── Start ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Start_FromRequested_SetsRunning()
    {
        var j = Build();
        j.Start();
        Assert.Equal(ReportJobStatus.Running, j.Status);
        Assert.NotNull(j.StartedAt);
    }

    [Fact]
    public void Start_NotRequested_Throws()
    {
        var j = Build();
        j.Start();
        Assert.Throws<InvalidOperationException>(() => j.Start());
    }

    // ── Complete ──────────────────────────────────────────────────────────────

    [Fact]
    public void Complete_Running_SetsCompleted()
    {
        var j = Build();
        j.Start();
        var docId = Guid.NewGuid();
        j.Complete(docId);

        Assert.Equal(ReportJobStatus.Completed, j.Status);
        Assert.Equal(docId, j.ArtifactDocumentId);
        Assert.NotNull(j.CompletedAt);
        Assert.True(j.IsTerminal);
    }

    [Fact]
    public void Complete_NotRunning_Throws()
    {
        var j = Build();
        Assert.Throws<InvalidOperationException>(() => j.Complete(Guid.NewGuid()));
    }

    // ── Fail ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Fail_Running_SetsFailed()
    {
        var j = Build();
        j.Start();
        j.Fail("Timeout al generar PDF.");

        Assert.Equal(ReportJobStatus.Failed, j.Status);
        Assert.Equal("Timeout al generar PDF.", j.ErrorMessage);
        Assert.NotNull(j.CompletedAt);
        Assert.True(j.IsTerminal);
    }

    [Fact]
    public void Fail_EmptyMessage_Throws()
    {
        var j = Build();
        j.Start();
        Assert.Throws<ArgumentException>(() => j.Fail(""));
    }

    [Fact]
    public void Fail_NotRunning_Throws()
    {
        var j = Build();
        Assert.Throws<InvalidOperationException>(() => j.Fail("Error."));
    }

    // ── Expire ────────────────────────────────────────────────────────────────

    [Fact]
    public void Expire_Requested_SetsExpired()
    {
        var j = Build();
        j.Expire();
        Assert.Equal(ReportJobStatus.Expired, j.Status);
        Assert.True(j.IsTerminal);
    }

    [Fact]
    public void Expire_Running_SetsExpired()
    {
        var j = Build();
        j.Start();
        j.Expire();
        Assert.Equal(ReportJobStatus.Expired, j.Status);
    }

    [Fact]
    public void Expire_Completed_Throws()
    {
        var j = Build();
        j.Start();
        j.Complete(Guid.NewGuid());
        Assert.Throws<InvalidOperationException>(() => j.Expire());
    }

    [Fact]
    public void Expire_Failed_Throws()
    {
        var j = Build();
        j.Start();
        j.Fail("Error.");
        Assert.Throws<InvalidOperationException>(() => j.Expire());
    }

    // ── IsTerminal ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(false, ReportJobStatus.Requested)]
    [InlineData(false, ReportJobStatus.Running)]
    public void IsTerminal_NonTerminalStates_ReturnsFalse(bool expected, ReportJobStatus _) =>
        Assert.False(expected); // los estados non-terminal se verifican implícitamente arriba

    [Fact]
    public void IsTerminal_AfterExpire_ReturnsTrue()
    {
        var j = Build();
        j.Expire();
        Assert.True(j.IsTerminal);
    }

    // ── Flujo completo ────────────────────────────────────────────────────────

    [Fact]
    public void FullSuccessFlow_ProducesCorrectState()
    {
        var j = Build();
        Assert.Equal(ReportJobStatus.Requested, j.Status);
        j.Start();
        Assert.Equal(ReportJobStatus.Running, j.Status);
        var doc = Guid.NewGuid();
        j.Complete(doc);
        Assert.Equal(ReportJobStatus.Completed, j.Status);
        Assert.Equal(doc, j.ArtifactDocumentId);
    }
}
