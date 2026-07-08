namespace Evidata.Modules.Audit.Domain;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByResourceAsync(Guid tenantId, string resource, Guid resourceId, CancellationToken ct = default);
}
