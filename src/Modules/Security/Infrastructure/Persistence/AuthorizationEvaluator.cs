using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Persistence;

public class AuthorizationEvaluator : IAuthorizationEvaluator
{
    private readonly SecurityDbContext _ctx;

    public AuthorizationEvaluator(SecurityDbContext ctx) => _ctx = ctx;

    public async Task<bool> HasPermissionAsync(Guid userId, Guid tenantId, string resource, string action, CancellationToken ct = default)
    {
        var permissionName = $"{resource}:{action}";
        return await _ctx.UserRoleAssignments
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Join(_ctx.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_ctx.Permissions, id => id, p => p.Id, (id, p) => p.Name)
            .AnyAsync(name => name == permissionName, ct);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
    {
        return await _ctx.UserRoleAssignments
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .Join(_ctx.RolePermissions, a => a.RoleId, rp => rp.RoleId, (a, rp) => rp.PermissionId)
            .Join(_ctx.Permissions, id => id, p => p.Id, (id, p) => p.Name)
            .Distinct()
            .ToListAsync(ct);
    }
}
