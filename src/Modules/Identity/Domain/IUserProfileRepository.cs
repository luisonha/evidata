namespace Evidata.Modules.Identity.Domain;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<UserProfile?> GetByExternalIdAsync(string externalId, string provider, Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<UserProfile>> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task UpsertAsync(UserProfile profile, CancellationToken ct = default);
}
