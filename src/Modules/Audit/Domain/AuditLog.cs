using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Audit.Domain;

public class AuditLog : ITenantScoped
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = default!;       // e.g. "tenant.created"
    public string Resource { get; private set; } = default!;     // e.g. "Tenant"
    public Guid? ResourceId { get; private set; }
    public string? Details { get; private set; }                  // JSON snapshot (optional)
    public string? IpAddress { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public AuditSeverity Severity { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        Guid tenantId,
        Guid? userId,
        string action,
        string resource,
        Guid? resourceId = null,
        string? details = null,
        string? ipAddress = null,
        AuditSeverity severity = AuditSeverity.Info)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            Resource = resource,
            ResourceId = resourceId,
            Details = details,
            IpAddress = ipAddress,
            Severity = severity,
            OccurredAt = DateTime.UtcNow
        };
    }
}
