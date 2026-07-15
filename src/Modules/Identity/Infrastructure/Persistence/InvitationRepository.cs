using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class InvitationRepository : IInvitationRepository
{
    private readonly IdentityDbContext _context;

    public InvitationRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Invitation?> GetByIdAsync(Guid invitationId, CancellationToken ct = default)
    {
        return await _context.Invitations.FirstOrDefaultAsync(i => i.Id == invitationId, ct);
    }

    public async Task<Invitation?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Invitations.FirstOrDefaultAsync(i => i.UserId == userId, ct);
    }

    public async Task<Invitation?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Invitations.FirstOrDefaultAsync(
            i => i.Email == email && i.TenantId == tenantId,
            ct);
    }

    public async Task<IList<Invitation>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Invitations
            .Where(i => i.TenantId == tenantId)
            .ToListAsync(ct);
    }

    public async Task<IList<Invitation>> GetPendingByTenantAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Invitations
            .Where(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
    }

    public async Task UpsertAsync(Invitation invitation, CancellationToken ct = default)
    {
        var existing = await _context.Invitations.FirstOrDefaultAsync(i => i.Id == invitation.Id, ct);
        if (existing is null)
        {
            _context.Invitations.Add(invitation);
        }
        else
        {
            _context.Entry(existing).CurrentValues.SetValues(invitation);
        }
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await GetByIdAsync(invitationId, ct);
        if (invitation is not null)
        {
            _context.Invitations.Remove(invitation);
            await _context.SaveChangesAsync(ct);
        }
    }
}
