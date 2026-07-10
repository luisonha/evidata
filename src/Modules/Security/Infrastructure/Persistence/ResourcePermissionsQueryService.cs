using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Persistence;

/// <summary>
/// Calculates available and blocked actions for a user on a specific resource.
/// Implements the 6 critical permission rules from the RBAC contract.
/// </summary>
public class ResourcePermissionsQueryService : IResourcePermissionsQueryService
{
    private readonly SecurityDbContext _ctx;

    public ResourcePermissionsQueryService(SecurityDbContext ctx) => _ctx = ctx;

    public async Task<ResourcePermissionsResult> GetResourcePermissionsAsync(
        Guid userId,
        Guid tenantId,
        string resourceType,
        Guid resourceId,
        ResourceContextData? context = null,
        CancellationToken ct = default)
    {
        // Get user's roles in tenant
        var userRoles = await _ctx.UserRoleAssignments
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Join(_ctx.Roles, a => a.RoleId, r => r.Id, (a, r) => r)
            .ToListAsync(ct);

        var roleCodes = userRoles
            .Select(r => r.Name)
            .ToList();

        // Get user's permissions via roles
        var userPermissions = await _ctx.UserRoleAssignments
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Join(_ctx.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_ctx.Permissions, id => id, p => p.Id, (id, p) => p.Name)
            .Distinct()
            .ToListAsync(ct);

        var available = new List<AvailableActionResult>();
        var blocked = new List<BlockedActionResult>();

        // Evaluate each critical permission
        // SEC-APP-001: ProcessOwner cannot approve their own processing activity
        if (CanEvaluateActionForResource(userPermissions, "processingActivity:approve"))
        {
            if (IsBlocked_ApproveOwnActivity(userId, context))
            {
                blocked.Add(new BlockedActionResult(
                    "ApproveProcessingActivity",
                    "permission.approveProcessingActivity",
                    "SEC-APP-001",
                    "block.approveOwnActivity",
                    "High",
                    "severity.high",
                    "Review",
                    "node.review"));
            }
            else
            {
                available.Add(new AvailableActionResult(
                    "ApproveProcessingActivity",
                    "permission.approveProcessingActivity"));
            }
        }

        // SEC-ACT-001: Only TenantOwner/ComplianceAdmin can activate
        if (CanEvaluateActionForResource(userPermissions, "processingActivity:activate"))
        {
            if (IsBlocked_ActivateNotAdmin(userRoles))
            {
                blocked.Add(new BlockedActionResult(
                    "ActivateProcessingActivity",
                    "permission.activateProcessingActivity",
                    "SEC-ACT-001",
                    "block.activateNotAdmin",
                    "Critical",
                    "severity.critical",
                    "Review",
                    "node.review"));
            }
            else
            {
                available.Add(new AvailableActionResult(
                    "ActivateProcessingActivity",
                    "permission.activateProcessingActivity"));
            }
        }

        // SEC-EV-001: Validate evidence - role depends on evidence domain
        if (CanEvaluateActionForResource(userPermissions, "evidence:validate"))
        {
            if (IsBlocked_ValidateEvidenceWrongDomain(userRoles, context))
            {
                blocked.Add(new BlockedActionResult(
                    "ValidateEvidence",
                    "permission.validateEvidence",
                    "SEC-EV-001",
                    "block.validateEvidenceWrongRole",
                    "High",
                    "severity.high",
                    "Evidence",
                    "node.evidence"));
            }
            else if (HasEvidenceValidationRole(userRoles))
            {
                available.Add(new AvailableActionResult(
                    "ValidateEvidence",
                    "permission.validateEvidence"));
            }
            else
            {
                blocked.Add(new BlockedActionResult(
                    "ValidateEvidence",
                    "permission.validateEvidence",
                    "SEC-EV-001",
                    "block.validateEvidenceWrongRole",
                    "High",
                    "severity.high",
                    "Evidence",
                    "node.evidence"));
            }
        }

        // SEC-GAP-001: Accept gap with risk - only TenantOwner/ComplianceAdmin
        if (CanEvaluateActionForResource(userPermissions, "gap:acceptWithRisk"))
        {
            if (IsBlocked_AcceptGapNotAdmin(userRoles))
            {
                blocked.Add(new BlockedActionResult(
                    "AcceptGapWithRisk",
                    "permission.acceptGapWithRisk",
                    "SEC-GAP-001",
                    "block.acceptGapNotAdmin",
                    "Critical",
                    "severity.critical",
                    "Gaps",
                    "node.gaps"));
            }
            else
            {
                available.Add(new AvailableActionResult(
                    "AcceptGapWithRisk",
                    "permission.acceptGapWithRisk"));
            }
        }

        // SEC-EXP-001: Generate official export - only authorized roles
        if (CanEvaluateActionForResource(userPermissions, "export:generate"))
        {
            if (!HasExportGenerationRole(userRoles))
            {
                blocked.Add(new BlockedActionResult(
                    "GenerateOfficialExport",
                    "permission.generateOfficialExport",
                    "SEC-EXP-001",
                    "block.generateExportNotAuthorized",
                    "High",
                    "severity.high",
                    "Export",
                    "node.export"));
            }
            else
            {
                available.Add(new AvailableActionResult(
                    "GenerateOfficialExport",
                    "permission.generateOfficialExport"));
            }
        }

        // SEC-EVDOWN-001: Download evidence - Viewer cannot download sensitive evidence
        if (CanEvaluateActionForResource(userPermissions, "evidence:download"))
        {
            if (IsBlocked_DownloadSensitiveEvidence(userRoles))
            {
                blocked.Add(new BlockedActionResult(
                    "DownloadEvidence",
                    "permission.downloadEvidence",
                    "SEC-EVDOWN-001",
                    "block.downloadSensitive",
                    "High",
                    "severity.high",
                    "Evidence",
                    "node.evidence"));
            }
            else
            {
                available.Add(new AvailableActionResult(
                    "DownloadEvidence",
                    "permission.downloadEvidence"));
            }
        }

        return new ResourcePermissionsResult(
            roleCodes,
            available,
            IsReadOnly(userRoles),
            blocked);
    }

    // ── Rule Implementations ──────────────────────────────────────────────────

    /// <summary>SEC-APP-001: ProcessOwner cannot approve their own activity.</summary>
    private static bool IsBlocked_ApproveOwnActivity(Guid userId, ResourceContextData? context)
    {
        return context?.ResourceOwnerId == userId;
    }

    /// <summary>SEC-ACT-001: Only TenantOwner/ComplianceAdmin can activate.</summary>
    private static bool IsBlocked_ActivateNotAdmin(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        return !roleNames.Contains("TenantOwner") && !roleNames.Contains("ComplianceAdmin");
    }

    /// <summary>SEC-EV-001: Validate evidence requires LegalReviewer or SecurityReviewer role.</summary>
    private static bool HasEvidenceValidationRole(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        return roleNames.Contains("LegalReviewer") || roleNames.Contains("SecurityReviewer");
    }

    /// <summary>
    /// SEC-EV-001: If reviewDomain is provided in context, validate that the user has the correct domain-specific role.
    /// Legal domain requires LegalReviewer; Security domain requires SecurityReviewer.
    /// Returns true if the user's roles don't match the requirement's reviewDomain.
    /// FAIL-CLOSED: If ReviewDomain is missing, blocks the action (never auto-allows).
    /// </summary>
    private static bool IsBlocked_ValidateEvidenceWrongDomain(List<Role> userRoles, ResourceContextData? context)
    {
        // FAIL-CLOSED: If no context or ReviewDomain is null, block the action
        if (context?.ReviewDomain is null)
            return true; // Block if ReviewDomain is missing — security requirement always needs domain

        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        
        // If context.ReviewDomain is a string representation of the enum
        if (context.ReviewDomain is string domainStr)
        {
            return domainStr switch
            {
                "Legal" => !roleNames.Contains("LegalReviewer"),
                "Security" => !roleNames.Contains("SecurityReviewer"),
                _ => true // Unknown domain = blocked
            };
        }

        // If context.ReviewDomain is already the enum type (from Evidence module)
        if (context.ReviewDomain.GetType().Name == "ReviewDomain")
        {
            var domainValue = context.ReviewDomain.ToString() ?? "";
            return domainValue switch
            {
                "Legal" => !roleNames.Contains("LegalReviewer"),
                "Security" => !roleNames.Contains("SecurityReviewer"),
                _ => true
            };
        }

        return true; // Unknown type, block for safety
    }

    /// <summary>SEC-GAP-001: Accept gap with risk - only TenantOwner/ComplianceAdmin.</summary>
    private static bool IsBlocked_AcceptGapNotAdmin(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        return !roleNames.Contains("TenantOwner") && !roleNames.Contains("ComplianceAdmin");
    }

    /// <summary>SEC-EXP-001: Generate export - requires authorized role (not Viewer).</summary>
    private static bool HasExportGenerationRole(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        if (roleNames.Contains("Viewer") && roleNames.Count == 1)
            return false;
        return roleNames.Any(r => r != "Viewer");
    }

    /// <summary>SEC-EVDOWN-001: Download evidence - Viewer cannot download sensitive evidence.</summary>
    private static bool IsBlocked_DownloadSensitiveEvidence(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        return roleNames.Contains("Viewer") && roleNames.Count == 1;
    }

    /// <summary>Check if user has permission string registered.</summary>
    private static bool CanEvaluateActionForResource(IReadOnlyList<string> userPermissions, string permissionName)
    {
        return userPermissions.Contains(permissionName);
    }

    /// <summary>Check if resource is read-only for this user (e.g., Viewer role).</summary>
    private static bool IsReadOnly(List<Role> userRoles)
    {
        var roleNames = new HashSet<string>(userRoles.Select(r => r.Name));
        return roleNames.Contains("Viewer") && roleNames.Count == 1;
    }
}
