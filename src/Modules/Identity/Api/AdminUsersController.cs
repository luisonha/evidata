using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Application.Services;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Identity.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize]
public class AdminUsersController : ControllerBase
{
    private readonly AdminUsersService _service;
    private readonly IAntiforgery _antiforgery;

    public AdminUsersController(AdminUsersService service, IAntiforgery antiforgery)
    {
        _service = service;
        _antiforgery = antiforgery;
    }

    private IActionResult MapError(IdentityDomainException ex)
    {
        return ex.ErrorCode switch
        {
            IdentityErrorCodes.UserAlreadyExists => StatusCode(422, new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.InvalidStateTransition => StatusCode(409, new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.LastTenantOwnerBlocked => StatusCode(409, new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.UserNotFound => NotFound(new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.InvitationRevoked => StatusCode(409, new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.InvalidRole => StatusCode(422, new { errorCode = ex.ErrorCode, message = ex.Message }),
            IdentityErrorCodes.InvalidResponsibleArea => StatusCode(422, new { errorCode = ex.ErrorCode, message = ex.Message }),
            _ => StatusCode(500, new { errorCode = "InternalError", message = "An error occurred" })
        };
    }

    private Guid GetTenantId() => Guid.Parse(User.FindFirst("tenant_id")?.Value ?? throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "Tenant not found"));
    private Guid GetActorId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? throw new IdentityDomainException(IdentityErrorCodes.UserNotFound, "Actor not found"));

    private async Task ValidateCsrfAsync(CancellationToken ct)
    {
        try
        {
            await _antiforgery.ValidateRequestAsync(HttpContext);
        }
        catch (AntiforgeryValidationException)
        {
            throw new IdentityDomainException(IdentityErrorCodes.CsrfValidationFailed, "CSRF validation failed");
        }
    }

    [HttpPost("users")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequestDto request, CancellationToken ct)
    {
        try
        {
            var tenantId = GetTenantId();
            var userId = GetActorId();
            var cmd = new InviteUserCommand(tenantId, userId, request.Email, request.DisplayName, request.Role, request.ResponsibleAreaId, request.Message);
            return StatusCode(201, await _service.InviteUserAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpGet("users")]
    [Authorize(Policy = "HasPermission:Admin.ReadUsers")]
    public async Task<IActionResult> ListUsers([FromQuery] string? q, [FromQuery] string? status, [FromQuery] string? role, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        try
        {
            var tenantId = GetTenantId();
            UserStatus? statusFilter = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<UserStatus>(status, ignoreCase: true, out var s))
            {
                statusFilter = s;
            }
            var query = new ListUsersQuery(tenantId, q, statusFilter, role, page, pageSize);
            return Ok(await _service.ListUsersAsync(query, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpGet("users/{userId:guid}")]
    [Authorize(Policy = "HasPermission:Admin.ReadUsers")]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        try
        {
            var tenantId = GetTenantId();
            return Ok(await _service.GetUserAsync(tenantId, userId, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPatch("users/{userId:guid}")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new UpdateUserCommand(tenantId, userId, actorId, request.DisplayName, request.ResponsibleAreaId);
            return Ok(await _service.UpdateUserAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPost("users/{userId:guid}/resend-invitation")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> ResendInvitation(Guid userId, [FromBody] ResendInvitationRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new ResendInvitationCommand(tenantId, userId, actorId);
            return Ok(await _service.ResendInvitationAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPost("users/{userId:guid}/revoke-invitation")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> RevokeInvitation(Guid userId, [FromBody] RevokeInvitationRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new RevokeInvitationCommand(tenantId, userId, actorId, request.Reason);
            return Ok(await _service.RevokeInvitationAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPost("users/{userId:guid}/suspend")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> SuspendUser(Guid userId, [FromBody] SuspendUserRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new SuspendUserCommand(tenantId, userId, actorId, request.Reason);
            return Ok(await _service.SuspendUserAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPost("users/{userId:guid}/reactivate")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> ReactivateUser(Guid userId, [FromBody] ReactivateUserRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new ReactivateUserCommand(tenantId, userId, actorId, request.Reason);
            return Ok(await _service.ReactivateUserAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPost("users/{userId:guid}/disable")]
    [Authorize(Policy = "HasPermission:Admin.ManageUsers")]
    public async Task<IActionResult> DisableUser(Guid userId, [FromBody] DisableUserRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new DisableUserCommand(tenantId, userId, actorId, request.Reason);
            return Ok(await _service.DisableUserAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }

    [HttpPatch("users/{userId:guid}/role")]
    [Authorize(Policy = "HasPermission:Admin.ChangeUserRole")]
    public async Task<IActionResult> ChangeUserRoles(Guid userId, [FromBody] ChangeUserRolesRequestDto request, CancellationToken ct)
    {
        try
        {
            await ValidateCsrfAsync(ct);
            var tenantId = GetTenantId();
            var actorId = GetActorId();
            var cmd = new ChangeUserRolesCommand(tenantId, userId, actorId, request.Roles, request.Reason);
            return Ok(await _service.ChangeUserRolesAsync(cmd, ct));
        }
        catch (IdentityDomainException ex)
        {
            return MapError(ex);
        }
    }
}
