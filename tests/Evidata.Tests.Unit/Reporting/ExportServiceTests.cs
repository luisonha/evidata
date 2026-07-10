using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Application.Services;
using Evidata.Modules.Reporting.Domain;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Evidata.Tests.Unit.Reporting;

public class ExportServiceTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();
    private static readonly Guid _activity = Guid.NewGuid();
    private const string _contentType = "application/pdf";
    private const string _correlationId = "corr-123";

    private IExportRepository _repositoryMock = null!;
    private IAuditService _auditMock = null!;
    private ILogger<ExportService> _loggerMock = null!;
    private ExportService _service = null!;

    private void Setup()
    {
        _repositoryMock = Substitute.For<IExportRepository>();
        _auditMock = Substitute.For<IAuditService>();
        _loggerMock = Substitute.For<ILogger<ExportService>>();
        _service = new ExportService(_repositoryMock, _auditMock, _loggerMock);
    }

    // ── RequestExportAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RequestExportAsync_CreatesExportWithCorrectFields()
    {
        Setup();
        const int expectedVersion = 1;
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(expectedVersion);

        var result = await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_tenant, result.TenantId);
        Assert.Equal(_activity, result.ProcessingActivityId);
        Assert.Equal(ExportType.ProcessingActivityPdfSummary, result.ExportType);
        Assert.Equal(ExportStatus.Requested, result.Status);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Equal(_contentType, result.ContentType);
        Assert.Equal(_user, result.RequestedByUserId);
        Assert.Equal(_correlationId, result.CorrelationId);
    }

    [Fact]
    public async Task RequestExportAsync_CallsGetNextVersion()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.GlobalRatExcel,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _repositoryMock.Received(1).GetNextVersionAsync(_activity, ExportType.GlobalRatExcel, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestExportAsync_PersistsExportAndSavesChanges()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _repositoryMock.Received(1).AddAsync(Arg.Any<Export>(), Arg.Any<CancellationToken>());
        await _repositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestExportAsync_LogsAuditEvent()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _auditMock.Received(1).LogAsync(
            _tenant,
            _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export",
            Arg.Any<Guid>(),
            AuditEventResult.Success,
            _correlationId,
            Arg.Is<Dictionary<string, object?>>(m =>
                m.ContainsKey("exportType") &&
                m.ContainsKey("processingActivityId") &&
                m.ContainsKey("version") &&
                m.ContainsKey("contentType")),
            null,
            AuditSeverity.Info,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RequestExportAsync_EmptyContentType_Throws(string ct)
    {
        Setup();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RequestExportAsync(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
                ct, _user, _correlationId, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RequestExportAsync_EmptyCorrelationId_Throws(string corrId)
    {
        Setup();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RequestExportAsync(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
                _contentType, _user, corrId, CancellationToken.None));
    }

    // ── StartGenerationAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task StartGenerationAsync_TransitionsToGenerating()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        await _service.StartGenerationAsync(export.Id, CancellationToken.None);

        Assert.Equal(ExportStatus.Generating, export.Status);
        await _repositoryMock.Received(1).UpdateAsync(export, Arg.Any<CancellationToken>());
        await _repositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGenerationAsync_NotFound_Throws()
    {
        Setup();
        var notFoundId = Guid.NewGuid();
        _repositoryMock.GetByIdAsync(notFoundId, Arg.Any<CancellationToken>())
            .Returns((Export?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartGenerationAsync(notFoundId, CancellationToken.None));
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_TransitionsToCompleted()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var artifactId = Guid.NewGuid();
        await _service.CompleteAsync(export.Id, artifactId, CancellationToken.None);

        Assert.Equal(ExportStatus.Completed, export.Status);
        Assert.Equal(artifactId, export.ArtifactDocumentId);
        await _auditMock.Received(1).LogAsync(
            _tenant, _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export", export.Id, AuditEventResult.Success,
            _correlationId, Arg.Any<Dictionary<string, object?>>(),
            null, AuditSeverity.Info, Arg.Any<CancellationToken>());
    }

    // ── FailAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FailAsync_TransitionsToFailed()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        const string errorMsg = "Database error";
        await _service.FailAsync(export.Id, errorMsg, CancellationToken.None);

        Assert.Equal(ExportStatus.Failed, export.Status);
        Assert.Equal(errorMsg, export.ErrorMessage);
        await _auditMock.Received(1).LogAsync(
            _tenant, _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export", export.Id, AuditEventResult.Failure,
            _correlationId, Arg.Any<Dictionary<string, object?>>(),
            null, AuditSeverity.Info, Arg.Any<CancellationToken>());
    }

    // ── AddWarningAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddWarningAsync_AddsWarningToExport()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        const string warning = "Incomplete section";
        await _service.AddWarningAsync(export.Id, warning, CancellationToken.None);

        Assert.Contains(warning, export.Warnings);
    }

    // ── GetExportDownloadAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetExportDownloadAsync_CompletedExport_ReturnsDownloadInfo()
    {
        Setup();
        var artifactId = Guid.NewGuid();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        export.Complete(artifactId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, _tenant, _user, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(export.Id, result.ExportId);
        Assert.Equal(_contentType, result.ContentType);
        Assert.Equal(artifactId, result.ArtifactDocumentId);
    }

    [Fact]
    public async Task GetExportDownloadAsync_NotCompleted_ReturnsNull()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, _tenant, _user, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExportDownloadAsync_WrongTenant_ReturnsNull()
    {
        Setup();
        var artifactId = Guid.NewGuid();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        export.Complete(artifactId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, Guid.NewGuid(), _user, CancellationToken.None);

        Assert.Null(result);
    }
}

    // ── RequestExportAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task RequestExportAsync_CreatesExportWithCorrectFields()
    {
        Setup();
        const int expectedVersion = 1;
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(expectedVersion);

        var result = await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(_tenant, result.TenantId);
        Assert.Equal(_activity, result.ProcessingActivityId);
        Assert.Equal(ExportType.ProcessingActivityPdfSummary, result.ExportType);
        Assert.Equal(ExportStatus.Requested, result.Status);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Equal(_contentType, result.ContentType);
        Assert.Equal(_user, result.RequestedByUserId);
        Assert.Equal(_correlationId, result.CorrelationId);
    }

    [Fact]
    public async Task RequestExportAsync_CallsGetNextVersion()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.GlobalRatExcel,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _repositoryMock.Received(1).GetNextVersionAsync(_activity, ExportType.GlobalRatExcel, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestExportAsync_PersistsExportAndSavesChanges()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _repositoryMock.Received(1).AddAsync(Arg.Any<Export>(), Arg.Any<CancellationToken>());
        await _repositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RequestExportAsync_LogsAuditEvent()
    {
        Setup();
        _repositoryMock.GetNextVersionAsync(
            Arg.Any<Guid>(), Arg.Any<ExportType>(), Arg.Any<CancellationToken>())
            .Returns(1);

        await _service.RequestExportAsync(
            _tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, _user, _correlationId, CancellationToken.None);

        await _auditMock.Received(1).LogAsync(
            _tenant,
            _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export",
            Arg.Any<Guid>(),
            AuditEventResult.Success,
            _correlationId,
            Arg.Is<Dictionary<string, object?>>(m =>
                m.ContainsKey("exportType") &&
                m.ContainsKey("processingActivityId") &&
                m.ContainsKey("version") &&
                m.ContainsKey("contentType")),
            null,
            AuditSeverity.Info,
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RequestExportAsync_EmptyContentType_Throws(string ct)
    {
        Setup();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RequestExportAsync(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
                ct, _user, _correlationId, CancellationToken.None));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task RequestExportAsync_EmptyCorrelationId_Throws(string corrId)
    {
        Setup();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.RequestExportAsync(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
                _contentType, _user, corrId, CancellationToken.None));
    }

    // ── StartGenerationAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task StartGenerationAsync_TransitionsToGenerating()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        await _service.StartGenerationAsync(export.Id, CancellationToken.None);

        Assert.Equal(ExportStatus.Generating, export.Status);
        await _repositoryMock.Received(1).UpdateAsync(export, Arg.Any<CancellationToken>());
        await _repositoryMock.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartGenerationAsync_NotFound_Throws()
    {
        Setup();
        var notFoundId = Guid.NewGuid();
        _repositoryMock.GetByIdAsync(notFoundId, Arg.Any<CancellationToken>())
            .Returns((Export?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.StartGenerationAsync(notFoundId, CancellationToken.None));
    }

    // ── CompleteAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task CompleteAsync_TransitionsToCompleted()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var artifactId = Guid.NewGuid();
        await _service.CompleteAsync(export.Id, artifactId, CancellationToken.None);

        Assert.Equal(ExportStatus.Completed, export.Status);
        Assert.Equal(artifactId, export.ArtifactDocumentId);
        await _auditMock.Received(1).LogAsync(
            _tenant, _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export", export.Id, AuditEventResult.Success,
            _correlationId, Arg.Any<Dictionary<string, object?>>(),
            null, AuditSeverity.Info, Arg.Any<CancellationToken>());
    }

    // ── FailAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FailAsync_TransitionsToFailed()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        const string errorMsg = "Database error";
        await _service.FailAsync(export.Id, errorMsg, CancellationToken.None);

        Assert.Equal(ExportStatus.Failed, export.Status);
        Assert.Equal(errorMsg, export.ErrorMessage);
        await _auditMock.Received(1).LogAsync(
            _tenant, _user,
            AuditEventType.GenerateOfficialExport.ToString(),
            "Export", export.Id, AuditEventResult.Failure,
            _correlationId, Arg.Any<Dictionary<string, object?>>(),
            null, AuditSeverity.Info, Arg.Any<CancellationToken>());
    }

    // ── AddWarningAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task AddWarningAsync_AddsWarningToExport()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        const string warning = "Incomplete section";
        await _service.AddWarningAsync(export.Id, warning, CancellationToken.None);

        Assert.Contains(warning, export.Warnings);
    }

    // ── GetExportDownloadAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetExportDownloadAsync_CompletedExport_ReturnsDownloadInfo()
    {
        Setup();
        var artifactId = Guid.NewGuid();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        export.Complete(artifactId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, _tenant, _user, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(export.Id, result.ExportId);
        Assert.Equal(_contentType, result.ContentType);
        Assert.Equal(artifactId, result.ArtifactDocumentId);
    }

    [Fact]
    public async Task GetExportDownloadAsync_NotCompleted_ReturnsNull()
    {
        Setup();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, _tenant, _user, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetExportDownloadAsync_WrongTenant_ReturnsNull()
    {
        Setup();
        var artifactId = Guid.NewGuid();
        var export = Export.Create(_tenant, _activity, ExportType.ProcessingActivityPdfSummary,
            _contentType, 1, _user, _correlationId);
        export.Start();
        export.Complete(artifactId);
        _repositoryMock.GetByIdAsync(export.Id, Arg.Any<CancellationToken>())
            .Returns(export);

        var result = await _service.GetExportDownloadAsync(
            export.Id, Guid.NewGuid(), _user, CancellationToken.None);

        Assert.Null(result);
    }
}
