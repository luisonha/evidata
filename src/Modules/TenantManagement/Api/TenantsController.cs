using Evidata.Modules.TenantManagement.Application.Commands;
using Evidata.Modules.TenantManagement.Application.DTOs;
using Evidata.Modules.TenantManagement.Application.Queries;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.TenantManagement.Api;

[ApiController]
[Route("api/tenants")]
public class TenantsController(
    CreateTenantCommandHandler createHandler,
    UpdateTenantSettingsCommandHandler settingsHandler,
    ChangeTenantStatusCommandHandler statusHandler,
    GetTenantQueryHandler getHandler) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<TenantDto>> Create([FromBody] CreateTenantRequest request, CancellationToken ct)
    {
        var result = await createHandler.HandleAsync(new CreateTenantCommand(request.Slug, request.Name), ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TenantDto>> GetById(Guid id, CancellationToken ct)
    {
        var result = await getHandler.HandleAsync(new GetTenantQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPatch("{id:guid}/settings")]
    public async Task<ActionResult<TenantDto>> UpdateSettings(Guid id, [FromBody] UpdateTenantSettingsRequest request, CancellationToken ct)
    {
        var result = await settingsHandler.HandleAsync(
            new UpdateTenantSettingsCommand(id, request.TimeZone, request.Locale, request.MaxUsers, request.MfaRequired), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult<TenantDto>> ChangeStatus(Guid id, [FromBody] ChangeTenantStatusRequest request, CancellationToken ct)
    {
        var result = await statusHandler.HandleAsync(
            new ChangeTenantStatusCommand(id, Enum.Parse<TenantStatus>(request.Status, ignoreCase: true)), ct);
        return Ok(result);
    }
}

public record CreateTenantRequest(string Slug, string Name);
public record UpdateTenantSettingsRequest(string? TimeZone, string? Locale, int? MaxUsers, bool? MfaRequired);
public record ChangeTenantStatusRequest(string Status);
