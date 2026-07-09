using Evidata.Modules.Mcp.Application.CitationVerification;
using Evidata.Modules.Mcp.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Mcp;

public class McpCitationVerifierTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid InteractionId = Guid.NewGuid();

    private static McpCitation BuildCitation(McpCitationSourceType sourceType, string? sourceId = null)
    {
        var id = sourceId ?? Guid.NewGuid().ToString();
        return McpCitation.Create(InteractionId, sourceType, id, "fragment de prueba");
    }

    // ── CitationVerificationReport — propiedades derivadas ────────────────────

    [Fact]
    public void Report_AllVerified_IsFullyVerified()
    {
        var results = new List<CitationVerificationResult>
        {
            new(Guid.NewGuid(), McpCitationSourceType.LegalNorm, "id1", CitationVerificationStatus.Verified, null),
            new(Guid.NewGuid(), McpCitationSourceType.ProcessingActivity, "id2", CitationVerificationStatus.Verified, null),
        };
        var report = new CitationVerificationReport(InteractionId, results);

        Assert.Equal(2, report.VerifiedCount);
        Assert.Equal(0, report.NotFoundCount);
        Assert.True(report.IsFullyVerified);
    }

    [Fact]
    public void Report_OneNotFound_IsNotFullyVerified()
    {
        var results = new List<CitationVerificationResult>
        {
            new(Guid.NewGuid(), McpCitationSourceType.LegalNorm, "id1", CitationVerificationStatus.Verified, null),
            new(Guid.NewGuid(), McpCitationSourceType.TenantDocument, "id2", CitationVerificationStatus.NotFound, "No encontrado"),
        };
        var report = new CitationVerificationReport(InteractionId, results);

        Assert.Equal(1, report.NotFoundCount);
        Assert.False(report.IsFullyVerified);
    }

    [Fact]
    public void Report_Unverifiable_DoesNotAffectIsFullyVerified()
    {
        var results = new List<CitationVerificationResult>
        {
            new(Guid.NewGuid(), McpCitationSourceType.Other, "x", CitationVerificationStatus.Unverifiable,
                "Tipo Other no verificable"),
        };
        var report = new CitationVerificationReport(InteractionId, results);

        Assert.Equal(0, report.NotFoundCount);
        Assert.Equal(1, report.UnverifiableCount);
        Assert.True(report.IsFullyVerified); // unverifiable no bloquea
    }

    [Fact]
    public void Report_MixedResults_CountsAreCorrect()
    {
        var results = new List<CitationVerificationResult>
        {
            new(Guid.NewGuid(), McpCitationSourceType.LegalNorm, "a", CitationVerificationStatus.Verified, null),
            new(Guid.NewGuid(), McpCitationSourceType.TenantDocument, "b", CitationVerificationStatus.NotFound, "x"),
            new(Guid.NewGuid(), McpCitationSourceType.Other, "c", CitationVerificationStatus.Unverifiable, "y"),
        };
        var report = new CitationVerificationReport(InteractionId, results);

        Assert.Equal(1, report.VerifiedCount);
        Assert.Equal(1, report.NotFoundCount);
        Assert.Equal(1, report.UnverifiableCount);
        Assert.False(report.IsFullyVerified);
    }

    [Fact]
    public void Report_Empty_IsFullyVerified()
    {
        var report = new CitationVerificationReport(InteractionId, []);
        Assert.True(report.IsFullyVerified);
        Assert.Equal(0, report.VerifiedCount);
    }

    // ── Contract via mock ─────────────────────────────────────────────────────

    [Fact]
    public async Task Verifier_NoCitations_ReturnsEmptyReport()
    {
        var verifier = Substitute.For<IMcpCitationVerifier>();
        var empty = new CitationVerificationReport(InteractionId, []);
        verifier.VerifyAsync(InteractionId, TenantId, Arg.Any<IReadOnlyList<McpCitation>>(),
            Arg.Any<CancellationToken>()).Returns(empty);

        var result = await verifier.VerifyAsync(InteractionId, TenantId, []);
        Assert.True(result.IsFullyVerified);
        Assert.Equal(0, result.VerifiedCount);
    }

    [Fact]
    public async Task Verifier_MultipleTypes_ReturnsMockedReport()
    {
        var verifier = Substitute.For<IMcpCitationVerifier>();
        var citations = new List<McpCitation>
        {
            BuildCitation(McpCitationSourceType.LegalNorm),
            BuildCitation(McpCitationSourceType.TenantDocument),
            BuildCitation(McpCitationSourceType.Other)
        };

        var results = citations.Select((c, i) => new CitationVerificationResult(
            c.Id, c.SourceType, c.SourceId,
            i == 1 ? CitationVerificationStatus.NotFound : CitationVerificationStatus.Verified,
            i == 1 ? "No encontrado" : null)).ToList();

        var report = new CitationVerificationReport(InteractionId, results);
        verifier.VerifyAsync(InteractionId, TenantId, Arg.Any<IReadOnlyList<McpCitation>>(),
            Arg.Any<CancellationToken>()).Returns(report);

        var r = await verifier.VerifyAsync(InteractionId, TenantId, citations);
        Assert.Equal(3, r.Results.Count);
        Assert.False(r.IsFullyVerified);
    }

    // ── CitationVerificationResult record ────────────────────────────────────

    [Fact]
    public void VerificationResult_Verified_HasNullReason()
    {
        var r = new CitationVerificationResult(
            Guid.NewGuid(), McpCitationSourceType.ProcessingActivity,
            Guid.NewGuid().ToString(), CitationVerificationStatus.Verified, null);

        Assert.Equal(CitationVerificationStatus.Verified, r.Status);
        Assert.Null(r.Reason);
    }

    [Fact]
    public void VerificationResult_NotFound_HasReason()
    {
        var r = new CitationVerificationResult(
            Guid.NewGuid(), McpCitationSourceType.ComplianceGap,
            Guid.NewGuid().ToString(), CitationVerificationStatus.NotFound,
            "Brecha no encontrada.");

        Assert.Equal(CitationVerificationStatus.NotFound, r.Status);
        Assert.NotNull(r.Reason);
    }
}
