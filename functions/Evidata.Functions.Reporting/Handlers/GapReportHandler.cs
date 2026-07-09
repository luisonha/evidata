using ClosedXML.Excel;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Evidata.Modules.Reporting.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Handlers;

/// <summary>
/// Genera el reporte de brechas de cumplimiento como Excel y lo sube a Blob Storage.
///
/// Path del blob: {tenantId}/reports/{yyyy/MM/dd}/{jobId}_gaps.xlsx
/// </summary>
public class GapReportHandler
{
    private readonly GapManagementDbContext _gaps;
    private readonly IReportJobService _jobs;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<GapReportHandler> _logger;

    public GapReportHandler(
        GapManagementDbContext gaps,
        IReportJobService jobs,
        IBlobStorageService blobStorage,
        ILogger<GapReportHandler> logger)
    {
        _gaps        = gaps;
        _jobs        = jobs;
        _blobStorage = blobStorage;
        _logger      = logger;
    }

    public async Task HandleAsync(Guid jobId, Guid tenantId, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(jobId, ct);
        if (job is null)
        {
            _logger.LogWarning("⚠ ReportJob {JobId} no encontrado — mensaje descartado", jobId);
            return;
        }

        if (job.IsTerminal)
        {
            _logger.LogInformation(
                "ReportJob {JobId} ya en estado terminal {Status} — omitiendo (idempotente)",
                jobId, job.Status);
            return;
        }

        await _jobs.StartAsync(jobId, ct);
        _logger.LogInformation("🔄 Generando Gaps Excel para tenant {TenantId} job {JobId}", tenantId, jobId);

        try
        {
            var gaps = await _gaps.ComplianceGaps
                .AsNoTracking()
                .Where(g => g.TenantId == tenantId && g.Status != GapStatus.Closed)
                .ToListAsync(ct);

            var excelBytes = GenerateExcel(gaps, tenantId);

            var blobPath = $"{tenantId}/reports/{DateTimeOffset.UtcNow:yyyy/MM/dd}/{jobId}_gaps.xlsx";
            await _blobStorage.UploadAsync(
                blobPath, excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ct);

            var artifactId = Guid.NewGuid();

            _logger.LogInformation(
                "✅ Gaps Excel generado y subido: {Rows} brechas, {Bytes} bytes, BlobPath={BlobPath}",
                gaps.Count, excelBytes.Length, blobPath);

            await _jobs.CompleteAsync(jobId, artifactId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generando Gaps Excel para job {JobId}", jobId);
            await _jobs.FailAsync(jobId, ex.Message, ct);
            throw;
        }
    }

    // ── Generación Excel ──────────────────────────────────────────────────────

    public static byte[] GenerateExcel(IReadOnlyList<ComplianceGap> gaps, Guid tenantId)
    {
        using var workbook = new XLWorkbook();

        BuildSummarySheet(workbook, gaps);
        BuildDetailSheet(workbook, gaps);
        BuildMetadataSheet(workbook, gaps, tenantId);

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static void BuildSummarySheet(XLWorkbook wb, IReadOnlyList<ComplianceGap> gaps)
    {
        var ws = wb.Worksheets.Add("Resumen");

        // Cabeceras
        var headers = new[] { "Severidad", "Total", "Abierto", "En Progreso", "Bloqueado", "Resuelto", "Riesgo Aceptado" };
        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
        }

        var severities = new[] { GapSeverity.Critical, GapSeverity.High, GapSeverity.Medium, GapSeverity.Low };
        for (var r = 0; r < severities.Length; r++)
        {
            var sev = severities[r];
            var subset = gaps.Where(g => g.Severity == sev).ToList();
            var row = r + 2;

            ws.Cell(row, 1).Value = sev.ToString();
            ws.Cell(row, 2).Value = subset.Count;
            ws.Cell(row, 3).Value = subset.Count(g => g.Status == GapStatus.Open);
            ws.Cell(row, 4).Value = subset.Count(g => g.Status == GapStatus.InProgress);
            ws.Cell(row, 5).Value = subset.Count(g => g.Status == GapStatus.Blocked);
            ws.Cell(row, 6).Value = subset.Count(g => g.Status == GapStatus.Resolved);
            ws.Cell(row, 7).Value = subset.Count(g => g.Status == GapStatus.AcceptedRisk);

            if (sev == GapSeverity.Critical)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
            else if (sev == GapSeverity.High)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF0CC");
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildDetailSheet(XLWorkbook wb, IReadOnlyList<ComplianceGap> gaps)
    {
        var ws = wb.Worksheets.Add("Brechas");

        var headers = new[]
        {
            "Id", "Título", "Severidad", "Estado", "Módulo Origen", "EntityId Origen",
            "Propietario", "Vence el", "Bloquea Aprobación", "Creado el", "Cerrado el"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Orden: Critical primero, luego High, Medium, Low
        var ordered = gaps
            .OrderBy(g => g.Severity switch
            {
                GapSeverity.Critical => 0,
                GapSeverity.High => 1,
                GapSeverity.Medium => 2,
                GapSeverity.Low => 3,
                _ => 4
            })
            .ThenBy(g => g.Status.ToString())
            .ToList();

        for (var r = 0; r < ordered.Count; r++)
        {
            var g = ordered[r];
            var row = r + 2;

            ws.Cell(row, 1).Value = g.Id.ToString();
            ws.Cell(row, 2).Value = g.Title;
            ws.Cell(row, 3).Value = g.Severity.ToString();
            ws.Cell(row, 4).Value = g.Status.ToString();
            ws.Cell(row, 5).Value = g.SourceModule;
            ws.Cell(row, 6).Value = g.SourceEntityId.ToString();
            ws.Cell(row, 7).Value = g.OwnerId.HasValue ? g.OwnerId.Value.ToString() : string.Empty;
            ws.Cell(row, 8).Value = g.DueAt.HasValue ? g.DueAt.Value.ToString("yyyy-MM-dd") : string.Empty;
            ws.Cell(row, 9).Value = g.BlocksApproval ? "Sí" : "No";
            ws.Cell(row, 10).Value = g.CreatedAt.ToString("yyyy-MM-dd");
            ws.Cell(row, 11).Value = g.ClosedAt.HasValue ? g.ClosedAt.Value.ToString("yyyy-MM-dd") : string.Empty;

            // Resaltar filas críticas
            if (g.Severity == GapSeverity.Critical)
                ws.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml("#FFE0E0");
        }

        ws.Columns().AdjustToContents();
    }

    private static void BuildMetadataSheet(XLWorkbook wb, IReadOnlyList<ComplianceGap> gaps, Guid tenantId)
    {
        var ws = wb.Worksheets.Add("Metadata");
        ws.Cell(1, 1).Value = "TenantId";
        ws.Cell(1, 2).Value = tenantId.ToString();
        ws.Cell(2, 1).Value = "Generado el";
        ws.Cell(2, 2).Value = DateTimeOffset.UtcNow.ToString("O");
        ws.Cell(3, 1).Value = "Total brechas";
        ws.Cell(3, 2).Value = gaps.Count;
        ws.Cell(4, 1).Value = "Brechas críticas";
        ws.Cell(4, 2).Value = gaps.Count(g => g.Severity == GapSeverity.Critical);
        ws.Cell(5, 1).Value = "Brechas high";
        ws.Cell(5, 2).Value = gaps.Count(g => g.Severity == GapSeverity.High);
        ws.Cell(6, 1).Value = "Bloquean aprobación";
        ws.Cell(6, 2).Value = gaps.Count(g => g.BlocksApproval);
    }
}
