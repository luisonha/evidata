using Evidata.Modules.Mcp.Application.Abstractions;
using Evidata.Modules.Mcp.Application.Query;
using Evidata.Modules.Mcp.Application.RatContext;
using Evidata.Modules.Mcp.Application.RiskRouting;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Domain.RiskRouting;
using Evidata.Modules.Mcp.Infrastructure.Query;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evidata.Tests.Unit.Mcp;

public class McpQueryServiceTests
{
    // ── Fakes ─────────────────────────────────────────────────────────────────

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    private sealed class FakeRiskRouter : IMcpRiskRouter
    {
        public RiskClassificationResult? Preset { get; set; }

        public RiskClassificationResult Classify(string question) =>
            Preset ?? RiskClassificationResult.Low();

        public bool RequiresHumanReview(RiskClassificationResult result) =>
            result.RiskLevel == McpRiskLevel.High && !result.ShouldAbstain;
    }

    private sealed class FakeRatContextProvider : IRatContextProvider
    {
        public RatContextSnapshot Snapshot { get; set; } = EmptySnapshot();

        public Task<RatContextSnapshot> GetSnapshotAsync(Guid tenantId, CancellationToken ct = default)
            => Task.FromResult(Snapshot);

        public static RatContextSnapshot EmptySnapshot() =>
            new(TenantId, Array.Empty<RatActivitySummary>());

        public static RatContextSnapshot WithRat(string name, bool sensitive = false) =>
            new(TenantId, new[]
            {
                new RatActivitySummary(
                    ActivityId: Guid.NewGuid(),
                    Name: name,
                    Department: "TI",
                    LegalBasis: "Consentimiento",
                    DataCategoryNames: new[] { "Identificativos" },
                    HasSensitiveData: sensitive,
                    HasInternationalTransfer: false,
                    HasAutomatedDecision: false,
                    RequiresEnhancedReview: false)
            });
    }

    private sealed class FakeInteractionService : IMcpInteractionService
    {
        public List<McpInteraction> Recorded { get; } = [];
        public List<McpInteraction> Failed    { get; } = [];
        public List<(Guid Id, McpCitationSourceType SourceType, string SourceId)> Citations { get; } = [];

        public Task<McpInteraction> RecordAsync(
            Guid tenantId, Guid userId, string question, string answer,
            McpRiskLevel riskLevel, bool usedTenantContext, bool requiresHumanReview,
            CancellationToken ct = default)
        {
            var interaction = BuildInteraction(tenantId, userId, question, riskLevel, usedTenantContext, requiresHumanReview);
            Recorded.Add(interaction);
            return Task.FromResult(interaction);
        }

        public Task<McpInteraction> RecordFailedAsync(Guid tenantId, Guid userId, string question, CancellationToken ct = default)
        {
            var interaction = McpInteraction.RecordFailed(tenantId, userId, question);
            Failed.Add(interaction);
            return Task.FromResult(interaction);
        }

        public Task<McpCitation> AddCitationAsync(
            Guid interactionId, McpCitationSourceType sourceType, string sourceId,
            string fragment, string? sourceVersion = null, CancellationToken ct = default)
        {
            Citations.Add((interactionId, sourceType, sourceId));
            return Task.FromResult(McpCitation.Create(interactionId, sourceType, sourceId, fragment, sourceVersion));
        }

        public Task RequestHumanReviewAsync(Guid interactionId, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task RecordFeedbackAsync(
            Guid interactionId, Guid tenantId, Guid userId,
            McpFeedbackRating rating, string? comment, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<McpInteraction?> GetByIdAsync(Guid interactionId, CancellationToken ct = default)
            => Task.FromResult<McpInteraction?>(null);

        public Task<IReadOnlyList<McpInteraction>> GetRecentByTenantAsync(
            Guid tenantId, int limit = 20, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<McpInteraction>>(Array.Empty<McpInteraction>());

        public Task<IReadOnlyList<McpInteraction>> GetPendingReviewAsync(
            Guid tenantId, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<McpInteraction>>(Array.Empty<McpInteraction>());

        private static McpInteraction BuildInteraction(
            Guid tenantId, Guid userId, string question,
            McpRiskLevel riskLevel, bool usedTenantContext, bool requiresHumanReview) =>
            McpInteraction.Record(
                tenantId, userId,
                question, "answer",
                riskLevel, usedTenantContext, requiresHumanReview);
    }

    private McpQueryService BuildService(
        FakeRiskRouter? router = null,
        FakeRatContextProvider? ratProvider = null,
        FakeInteractionService? interactions = null) =>
        new(
            router       ?? new FakeRiskRouter(),
            ratProvider  ?? new FakeRatContextProvider(),
            interactions ?? new FakeInteractionService(),
            NullLogger<McpQueryService>.Instance);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Query_LowRisk_ReturnsSuccessfulResponse()
    {
        var service = BuildService();

        var result = await service.QueryAsync(
            new McpQueryRequest(TenantId, UserId, "¿Qué datos tratamos?"));

        Assert.Equal(McpRiskLevel.Low, result.RiskLevel);
        Assert.False(result.RequiresHumanReview);
        Assert.False(result.UsedTenantContext);
        Assert.Null(result.AbstentionReason);
    }

    [Fact]
    public async Task Query_Abstain_ReturnsAbstentionWithoutRecordingSuccessful()
    {
        var router       = new FakeRiskRouter { Preset = RiskClassificationResult.Abstain("Consulta legal fuera de alcance.", ["asesoría"]) };
        var interactions = new FakeInteractionService();

        var result = await BuildService(router, interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Puedo usar datos sin consentimiento?"));

        Assert.Equal(McpRiskLevel.High, result.RiskLevel);
        Assert.NotNull(result.AbstentionReason);
        Assert.Contains("⛔", result.Answer);
        Assert.Empty(interactions.Recorded);
        Assert.Single(interactions.Failed);
    }

    [Fact]
    public async Task Query_WithApprovedRats_SetsUsedTenantContextTrue()
    {
        var ratProvider  = new FakeRatContextProvider { Snapshot = FakeRatContextProvider.WithRat("Gestión de clientes") };
        var interactions = new FakeInteractionService();

        var result = await BuildService(ratProvider: ratProvider, interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Cómo gestionamos datos de clientes?"));

        Assert.True(result.UsedTenantContext);
        Assert.Single(interactions.Recorded);
        Assert.True(interactions.Recorded[0].UsedTenantContext);
    }

    [Fact]
    public async Task Query_WithSensitiveDataRat_AnswerMentionsSensitiveData()
    {
        var ratProvider = new FakeRatContextProvider
        {
            Snapshot = FakeRatContextProvider.WithRat("Salud empleados", sensitive: true)
        };

        var result = await BuildService(ratProvider: ratProvider)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Qué precauciones necesitamos?"));

        Assert.Contains("sensibles", result.Answer, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Query_WithRats_AddsCitationsForRelevantActivities()
    {
        var ratProvider  = new FakeRatContextProvider { Snapshot = FakeRatContextProvider.WithRat("Nómina") };
        var interactions = new FakeInteractionService();

        await BuildService(ratProvider: ratProvider, interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Cuánto tiempo conservamos nómina?"));

        Assert.NotEmpty(interactions.Citations);
        Assert.All(interactions.Citations, c => Assert.Equal(McpCitationSourceType.ProcessingActivity, c.SourceType));
    }

    [Fact]
    public async Task Query_EmptySnapshot_SetsUsedTenantContextFalse()
    {
        var interactions = new FakeInteractionService();

        var result = await BuildService(interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Tenemos RATs?"));

        Assert.False(result.UsedTenantContext);
        Assert.Empty(result.RatReferences);
        Assert.False(interactions.Recorded[0].UsedTenantContext);
    }

    [Fact]
    public async Task Query_HighRisk_RequiresHumanReview()
    {
        var router = new FakeRiskRouter
        {
            Preset = RiskClassificationResult.High(["transferencia internacional", "datos biométricos"])
        };
        var interactions = new FakeInteractionService();

        var result = await BuildService(router, interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Podemos transferir datos biométricos a EE.UU.?"));

        Assert.Equal(McpRiskLevel.High, result.RiskLevel);
        Assert.True(result.RequiresHumanReview);
        Assert.True(interactions.Recorded[0].RequiresHumanReview);
    }

    [Fact]
    public async Task Query_EmptyQuestion_ThrowsArgumentException()
    {
        var service = BuildService();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(new McpQueryRequest(TenantId, UserId, "   ")));
    }

    [Fact]
    public async Task Query_InteractionIdMatchesRecordedInteraction()
    {
        var interactions = new FakeInteractionService();

        var result = await BuildService(interactions: interactions)
            .QueryAsync(new McpQueryRequest(TenantId, UserId, "¿Tenemos base legal?"));

        Assert.Equal(interactions.Recorded[0].Id, result.InteractionId);
    }
}
