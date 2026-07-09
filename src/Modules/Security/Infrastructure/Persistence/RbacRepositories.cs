using Evidata.Modules.Security.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Persistence;

public class RoleRepository : IRoleRepository
{
    private readonly SecurityDbContext _ctx;
    public RoleRepository(SecurityDbContext ctx) => _ctx = ctx;

    public Task<Role?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
        => _ctx.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Name == name, ct);

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Roles.Include(r => r.Permissions).ToListAsync(ct);

    public async Task AddAsync(Role role, CancellationToken ct = default)
    {
        _ctx.Roles.Add(role);
        await _ctx.SaveChangesAsync(ct);
    }
}

public class PermissionRepository : IPermissionRepository
{
    private readonly SecurityDbContext _ctx;
    public PermissionRepository(SecurityDbContext ctx) => _ctx = ctx;

    public Task<Permission?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Permissions.FindAsync([id], ct).AsTask();

    public Task<Permission?> GetByNameAsync(string name, CancellationToken ct = default)
        => _ctx.Permissions.FirstOrDefaultAsync(p => p.Name == name, ct);

    public async Task<IReadOnlyList<Permission>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Permissions.ToListAsync(ct);

    public async Task AddAsync(Permission permission, CancellationToken ct = default)
    {
        _ctx.Permissions.Add(permission);
        await _ctx.SaveChangesAsync(ct);
    }
}

public class UserRoleAssignmentRepository : IUserRoleAssignmentRepository
{
    private readonly SecurityDbContext _ctx;
    public UserRoleAssignmentRepository(SecurityDbContext ctx) => _ctx = ctx;

    public async Task<IReadOnlyList<UserRoleAssignment>> GetByUserAsync(Guid userId, Guid tenantId, CancellationToken ct = default)
        => await _ctx.UserRoleAssignments
            .Where(a => a.UserId == userId && a.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task AssignAsync(UserRoleAssignment assignment, CancellationToken ct = default)
    {
        _ctx.UserRoleAssignments.Add(assignment);
        await _ctx.SaveChangesAsync(ct);
    }

    public async Task RemoveAsync(Guid userId, Guid roleId, Guid tenantId, CancellationToken ct = default)
    {
        var a = await _ctx.UserRoleAssignments
            .FirstOrDefaultAsync(x => x.UserId == userId && x.RoleId == roleId && x.TenantId == tenantId, ct);
        if (a is not null)
        {
            _ctx.UserRoleAssignments.Remove(a);
            await _ctx.SaveChangesAsync(ct);
        }
    }
}
