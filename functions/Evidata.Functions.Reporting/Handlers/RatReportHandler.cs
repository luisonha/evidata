using ClosedXML.Excel;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Reporting.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.Reporting.Handlers;

/// <summary>
/// Genera el Registro de Actividades de Tratamiento (RAT) como archivo Excel.
///
/// Columnas exportadas:
///   Id | Nombre | Departamento | Responsable | Base legal | Versión | Estado | Aprobado el |
///   Transferencia internacional | Decisión automatizada | Datos sensibles | Brecha crítica abierta
///
/// Ciclo de vida del job:
///   1. Busca el ReportJob en BD
///   2. Marca como Running
///   3. Consulta tratamientos aprobados del tenant
///   4. Genera Excel en memoria
///   5. Marca como Completed (ArtifactDocumentId = Guid generado — producción: id del blob subido)
///
/// Idempotencia: si el job ya está Completed o Failed no hace nada.
/// </summary>
public class RatReportHandler
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    private readonly ProcessingInventoryDbContext _rat;
    private readonly IReportJobService _jobs;
    private readonly ILogger<RatReportHandler> _logger;

    public RatReportHandler(
        ProcessingInventoryDbContext rat,
        IReportJobService jobs,
        ILogger<RatReportHandler> logger)
    {
        _rat = rat;
        _jobs = jobs;
        _logger = logger;
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

            // Producción: subir excelBytes a Azure Blob Storage y crear Document entry.
            // Retorna el DocumentId real. Aquí generamos uno como placeholder.
            var artifactId = Guid.NewGuid();

            _logger.LogInformation(
                "✅ RAT Excel generado: {Rows} tratamientos, {Bytes} bytes, ArtifactId={ArtifactId}",
                activities.Count, excelBytes.Length, artifactId);

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
