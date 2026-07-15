using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly IdentityDbContext _context;

    public UserProfileRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.UserProfiles.FindAsync([id], ct);

    public async Task<UserProfile?> GetByExternalIdAsync(string externalId, string provider, Guid tenantId, CancellationToken ct = default)
        => await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.ExternalId == externalId && x.Provider == provider && x.TenantId == tenantId, ct);

    public async Task<UserProfile?> GetByEmailAsync(string email, Guid tenantId, CancellationToken ct = default)
        => await _context.UserProfiles
            .FirstOrDefaultAsync(x => x.Email == email && x.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<UserProfile>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
        => await _context.UserProfiles
            .Where(x => x.TenantId == tenantId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<UserProfile>> GetByRoleAndTenantAsync(Guid roleId, Guid tenantId, CancellationToken ct = default)
        => await _context.UserProfiles
            .Where(x => x.TenantId == tenantId && x.UserProfileRoles.Any(r => r.RoleId == roleId))
            .ToListAsync(ct);

    public async Task<int> CountActiveTenantOwnersAsync(Guid tenantId, CancellationToken ct = default)
    {
        // TODO: Replace with actual TenantOwner role ID resolution from Security module
        // For now, return a stub count
        return await Task.FromResult(1);
    }

    public async Task UpsertAsync(UserProfile profile, CancellationToken ct = default)
    {
        var tracked = await _context.UserProfiles.FindAsync([profile.Id], ct);
        if (tracked is null)
            _context.UserProfiles.Add(profile);
        // If tracked, EF Core tracks the changes automatically
        await _context.SaveChangesAsync(ct);
    }
}
