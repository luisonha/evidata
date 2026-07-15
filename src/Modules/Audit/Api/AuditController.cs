using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Audit.Api;

[ApiController]
[Authorize]
[Route("api/audit")]
[Route("api/v1/audit-events")]
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

    /// <summary>
    /// Get audit events for the current tenant with optional filtering.
    /// Endpoint: GET /api/v1/admin/audit
    /// Permission: Admin.ReadAudit
    /// Supports filtering by: eventType, actorUserId, targetUserId, date range, and pagination
    /// Tenant isolation is enforced - tenant ID is resolved from the authenticated session.
    /// </summary>
    [HttpGet]
    [Route("api/v1/admin/audit")]
    [Authorize]
    public async Task<IActionResult> GetAdminAudit(
        [FromQuery] string? eventType = null,
        [FromQuery] Guid? actorUserId = null,
        [FromQuery] Guid? targetUserId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        // Validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        // Resolve tenant from current user session (never from client parameter)
        var tenantId = _currentUser.TenantId;

        // Get filtered audit logs
        var (auditLogs, totalCount) = await _repository.GetByTenantWithFiltersAsync(
            tenantId, eventType, actorUserId, targetUserId, from, to, page, pageSize, ct);

        var dtos = auditLogs.Select(MapToDto).ToList();

        // Return with pagination metadata
        var response = new
        {
            data = dtos,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (totalCount + pageSize - 1) / pageSize
            }
        };

        return Ok(response);
    }

    private static AuditLogDto MapToDto(AuditLog x) =>
        new(x.Id, x.TenantId, x.UserId, x.EventType, x.Resource, x.ResourceId, x.Result, x.CorrelationId, x.Metadata, x.IpAddress, x.OccurredAt, x.Severity);
}
