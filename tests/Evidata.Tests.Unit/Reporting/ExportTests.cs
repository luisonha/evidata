using Evidata.Modules.Reporting.Domain;

namespace Evidata.Tests.Unit.Reporting;

public class ExportTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();
    private static readonly Guid _activity = Guid.NewGuid();
    private const string _contentType = "application/pdf";
    private const string _correlationId = "corr-123-456";

    private static Export Build(
        int version = 1,
        ExportType exportType = ExportType.ProcessingActivityPdfSummary) =>
        Export.Create(
            tenantId: _tenant,
            processingActivityId: _activity,
            exportType: exportType,
            contentType: _contentType,
            version: version,
            requestedByUserId: _user,
            correlationId: _correlationId);

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsStatusRequested()
    {
        var ex = Build();
        Assert.Equal(ExportStatus.Requested, ex.Status);
    }

    [Fact]
    public void Create_SetsAllRequiredFields()
    {
        var ex = Build(version: 2);
        Assert.Equal(_tenant, ex.TenantId);
        Assert.Equal(_activity, ex.ProcessingActivityId);
        Assert.Equal(ExportType.ProcessingActivityPdfSummary, ex.ExportType);
        Assert.Equal(ExportStatus.Requested, ex.Status);
        Assert.Equal(2, ex.Version);
        Assert.Equal(_contentType, ex.ContentType);
        Assert.Equal(_user, ex.RequestedByUserId);
        Assert.Equal(_correlationId, ex.CorrelationId);
        Assert.Null(ex.ArtifactDocumentId);
        Assert.Null(ex.GeneratedAt);
        Assert.Null(ex.ErrorMessage);
        Assert.NotEqual(Guid.Empty, ex.Id);
        Assert.False(ex.IsTerminal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyContentType_Throws(string ct)
    {
        Assert.Throws<ArgumentException>(() =>
            Export.Create(_tenant, _activity, ExportType.GlobalRatExcel, ct, 1, _user, _correlationId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_EmptyCorrelationId_Throws(string corrId)
    {
        Assert.Throws<ArgumentException>(() =>
            Export.Create(_tenant, _activity, ExportType.GlobalRatExcel, _contentType, 1, _user, corrId));
    }

    [Fact]
    public void Create_TrimsContentTypeAndCorrelationId()
    {
        var ex = Export.Create(_tenant, _activity, ExportType.GlobalRatExcel, "  " + _contentType + "  ", 1, _user, "  " + _correlationId + "  ");
        Assert.Equal(_contentType, ex.ContentType);
        Assert.Equal(_correlationId, ex.CorrelationId);
    }

    // ── Lifecycle: Requested → Generating ─────────────────────────────────────

    [Fact]
    public void Start_FromRequested_SetsGenerating()
    {
        var ex = Build();
        ex.Start();
        Assert.Equal(ExportStatus.Generating, ex.Status);
    }

    [Fact]
    public void Start_NotFromRequested_Throws()
    {
        var ex = Build();
        ex.Start();
        var ex2 = Assert.Throws<InvalidOperationException>(() => ex.Start());
        Assert.Contains("Requested", ex2.Message);
    }

    // ── Lifecycle: Generating → Completed ─────────────────────────────────────

    [Fact]
    public void Complete_FromGenerating_SetsCompletedAndGeneratedAt()
    {
        var ex = Build();
        ex.Start();
        var artifactId = Guid.NewGuid();
        var beforeComplete = DateTimeOffset.UtcNow;

        ex.Complete(artifactId);

        Assert.Equal(ExportStatus.Completed, ex.Status);
        Assert.Equal(artifactId, ex.ArtifactDocumentId);
        Assert.NotNull(ex.GeneratedAt);
        Assert.True(ex.GeneratedAt >= beforeComplete);
        Assert.True(ex.IsTerminal);
    }

    [Fact]
    public void Complete_NotFromGenerating_Throws()
    {
        var ex = Build();
        Assert.Throws<InvalidOperationException>(() => ex.Complete(Guid.NewGuid()));
    }

    // ── Lifecycle: Generating → Failed ────────────────────────────────────────

    [Fact]
    public void Fail_FromGenerating_SetsFailed()
    {
        var ex = Build();
        ex.Start();
        const string error = "Database timeout";

        ex.Fail(error);

        Assert.Equal(ExportStatus.Failed, ex.Status);
        Assert.Equal(error, ex.ErrorMessage);
        Assert.NotNull(ex.GeneratedAt);
        Assert.True(ex.IsTerminal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Fail_EmptyErrorMessage_Throws(string errMsg)
    {
        var ex = Build();
        ex.Start();
        Assert.Throws<ArgumentException>(() => ex.Fail(errMsg));
    }

    [Fact]
    public void Fail_TrimsErrorMessage()
    {
        var ex = Build();
        ex.Start();
        const string error = "timeout";
        ex.Fail("  " + error + "  ");
        Assert.Equal(error, ex.ErrorMessage);
    }

    [Fact]
    public void Fail_NotFromGenerating_Throws()
    {
        var ex = Build();
        Assert.Throws<InvalidOperationException>(() => ex.Fail("error"));
    }

    // ── Warnings ──────────────────────────────────────────────────────────────

    [Fact]
    public void AddWarning_AddsWarning()
    {
        var ex = Build();
        const string warning = "Incomplete data in section X";

        ex.AddWarning(warning);

        Assert.Single(ex.Warnings);
        Assert.Contains(warning, ex.Warnings);
    }

    [Fact]
    public void AddWarning_DeduplicatesWarnings()
    {
        var ex = Build();
        const string warning = "Incomplete data";

        ex.AddWarning(warning);
        ex.AddWarning(warning);

        Assert.Single(ex.Warnings);
    }

    [Fact]
    public void AddWarning_TrimsWarning()
    {
        var ex = Build();
        const string warning = "Incomplete data";

        ex.AddWarning("  " + warning + "  ");

        Assert.Single(ex.Warnings);
        Assert.Contains(warning, ex.Warnings);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddWarning_EmptyWarning_Throws(string warning)
    {
        var ex = Build();
        Assert.Throws<ArgumentException>(() => ex.AddWarning(warning));
    }

    [Fact]
    public void AddWarning_MultipleWarnings()
    {
        var ex = Build();
        const string w1 = "Warning 1";
        const string w2 = "Warning 2";

        ex.AddWarning(w1);
        ex.AddWarning(w2);

        Assert.Equal(2, ex.Warnings.Count);
        Assert.Contains(w1, ex.Warnings);
        Assert.Contains(w2, ex.Warnings);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    [Fact]
    public void IsTerminal_ForRequested_False()
    {
        var ex = Build();
        Assert.False(ex.IsTerminal);
    }

    [Fact]
    public void IsTerminal_ForGenerating_False()
    {
        var ex = Build();
        ex.Start();
        Assert.False(ex.IsTerminal);
    }

    [Fact]
    public void IsTerminal_ForCompleted_True()
    {
        var ex = Build();
        ex.Start();
        ex.Complete(Guid.NewGuid());
        Assert.True(ex.IsTerminal);
    }

    [Fact]
    public void IsTerminal_ForFailed_True()
    {
        var ex = Build();
        ex.Start();
        ex.Fail("error");
        Assert.True(ex.IsTerminal);
    }

    // ── Export Types ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData(ExportType.ProcessingActivityPdfSummary)]
    [InlineData(ExportType.GlobalRatExcel)]
    [InlineData(ExportType.ApprovalHistory)]
    [InlineData(ExportType.InternalJson)]
    public void Create_AllExportTypes_SucceedsSetCorrectType(ExportType exportType)
    {
        var ex = Build(exportType: exportType);
        Assert.Equal(exportType, ex.ExportType);
    }

    // ── Version Tracking ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    [InlineData(100)]
    public void Create_VersionField_SetCorrectly(int version)
    {
        var ex = Build(version: version);
        Assert.Equal(version, ex.Version);
    }
}
