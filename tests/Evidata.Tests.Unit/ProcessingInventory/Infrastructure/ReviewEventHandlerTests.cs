using System;
using System.Threading.Tasks;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Notifications;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Application.Notifications;
using Evidata.Modules.Workflow.Domain;
using Evidata.Modules.Workflow.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Infrastructure.Reviews;
using Microsoft.EntityFrameworkCore;
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
}
