using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Domain;
using Microsoft.AspNetCore.Mvc;
using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Reporting.Api;

[ApiController]
[Route("api/reports")]
public class ReportsController(
    IReportJobService reportService,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ReportJobDto>>> List(CancellationToken ct)
    {
        var items = await reportService.GetByTenantAsync(currentUser.TenantId, ct: ct);
        return Ok(items.Select(ReportJobDto.From).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportJobDto>> GetById(Guid id, CancellationToken ct)
    {
        var item = await reportService.GetByIdAsync(id, ct);
        if (item is null || item.TenantId != currentUser.TenantId) return NotFound();
        return Ok(ReportJobDto.From(item));
    }

    [HttpPost]
    public async Task<ActionResult<ReportJobDto>> Create(
        [FromBody] CreateReportRequest req, CancellationToken ct)
    {
        if (!Enum.TryParse<ReportType>(req.ReportType, ignoreCase: true, out var reportType))
            return BadRequest($"ReportType inválido: {req.ReportType}. Valores: {string.Join(", ", Enum.GetNames<ReportType>())}");

        var job = await reportService.RequestAsync(
            currentUser.TenantId, reportType, req.Parameters ?? "{}",
            currentUser.UserId, ct);

        return CreatedAtAction(nameof(GetById), new { id = job.Id }, ReportJobDto.From(job));
    }
}

public record CreateReportRequest(string ReportType, string? Parameters = null);

public sealed record ReportJobDto(
    Guid Id, Guid TenantId, string ReportType, string Status,
    string Parameters, Guid RequestedBy, DateTimeOffset RequestedAt,
    DateTimeOffset? CompletedAt, Guid? ArtifactDocumentId, string? ErrorMessage)
{
    public static ReportJobDto From(ReportJob j) => new(
        j.Id, j.TenantId, j.ReportType.ToString(), j.Status.ToString(),
        j.Parameters, j.RequestedBy, j.RequestedAt,
        j.CompletedAt, j.ArtifactDocumentId, j.ErrorMessage);
}
