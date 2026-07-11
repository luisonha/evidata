using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Commands;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Workflow.Api;

/// <summary>
/// Endpoints para administrar la política de requerimientos de revisión por tenant.
/// Requiere autorización TenantOwnerOrComplianceAdmin.
/// 
/// P1-018: Configuración de ReviewRequirement.
/// 
/// BLOCKER #2 FIX (Legolas): All endpoints operate on the authenticated user's tenant.
/// No arbitrary tenantId accepted for read/write/delete operations.
/// Prevents multi-tenant isolation bypass via query parameter manipulation.
/// </summary>
[ApiController]
[Authorize]
[Route("api/review-requirements")]
public class ReviewRequirementsController : ControllerBase
{
    private readonly IReviewRequirementPolicyService _policyService;
    private readonly SetReviewRequirementCommandHandler _setHandler;
    private readonly ICurrentUserContext _currentUser;

    public ReviewRequirementsController(
        IReviewRequirementPolicyService policyService,
        SetReviewRequirementCommandHandler setHandler,
        ICurrentUserContext currentUser)
    {
        _policyService = policyService;
        _setHandler = setHandler;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Obtiene todas las configuraciones de requerimientos de revisión para el tenant del usuario autenticado.
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// 
    /// BLOCKER #2 FIX: No longer accepts arbitrary tenantId query parameter.
    /// Tenant is extracted from authenticated user context (ICurrentUserContext).
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<ActionResult<IEnumerable<ReviewRequirementDto>>> GetByTenant(CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized("Cannot determine tenant ID from user context.");

        var requirements = await _policyService.GetAllForTenantAsync(tenantId, ct);
        var dtos = requirements.Select(ReviewRequirementDto.From).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Crea o actualiza un requerimiento de revisión para el tenant del usuario autenticado.
    /// Si ya existe configuración para el mismo tenant/entityType/reviewType, se actualiza.
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// 
    /// BLOCKER #2 FIX: Tenant is extracted from authenticated user context, not from request body.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<ActionResult<ReviewRequirementDto>> SetRequirement(
        [FromBody] SetReviewRequirementCommandBody cmd,
        CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized("Cannot determine tenant ID from user context.");

        if (string.IsNullOrWhiteSpace(cmd.EntityType))
            return BadRequest("EntityType is required.");

        // Get current user ID from HttpContext
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var modifiedBy))
            return Unauthorized("Cannot determine user ID.");

        // Create command with tenant from authenticated user
        var command = new SetReviewRequirementCommand(
            tenantId,
            cmd.ReviewType,
            cmd.EntityType,
            cmd.IsRequired);

        var result = await _setHandler.HandleAsync(command, modifiedBy, ct);
        return CreatedAtAction(nameof(GetByTenant), new { }, result);
    }

    /// <summary>
    /// Elimina una configuración de requerimiento para el tenant del usuario autenticado
    /// (revierte al comportamiento por defecto: requerido).
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// 
    /// BLOCKER #2 FIX: Tenant is extracted from authenticated user context, not from query parameter.
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> DeleteRequirement(
        [FromQuery] int reviewType,
        [FromQuery] string entityType,
        CancellationToken ct)
    {
        var tenantId = _currentUser.TenantId;
        if (tenantId == Guid.Empty)
            return Unauthorized("Cannot determine tenant ID from user context.");

        if (string.IsNullOrWhiteSpace(entityType))
            return BadRequest("entityType is required.");

        if (!Enum.IsDefined(typeof(ReviewType), reviewType))
            return BadRequest("reviewType is invalid.");

        await _policyService.DeleteRequirementAsync(
            tenantId,
            (ReviewType)reviewType,
            entityType,
            ct);

        return NoContent();
    }
}

/// <summary>
/// DTO for SetRequirement request body (without TenantId, which is extracted from authenticated user).
/// </summary>
public sealed record SetReviewRequirementCommandBody(
    int ReviewType,
    string EntityType,
    bool IsRequired);
