using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Audit.Api;

[ApiController]
[Authorize]
[Route("api/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditLogRepository _repository;
    private readonly ICurrentUserContext _currentUser;

    public AuditController(IAuditLogRepository repository, ICurrentUserContext currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    [HttpGet("tenant/{tenantId:guid}")]
    public async Task<IActionResult> GetByTenant(
        Guid tenantId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (tenantId != _currentUser.TenantId)
            return Forbid();

        var logs = await _repository.GetByTenantAsync(tenantId, page, pageSize, ct);
        return Ok(logs.Select(MapToDto));
    }

    [HttpGet("tenant/{tenantId:guid}/resource/{resource}/{resourceId:guid}")]
    public async Task<IActionResult> GetByResource(
        Guid tenantId, string resource, Guid resourceId, CancellationToken ct)
    {
        if (tenantId != _currentUser.TenantId)
            return Forbid();

        var logs = await _repository.GetByResourceAsync(tenantId, resource, resourceId, ct);
        return Ok(logs.Select(MapToDto));
    }

    private static AuditLogDto MapToDto(AuditLog x) =>
        new(x.Id, x.TenantId, x.UserId, x.Action, x.Resource, x.ResourceId, x.Details, x.IpAddress, x.OccurredAt, x.Severity);
}
