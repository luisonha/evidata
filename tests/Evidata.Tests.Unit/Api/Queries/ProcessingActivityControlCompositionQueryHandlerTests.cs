using NSubstitute;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Evidata.Api.Queries;
using Evidata.Modules.ProcessingInventory.Application.ViewModels;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.GapManagement.Application.Queries;
using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Application.Queries;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Application.DTOs;
using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Security.Application.Abstractions;

namespace Evidata.Tests.Unit.Api.Queries;

/// <summary>
/// Behavioral tests for ProcessingActivityControlCompositionQueryHandler.
/// Verifies orchestration, tenant isolation, P1-004 security permissions, and error handling.
/// Tests use NSubstitute to mock all 6 module services.
/// </summary>
public class ProcessingActivityControlCompositionQueryHandlerTests
{
    private readonly ProcessingInventoryDbContext _dbContext;
    private readonly IEvidenceSummaryQueryService _evidenceService;
    private readonly IGapSummaryQueryService _gapService;
    private readonly IReviewSummaryQueryService _reviewService;
    private readonly ITimelineQueryService _timelineService;
    private readonly IExportOptionsQueryService _exportService;
    private readonly IResourcePermissionsQueryService _permissionsService;
    private readonly ILogger<ProcessingActivityControlCompositionQueryHandler> _logger;
    private readonly GetProcessingActivityControlQueryHandler _stubHandler;
    private readonly ProcessingActivityControlCompositionQueryHandler _handler;

    public ProcessingActivityControlCompositionQueryHandlerTests()
    {
        // Create an in-memory database for the base handler
        var options = new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
            .UseInMemoryDatabase(databaseName: $"CompositionTestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new ProcessingInventoryDbContext(options);

        // Create mocks for all service interfaces
        _evidenceService = Substitute.For<IEvidenceSummaryQueryService>();
        _gapService = Substitute.For<IGapSummaryQueryService>();
        _reviewService = Substitute.For<IReviewSummaryQueryService>();
        _timelineService = Substitute.For<ITimelineQueryService>();
        _exportService = Substitute.For<IExportOptionsQueryService>();
        _permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        _logger = Substitute.For<ILogger<ProcessingActivityControlCompositionQueryHandler>>();
        
        // Create a real base handler with in-memory database
        _stubHandler = new GetProcessingActivityControlQueryHandler(_dbContext);
        
        _handler = new ProcessingActivityControlCompositionQueryHandler(
            _stubHandler,
            _evidenceService,
            _gapService,
            _reviewService,
            _timelineService,
            _exportService,
            _permissionsService,
            _logger);
    }

    /// <summary>
    /// Helper to create and save a test ProcessingActivity to the in-memory DB.
    /// </summary>
    private ProcessingActivity CreateAndSaveTestActivity(
        Guid tenantId, Guid processingActivityId, Guid userId, string name = "Test Activity")
    {
        var activity = ProcessingActivity.Create(
            tenantId, name, userId, "Test Description", "Controller", "Department");
        
        // Use reflection to set the Id
        activity.GetType().GetProperty("Id")?.SetValue(activity, processingActivityId);
        
        _dbContext.ProcessingActivities.Add(activity);
        _dbContext.SaveChanges();
        
        return activity;
    }

    /// <summary>
    /// Scenario 1: Happy Path - All 6 services return valid data, composition completes successfully.
    /// Verifies that all data from each service is mapped and included in the final ViewModel.
    /// </summary>
    [Fact]
    public async Task HandleAsync_AllServicesReturnData_ComposesCompleteViewModel()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId, "Test Activity");

        // Setup all mocks with ReturnsForAnyArgs to avoid parameter matching issues
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(
                processingActivityId, versionId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, 
                new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] {
                new Modules.Audit.Application.DTOs.TimelineEventViewModel(
                    "event1", "CreateProcessingActivity", "audit.eventType.CreateProcessingActivity",
                    "ProcessingActivity", processingActivityId.ToString(), userId.ToString(),
                    DateTimeOffset.UtcNow.ToString(), "Success", "audit.result.Success", null, null)
            }.ToList().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[] {
                new Modules.Reporting.Application.Abstractions.ExportOptionViewModel(
                    "ProcessingActivityPdfSummary", "export.pdf",
                    "TechnicalOnly", "visibility.technical", true, null, null)
            }.ToList().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ProcessOwner" },
                new[] { new AvailableActionResult("EditProcessingActivity", "permission.edit") },
                false,
                new[] { new BlockedActionResult("DeleteProcessingActivity", "permission.delete", "SEC-001", "block.reason", "High", "severity.high", null, null) }));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert: Verify complete composition
        Assert.NotNull(result);
        Assert.Equal(processingActivityId, result.ProcessingActivity.Id);
        Assert.NotNull(result.EvidenceSummary);
        Assert.Equal(10, result.EvidenceSummary.TotalRequirements);
        Assert.NotNull(result.GapSummary);
        Assert.Equal(5, result.GapSummary.TotalCount);
        Assert.NotNull(result.ReviewSummary);
        Assert.NotEmpty(result.Timeline);
        Assert.Equal(AuditEventType.CreateProcessingActivity, result.Timeline[0].EventType);
        Assert.NotEmpty(result.Exports);
        Assert.NotNull(result.Permissions);
    }

    /// <summary>
    /// Scenario 2: Tenant Isolation - Verifies tenantId propagates to all 6 services.
    /// </summary>
    [Fact]
    public async Task HandleAsync_PropagatesToAllServicesWithCorrectTenantId()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(expectedTenantId, processingActivityId, userId);

        // Setup all mocks to return data for any parameters
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, processingActivityId, 10, 0, 10, 10, 0, 0, 0, 100m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, processingActivityId, 0, 0, 0, 0, 0, 0, null, "", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, processingActivityId, "Draft", "", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "Viewer" }, new AvailableActionResult[0], true, new BlockedActionResult[0]));

        // Act
        await _handler.HandleAsync(expectedTenantId, processingActivityId, userId, ct);

        // Assert: Verify tenantId was propagated to each service
        _ = _evidenceService.Received(1).GetSummaryAsync(
            Arg.Is<Guid>(g => g == expectedTenantId), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _ = _gapService.Received(1).GetByProcessingActivityAsync(
            Arg.Is<Guid>(g => g == expectedTenantId), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _ = _reviewService.Received(1).GetReviewSummaryAsync(
            Arg.Is<Guid>(g => g == expectedTenantId), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        _ = _timelineService.Received(1).GetTimelineAsync(
            Arg.Is<Guid>(g => g == expectedTenantId), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());

        _ = _permissionsService.Received(1).GetResourcePermissionsAsync(
            Arg.Any<Guid>(), Arg.Is<Guid>(g => g == expectedTenantId), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>());

        _ = _exportService.Received(1).GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Scenario 3: P1-004 CRITICAL - Blocked Actions Security Enforcement
    /// Verifies blocked actions are properly separated from available actions.
    /// </summary>
    [Fact]
    public async Task HandleAsync_PermissionsServiceReturnsBlockedAction_AppearsInBlockedActionsNotAvailable()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup minimal mocks
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, processingActivityId, 10, 0, 10, 10, 0, 0, 0, 100m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, processingActivityId, 0, 0, 0, 0, 0, 0, null, "", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, processingActivityId, "Draft", "", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        // Setup permissions: some actions blocked, some available
        // Using VALID PermissionCode enum values so parsing doesn't fallback to ApproveProcessingActivity default
        // This tests P1-004 security enforcement - blocked vs available separation
        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                RoleCodes: new[] { "ProcessOwner" },
                AvailableActions: new[] { 
                    new AvailableActionResult("ValidateEvidence", "permission.validateEvidence"),
                    new AvailableActionResult("DownloadEvidence", "permission.download")
                },
                ReadOnly: false,
                BlockedActions: new[] { 
                    new BlockedActionResult("ApproveProcessingActivity", "permission.approve", "SEC-APP-001", "block.approveOwn", "High", "severity.high", null, null)
                }));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert: P1-004 compliance - blocked actions properly separated
        Assert.NotNull(result);
        
        // Verify that there ARE blocked actions (demonstrating the handler composition is working)
        Assert.NotEmpty(result.BlockedActions);
        
        // Verify that ApproveProcessingActivity is in blocked, not in available
        var availableActionCodes = result.Permissions.AvailableActions.Select(a => a.Action).ToList();
        var blockedActionCodes = result.BlockedActions.Select(a => a.Action).ToList();
        
        // ApproveProcessingActivity should be blocked
        Assert.Contains(PermissionCode.ApproveProcessingActivity, blockedActionCodes);
        
        // ApproveProcessingActivity should NOT be available
        Assert.DoesNotContain(PermissionCode.ApproveProcessingActivity, availableActionCodes);
        
        // Available actions should have the ones we marked as available
        Assert.Contains(PermissionCode.ValidateEvidence, availableActionCodes);
        Assert.Contains(PermissionCode.DownloadEvidence, availableActionCodes);
    }

    /// <summary>
    /// Scenario 4: Graceful Degradation - Optional Evidence Service fails
    /// Verifies that if Evidence service fails, the endpoint responds with empty evidence summary
    /// and logs a warning. The rest of the data is still composed correctly.
    /// </summary>
    [Fact]
    public async Task HandleAsync_OptionalEvidenceServiceFails_DegradesAndLogsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup evidence service to throw
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<EvidenceSummaryDto>(
                new InvalidOperationException("Evidence service timeout")));

        // Setup other mocks to return data
        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, processingActivityId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, processingActivityId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "Viewer" }, new AvailableActionResult[0], true, new BlockedActionResult[0]));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert: Endpoint responds with data, evidence is empty/default
        Assert.NotNull(result);
        Assert.Equal(processingActivityId, result.ProcessingActivity.Id);
        
        // Evidence is degraded: empty summary with 0 requirements and 0% completion
        Assert.NotNull(result.EvidenceSummary);
        Assert.Equal(0, result.EvidenceSummary.TotalRequirements);
        Assert.Equal(0m, result.EvidenceSummary.CompletionPercentage);
        
        // Gap summary is still present (other service succeeded)
        Assert.NotNull(result.GapSummary);
        Assert.Equal(5, result.GapSummary.TotalCount);
        
        // Verify warning was logged
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Evidence service failed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Scenario 5: Graceful Degradation - Optional Gap Management Service fails
    /// </summary>
    [Fact]
    public async Task HandleAsync_OptionalGapServiceFails_DegradesAndLogsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup gap service to throw
        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProcessingActivityGapSummaryDto>(
                new TimeoutException("Gap Management service timeout")));

        // Setup other mocks
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "ProcessOwner" }, new AvailableActionResult[0], false, new BlockedActionResult[0]));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.EvidenceSummary);
        Assert.Equal(10, result.EvidenceSummary.TotalRequirements);
        
        // Gap summary is degraded: empty with 0 gaps
        Assert.NotNull(result.GapSummary);
        Assert.Equal(0, result.GapSummary.TotalCount);
        
        // Verify warning was logged
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Gap Management service failed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Scenario 6: Graceful Degradation - Optional Timeline Service fails
    /// </summary>
    [Fact]
    public async Task HandleAsync_OptionalTimelineServiceFails_DegradesAndLogsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup timeline service to throw
        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Modules.Audit.Application.DTOs.TimelineEventViewModel>>(
                new InvalidOperationException("Timeline service error")));

        // Setup other services
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, versionId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "Viewer" }, new AvailableActionResult[0], true, new BlockedActionResult[0]));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.EvidenceSummary);
        Assert.NotNull(result.GapSummary);
        
        // Timeline is degraded: empty list
        Assert.NotNull(result.Timeline);
        Assert.Empty(result.Timeline);
        
        // Verify warning was logged
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Timeline service failed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Scenario 7: Graceful Degradation - Optional Export Service fails
    /// </summary>
    [Fact]
    public async Task HandleAsync_OptionalExportServiceFails_DegradesAndLogsWarning()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup export service to throw
        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>>(
                new InvalidOperationException("Export service error")));

        // Setup other services
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, versionId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "Viewer" }, new AvailableActionResult[0], true, new BlockedActionResult[0]));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.EvidenceSummary);
        Assert.NotNull(result.GapSummary);
        Assert.Empty(result.Timeline);
        
        // Exports are degraded: empty list
        Assert.NotNull(result.Exports);
        Assert.Empty(result.Exports);
        
        // Verify warning was logged
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Export service failed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Scenario 8: Multiple Optional Services Fail Simultaneously
    /// Verifies that multiple optional service failures are handled gracefully,
    /// with each degraded appropriately.
    /// </summary>
    [Fact]
    public async Task HandleAsync_MultipleOptionalServicesFail_AllDegradedCorrectly()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup Timeline and Export services to fail
        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Modules.Audit.Application.DTOs.TimelineEventViewModel>>(
                new InvalidOperationException("Timeline error")));

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>>(
                new InvalidOperationException("Export error")));

        // Setup other services to succeed
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, versionId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(new[] { "ProcessOwner" }, 
                new[] { new AvailableActionResult("EditProcessingActivity", "permission.edit") }, false, new BlockedActionResult[0]));

        // Act
        var result = await _handler.HandleAsync(tenantId, processingActivityId, userId, ct);

        // Assert: All services that succeeded are present, failed services are degraded
        Assert.NotNull(result);
        Assert.Equal(10, result.EvidenceSummary.TotalRequirements);
        Assert.Equal(5, result.GapSummary.TotalCount);
        Assert.Empty(result.Timeline);
        Assert.Empty(result.Exports);
        
        // Verify 2 warnings were logged
        _logger.Received(2).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    /// <summary>
    /// Scenario 9: CRITICAL Service (Permissions) Fails - Fail-Closed Behavior
    /// Verifies that if the critical permissions service fails, the entire endpoint fails (no graceful degradation).
    /// </summary>
    [Fact]
    public async Task HandleAsync_CriticalPermissionsServiceFails_PropagatesException()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var versionId = processingActivityId;
        var ct = CancellationToken.None;

        CreateAndSaveTestActivity(tenantId, processingActivityId, userId);

        // Setup permissions service to throw (CRITICAL - must fail the endpoint)
        _permissionsService.GetResourcePermissionsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<ResourceContextData>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ResourcePermissionsResult>(
                new UnauthorizedAccessException("Permissions service failed")));

        // Setup other services to return data
        _evidenceService.GetSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new EvidenceSummaryDto(processingActivityId, versionId, 10, 2, 5, 3, 0, 0, 0, 80m));

        _gapService.GetByProcessingActivityAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessingActivityGapSummaryDto(processingActivityId, versionId, 5, 2, 1, 2, 0, 0, 
                Modules.GapManagement.Domain.GapSeverity.Medium, "severity.medium", false));

        _reviewService.GetReviewSummaryAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new ReviewSummaryDto(processingActivityId, versionId, "Draft", "review.status.draft", null, null, new List<object>(), new List<object>(), null));

        _timelineService.GetTimelineAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Audit.Application.DTOs.TimelineEventViewModel>().AsReadOnly());

        _exportService.GetExportOptionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new List<Modules.Reporting.Application.Abstractions.ExportOptionViewModel>().AsReadOnly());

        // Act & Assert: Exception is propagated (fail-closed)
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _handler.HandleAsync(tenantId, processingActivityId, userId, ct));

        Assert.Contains("Permissions service failed", ex.Message);
    }
}
