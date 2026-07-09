using Evidata.ServiceDefaults;
using System.Diagnostics.Metrics;

namespace Evidata.Tests.Unit.Observability;

/// <summary>
/// Tests para EvidataMetrics — verifica que los instrumentos OTel se registran correctamente.
/// </summary>
public class EvidataMetricsTests : IDisposable
{
    private readonly MeterFactory _factory;
    private readonly EvidataMetrics _metrics;

    public EvidataMetricsTests()
    {
        _factory = new MeterFactory();
        _metrics = new EvidataMetrics(_factory);
    }

    public void Dispose()
    {
        _metrics.Dispose();
        _factory.Dispose();
    }

    // ── Meter registrado ──────────────────────────────────────────────────────

    [Fact]
    public void MeterName_IsEvidataConvention()
    {
        Assert.Equal("Evidata", EvidataMetrics.MeterName);
    }

    // ── MCP ───────────────────────────────────────────────────────────────────

    [Fact]
    public void McpQueryRecorded_LowRisk_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.McpQueryRecorded(Guid.NewGuid(), "Low"));
        Assert.Null(ex);
    }

    [Fact]
    public void McpQueryRecorded_HighRisk_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.McpQueryRecorded(Guid.NewGuid(), "High"));
        Assert.Null(ex);
    }

    [Fact]
    public void McpErrorRecorded_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.McpErrorRecorded(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void McpHitlTaskCreated_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.McpHitlTaskCreated(Guid.NewGuid()));
        Assert.Null(ex);
    }

    // ── Documentos ────────────────────────────────────────────────────────────

    [Fact]
    public void DocumentProcessed_DoesNotThrow()
    {
        var ex = Record.Exception(() => _metrics.DocumentProcessed(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void DocumentIndexed_DoesNotThrow()
    {
        var ex = Record.Exception(() => _metrics.DocumentIndexed(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void DocumentDownloaded_DoesNotThrow()
    {
        var ex = Record.Exception(() => _metrics.DocumentDownloaded(Guid.NewGuid()));
        Assert.Null(ex);
    }

    // ── Reporting ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("RAT")]
    [InlineData("Gaps")]
    public void ReportGenerated_KnownTypes_DoesNotThrow(string reportType)
    {
        var ex = Record.Exception(() =>
            _metrics.ReportGenerated(Guid.NewGuid(), reportType));
        Assert.Null(ex);
    }

    // ── RAT ───────────────────────────────────────────────────────────────────

    [Fact]
    public void RatApproved_DoesNotThrow()
    {
        var ex = Record.Exception(() => _metrics.RatApproved(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void RatIndexed_DoesNotThrow()
    {
        var ex = Record.Exception(() => _metrics.RatIndexed(Guid.NewGuid()));
        Assert.Null(ex);
    }

    // ── Seguridad ─────────────────────────────────────────────────────────────

    [Fact]
    public void AuthorizationFailure_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.AuthorizationFailure(Guid.NewGuid()));
        Assert.Null(ex);
    }

    [Fact]
    public void CrossTenantAttempt_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            _metrics.CrossTenantAttempt(Guid.NewGuid()));
        Assert.Null(ex);
    }
}

/// <summary>Implementación mínima de IMeterFactory para tests sin DI.</summary>
internal sealed class MeterFactory : IMeterFactory
{
    private readonly List<Meter> _meters = new();

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options);
        _meters.Add(meter);
        return meter;
    }

    public void Dispose() => _meters.ForEach(m => m.Dispose());
}
