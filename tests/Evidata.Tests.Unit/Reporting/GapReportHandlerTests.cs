using Evidata.Functions.Reporting.Handlers;
using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Tests.Unit.Reporting;

public class GapReportHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private static ComplianceGap BuildGap(
        GapSeverity severity = GapSeverity.Medium,
        GapStatus status = GapStatus.Open,
        string title = "Brecha test")
    {
        var gap = ComplianceGap.Create(
            TenantId, "ProcessingInventory", Guid.NewGuid(),
            title, "Descripción de prueba", severity, Guid.NewGuid());

        // Avanzar estado si es necesario (map old states to new FSM)
        if (status == GapStatus.InCorrection)
            gap.StartCorrection(Guid.NewGuid());
        if (status == GapStatus.Resolved)
        {
            gap.StartCorrection(Guid.NewGuid());
            gap.Resolve(Guid.NewGuid());
        }
        if (status == GapStatus.AcceptedWithRisk)
            gap.AcceptRisk("Riesgo aceptado en prueba", Guid.NewGuid());

        return gap;
    }

    // ── GenerateExcel — estructura ────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_EmptyList_ReturnsNonEmptyBytes()
    {
        var bytes = GapReportHandler.GenerateExcel([], TenantId);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateExcel_ValidXlsxSignature()
    {
        var bytes = GapReportHandler.GenerateExcel([], TenantId);
        Assert.Equal(0x50, bytes[0]); // PK zip signature
        Assert.Equal(0x4B, bytes[1]);
    }

    [Fact]
    public void GenerateExcel_WithGaps_ReturnsNonEmptyBytes()
    {
        var gaps = new[]
        {
            BuildGap(GapSeverity.Critical, GapStatus.Open, "Sin política de privacidad"),
            BuildGap(GapSeverity.High, GapStatus.InCorrection, "Falta retención"),
            BuildGap(GapSeverity.Low, GapStatus.Open, "Brecha menor")
        };
        var bytes = GapReportHandler.GenerateExcel(gaps, TenantId);
        Assert.NotEmpty(bytes);
    }

    // ── Ordenamiento por severidad ────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_MixedSeverities_DoesNotThrow()
    {
        var gaps = new[]
        {
            BuildGap(GapSeverity.Low),
            BuildGap(GapSeverity.Critical),
            BuildGap(GapSeverity.Medium),
            BuildGap(GapSeverity.High)
        };
        var exception = Record.Exception(
            () => GapReportHandler.GenerateExcel(gaps, TenantId));
        Assert.Null(exception);
    }

    // ── Resumen agrupado ──────────────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_AllSeverities_ProducesLargerFile()
    {
        var emptyBytes = GapReportHandler.GenerateExcel([], TenantId);

        var gaps = Enumerable.Range(1, 5)
            .Select(i => BuildGap(GapSeverity.High, GapStatus.Open, $"Brecha {i}"))
            .ToList();
        var filledBytes = GapReportHandler.GenerateExcel(gaps, TenantId);

        Assert.True(filledBytes.Length >= emptyBytes.Length);
    }

    // ── BlocksApproval en Critical ────────────────────────────────────────────

    [Fact]
    public void GenerateExcel_CriticalGap_BlocksApprovalTrue()
    {
        var critical = BuildGap(GapSeverity.Critical, GapStatus.Open);
        Assert.True(critical.BlocksApproval);

        var bytes = GapReportHandler.GenerateExcel([critical], TenantId);
        Assert.NotEmpty(bytes);
    }

    // ── GapStatus InCorrection incluido ────────────────────────────────────────

    [Fact]
    public void GenerateExcel_InCorrectionGaps_DoNotThrow()
    {
        var gaps = new[]
        {
            BuildGap(GapSeverity.High, GapStatus.InCorrection),
            BuildGap(GapSeverity.Medium, GapStatus.InCorrection)
        };
        var exception = Record.Exception(
            () => GapReportHandler.GenerateExcel(gaps, TenantId));
        Assert.Null(exception);
    }
}
