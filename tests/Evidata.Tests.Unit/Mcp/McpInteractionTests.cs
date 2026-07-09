using Evidata.Modules.Mcp.Domain;

namespace Evidata.Tests.Unit.Mcp;

public class McpInteractionTests
{
    private static readonly Guid _tenant = Guid.NewGuid();
    private static readonly Guid _user = Guid.NewGuid();

    private static McpInteraction Build(
        McpRiskLevel risk = McpRiskLevel.Low,
        bool usedContext = false,
        bool requiresReview = false) =>
        McpInteraction.Record(_tenant, _user,
            "¿Qué datos personales se tratan?",
            "Según el RAT vigente, se tratan datos de identificación.",
            risk, usedContext, requiresReview);

    // ── Record ────────────────────────────────────────────────────────────────

    [Fact]
    public void Record_NoReview_SetsCompleted()
    {
        var i = Build();
        Assert.Equal(McpInteractionStatus.Completed, i.Status);
        Assert.False(i.RequiresHumanReview);
    }

    [Fact]
    public void Record_RequiresReview_SetsReviewRequested()
    {
        var i = Build(requiresReview: true);
        Assert.Equal(McpInteractionStatus.ReviewRequested, i.Status);
        Assert.True(i.RequiresHumanReview);
    }

    [Fact]
    public void Record_SetsFields()
    {
        var i = Build(McpRiskLevel.Medium, usedContext: true);
        Assert.Equal(_tenant, i.TenantId);
        Assert.Equal(_user, i.UserId);
        Assert.Equal(McpRiskLevel.Medium, i.RiskLevel);
        Assert.True(i.UsedTenantContext);
        Assert.NotEqual(Guid.Empty, i.Id);
        Assert.Empty(i.Citations);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyQuestion_Throws(string q)
    {
        Assert.Throws<ArgumentException>(() =>
            McpInteraction.Record(_tenant, _user, q, "respuesta", McpRiskLevel.Low, false, false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Record_EmptyAnswer_Throws(string a)
    {
        Assert.Throws<ArgumentException>(() =>
            McpInteraction.Record(_tenant, _user, "pregunta", a, McpRiskLevel.Low, false, false));
    }

    // ── RecordFailed ──────────────────────────────────────────────────────────

    [Fact]
    public void RecordFailed_SetsFailed_AndHighRisk()
    {
        var i = McpInteraction.RecordFailed(_tenant, _user, "¿Pregunta?");
        Assert.Equal(McpInteractionStatus.Failed, i.Status);
        Assert.Equal(McpRiskLevel.High, i.RiskLevel);
        Assert.True(i.RequiresHumanReview);
        Assert.Equal(string.Empty, i.Answer);
    }

    // ── RequestHumanReview ────────────────────────────────────────────────────

    [Fact]
    public void RequestHumanReview_FromCompleted_SetsReviewRequested()
    {
        var i = Build();
        i.RequestHumanReview();
        Assert.Equal(McpInteractionStatus.ReviewRequested, i.Status);
        Assert.True(i.RequiresHumanReview);
    }

    [Fact]
    public void RequestHumanReview_AlreadyRequested_IsIdempotent()
    {
        var i = Build(requiresReview: true);
        i.RequestHumanReview(); // no lanza
        Assert.Equal(McpInteractionStatus.ReviewRequested, i.Status);
    }

    [Fact]
    public void RequestHumanReview_Failed_Throws()
    {
        var i = McpInteraction.RecordFailed(_tenant, _user, "pregunta");
        Assert.Throws<InvalidOperationException>(() => i.RequestHumanReview());
    }

    // ── AddCitation ───────────────────────────────────────────────────────────

    [Fact]
    public void AddCitation_AddsToCitations()
    {
        var i = Build();
        var c = i.AddCitation(McpCitationSourceType.LegalNorm, "ley-21719", "Art. 16 — Principio de finalidad.");
        Assert.Single(i.Citations);
        Assert.Equal(i.Id, c.InteractionId);
        Assert.Equal("ley-21719", c.SourceId);
        Assert.Equal(McpCitationSourceType.LegalNorm, c.SourceType);
    }

    [Fact]
    public void AddCitation_MultipleAllowed()
    {
        var i = Build();
        i.AddCitation(McpCitationSourceType.LegalNorm, "src1", "frag1");
        i.AddCitation(McpCitationSourceType.TenantDocument, "doc-42", "frag2", sourceVersion: "v3");
        Assert.Equal(2, i.Citations.Count);
    }

    [Fact]
    public void AddCitation_EmptySourceId_Throws()
    {
        var i = Build();
        Assert.Throws<ArgumentException>(() =>
            i.AddCitation(McpCitationSourceType.Other, "", "fragmento"));
    }
}
