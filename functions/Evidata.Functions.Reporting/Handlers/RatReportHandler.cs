using ClosedXML.Excel;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Reporting.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Handlers;

/// <summary>
/// Genera el Registro de Actividades de Tratamiento (RAT) como Excel
/// y lo sube a Azure Blob Storage (Azurite en local).
///
/// Path del blob: {tenantId}/reports/{yyyy/MM/dd}/{jobId}_rat.xlsx
/// </summary>
public class RatReportHandler
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    private readonly ProcessingInventoryDbContext _rat;
    private readonly IReportJobService _jobs;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<RatReportHandler> _logger;

    public RatReportHandler(
        ProcessingInventoryDbContext rat,
        IReportJobService jobs,
        IBlobStorageService blobStorage,
        ILogger<RatReportHandler> logger)
    {
        _rat         = rat;
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
        _logger.LogInformation("🔄 Generando RAT Excel para tenant {TenantId} job {JobId}", tenantId, jobId);

        try
        {
            var activities = await _rat.ProcessingActivities
                .AsNoTracking()
                .Where(a => a.TenantId == tenantId)
                .ToListAsync(ct);

            var excelBytes = GenerateExcel(activities, tenantId);

            var blobPath = $"{tenantId}/reports/{DateTimeOffset.UtcNow:yyyy/MM/dd}/{jobId}_rat.xlsx";
            await _blobStorage.UploadAsync(
                blobPath, excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ct);

            // ArtifactDocumentId se usará para enlazar con el Document entry en el módulo Documents
            var artifactId = Guid.NewGuid();

            _logger.LogInformation(
                "✅ RAT Excel generado y subido: {Rows} tratamientos, {Bytes} bytes, BlobPath={BlobPath}",
                activities.Count, excelBytes.Length, blobPath);

            await _jobs.CompleteAsync(jobId, artifactId, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generando RAT Excel para job {JobId}", jobId);
            await _jobs.FailAsync(jobId, ex.Message, ct);
            throw;
        }
    }

    // ── Generación Excel ──────────────────────────────────────────────────────

    public static byte[] GenerateExcel(IReadOnlyList<ProcessingActivity> activities, Guid tenantId)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("RAT");

        // Cabeceras
        var headers = new[]
        {
            "Id", "Nombre", "Departamento", "Responsable", "Base Legal", "Versión",
            "Estado", "Aprobado el", "Transfer. Internacional", "Decisión Automatizada",
            "Datos Sensibles", "Brecha Crítica Abierta"
        };

        for (var i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            cell.Style.Font.FontColor = XLColor.White;
        }

        // Datos
        for (var r = 0; r < activities.Count; r++)
        {
            var a = activities[r];
            var row = r + 2;

            ws.Cell(row, 1).Value = a.Id.ToString();
            ws.Cell(row, 2).Value = a.Name;
            ws.Cell(row, 3).Value = a.Department ?? string.Empty;
            ws.Cell(row, 4).Value = a.Controller ?? string.Empty;
            ws.Cell(row, 5).Value = a.Purpose?.LegalBasis.ToString() ?? string.Empty;
            ws.Cell(row, 6).Value = a.Version;
            ws.Cell(row, 7).Value = a.Status.ToString();
            ws.Cell(row, 8).Value = a.ApprovedAt.HasValue
                ? a.ApprovedAt.Value.ToString("yyyy-MM-dd")
                : string.Empty;
            ws.Cell(row, 9).Value = a.HasInternationalTransfer ? "Sí" : "No";
            ws.Cell(row, 10).Value = a.HasAutomatedDecision ? "Sí" : "No";
            ws.Cell(row, 11).Value = a.Flags.SensitiveData ? "Sí" : "No";
            ws.Cell(row, 12).Value = a.Flags.CriticalGapOpen ? "Sí" : "No";
        }

        ws.Columns().AdjustToContents();

        // Metadata sheet
        var meta = workbook.Worksheets.Add("Metadata");
        meta.Cell(1, 1).Value = "TenantId";
        meta.Cell(1, 2).Value = tenantId.ToString();
        meta.Cell(2, 1).Value = "Generado el";
        meta.Cell(2, 2).Value = DateTimeOffset.UtcNow.ToString("O");
        meta.Cell(3, 1).Value = "Total tratamientos";
        meta.Cell(3, 2).Value = activities.Count;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
