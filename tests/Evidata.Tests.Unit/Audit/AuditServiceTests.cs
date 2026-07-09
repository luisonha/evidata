using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Audit.Infrastructure;
using NSubstitute;

namespace Evidata.Tests.Unit.Audit;

public class AuditServiceTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _service = new AuditService(_repository);
    }

    [Fact]
    public async Task LogAsync_ValidEntry_CallsRepository()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        await _service.LogAsync(tenantId, userId, "tenant.created", "Tenant", tenantId);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.Action == "tenant.created" &&
                x.Resource == "Tenant"));
    }

    [Fact]
    public async Task LogAsync_CriticalSeverity_SetsCorrectSeverity()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        await _service.LogAsync(tenantId, null, "access.denied", "Document",
            severity: AuditSeverity.Critical);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x => x.Severity == AuditSeverity.Critical));
    }

    [Fact]
    public void AuditLog_Create_SetsOccurredAtUtc()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var log = AuditLog.Create(Guid.NewGuid(), null, "test.action", "Resource");

        // Assert
        Assert.True(log.OccurredAt >= before);
        Assert.Equal(DateTimeKind.Utc, log.OccurredAt.Kind);
    }
}
