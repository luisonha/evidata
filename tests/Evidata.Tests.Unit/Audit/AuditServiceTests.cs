using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Audit.Infrastructure;
using NSubstitute;

namespace Evidata.Tests.Unit.Audit;

/// <summary>
/// Tests para AuditService con nuevos campos del contrato P1-009: eventType, result, correlationId, metadata.
/// Verifican creación de eventos con shape completo y propagación de correlationId.
/// </summary>
public class AuditServiceTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _service = new AuditService(_repository);
    }

    [Fact]
    public async Task LogAsync_WithFormalEventType_CreatesAuditEventCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var metadata = new Dictionary<string, object?> { { "tenantName", "Acme Corp" } };

        // Act
        await _service.LogAsync(
            tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId: tenantId,
            result: AuditEventResult.Success,
            correlationId: correlationId,
            metadata: metadata);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x =>
                x.TenantId == tenantId &&
                x.UserId == userId &&
                x.EventType == "CreateProcessingActivity" &&
                x.Resource == "ProcessingActivity" &&
                x.Result == AuditEventResult.Success &&
                x.CorrelationId == correlationId));
    }

    [Fact]
    public async Task LogAsync_WithFailureResult_SetsResultCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        await _service.LogAsync(tenantId, null, "ValidateEvidence", "Evidence",
            result: AuditEventResult.Failure);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x => x.Result == AuditEventResult.Failure));
    }

    [Fact]
    public async Task LogAsync_WithBlockedResult_SetsResultCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        await _service.LogAsync(tenantId, null, "Approve", "ProcessingActivity",
            result: AuditEventResult.Blocked);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x => x.Result == AuditEventResult.Blocked));
    }

    [Fact]
    public async Task LogAsync_WithMetadata_SerializesAsJson()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var metadata = new Dictionary<string, object?> 
        { 
            { "action", "approve" }, 
            { "reason", "All conditions met" } 
        };

        // Act
        await _service.LogAsync(tenantId, null, "Approve", "ProcessingActivity",
            metadata: metadata);

        // Assert
        await _repository.Received(1).AddAsync(
            Arg.Is<AuditLog>(x =>
                x.Metadata != null && 
                x.Metadata.Contains("\"action\"")));
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
        var log = AuditLog.Create(Guid.NewGuid(), null, "CreateProcessingActivity", "ProcessingActivity");

        // Assert
        Assert.True(log.OccurredAt >= before);
        Assert.Equal(DateTimeKind.Utc, log.OccurredAt.Kind);
    }

    [Fact]
    public void AuditLog_CreateWithMetadata_SerializesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object?> { { "key", "value" } };

        // Act
        var log = AuditLog.Create(Guid.NewGuid(), null, "test.action", "Resource", 
            metadata: metadata);

        // Assert
        Assert.NotNull(log.Metadata);
        Assert.Contains("key", log.Metadata);
    }

    [Fact]
    public void AuditLog_GetMetadata_DeserializesJson()
    {
        // Arrange
        var metadata = new Dictionary<string, object?> { { "field1", "value1" }, { "field2", 42 } };
        var log = AuditLog.Create(Guid.NewGuid(), null, "test", "Resource", metadata: metadata);

        // Act
        var deserialized = log.GetMetadata();

        // Assert
        Assert.NotEmpty(deserialized);
        Assert.Equal("value1", deserialized["field1"]?.ToString());
    }

    [Fact]
    public void AuditLog_GetMetadata_WithoutMetadata_ReturnsEmpty()
    {
        // Arrange
        var log = AuditLog.Create(Guid.NewGuid(), null, "test", "Resource");

        // Act
        var result = log.GetMetadata();

        // Assert
        Assert.Empty(result);
    }
}
