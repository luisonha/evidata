using Evidata.Modules.Mcp.Application.RatContext;
using NSubstitute;

namespace Evidata.Tests.Unit.Mcp;

public class RatContextProviderTests
{
    private static RatContextSnapshot BuildSnapshot(Guid tenantId,
        params RatActivitySummary[] activities) =>
        new(tenantId, activities);

    private static RatActivitySummary BuildSummary(
        string name = "Tratamiento Marketing",
        bool sensitive = false,
        bool international = false,
        bool automated = false,
        bool enhancedReview = false,
        string? legalBasis = "Consent") =>
        new(
            ActivityId: Guid.NewGuid(),
            Name: name,
            Department: "Marketing",
            LegalBasis: legalBasis,
            DataCategoryNames: new[] { "Email", "Teléfono" },
            HasSensitiveData: sensitive,
            HasInternationalTransfer: international,
            HasAutomatedDecision: automated,
            RequiresEnhancedReview: enhancedReview);

    // ── Snapshot básico ───────────────────────────────────────────────────────

    [Fact]
    public void Snapshot_Empty_ReturnsIsEmpty()
    {
        var tenantId = Guid.NewGuid();
        var snapshot = BuildSnapshot(tenantId);
        Assert.True(snapshot.IsEmpty);
    }

    [Fact]
    public void Snapshot_WithActivities_IsNotEmpty()
    {
        var tenantId = Guid.NewGuid();
        var snapshot = BuildSnapshot(tenantId, BuildSummary());
        Assert.False(snapshot.IsEmpty);
        Assert.Single(snapshot.ApprovedActivities);
    }

    // ── Propiedades derivadas ─────────────────────────────────────────────────

    [Fact]
    public void Snapshot_WithSensitiveActivity_HasAnySensitiveDataTrue()
    {
        var snapshot = BuildSnapshot(Guid.NewGuid(),
            BuildSummary(sensitive: true));
        Assert.True(snapshot.HasAnySensitiveData);
    }

    [Fact]
    public void Snapshot_WithoutSensitiveActivity_HasAnySensitiveDataFalse()
    {
        var snapshot = BuildSnapshot(Guid.NewGuid(),
            BuildSummary(sensitive: false));
        Assert.False(snapshot.HasAnySensitiveData);
    }

    [Fact]
    public void Snapshot_WithInternationalTransfer_HasAnyInternationalTrue()
    {
        var snapshot = BuildSnapshot(Guid.NewGuid(),
            BuildSummary(international: true));
        Assert.True(snapshot.HasAnyInternationalTransfer);
    }

    [Fact]
    public void Snapshot_WithoutInternationalTransfer_HasAnyInternationalFalse()
    {
        var snapshot = BuildSnapshot(Guid.NewGuid(),
            BuildSummary(international: false));
        Assert.False(snapshot.HasAnyInternationalTransfer);
    }

    // ── Mixed activities ──────────────────────────────────────────────────────

    [Fact]
    public void Snapshot_MixedActivities_AggregatesCorrectly()
    {
        var snapshot = BuildSnapshot(Guid.NewGuid(),
            BuildSummary("A", sensitive: false, international: false),
            BuildSummary("B", sensitive: true, international: true));

        Assert.Equal(2, snapshot.ApprovedActivities.Count);
        Assert.True(snapshot.HasAnySensitiveData);
        Assert.True(snapshot.HasAnyInternationalTransfer);
    }

    // ── Contract via mock ─────────────────────────────────────────────────────

    [Fact]
    public async Task Provider_ReturnsMockedSnapshot()
    {
        var tenantId = Guid.NewGuid();
        var expected = BuildSnapshot(tenantId, BuildSummary());

        var provider = Substitute.For<IRatContextProvider>();
        provider.GetSnapshotAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await provider.GetSnapshotAsync(tenantId);

        Assert.Equal(tenantId, result.TenantId);
        Assert.Single(result.ApprovedActivities);
    }

    [Fact]
    public async Task Provider_EmptyTenant_ReturnsEmptySnapshot()
    {
        var tenantId = Guid.NewGuid();
        var expected = BuildSnapshot(tenantId);

        var provider = Substitute.For<IRatContextProvider>();
        provider.GetSnapshotAsync(tenantId, Arg.Any<CancellationToken>())
            .Returns(expected);

        var result = await provider.GetSnapshotAsync(tenantId);
        Assert.True(result.IsEmpty);
    }

    // ── Summary record ────────────────────────────────────────────────────────

    [Fact]
    public void Summary_RequiresEnhancedReview_PropagatesCorrectly()
    {
        var summary = BuildSummary(automated: true, enhancedReview: true);
        Assert.True(summary.RequiresEnhancedReview);
        Assert.True(summary.HasAutomatedDecision);
    }

    [Fact]
    public void Summary_DataCategoryNames_ArePreserved()
    {
        var summary = BuildSummary();
        Assert.Contains("Email", summary.DataCategoryNames);
        Assert.Contains("Teléfono", summary.DataCategoryNames);
    }
}
