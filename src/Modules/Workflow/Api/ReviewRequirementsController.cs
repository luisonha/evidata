using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Commands;
using Evidata.Modules.Workflow.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Workflow.Api;

/// <summary>
/// Endpoints para administrar la política de requerimientos de revisión por tenant.
/// Requiere autorización TenantOwnerOrComplianceAdmin.
/// 
/// P1-018: Configuración de ReviewRequirement.
/// </summary>
[ApiController]
[Authorize]
[Route("api/review-requirements")]
public class ReviewRequirementsController : ControllerBase
{
    private readonly IReviewRequirementPolicyService _policyService;
    private readonly SetReviewRequirementCommandHandler _setHandler;

    public ReviewRequirementsController(
        IReviewRequirementPolicyService policyService,
        SetReviewRequirementCommandHandler setHandler)
    {
        _policyService = policyService;
        _setHandler = setHandler;
    }

    /// <summary>
    /// Obtiene todas las configuraciones de requerimientos de revisión para un tenant.
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<ActionResult<IEnumerable<ReviewRequirementDto>>> GetByTenant(
        [FromQuery] Guid tenantId,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            return BadRequest("tenantId is required.");

        var requirements = await _policyService.GetAllForTenantAsync(tenantId, ct);
        var dtos = requirements.Select(ReviewRequirementDto.From).ToList();
        
        return Ok(dtos);
    }

    /// <summary>
    /// Crea o actualiza un requerimiento de revisión para un tenant.
    /// Si ya existe configuración para el mismo tenant/entityType/reviewType, se actualiza.
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<ActionResult<ReviewRequirementDto>> SetRequirement(
        [FromBody] SetReviewRequirementCommand cmd,
        CancellationToken ct)
    {
        if (cmd.TenantId == Guid.Empty)
            return BadRequest("TenantId is required.");

        if (string.IsNullOrWhiteSpace(cmd.EntityType))
            return BadRequest("EntityType is required.");

        // Get current user from HttpContext
        var userId = User.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var modifiedBy))
            return Unauthorized("Cannot determine user ID.");

        var result = await _setHandler.HandleAsync(cmd, modifiedBy, ct);
        return CreatedAtAction(nameof(GetByTenant), new { tenantId = cmd.TenantId }, result);
    }

    /// <summary>
    /// Elimina una configuración de requerimiento (revierte al comportamiento por defecto: requerido).
    /// Requiere rol TenantOwner o ComplianceAdmin.
    /// </summary>
    [HttpDelete]
    [Authorize(Policy = "TenantOwnerOrComplianceAdmin")]
    public async Task<IActionResult> DeleteRequirement(
        [FromQuery] Guid tenantId,
        [FromQuery] int reviewType,
        [FromQuery] string entityType,
        CancellationToken ct)
    {
        if (tenantId == Guid.Empty)
            return BadRequest("tenantId is required.");

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
