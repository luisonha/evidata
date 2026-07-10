using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Evidata.Modules.Workflow.Application.Abstractions;
using Evidata.Modules.Workflow.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Evidata.Tests.Unit.ProcessingInventory.Application.Commands;

/// <summary>
/// P1-013: Tests para ApproveProcessingActivityCommandHandler.
/// Verifica autorización (RBAC), blockers de negocio y auditoría para aprobación formal.
///
/// Incluye test SEC-APP-001: ProcessOwner no puede aprobar su propio tratamiento.
/// </summary>
public class ApproveProcessingActivityCommandHandlerTests
{
    private static ProcessingInventoryDbContext BuildProcessingInventoryContext() =>
        new(new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static SecurityDbContext BuildSecurityContext() =>
        new(new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IHttpContextAccessor BuildHttpContextAccessor(string? correlationId = null)
    {
        var identity = new ClaimsIdentity([], "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        if (!string.IsNullOrEmpty(correlationId))
            httpContext.Items["CorrelationId"] = correlationId;

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }

    /// <summary>
    /// SEC-APP-001: ProcessOwner cannot approve their own treatment.
    /// 
    /// Given: A ProcessingActivity whose creator is ProcessOwner
    /// When: That same ProcessOwner attempts to approve it
    /// Then: Backend returns 403 InsufficientPermissions
    /// And: AuditEvent has result = Denied
    /// </summary>
    [Fact]
    public async Task HandleAsync_ProcessOwnerApprovesOwn_Returns403Denied()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid(); // The ProcessOwner who created and owns the activity
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state (suitable for approval)
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        
        // Add minimum required data for SubmitForReview validation
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        
        activity.SubmitForReview(ownerUserId); // Transition to UnderReview

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: ProcessOwner attempts to approve
        // ResourcePermissionsQueryService will detect that ResourceOwnerId (activity.CreatedBy) == userId
        // and mark ApproveProcessingActivity as blocked via IsBlocked_ApproveOwnActivity

        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                ownerUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(x =>
            {
                var context = (ResourceContextData?)x[4];
                // ResourceOwnerId should equal ownerUserId, triggering block
                var blockedAction = context?.ResourceOwnerId == ownerUserId
                    ? new BlockedActionResult(
                        "ApproveProcessingActivity",
                        "permission.approveProcessingActivity",
                        "SEC-APP-001",
                        "block.approveOwnActivity",
                        "High",
                        "severity.high",
                        "Review",
                        "node.review")
                    : null;

                return Task.FromResult(new ResourcePermissionsResult(
                    new[] { "ProcessOwner" },
                    new List<AvailableActionResult>(),
                    false,
                    blockedAction != null ? new[] { blockedAction }.ToList() : new List<BlockedActionResult>()));
            });

        var auditService = Substitute.For<IAuditService>();
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, ownerUserId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Contains("cannot approve", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Verify audit log was called with Denied result
        await auditService.Received(1).LogAsync(
            tenantId,
            ownerUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Happy path: Authorized user successfully approves a valid ProcessingActivity.
    /// </summary>
    [Fact]
    public async Task HandleAsync_AuthorizedUserApprovesValid_ReturnsApprovedSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid(); // The ProcessOwner (creator)
        var approverUserId = Guid.NewGuid(); // Different user with ApproveProcessingActivity permission
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        
        // Add minimum required data for SubmitForReview validation
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        
        activity.SubmitForReview(ownerUserId);

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: Approver (not owner) has permission and is not blocked
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        var auditService = Substitute.For<IAuditService>();
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Approved.ToString(), result.Status.ToString());
        Assert.Equal(approverUserId.ToString(), result.ApprovedBy.ToString());

        // Verify activity was updated in database
        var dbActivity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(dbActivity);
        Assert.Equal(ProcessingActivityStatus.Approved, dbActivity.Status);
        Assert.Equal(approverUserId, dbActivity.ApprovedBy);

        // Verify audit log was called with Success
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Blocker: Critical gap is open, preventing approval.
    /// </summary>
    [Fact]
    public async Task HandleAsync_CriticalGapOpen_Returns422Blocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        
        // Add minimum required data for SubmitForReview validation
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        
        activity.SubmitForReview(ownerUserId);

        // Simulate critical gap open by setting flag (normally set by GapManagement module)
        activity.SetCriticalGapFlag(true, ownerUserId);

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: Approver has permission
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        var auditService = Substitute.For<IAuditService>();
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Contains("ApprovalBlocked", ex.Message);
        Assert.Contains("CriticalGapOpen", ex.Message);

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Invalid state: Cannot approve from Draft or other non-UnderReview states.
    /// </summary>
    [Fact]
    public async Task HandleAsync_NotUnderReview_Returns422InvalidState()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity but keep it in Draft (don't submit for review)
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        // Status is Draft, not UnderReview

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: Approver has permission
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        var auditService = Substitute.For<IAuditService>();
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                Arg.Any<Guid>(),
                Arg.Any<Guid>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Contains("VersionNotApprovalReady", ex.Message);
        Assert.Contains("Draft", ex.Message);

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// P1-016 Blocker: Required review pending, preventing approval.
    /// When an activity has open reviews (not Approved), approval is blocked.
    /// </summary>
    [Fact]
    public async Task HandleAsync_RequiredReviewPending_Returns422Blocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var reviewerUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        activity.SubmitForReview(ownerUserId);

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Create a review in InProgress state (not Approved) for this activity
        var review = Review.Create(
            tenantId,
            "ProcessingInventory",
            "ProcessingActivity",
            activity.Id,
            ownerUserId);
        review.Start(reviewerUserId);
        // Review is now in InProgress state — this is "pending" and blocks approval

        // Setup security: Approver has permission
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        // Setup review service to return the pending review
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                tenantId,
                activity.Id,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new[] { review }.ToList()));

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Contains("ApprovalBlocked", ex.Message);
        Assert.Contains("RequiredReviewPending", ex.Message);

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// P1-016 Blocker: Version modified after review, preventing approval.
    /// When activity's LastModifiedAt is after ReviewedAt, approval is blocked.
    /// </summary>
    [Fact]
    public async Task HandleAsync_VersionModifiedAfterReview_Returns422Blocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        activity.SubmitForReview(ownerUserId);

        // Set ReviewedAt in the past (simulating completed review)
        var reviewedTime = DateTimeOffset.UtcNow.AddHours(-1);
        
        // Use reflection to set both ReviewedAt and LastModifiedAt to simulate the condition:
        // ReviewedAt was set in the past, but then LastModifiedAt was updated later
        var reviewedAtProperty = typeof(ProcessingActivity).GetProperty(nameof(ProcessingActivity.ReviewedAt))
            ?.GetSetMethod(true);
        reviewedAtProperty?.Invoke(activity, new object?[] { reviewedTime });

        // Set LastModifiedAt to a time after ReviewedAt (simulating modification after review)
        var modifiedTime = DateTimeOffset.UtcNow;
        var lastModifiedAtProperty = typeof(ProcessingActivity).GetProperty(nameof(ProcessingActivity.LastModifiedAt))
            ?.GetSetMethod(true);
        lastModifiedAtProperty?.Invoke(activity, new object?[] { modifiedTime });

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: Approver has permission
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        // Setup review service to return no open reviews (all approved)
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                tenantId,
                activity.Id,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(cmd, CancellationToken.None));

        Assert.Contains("ApprovalBlocked", ex.Message);
        Assert.Contains("VersionModifiedAfterReview", ex.Message);

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// P1-016 Success: Both RequiredReviewPending and VersionModifiedAfterReview conditions are satisfied.
    /// Activity with no pending reviews and not modified after review is approved successfully.
    /// </summary>
    [Fact]
    public async Task HandleAsync_NoPendingReviewsAndNotModifiedAfterReview_ReturnsSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var ownerUserId = Guid.NewGuid();
        var approverUserId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity in UnderReview state
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", ownerUserId);
        activity.SetPurpose(PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Test legal reference"), ownerUserId);
        activity.SetDataCategories([DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)], ownerUserId);
        activity.SetDataSubjects([DataSubjectEntry.Create(DataSubjectType.Employees)], ownerUserId);
        activity.SubmitForReview(ownerUserId);

        await using var db = BuildProcessingInventoryContext();
        await using var securityDb = BuildSecurityContext();

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security: Approver has permission
        var permissionsService = Substitute.For<IResourcePermissionsQueryService>();
        permissionsService.GetResourcePermissionsAsync(
                approverUserId,
                tenantId,
                Arg.Any<string>(),
                Arg.Any<Guid>(),
                Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new[] { "ComplianceAdmin" },
                new[] { new AvailableActionResult("ApproveProcessingActivity", "permission.approveProcessingActivity") }.ToList(),
                false,
                new List<BlockedActionResult>()));

        // Setup review service to return no open reviews
        var reviewService = Substitute.For<IReviewService>();
        reviewService.GetOpenReviewsForEntityAsync(
                tenantId,
                activity.Id,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult((IReadOnlyList<Review>)new List<Review>()));

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);

        var handler = new ApproveProcessingActivityCommandHandler(
            db, securityDb, permissionsService, auditService, reviewService, httpAccessor);

        var cmd = new ApproveProcessingActivityCommand(tenantId, activity.Id, approverUserId);

        // Act
        var result = await handler.HandleAsync(cmd, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Approved.ToString(), result.Status.ToString());
        Assert.Equal(approverUserId.ToString(), result.ApprovedBy.ToString());

        // Verify activity was updated in database
        var dbActivity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(dbActivity);
        Assert.Equal(ProcessingActivityStatus.Approved, dbActivity.Status);
        Assert.Equal(approverUserId, dbActivity.ApprovedBy);

        // Verify audit log was called with Success
        await auditService.Received(1).LogAsync(
            tenantId,
            approverUserId,
            AuditEventType.ProcessingActivityApproved.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }
}
