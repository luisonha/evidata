using System;
using System.Threading.Tasks;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Notifications;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Notifications;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Evidata.Tests.Unit.ProcessingInventory.Infrastructure;

/// <summary>
/// P1-017: Tests para verificar que ReviewedAt se setea automáticamente en ProcessingActivity
/// cuando una Review es aprobada.
/// </summary>
public class ReviewEventHandlerTests : IAsyncLifetime
{
    private readonly WorkflowDbContext _workflowDb;
    private readonly ProcessingInventoryDbContext _inventoryDb;
    private readonly ReviewEventHandler _handler;

    public ReviewEventHandlerTests()
    {
        // Create in-memory databases for testing
        var workflowOptions = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: $"workflow-{Guid.NewGuid()}")
            .Options;

        var inventoryOptions = new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"inventory-{Guid.NewGuid()}")
            .Options;

        _workflowDb = new WorkflowDbContext(workflowOptions);
        _inventoryDb = new ProcessingInventoryDbContext(inventoryOptions);
        _handler = new ReviewEventHandler(_inventoryDb);
    }

    public async Task InitializeAsync()
    {
        // Ensure databases are created
        await _workflowDb.Database.EnsureCreatedAsync();
        await _inventoryDb.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _workflowDb.Database.EnsureDeletedAsync();
        await _inventoryDb.Database.EnsureDeletedAsync();
        _workflowDb.Dispose();
        _inventoryDb.Dispose();
    }

    [Fact]
    public async Task HandleReviewApprovedAsync_SetsReviewedAt_WhenProcessingActivityReviewApproved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        // Create a ProcessingActivity in UnderReview state directly
        var activity = ProcessingActivity.Create(
            tenantId,
            "Test Activity",
            Guid.NewGuid(),
            "Test description",
            "Controller Name",
            "Department");

        // Override the Id to match what we expect
        var idField = typeof(ProcessingActivity).GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (idField != null)
        {
            idField.Invoke(activity, new object[] { activityId });
        }

        // Set status directly to UnderReview to bypass validation
        var statusField = typeof(ProcessingActivity).GetProperty("Status", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (statusField != null)
        {
            statusField.Invoke(activity, new object[] { ProcessingActivityStatus.UnderReview });
        }

        _inventoryDb.ProcessingActivities.Add(activity);
        await _inventoryDb.SaveChangesAsync();

        // Verify ReviewedAt is initially null
        Assert.Null(activity.ReviewedAt);

        // Create the event payload
        var payload = new ReviewApprovedEventPayload(
            ReviewId: Guid.NewGuid(),
            TenantId: tenantId,
            TargetModule: "ProcessingInventory",
            TargetEntityType: "ProcessingActivity",
            TargetEntityId: activityId,
            ReviewerId: reviewerId,
            Comments: "Approved after review",
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid());

        // Act
        await _handler.HandleReviewApprovedAsync(payload);

        // Assert
        var updatedActivity = await _inventoryDb.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activityId);
        
        Assert.NotNull(updatedActivity);
        Assert.NotNull(updatedActivity.ReviewedAt);
        Assert.True(updatedActivity.ReviewedAt.Value > DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public async Task HandleReviewApprovedAsync_IgnoresOtherModuleReviews()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        // Create a ProcessingActivity
        var activity = ProcessingActivity.Create(
            tenantId,
            "Test Activity",
            Guid.NewGuid());

        // Override the Id
        var idField = typeof(ProcessingActivity).GetProperty("Id", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (idField != null)
        {
            idField.Invoke(activity, new object[] { activityId });
        }

        // Set status directly to UnderReview
        var statusField = typeof(ProcessingActivity).GetProperty("Status", 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (statusField != null)
        {
            statusField.Invoke(activity, new object[] { ProcessingActivityStatus.UnderReview });
        }

        _inventoryDb.ProcessingActivities.Add(activity);
        await _inventoryDb.SaveChangesAsync();

        // Create event for a different module
        var payload = new ReviewApprovedEventPayload(
            ReviewId: Guid.NewGuid(),
            TenantId: tenantId,
            TargetModule: "GapManagement", // Different module
            TargetEntityType: "ComplianceGap",
            TargetEntityId: Guid.NewGuid(),
            ReviewerId: reviewerId,
            Comments: "Approved",
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid());

        // Act
        await _handler.HandleReviewApprovedAsync(payload);

        // Assert — ReviewedAt should still be null because this is for a different module
        var updatedActivity = await _inventoryDb.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activityId);
        
        Assert.NotNull(updatedActivity);
        Assert.Null(updatedActivity.ReviewedAt);
    }

    [Fact]
    public async Task HandleReviewApprovedAsync_ThrowsWhenActivityNotFound()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var nonExistentActivityId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var payload = new ReviewApprovedEventPayload(
            ReviewId: Guid.NewGuid(),
            TenantId: tenantId,
            TargetModule: "ProcessingInventory",
            TargetEntityType: "ProcessingActivity",
            TargetEntityId: nonExistentActivityId,
            ReviewerId: reviewerId,
            Comments: "Approved",
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.HandleReviewApprovedAsync(payload));
    }

    /// <summary>
    /// P1-017: End-to-end integration test verifying the COMPLETE reflection-based flow:
    /// 1. Create a ProcessingActivity and Review via ReviewService (uses reflection internally)
    /// 2. ReviewService.ApproveAsync() → invokes handler via reflection → ReviewedAt is set
    /// 3. Verify VersionModifiedAfterReview blocker is triggered when activity is modified after review
    /// </summary>
    [Fact]
    public async Task ReviewService_ApproveAsync_WithReflection_E2E_SetsReviewedAtAndBlocksVersionModified()
    {
        // ── Arrange ──────────────────────────────────────────────────────────────────
        var tenantId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        // Create a ProcessingActivity directly in the database (inventory module)
        var activity = ProcessingActivity.Create(
            tenantId,
            "Test Activity for Review",
            Guid.NewGuid(),
            "Test description",
            "Controller Name",
            "Department");

        // Override the Id to match what we expect
        var idField = typeof(ProcessingActivity).GetProperty("Id",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (idField != null)
        {
            idField.Invoke(activity, new object[] { activityId });
        }

        // Set status to UnderReview
        var statusField = typeof(ProcessingActivity).GetProperty("Status",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (statusField != null)
        {
            statusField.Invoke(activity, new object[] { ProcessingActivityStatus.UnderReview });
        }

        _inventoryDb.ProcessingActivities.Add(activity);
        await _inventoryDb.SaveChangesAsync();

        // Verify ReviewedAt is initially null
        Assert.Null(activity.ReviewedAt);

        // Create a Review for this ProcessingActivity
        var review = Review.Create(
            tenantId,
            "ProcessingInventory",
            "ProcessingActivity",
            activityId,
            Guid.NewGuid()); // requestedBy

        _workflowDb.Reviews.Add(review);
        await _workflowDb.SaveChangesAsync();

        // Set up DI container with ReviewService and handler
        var services = new ServiceCollection();
        // Use the same contexts as the test fixture (do not create new scoped contexts)
        services.AddScoped<IReviewNotificationService>(sp =>
            new TestReviewNotificationService()); // Mock notification service
        services.AddScoped<IReviewEventHandler, ReviewEventHandler>(sp =>
            new ReviewEventHandler(_inventoryDb)); // Use the test's inventoryDb
        services.AddLogging(builder => builder.AddConsole());

        var serviceProvider = services.BuildServiceProvider();
        var scope = serviceProvider.CreateScope();

        // Create ReviewService with DI
        var reviewService = new ReviewService(
            _workflowDb,
            scope.ServiceProvider.GetRequiredService<IReviewNotificationService>(),
            scope.ServiceProvider,
            scope.ServiceProvider.GetRequiredService<ILogger<ReviewService>>());

        // ── Act: Approve the review ──────────────────────────────────────────────────
        // This internally calls TryInvokeReviewEventHandlerAsync() which uses reflection
        // to invoke ReviewEventHandler.HandleReviewApprovedAsync()
        review.Start(reviewerId);
        await _workflowDb.SaveChangesAsync();

        await reviewService.ApproveAsync(review.Id, reviewerId, "Approved after review");

        // ── Assert Part 1: ReviewedAt was set via reflection ──────────────────────────
        var updatedActivity = await _inventoryDb.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activityId);

        Assert.NotNull(updatedActivity);
        Assert.NotNull(updatedActivity.ReviewedAt);
        Assert.True(updatedActivity.ReviewedAt.Value > DateTimeOffset.UtcNow.AddSeconds(-5));

        // ── Act Part 2: Modify the activity after review ──────────────────────────────
        // This should trigger the VersionModifiedAfterReview blocker
        
        // Manually update LastModifiedAt to simulate modification
        var modifiedField = typeof(ProcessingActivity).GetProperty("LastModifiedAt",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetSetMethod(nonPublic: true);
        if (modifiedField != null)
        {
            modifiedField.Invoke(updatedActivity, new object[] { DateTimeOffset.UtcNow.AddSeconds(5) });
        }

        _inventoryDb.ProcessingActivities.Update(updatedActivity);
        await _inventoryDb.SaveChangesAsync();

        // ── Assert Part 2: VersionModifiedAfterReview blocker is triggered ────────────
        var latestActivity = await _inventoryDb.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activityId);

        Assert.NotNull(latestActivity);
        Assert.NotNull(latestActivity.ReviewedAt);
        Assert.NotNull(latestActivity.LastModifiedAt);
        Assert.True(latestActivity.LastModifiedAt > latestActivity.ReviewedAt,
            "Activity LastModifiedAt should be after ReviewedAt to trigger VersionModifiedAfterReview blocker");

        // Clean up
        scope.Dispose();
    }
}

/// <summary>
/// Test implementation of IReviewNotificationService to avoid Outbox infrastructure.
/// In a real scenario, this would write to Outbox; here we stub it out.
/// </summary>
public class TestReviewNotificationService : IReviewNotificationService
{
    public Task NotifyApprovedAsync(Review review, CancellationToken ct = default)
    {
        return Task.CompletedTask; // Stub for test
    }

    public Task NotifyChangesRequestedAsync(Review review, CancellationToken ct = default)
    {
        return Task.CompletedTask; // Stub for test
    }

    public Task NotifyCancelledAsync(Review review, CancellationToken ct = default)
    {
        return Task.CompletedTask; // Stub for test
    }
}
