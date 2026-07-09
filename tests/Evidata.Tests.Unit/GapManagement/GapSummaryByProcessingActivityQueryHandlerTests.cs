using Evidata.Modules.GapManagement.Application.Queries;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.GapManagement;

public class GapSummaryByProcessingActivityQueryHandlerTests : IDisposable
{
    private readonly GapManagementDbContext _db;
    private readonly GetGapSummaryByProcessingActivityQueryHandler _handler;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _processingActivityId = Guid.NewGuid();
    private readonly Guid _otherTenantId = Guid.NewGuid();
    private readonly Guid _otherProcessingActivityId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public GapSummaryByProcessingActivityQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GapManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new GapManagementDbContext(options);
        _handler = new GetGapSummaryByProcessingActivityQueryHandler(_db);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetByProcessingActivityAsync_NoGaps_ReturnsEmptySummaryAndEchoesVersion()
    {
        var versionId = Guid.NewGuid();

        var result = await _handler.GetByProcessingActivityAsync(_tenantId, _processingActivityId, versionId);

        Assert.Equal(_processingActivityId, result.ProcessingActivityId);
        Assert.Equal(versionId, result.VersionId);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.OpenCount);
        Assert.Equal(0, result.InCorrectionCount);
        Assert.Equal(0, result.ResolvedCount);
        Assert.Equal(0, result.AcceptedWithRiskCount);
        Assert.Equal(0, result.DismissedCount);
        Assert.Null(result.HighestSeverity);
        Assert.Null(result.HighestSeverityLabelKey);
        Assert.False(result.ApprovalBlocked);
    }

    [Fact]
    public async Task GetByProcessingActivityAsync_ComputesCountsAcrossSupportedStates()
    {
        var versionId = Guid.NewGuid();

        _db.ComplianceGaps.AddRange(
            CreateGap(_processingActivityId, GapSeverity.Critical),
            CreateGap(_processingActivityId, GapSeverity.High, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Medium, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Low, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
                gap.Block(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.High, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
                gap.Resolve(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Medium, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
                gap.Resolve(_userId);
                gap.Close(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.High, gap =>
            {
                gap.AcceptRisk("Riesgo aceptado por comité.", _userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Low, gap =>
            {
                gap.AcceptRisk("Riesgo residual aceptado.", _userId);
                gap.Close(_userId);
            }),
            CreateGap(_otherProcessingActivityId, GapSeverity.Critical),
            CreateGapForTenant(_otherTenantId, _processingActivityId, GapSeverity.Critical));

        await _db.SaveChangesAsync();

        var result = await _handler.GetByProcessingActivityAsync(_tenantId, _processingActivityId, versionId);

        Assert.Equal(8, result.TotalCount);
        Assert.Equal(2, result.OpenCount);
        Assert.Equal(2, result.InCorrectionCount);
        Assert.Equal(2, result.ResolvedCount);
        Assert.Equal(2, result.AcceptedWithRiskCount);
        Assert.Equal(0, result.DismissedCount);
        Assert.Equal(GapSeverity.Critical, result.HighestSeverity);
        Assert.Equal("gap.severity.critical", result.HighestSeverityLabelKey);
        Assert.True(result.ApprovalBlocked);
    }

    [Fact]
    public async Task GetByProcessingActivityAsync_IgnoresResolvedAcceptedAndClosedForHighestSeverity()
    {
        _db.ComplianceGaps.AddRange(
            CreateGap(_processingActivityId, GapSeverity.Medium, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Critical, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
                gap.Resolve(_userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Critical, gap =>
            {
                gap.AcceptRisk("Aceptado formalmente.", _userId);
            }),
            CreateGap(_processingActivityId, GapSeverity.Critical, gap =>
            {
                gap.Assign(Guid.NewGuid(), _userId);
                gap.StartProgress(_userId);
                gap.Resolve(_userId);
                gap.Close(_userId);
            }));

        await _db.SaveChangesAsync();

        var result = await _handler.GetByProcessingActivityAsync(_tenantId, _processingActivityId, Guid.NewGuid());

        Assert.Equal(GapSeverity.Medium, result.HighestSeverity);
        Assert.Equal("gap.severity.medium", result.HighestSeverityLabelKey);
        Assert.False(result.ApprovalBlocked);
    }

    private ComplianceGap CreateGap(Guid processingActivityId, GapSeverity severity, Action<ComplianceGap>? mutate = null) =>
        CreateGapForTenant(_tenantId, processingActivityId, severity, mutate);

    private ComplianceGap CreateGapForTenant(
        Guid tenantId,
        Guid processingActivityId,
        GapSeverity severity,
        Action<ComplianceGap>? mutate = null)
    {
        var gap = ComplianceGap.Create(
            tenantId,
            "ProcessingInventory",
            processingActivityId,
            $"Gap {Guid.NewGuid()}",
            "Descripción de prueba",
            severity,
            _userId);

        mutate?.Invoke(gap);
        return gap;
    }
}
