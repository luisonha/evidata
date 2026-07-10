using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Application.ViewModels;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.ProcessingInventory.Application.Queries;

public class GetProcessingActivityControlQueryHandlerTests
{
    private static ProcessingInventoryDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProcessingInventoryTestDb_{Guid.NewGuid()}")
            .Options;

        return new ProcessingInventoryDbContext(options);
    }

    private static ProcessingActivity CreateTestActivity(
        Guid tenantId,
        string name = "Test Activity",
        ProcessingActivityStatus status = ProcessingActivityStatus.Draft,
        int version = 1)
    {
        var userId = Guid.NewGuid();
        var activity = ProcessingActivity.Create(tenantId, name, userId, "Test Description", "Controller", "Department");
        
        // Set version and status if needed for specific test scenarios
        var statusProp = activity.GetType().GetProperty("Status");
        if (statusProp != null && status != ProcessingActivityStatus.Draft)
        {
            // Note: This is a simplified approach for testing. In real scenarios, 
            // use domain methods to transition status if available.
            statusProp.SetValue(activity, status);
        }

        return activity;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC1: Happy Path — Returns valid ProcessingActivityControlViewModel
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithValidActivity_ReturnsProcessingActivityControlViewModel()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Sample Activity", ProcessingActivityStatus.Draft);
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessingActivityControlViewModel>(result);
        Assert.Equal(processingActivityId, result.ProcessingActivity.Id);
        Assert.Equal(tenantId, result.ProcessingActivity.TenantId);
        Assert.Equal("Sample Activity", result.ProcessingActivity.Name);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC2: Null Case — Returns null when activity does not exist
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithNonExistentActivity_ReturnsNull()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC3: Tenant Isolation — Returns null when TenantId does not match
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithMismatchedTenantId_ReturnsNull()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var differentTenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Sample Activity");
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act - query with different tenant ID
        var result = await handler.HandleAsync(differentTenantId, processingActivityId, userId);

        // Assert
        Assert.Null(result);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC4: Status Mapping — Verifies all ProcessingActivityStatus enum values 
    //      map correctly to ProcessingActivityVersionStatus
    // ─────────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(ProcessingActivityStatus.Draft, ProcessingActivityVersionStatus.Draft)]
    [InlineData(ProcessingActivityStatus.UnderReview, ProcessingActivityVersionStatus.InReview)]
    [InlineData(ProcessingActivityStatus.Approved, ProcessingActivityVersionStatus.Approved)]
    [InlineData(ProcessingActivityStatus.Archived, ProcessingActivityVersionStatus.Archived)]
    public async Task HandleAsync_WithVariousStatuses_MapsStatusCorrectly(
        ProcessingActivityStatus domainStatus,
        ProcessingActivityVersionStatus expectedViewModelStatus)
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Status Test Activity", domainStatus);
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedViewModelStatus, result.Version.Status);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC5: Stub Data — Verifies all stub collections and counts are initialized correctly
    //      (empty collections, zero counts)
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithValidActivity_ReturnsStubsWithZeroCountsAndEmptyCollections()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Stub Test Activity");
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);

        // Evidence summary stub - all counts should be 0 or reflect initial state
        Assert.Equal(0, result.EvidenceSummary.TotalRequirements);
        Assert.Equal(0, result.EvidenceSummary.PendingCount);
        Assert.Equal(0, result.EvidenceSummary.AttachedCount);

        // Gap summary stub - all counts should be 0
        Assert.Equal(0, result.GapSummary.TotalCount);
        Assert.Equal(0, result.GapSummary.OpenCount);
        Assert.Equal(0, result.GapSummary.ResolvedCount);

        // Review summary stub - empty required domains
        Assert.NotNull(result.ReviewSummary);
        Assert.Empty(result.ReviewSummary.RequiredDomains ?? []);

        // Timeline stub - empty
        Assert.NotNull(result.Timeline);
        Assert.Empty(result.Timeline);

        // Priority actions stub - empty
        Assert.NotNull(result.PriorityActions);
        Assert.Empty(result.PriorityActions);

        // Blocked actions stub - empty
        Assert.NotNull(result.BlockedActions);
        Assert.Empty(result.BlockedActions);

        // Operational map stub - empty node list
        Assert.NotNull(result.OperationalMap);
        Assert.Empty(result.OperationalMap.Nodes);

        // Exports stub - empty
        Assert.NotNull(result.Exports);
        Assert.Empty(result.Exports);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC6: Localization Keys — Verifies status localization keys are formatted correctly
    // ─────────────────────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(ProcessingActivityStatus.Draft, "status.draft", "version.status.draft")]
    [InlineData(ProcessingActivityStatus.UnderReview, "status.underReview", "version.status.underReview")]
    [InlineData(ProcessingActivityStatus.Approved, "status.approved", "version.status.approved")]
    [InlineData(ProcessingActivityStatus.Archived, "status.archived", "version.status.archived")]
    public async Task HandleAsync_WithVariousStatuses_GeneratesCorrectLocalizationKeys(
        ProcessingActivityStatus domainStatus,
        string expectedDetailStatusKey,
        string expectedVersionStatusKey)
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Localization Test Activity", domainStatus);
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDetailStatusKey, result.ProcessingActivity.StatusLabelKey);
        Assert.Equal(expectedVersionStatusKey, result.Version.StatusLabelKey);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC7: Resource Permissions Stub — Verifies empty user roles and correct ReadOnly flag
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithValidActivity_ReturnsResourcePermissionsWithEmptyRolesAndReadOnlyFalse()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Permissions Test Activity");
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Permissions);
        Assert.Empty(result.Permissions.RoleCodes);
        Assert.False(result.Permissions.ReadOnly);
        Assert.NotNull(result.Permissions.AvailableActions);
        Assert.Empty(result.Permissions.AvailableActions);
        Assert.NotNull(result.Permissions.BlockedActions);
        Assert.Empty(result.Permissions.BlockedActions);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // TC8: Control Tower Stub — Verifies ControlTower is initialized with correct values
    // ─────────────────────────────────────────────────────────────────────────────
    [Fact]
    public async Task HandleAsync_WithValidActivity_ReturnsControlTowerWithZeroCompletionPercentage()
    {
        // Arrange
        var db = CreateInMemoryDbContext();
        var handler = new GetProcessingActivityControlQueryHandler(db);

        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var activity = CreateTestActivity(tenantId, "Control Tower Test Activity");
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Act
        var result = await handler.HandleAsync(tenantId, processingActivityId, userId);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.ControlTower);
        Assert.Equal(0m, result.ControlTower.CompletionPercentage);
        Assert.Empty(result.ControlTower.AvailableActions);
        Assert.Empty(result.ControlTower.BlockedActions);
    }
}
