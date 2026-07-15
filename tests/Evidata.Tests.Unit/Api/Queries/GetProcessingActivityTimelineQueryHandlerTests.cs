using Xunit;
using System.Text.Json;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Audit.Application.Queries;
using Evidata.Modules.Audit.Domain;

namespace Evidata.Tests.Unit.Api.Queries;

/// <summary>
/// Tests for P1-010: TimelineEvent projection from AuditLog.
/// Verifies that all 10 critical auditable actions are correctly projected to timeline.
/// 
/// Test codes (per contract):
/// - AUD-PA-001: CreateProcessingActivity → timeline event
/// - AUD-NODE-001: UpdateNode → timeline event
/// - AUD-REV-001: SubmitForReview → timeline event (P1, not yet implemented)
/// - AUD-APP-001: Approve → timeline event (P1, in progress)
/// - AUD-ACT-001: Activate → timeline event (P1, not yet implemented)
/// - AUD-ARC-001: Archive → timeline event (P1, not yet implemented)
/// - AUD-EV-001: ValidateEvidence → timeline event (P1, not yet implemented)
/// - AUD-EV-002: RejectEvidence → timeline event (P1, not yet implemented)
/// - AUD-GAP-001: AcceptGapWithRisk → timeline event (P1, not yet implemented)
/// - AUD-EXP-001: GenerateOfficialExport → timeline event (P1, not yet implemented)
/// </summary>
public class GetProcessingActivityTimelineQueryHandlerTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _actorUserId = Guid.NewGuid();
    private readonly Guid _processingActivityId = Guid.NewGuid();

    /// <summary>Mock repository that stores audit logs in memory.</summary>
    private class MockAuditLogRepository : IAuditLogRepository
    {
        private readonly List<AuditLog> _logs;

        public MockAuditLogRepository(IReadOnlyList<AuditLog> logs) => _logs = logs.ToList();

        public Task AddAsync(AuditLog entry, CancellationToken ct = default)
        {
            _logs.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<AuditLog>> GetByTenantAsync(Guid tenantId, int page = 1, int pageSize = 50, CancellationToken ct = default)
        {
            var result = _logs
                .Where(l => l.TenantId == tenantId)
                .OrderByDescending(l => l.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<IReadOnlyList<AuditLog>>(result);
        }

        public Task<IReadOnlyList<AuditLog>> GetByResourceAsync(Guid tenantId, string resource, Guid resourceId, CancellationToken ct = default)
        {
            var result = _logs
                .Where(l => l.TenantId == tenantId && l.Resource == resource && l.ResourceId == resourceId)
                .OrderByDescending(l => l.OccurredAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<AuditLog>>(result);
        }

        public Task<(IReadOnlyList<AuditLog> Events, int TotalCount)> GetByTenantWithFiltersAsync(
            Guid tenantId,
            string? eventType = null,
            Guid? actorUserId = null,
            Guid? targetUserId = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int page = 1,
            int pageSize = 50,
            CancellationToken ct = default)
        {
            var query = _logs
                .Where(x => x.TenantId == tenantId);

            if (!string.IsNullOrWhiteSpace(eventType))
                query = query.Where(x => x.EventType == eventType);

            if (actorUserId.HasValue)
                query = query.Where(x => x.UserId == actorUserId.Value);

            if (targetUserId.HasValue)
                query = query.Where(x => x.ResourceId == targetUserId.Value);

            if (fromDate.HasValue)
                query = query.Where(x => x.OccurredAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(x => x.OccurredAt <= toDate.Value);

            var totalCount = query.Count();
            var events = query
                .OrderByDescending(x => x.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Task.FromResult<(IReadOnlyList<AuditLog>, int)>((events, totalCount));
        }
    }

    private GetProcessingActivityTimelineQueryHandler CreateHandler(IReadOnlyList<AuditLog> logs)
    {
        var repository = new MockAuditLogRepository(logs);
        return new GetProcessingActivityTimelineQueryHandler(repository);
    }

    /// <summary>
    /// AUD-PA-001: Verify CreateProcessingActivity event appears in timeline with correct shape.
    /// </summary>
    [Fact]
    public async Task CreateProcessingActivity_ShouldProjectToTimeline()
    {
        // Arrange: Create audit event for CreateProcessingActivity
        var createEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Success,
            metadata: new Dictionary<string, object?>
            {
                { "name", "Test Activity" },
                { "description", "Test description" },
                { "controller", "John Doe" },
                { "department", "Compliance" }
            });

        var handler = CreateHandler(new[] { createEvent });

        // Act: Query timeline for ProcessingActivity
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId,
            resource: "ProcessingActivity");

        // Assert: Event should appear with correct fields
        var evt = Assert.Single(events);
        Assert.Equal("CreateProcessingActivity", evt.EventType);
        Assert.Equal("ProcessingActivity", evt.ResourceType);
        Assert.Equal(_processingActivityId.ToString(), evt.ResourceId);
        Assert.Equal(_actorUserId.ToString(), evt.ActorUserId);
        Assert.Equal("Success", evt.Result);
        Assert.NotNull(evt.Metadata);
    }

    /// <summary>
    /// AUD-NODE-001: Verify UpdateNode event appears in timeline with correct shape.
    /// </summary>
    [Fact]
    public async Task UpdateNode_ShouldProjectToTimeline()
    {
        // Arrange: Create audit event for UpdateNode
        var updateEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.UpdateNode.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Success,
            metadata: new Dictionary<string, object?>
            {
                { "name", "Updated Activity" },
                { "controller", "Jane Doe" }
            });

        var handler = CreateHandler(new[] { updateEvent });

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert
        var evt = Assert.Single(events);
        Assert.Equal("UpdateNode", evt.EventType);
        Assert.Equal("Success", evt.Result);
    }

    /// <summary>
    /// Verify Approve event appears in timeline with correct shape.
    /// AUD-APP-001
    /// </summary>
    [Fact]
    public async Task Approve_ShouldProjectToTimeline()
    {
        // Arrange: Create audit event for Approve
        var approveEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.Approve.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Success,
            metadata: new Dictionary<string, object?>
            {
                { "snapshotVersion", 1 },
                { "retentionRequired", false }
            });

        var handler = CreateHandler(new[] { approveEvent });

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert
        var evt = Assert.Single(events);
        Assert.Equal("Approve", evt.EventType);
        Assert.Equal("Success", evt.Result);
    }

    /// <summary>
    /// Verify pagination works correctly: skip and take parameters are applied.
    /// </summary>
    [Fact]
    public async Task Pagination_ShouldWorkCorrectly()
    {
        // Arrange: Create 5 audit events
        var logs = new List<AuditLog>();
        for (int i = 0; i < 5; i++)
        {
            var evt = AuditLog.Create(
                tenantId: _tenantId,
                userId: _actorUserId,
                eventType: AuditEventType.CreateProcessingActivity.ToString(),
                resource: "ProcessingActivity",
                resourceId: _processingActivityId,
                metadata: new Dictionary<string, object?> { { "index", i } });

            logs.Add(evt);
        }

        var handler = CreateHandler(logs);

        // Act: Query with skip=2, take=2
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId,
            skip: 2,
            take: 2);

        // Assert: Should return 2 events (5 total - 2 skip = 3 available, take 2)
        Assert.Equal(2, events.Count);
    }

    /// <summary>
    /// Verify events are ordered by OccurredAt descending (most recent first).
    /// </summary>
    [Fact]
    public async Task ChronologicalOrder_ShouldBeMostRecentFirst()
    {
        // Arrange: Create 3 events with staggered creation times using occurredAtOverride
        var now = DateTime.UtcNow;
        
        var createLog = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            occurredAtOverride: now.AddSeconds(-20));

        var updateLog = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.UpdateNode.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            occurredAtOverride: now.AddSeconds(-10));

        var approveLog = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.Approve.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            occurredAtOverride: now);

        var logs = new[] { createLog, updateLog, approveLog };
        var handler = CreateHandler(logs);

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: Events should be ordered by OccurredAt descending
        // Most recent (approveLog - Approve) should be first
        Assert.Equal("Approve", events[0].EventType);
        Assert.Equal("UpdateNode", events[1].EventType);
        Assert.Equal("CreateProcessingActivity", events[2].EventType);
    }

    /// <summary>
    /// Verify tenant isolation: events from other tenants are NOT returned.
    /// </summary>
    [Fact]
    public async Task TenantIsolation_ShouldNotLeakBetweenTenants()
    {
        // Arrange: Create events for two different tenants
        var tenant1Id = Guid.NewGuid();
        var tenant2Id = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var tenant1Event = AuditLog.Create(
            tenantId: tenant1Id,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: resourceId);

        var tenant2Event = AuditLog.Create(
            tenantId: tenant2Id,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: resourceId);

        var handler = CreateHandler(new[] { tenant1Event, tenant2Event });

        // Act: Query timeline for tenant1
        var events = await handler.HandleAsync(
            tenantId: tenant1Id,
            resourceId: resourceId);

        // Assert: Only tenant1's event should be returned
        var evt = Assert.Single(events);
        Assert.Equal(resourceId.ToString(), evt.ResourceId);
    }

    /// <summary>
    /// Verify malformed metadata JSON is handled gracefully without breaking the endpoint.
    /// </summary>
    [Fact]
    public async Task MalformedMetadata_ShouldNotBreakTimeline()
    {
        // Arrange: Create event with invalid JSON metadata using metadataJsonRaw parameter
        var evt = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            metadataJsonRaw: "{invalid json}");

        var handler = CreateHandler(new[] { evt });

        // Act: Query timeline - should not throw
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: Event should still be returned, but Metadata should be null/handled gracefully
        var timelineEvent = Assert.Single(events);
        // Metadata should be null since the JSON was invalid and couldn't be deserialized
        Assert.Null(timelineEvent.Metadata);
    }

    /// <summary>
    /// Verify that events with unmappable EventType are excluded from timeline.
    /// </summary>
    [Fact]
    public async Task UnmappableEventType_ShouldBeExcludedFromTimeline()
    {
        // Arrange: Create event with invalid EventType
        var invalidEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: "UnknownEventType",
            resource: "ProcessingActivity",
            resourceId: _processingActivityId);

        var validEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId);

        var handler = CreateHandler(new[] { invalidEvent, validEvent });

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: Only valid event should be returned
        var evt = Assert.Single(events);
        Assert.Equal("CreateProcessingActivity", evt.EventType);
    }

    /// <summary>
    /// Verify that system actions (userId = null) are mapped to Guid.Empty placeholder.
    /// </summary>
    [Fact]
    public async Task SystemAction_NullUserId_MappedToGuidEmpty()
    {
        // Arrange: Create event with null userId (system action)
        var systemEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: null, // System action
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId);

        var handler = CreateHandler(new[] { systemEvent });

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: ActorUserId should be Guid.Empty as string
        var evt = Assert.Single(events);
        Assert.Equal(Guid.Empty.ToString(), evt.ActorUserId);
    }

    /// <summary>
    /// Verify result field is correctly mapped from AuditEventResult enum.
    /// </summary>
    [Fact]
    public async Task ResultField_ShouldMapCorrectly()
    {
        // Arrange: Create events with different result values
        var successEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.CreateProcessingActivity.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Success);

        var failureEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.UpdateNode.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Failure);

        var blockedEvent = AuditLog.Create(
            tenantId: _tenantId,
            userId: _actorUserId,
            eventType: AuditEventType.Approve.ToString(),
            resource: "ProcessingActivity",
            resourceId: _processingActivityId,
            result: AuditEventResult.Blocked);

        var handler = CreateHandler(new[] { successEvent, failureEvent, blockedEvent });

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: Results should be mapped correctly
        Assert.Contains(events, e => e.Result == "Success");
        Assert.Contains(events, e => e.Result == "Failure");
        Assert.Contains(events, e => e.Result == "Blocked");
    }

    /// <summary>
    /// Verify empty result when no events exist for a resource.
    /// </summary>
    [Fact]
    public async Task NoEvents_ShouldReturnEmptyList()
    {
        // Arrange: Empty log list
        var handler = CreateHandler(Array.Empty<AuditLog>());

        // Act: Query timeline
        var events = await handler.HandleAsync(
            tenantId: _tenantId,
            resourceId: _processingActivityId);

        // Assert: Empty list
        Assert.Empty(events);
    }
}
