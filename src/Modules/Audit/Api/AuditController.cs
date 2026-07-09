using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Audit.Api;

[ApiController]
[Route("api/audit")]
[Route("api/v1/audit-events")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _repository;

    public AuditController(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var logs = await _repository.GetByTenantAsync(tenantId, page, pageSize, ct);
        return Ok(logs.Select(MapToDto));
    }

    [HttpGet("tenant/{tenantId:guid}/resource/{resource}/{resourceId:guid}")]
    public async Task<IActionResult> GetByResource(
        Guid tenantId, string resource, Guid resourceId, CancellationToken ct)
    {
        var logs = await _repository.GetByResourceAsync(tenantId, resource, resourceId, ct);
        return Ok(logs.Select(MapToDto));
    }

    private static AuditLogDto MapToDto(AuditLog x) =>
        new(x.Id, x.TenantId, x.UserId, x.Action, x.Resource, x.ResourceId, x.Details, x.IpAddress, x.OccurredAt, x.Severity);
}
