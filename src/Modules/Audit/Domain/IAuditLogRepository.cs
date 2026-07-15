namespace Evidata.Modules.Audit.Domain;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog entry, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default);
    Task<IReadOnlyList<AuditLog>> GetByResourceAsync(Guid tenantId, string resource, Guid resourceId, CancellationToken ct = default);
    
    /// <summary>
    /// Obtener eventos de auditoría del tenant con filtros avanzados.
    /// Soporta filtrado por eventType, actorUserId, targetUserId, rango de fechas y paginación.
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Events, int TotalCount)> GetByTenantWithFiltersAsync(
        Guid tenantId,
        string? eventType = null,
        Guid? actorUserId = null,
        Guid? targetUserId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}
