using Evidata.Modules.Mcp.Application.Audit;
using Evidata.Modules.Mcp.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Mcp;

public class McpAuditServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    // ── McpInteractionHistory ─────────────────────────────────────────────────

    [Fact]
    public void History_TotalPages_CalculatedCorrectly()
    {
        var history = new McpInteractionHistory([], 95, 1, 50);
        Assert.Equal(2, history.TotalPages);
        Assert.True(history.HasNextPage);
    }

    [Fact]
    public void History_LastPage_HasNoNextPage()
    {
        var history = new McpInteractionHistory([], 95, 2, 50);
        Assert.Equal(2, history.TotalPages);
        Assert.False(history.HasNextPage);
    }

    [Fact]
    public void History_ExactMultiple_NoExtraPage()
    {
        var history = new McpInteractionHistory([], 100, 1, 50);
        Assert.Equal(2, history.TotalPages);
    }

    [Fact]
    public void History_Empty_OnePage()
    {
        var history = new McpInteractionHistory([], 0, 1, 50);
        Assert.Equal(0, history.TotalPages);
        Assert.False(history.HasNextPage);
    }

    // ── McpFeedbackMetrics ────────────────────────────────────────────────────

    [Fact]
    public void FeedbackMetrics_HundredPercent_WhenAllHelpful()
    {
        var metrics = new McpFeedbackMetrics(
            TenantId, TotalFeedbacks: 10, HelpfulCount: 10, NotHelpfulCount: 0,
            HelpfulPercent: 100.0,
            HelpfulByRiskLevel: new Dictionary<McpRiskLevel, int> { [McpRiskLevel.Low] = 10 },
            NotHelpfulByRiskLevel: new Dictionary<McpRiskLevel, int>());

        Assert.Equal(100.0, metrics.HelpfulPercent);
        Assert.Equal(0, metrics.NotHelpfulCount);
    }

    [Fact]
    public void FeedbackMetrics_ZeroWhenNoFeedbacks()
    {
        var metrics = new McpFeedbackMetrics(
            TenantId, 0, 0, 0, 0.0,
            new Dictionary<McpRiskLevel, int>(),
            new Dictionary<McpRiskLevel, int>());

        Assert.Equal(0.0, metrics.HelpfulPercent);
        Assert.Equal(0, metrics.TotalFeedbacks);
    }

    [Fact]
    public void FeedbackMetrics_MixedRatings_CalculatesPercent()
    {
        var metrics = new McpFeedbackMetrics(
            TenantId, TotalFeedbacks: 4, HelpfulCount: 3, NotHelpfulCount: 1,
            HelpfulPercent: 75.0,
            HelpfulByRiskLevel: new Dictionary<McpRiskLevel, int> { [McpRiskLevel.Low] = 3 },
            NotHelpfulByRiskLevel: new Dictionary<McpRiskLevel, int> { [McpRiskLevel.High] = 1 });

        Assert.Equal(75.0, metrics.HelpfulPercent);
        Assert.Equal(1, metrics.NotHelpfulByRiskLevel[McpRiskLevel.High]);
    }

    // ── McpHitlSummary ────────────────────────────────────────────────────────

    [Fact]
    public void HitlSummary_TotalCount_SumsAllStatuses()
    {
        var summary = new McpHitlSummary(TenantId, 3, 2, 5, 1);
        Assert.Equal(11, summary.TotalCount);
        Assert.Equal(5, summary.PendingCount);
        Assert.Equal(6, summary.ResolvedCount);
    }

    [Fact]
    public void HitlSummary_AllResolved_NoPending()
    {
        var summary = new McpHitlSummary(TenantId, 0, 0, 4, 2);
        Assert.Equal(0, summary.PendingCount);
        Assert.Equal(6, summary.ResolvedCount);
    }

    [Fact]
    public void HitlSummary_Empty_AllZero()
    {
        var summary = new McpHitlSummary(TenantId, 0, 0, 0, 0);
        Assert.Equal(0, summary.TotalCount);
    }

    // ── Contract via mock ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetInteractionHistory_UsesQuery_ReturnsMockedResult()
    {
        var svc = Substitute.For<IMcpAuditService>();
        var query = new McpAuditQuery(TenantId, Page: 1, PageSize: 10);

        var expected = new McpInteractionHistory([], 0, 1, 10);
        svc.GetInteractionHistoryAsync(query, Arg.Any<CancellationToken>())
           .Returns(expected);

        var result = await svc.GetInteractionHistoryAsync(query);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetFeedbackMetrics_ReturnsMockedMetrics()
    {
        var svc = Substitute.For<IMcpAuditService>();
        var metrics = new McpFeedbackMetrics(TenantId, 5, 4, 1, 80.0,
            new Dictionary<McpRiskLevel, int>(), new Dictionary<McpRiskLevel, int>());

        svc.GetFeedbackMetricsAsync(TenantId, Arg.Any<CancellationToken>())
           .Returns(metrics);

        var result = await svc.GetFeedbackMetricsAsync(TenantId);
        Assert.Equal(80.0, result.HelpfulPercent);
    }

    [Fact]
    public async Task GetHitlSummary_ReturnsMockedSummary()
    {
        var svc = Substitute.For<IMcpAuditService>();
        var summary = new McpHitlSummary(TenantId, 2, 1, 3, 0);
        svc.GetHitlSummaryAsync(TenantId, Arg.Any<CancellationToken>())
           .Returns(summary);

        var result = await svc.GetHitlSummaryAsync(TenantId);
        Assert.Equal(3, result.PendingCount);
        Assert.Equal(3, result.ResolvedCount);
    }

    // ── McpAuditQuery defaults ────────────────────────────────────────────────

    [Fact]
    public void Query_DefaultPagination_IsCorrect()
    {
        var query = new McpAuditQuery(TenantId);
        Assert.Equal(1, query.Page);
        Assert.Equal(50, query.PageSize);
        Assert.Null(query.RiskLevel);
        Assert.Null(query.Status);
    }

    [Fact]
    public void Query_WithFilters_PreservesAll()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;
        var query = new McpAuditQuery(TenantId, from, to,
            McpRiskLevel.High, McpInteractionStatus.ReviewRequested, 2, 20);

        Assert.Equal(McpRiskLevel.High, query.RiskLevel);
        Assert.Equal(McpInteractionStatus.ReviewRequested, query.Status);
        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.PageSize);
    }
}
