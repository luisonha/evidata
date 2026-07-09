using System.Text.Json;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Application.Queries;
using Evidata.Modules.Audit.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Audit;

/// <summary>
/// Unit tests for GetProcessingActivityTimelineQueryHandler.
/// Verifies correct mapping of AuditLog to TimelineEventViewModel and handling of model gaps.
/// </summary>
public class TimelineQueryHandlerTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly GetProcessingActivityTimelineQueryHandler _handler;

    public TimelineQueryHandlerTests()
    {
        _handler = new GetProcessingActivityTimelineQueryHandler(_repository);
    }

    [Fact]
    public async Task HandleAsync_NoEvents_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog>());

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_EventWithValidAuditEventType_MapsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, details: null, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Equal(log.Id.ToString(), vm.Id);
        Assert.Equal("CreateProcessingActivity", vm.EventType);
        Assert.Equal("audit.eventType.CreateProcessingActivity", vm.EventTypeLabelKey);
        Assert.Equal("ProcessingActivity", vm.ResourceType);
        Assert.Equal(resourceId.ToString(), vm.ResourceId);
        Assert.Equal(userId.ToString(), vm.ActorUserId);
        Assert.Equal("Success", vm.Result);
        Assert.Equal("audit.result.Success", vm.ResultLabelKey);
        Assert.Null(vm.CorrelationId);
        Assert.Null(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_EventWithUnmatchableAction_ExcludedFromTimeline()
    {
        // Arrange: Create an AuditLog with Action that doesn't match any AuditEventType enum value
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, userId, "some.random.action", "ProcessingActivity",
            resourceId, details: null, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Event should be excluded (not returned) because Action doesn't parse to an enum
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_EventWithNullUserId_UsesGuidEmpty()
    {
        // Arrange: System action (UserId = null)
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, null, "ActivateProcessingActivity", "ProcessingActivity",
            resourceId, details: null, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Equal(Guid.Empty.ToString(), vm.ActorUserId);
    }

    [Fact]
    public async Task HandleAsync_MultipleEvents_ReturnedInOrder()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var log1 = AuditLog.Create(tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);
        var log2 = AuditLog.Create(tenantId, userId, "ActivateProcessingActivity", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);

        // Repository returns sorted by OccurredAt descending
        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log2, log1 });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("ActivateProcessingActivity", result[0].EventType);
        Assert.Equal("CreateProcessingActivity", result[1].EventType);
    }

    [Fact]
    public async Task HandleAsync_PaginationSkip_SkipsEventsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var logs = Enumerable.Range(0, 5)
            .Select(i => AuditLog.Create(
                tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
                resourceId, severity: AuditSeverity.Info))
            .ToList();

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(logs);

        // Act: skip 2, take 2
        var result = await _handler.HandleAsync(tenantId, resourceId, "ProcessingActivity", skip: 2, take: 2);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task HandleAsync_PaginationTake_LimitsResultsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var logs = Enumerable.Range(0, 100)
            .Select(i => AuditLog.Create(
                tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
                resourceId, severity: AuditSeverity.Info))
            .ToList();

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(logs);

        // Act: default take=50
        var resultDefault = await _handler.HandleAsync(tenantId, resourceId);
        // Act: take=10
        var resultLimited = await _handler.HandleAsync(tenantId, resourceId, take: 10);

        // Assert
        Assert.Equal(50, resultDefault.Count);
        Assert.Equal(10, resultLimited.Count);
    }

    [Fact]
    public async Task HandleAsync_ValidJsonDetails_DeserializedToMetadata()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var jsonDetails = """{"key":"value","nested":{"data":"test"}}""";

        var log = AuditLog.Create(
            tenantId, userId, "UpdateProcessingActivityNode", "ProcessingActivity",
            resourceId, details: jsonDetails, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.NotNull(vm.Metadata);
        // Metadata should be deserialized object
        Assert.IsType<JsonElement>(vm.Metadata); // System.Text.Json deserializes to JsonElement by default
    }

    [Fact]
    public async Task HandleAsync_InvalidJsonDetails_MetadataNull()
    {
        // Arrange: Details is not valid JSON
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invalidJson = "{ not valid json";

        var log = AuditLog.Create(
            tenantId, userId, "UpdateProcessingActivityNode", "ProcessingActivity",
            resourceId, details: invalidJson, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Handler should not crash; metadata should be null
        Assert.Single(result);
        var vm = result[0];
        Assert.Null(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_EmptyDetails_MetadataNull()
    {
        // Arrange: Details is null or empty
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var log = AuditLog.Create(
            tenantId, userId, "ApproveProcessingActivity", "ProcessingActivity",
            resourceId, details: null, ipAddress: null, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Null(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_AllAuditEventTypeVariants_ParseCorrectly()
    {
        // Arrange: Test all enum variants map correctly
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var actions = new[]
        {
            "CreateProcessingActivity",
            "UpdateProcessingActivityNode",
            "SubmitProcessingActivityForReview",
            "ApproveProcessingActivity",
            "ActivateProcessingActivity",
            "ArchiveProcessingActivity",
            "ValidateEvidence",
            "RejectEvidence",
            "AcceptGapWithRisk",
            "GenerateOfficialExport"
        };

        var logs = actions
            .Select(action => AuditLog.Create(
                tenantId, userId, action, "ProcessingActivity",
                resourceId, severity: AuditSeverity.Info))
            .ToList();

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(logs);

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: All should map successfully
        Assert.Equal(actions.Length, result.Count);
        var resultedTypes = result.Select(r => r.EventType).ToList();
        foreach (var action in actions)
        {
            Assert.Contains(action, resultedTypes);
        }
    }

    [Fact]
    public async Task HandleAsync_NegativeSkip_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _handler.HandleAsync(tenantId, resourceId, skip: -1));
    }

    [Fact]
    public async Task HandleAsync_TakeZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _handler.HandleAsync(tenantId, resourceId, take: 0));
    }

    [Fact]
    public async Task HandleAsync_NegativeTake_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _handler.HandleAsync(tenantId, resourceId, take: -5));
    }

    [Fact]
    public async Task HandleAsync_MixedValidAndInvalidActions_OnlyValidMapped()
    {
        // Arrange: Mix of valid and invalid actions
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var log1 = AuditLog.Create(tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);
        var log2 = AuditLog.Create(tenantId, userId, "invalid.action.type", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);
        var log3 = AuditLog.Create(tenantId, userId, "ActivateProcessingActivity", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log1, log2, log3 });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Only the 2 valid actions should be returned
        Assert.Equal(2, result.Count);
        Assert.All(result, vm => Assert.NotNull(vm.EventType));
    }
}

/// <summary>
/// Tests for the TimelineQueryService wrapper.
/// </summary>
public class TimelineQueryServiceTests
{
    private readonly IAuditLogRepository _repository = Substitute.For<IAuditLogRepository>();
    private readonly TimelineQueryService _service;

    public TimelineQueryServiceTests()
    {
        _service = new TimelineQueryService(_repository);
    }

    [Fact]
    public async Task GetTimelineAsync_DelegatesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var log = AuditLog.Create(tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _service.GetTimelineAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        Assert.Equal("CreateProcessingActivity", result[0].EventType);
    }

    [Fact]
    public async Task GetTimelineAsync_WithCustomResourceFilter_DelegatesCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var log = AuditLog.Create(tenantId, userId, "ValidateEvidence", "Evidence",
            resourceId, severity: AuditSeverity.Info);

        _repository.GetByResourceAsync(tenantId, "Evidence", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _service.GetTimelineAsync(tenantId, resourceId, "Evidence");

        // Assert
        Assert.Single(result);
    }
}
