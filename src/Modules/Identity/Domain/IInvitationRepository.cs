namespace Evidata.Modules.Identity.Domain;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid invitationId, CancellationToken ct = default);
    Task<Invitation?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Invitation?> GetByEmailAndTenantAsync(string email, Guid tenantId, CancellationToken ct = default);
    Task<IList<Invitation>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IList<Invitation>> GetPendingByTenantAsync(Guid tenantId, CancellationToken ct = default);
    Task UpsertAsync(Invitation invitation, CancellationToken ct = default);
    Task DeleteAsync(Guid invitationId, CancellationToken ct = default);
}
