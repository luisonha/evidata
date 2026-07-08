using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;

namespace Evidata.Modules.Audit.Infrastructure;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;

    public AuditService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    public async Task LogAsync(
        Guid tenantId,
        Guid? userId,
        string action,
        string resource,
        Guid? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info,
        CancellationToken ct = default)
    {
        var entry = AuditLog.Create(tenantId, userId, action, resource, resourceId, details, ipAddress, severity);
        await _repository.AddAsync(entry, ct);
    }
}
