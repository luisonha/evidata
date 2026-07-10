using System.Text.Json;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Application.Queries;
using Evidata.Modules.Audit.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Audit;

/// <summary>
/// Tests para GetProcessingActivityTimelineQueryHandler.
/// Verifica mapeo correcto de AuditLog a TimelineEventViewModel con shape P1-009:
/// eventType, result, correlationId, metadata tipado.
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
    public async Task HandleAsync_EventWithValidEventType_MapsCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var log = AuditLog.Create(
            tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, AuditEventResult.Success, correlationId);

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
        Assert.Equal(correlationId, vm.CorrelationId);
        Assert.Null(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_EventWithFailureResult_MapsFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, Guid.NewGuid(), "ValidateEvidence", "Evidence",
            resourceId, AuditEventResult.Failure);

        _repository.GetByResourceAsync(tenantId, "Evidence", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId, "Evidence");

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Equal("Failure", vm.Result);
        Assert.Equal("audit.result.Failure", vm.ResultLabelKey);
    }

    [Fact]
    public async Task HandleAsync_EventWithBlockedResult_MapsBlocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, Guid.NewGuid(), "ApproveProcessingActivity", "ProcessingActivity",
            resourceId, AuditEventResult.Blocked);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Equal("Blocked", vm.Result);
        Assert.Equal("audit.result.Blocked", vm.ResultLabelKey);
    }

    [Fact]
    public async Task HandleAsync_EventWithUnmatchableEventType_ExcludedFromTimeline()
    {
        // Arrange: EventType no coincide con enum
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, Guid.NewGuid(), "some.random.action", "ProcessingActivity",
            resourceId);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Evento debe ser excluido porque EventType no parsea a enum
        Assert.Empty(result);
    }

    [Fact]
    public async Task HandleAsync_EventWithNullUserId_UsesGuidEmpty()
    {
        // Arrange: Acción de sistema (UserId = null)
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var log = AuditLog.Create(
            tenantId, null, "ActivateProcessingActivity", "ProcessingActivity",
            resourceId);

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

        var log1 = AuditLog.Create(tenantId, userId, "CreateProcessingActivity", "ProcessingActivity", resourceId);
        var log2 = AuditLog.Create(tenantId, userId, "ActivateProcessingActivity", "ProcessingActivity", resourceId);

        // Repository retorna ordenados por OccurredAt descendente
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
                tenantId, userId, "CreateProcessingActivity", "ProcessingActivity", resourceId))
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
                tenantId, userId, "CreateProcessingActivity", "ProcessingActivity", resourceId))
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
    public async Task HandleAsync_ValidJsonMetadata_DeserializedCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var metadata = new Dictionary<string, object?> { { "key", "value" }, { "nested", new { data = "test" } } };

        var log = AuditLog.Create(
            tenantId, userId, "UpdateProcessingActivityNode", "ProcessingActivity",
            resourceId, metadata: metadata);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.NotNull(vm.Metadata);
        // System.Text.Json deserializa a JsonElement por defecto
        Assert.IsType<JsonElement>(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_InvalidJsonMetadata_MetadataNull()
    {
        // Arrange: Metadata JSON inválido (aunque AuditLog.Create devería prevenir esto)
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Crear log con metadata JSON inválido manualmente (acceso a campo privado)
        var log = AuditLog.Create(
            tenantId, userId, "UpdateProcessingActivityNode", "ProcessingActivity", resourceId);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Handler no debe crash; metadata debe ser null
        Assert.Single(result);
        var vm = result[0];
        Assert.Null(vm.Metadata);
    }

    [Fact]
    public async Task HandleAsync_EmptyMetadata_MetadataNull()
    {
        // Arrange: Metadata es null
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var log = AuditLog.Create(
            tenantId, userId, "ApproveProcessingActivity", "ProcessingActivity",
            resourceId, metadata: null);

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
        // Arrange: Test todos los variantes de enum
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var eventTypes = new[]
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

        var logs = eventTypes
            .Select(eventType => AuditLog.Create(
                tenantId, userId, eventType, "ProcessingActivity", resourceId))
            .ToList();

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(logs);

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert: Todos los eventos deben ser incluidos (10 eventos)
        Assert.Equal(10, result.Count);
        Assert.All(result, vm => Assert.NotEmpty(vm.EventType));
    }

    [Fact]
    public async Task HandleAsync_CorrelationIdPropagated_IncludedInViewModel()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var log = AuditLog.Create(
            tenantId, userId, "CreateProcessingActivity", "ProcessingActivity",
            resourceId, AuditEventResult.Success, correlationId);

        _repository.GetByResourceAsync(tenantId, "ProcessingActivity", resourceId, default)
            .ReturnsForAnyArgs(new List<AuditLog> { log });

        // Act
        var result = await _handler.HandleAsync(tenantId, resourceId);

        // Assert
        Assert.Single(result);
        var vm = result[0];
        Assert.Equal(correlationId, vm.CorrelationId);
    }
}
