using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.AutoDetection;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Evidata.Tests.Unit.GapManagement;

public class RatGapDetectionTests : IDisposable
{
    private readonly GapManagementDbContext _gapDb;
    private readonly IRatFlagsProvider _flagsProvider;
    private readonly RatGapDetectionService _svc;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _activityId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public RatGapDetectionTests()
    {
        var opts = new DbContextOptionsBuilder<GapManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _gapDb = new GapManagementDbContext(opts);
        _flagsProvider = Substitute.For<IRatFlagsProvider>();
        _svc = new RatGapDetectionService(_flagsProvider, _gapDb);
    }

    public void Dispose() => _gapDb.Dispose();

    private RatFlagsSnapshot Flags(
        bool missingMeasures = false, bool missingEvidence = false,
        bool missingRetention = false, bool children = false,
        bool biometric = false, bool international = false, bool automated = false) =>
        new(_activityId, _tenantId, "Tratamiento Test",
            missingMeasures, missingEvidence, missingRetention,
            children, biometric, international, automated);

    private void SetupFlags(RatFlagsSnapshot f) =>
        _flagsProvider.GetFlagsAsync(_tenantId, _activityId, Arg.Any<CancellationToken>())
            .Returns(f);

    // ── No flags → sin brechas ────────────────────────────────────────────────

    [Fact]
    public async Task NoFlags_CreatesNoGaps()
    {
        SetupFlags(Flags());
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Empty(result.CreatedGaps);
    }

    // ── MissingSecurityMeasures → Critical ────────────────────────────────────

    [Fact]
    public async Task MissingSecurityMeasures_CreatesCriticalGap()
    {
        SetupFlags(Flags(missingMeasures: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Single(result.CreatedGaps);
        Assert.Equal(GapSeverity.Critical, result.CreatedGaps[0].Severity);
        Assert.Contains("medidas de seguridad", result.CreatedGaps[0].Title);
    }

    // ── MissingLegalBasisEvidence → High ──────────────────────────────────────

    [Fact]
    public async Task MissingLegalBasisEvidence_CreatesHighGap()
    {
        SetupFlags(Flags(missingEvidence: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Severity == GapSeverity.High && g.Title.Contains("licitud"));
    }

    // ── MissingRetention → Medium ─────────────────────────────────────────────

    [Fact]
    public async Task MissingRetention_CreatesMediumGap()
    {
        SetupFlags(Flags(missingRetention: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Severity == GapSeverity.Medium && g.Title.Contains("retención"));
    }

    // ── ChildrenData → High ───────────────────────────────────────────────────

    [Fact]
    public async Task ChildrenData_CreatesHighGap()
    {
        SetupFlags(Flags(children: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Severity == GapSeverity.High && g.Title.Contains("NNA"));
    }

    // ── BiometricData → High ──────────────────────────────────────────────────

    [Fact]
    public async Task BiometricData_CreatesHighGap()
    {
        SetupFlags(Flags(biometric: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Severity == GapSeverity.High && g.Title.Contains("biométricos"));
    }

    // ── InternationalTransfer → Medium ────────────────────────────────────────

    [Fact]
    public async Task InternationalTransfer_CreatesMediumGap()
    {
        SetupFlags(Flags(international: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Title.Contains("internacional"));
    }

    // ── AutomatedDecision → Medium ────────────────────────────────────────────

    [Fact]
    public async Task AutomatedDecision_CreatesMediumGap()
    {
        SetupFlags(Flags(automated: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Contains(result.CreatedGaps, g => g.Title.Contains("automatizada"));
    }

    // ── Deduplicación ─────────────────────────────────────────────────────────

    [Fact]
    public async Task RunTwice_SecondRunSkipsExisting()
    {
        SetupFlags(Flags(missingMeasures: true));
        var first = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        var second = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Single(first.CreatedGaps);
        Assert.Empty(second.CreatedGaps);
        Assert.Single(second.SkippedFlags);
    }

    // ── Multiple flags ────────────────────────────────────────────────────────

    [Fact]
    public async Task MultipleFlags_CreatesMultipleGaps()
    {
        SetupFlags(Flags(missingMeasures: true, missingEvidence: true, missingRetention: true));
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Equal(3, result.CreatedGaps.Count);
    }

    // ── FlagsProvider lanza excepción → se propaga ────────────────────────────

    [Fact]
    public async Task UnknownActivity_Throws()
    {
        _flagsProvider.GetFlagsAsync(_tenantId, _activityId, Arg.Any<CancellationToken>())
            .Returns<RatFlagsSnapshot>(_ => throw new InvalidOperationException("No encontrado."));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId));
    }

    // ── GapStatus.Closed no bloquea dedup (se puede re-crear) ────────────────

    [Fact]
    public async Task ClosedGap_AllowsRecreation()
    {
        SetupFlags(Flags(missingMeasures: true));

        // Crear y cerrar la brecha manualmente
        var existing = ComplianceGap.Create(_tenantId, "ProcessingInventory", _activityId,
            "Faltan medidas de seguridad para datos sensibles", "desc", GapSeverity.Critical, _userId);
        existing.Assign(Guid.NewGuid(), _userId);
        existing.StartProgress(_userId);
        existing.Resolve(_userId);
        existing.Close(_userId);
        _gapDb.ComplianceGaps.Add(existing);
        await _gapDb.SaveChangesAsync();

        // La brecha cerrada no cuenta para dedup → debe crear una nueva
        var result = await _svc.DetectAndCreateGapsAsync(_tenantId, _activityId, _userId);
        Assert.Single(result.CreatedGaps);
    }
}
