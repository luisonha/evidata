using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Evidata.Tests.Unit.ProcessingInventory.Application.Commands;

/// <summary>
/// P1-012: Tests para ActivateProcessingActivityCommandHandler.
/// Verifica activación formal de versiones aprobadas (AUD-ACT-001 / SEC-ACT-001).
/// </summary>
public class ActivateProcessingActivityCommandHandlerTests
{
    private static ProcessingInventoryDbContext BuildProcessingContext() =>
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

    private static ProcessingActivity BuildReadyForApproval(Guid tenantId, Guid userId)
    {
        var act = ProcessingActivity.Create(tenantId, "Test Activity", userId,
            description: "Test activity", controller: "Controller", department: "Department");
        act.SetPurpose(
            PurposeSection.Create("Test purpose", LegalBasis.ContractExecution, "Legal basis info"),
            userId);
        act.SetDataCategories(
            [DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary)],
            userId);
        act.SetDataSubjects(
            [DataSubjectEntry.Create(DataSubjectType.Employees)],
            userId);
        act.SetRetention(RetentionSection.Create("5 years"), userId);
        return act;
    }

    [Fact]
    public async Task HandleAsync_ActivateApprovedVersion_Success()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = BuildReadyForApproval(tenantId, userId);
        activity.SubmitForReview(userId);
        activity.Approve(userId);

        await using var db = BuildProcessingContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security context with TenantOwner role
        await using var securityDb = BuildSecurityContext();
        var tenantOwnerRole = Role.Create("TenantOwner", "Tenant owner", isSystemRole: true);
        securityDb.Roles.Add(tenantOwnerRole);
        await securityDb.SaveChangesAsync();

        var assignment = UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantId);
        securityDb.UserRoleAssignments.Add(assignment);
        await securityDb.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ActivateProcessingActivityCommandHandler(db, securityDb, auditService, httpAccessor);
        var cmd = new ActivateProcessingActivityCommand(tenantId, activity.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Active.ToString(), result.Status.ToString());

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Activate.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivateApprovedVersion_WithPreviousActiveVersion_DeprecatesPrevious()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create first version, approve it, and activate it
        var v1 = BuildReadyForApproval(tenantId, userId);
        v1.SubmitForReview(userId);
        v1.Approve(userId);

        // Create second version BEFORE activating v1
        var v2 = v1.CreateNewVersion(userId);
        v2.SubmitForReview(userId);
        v2.Approve(userId);

        // Now activate v1
        v1.Activate(userId);

        await using var db = BuildProcessingContext();
        db.ProcessingActivities.AddRange(v1, v2);
        await db.SaveChangesAsync();

        // Setup security context with ComplianceAdmin role
        await using var securityDb = BuildSecurityContext();
        var complianceRole = Role.Create("ComplianceAdmin", "Compliance admin", isSystemRole: true);
        securityDb.Roles.Add(complianceRole);
        await securityDb.SaveChangesAsync();

        var assignment = UserRoleAssignment.Create(userId, complianceRole.Id, tenantId);
        securityDb.UserRoleAssignments.Add(assignment);
        await securityDb.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ActivateProcessingActivityCommandHandler(db, securityDb, auditService, httpAccessor);
        var cmd = new ActivateProcessingActivityCommand(tenantId, v2.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Active.ToString(), result.Status.ToString());

        // Verify previous version was deprecated
        var refreshedV1 = await db.ProcessingActivities.FirstOrDefaultAsync(a => a.Id == v1.Id);
        Assert.NotNull(refreshedV1);
        Assert.Equal(ProcessingActivityStatus.Deprecated, refreshedV1.Status);

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Activate.ToString(),
            "ProcessingActivity",
            v2.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivateWithoutAdminRole_ReturnsBlocked403()
    {
        // Arrange: SEC-ACT-001
        // Given a ProcessOwner (without TenantOwner/ComplianceAdmin permission)
        // with an approved version,
        // when they try to activate,
        // then the backend responds 403 and does not modify activeVersionId.

        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = BuildReadyForApproval(tenantId, userId);
        activity.SubmitForReview(userId);
        activity.Approve(userId);

        await using var db = BuildProcessingContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security context with ProcessOwner role only (not admin)
        await using var securityDb = BuildSecurityContext();
        var processOwnerRole = Role.Create("ProcessOwner", "Process owner", isSystemRole: true);
        securityDb.Roles.Add(processOwnerRole);
        await securityDb.SaveChangesAsync();

        var assignment = UserRoleAssignment.Create(userId, processOwnerRole.Id, tenantId);
        securityDb.UserRoleAssignments.Add(assignment);
        await securityDb.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ActivateProcessingActivityCommandHandler(db, securityDb, auditService, httpAccessor);
        var cmd = new ActivateProcessingActivityCommand(tenantId, activity.Id, userId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(cmd));
        Assert.Contains("TenantOwner", ex.Message);

        // Verify status was NOT changed
        var refreshed = await db.ProcessingActivities.FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(ProcessingActivityStatus.Approved, refreshed.Status);

        // Verify audit log was called with Blocked result (authorization denied)
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Activate.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivateNonApprovedVersion_ReturnsBlocked422()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId);
        // Activity is still in Draft — not Approved

        await using var db = BuildProcessingContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security context with TenantOwner role
        await using var securityDb = BuildSecurityContext();
        var tenantOwnerRole = Role.Create("TenantOwner", "Tenant owner", isSystemRole: true);
        securityDb.Roles.Add(tenantOwnerRole);
        await securityDb.SaveChangesAsync();

        var assignment = UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantId);
        securityDb.UserRoleAssignments.Add(assignment);
        await securityDb.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ActivateProcessingActivityCommandHandler(db, securityDb, auditService, httpAccessor);
        var cmd = new ActivateProcessingActivityCommand(tenantId, activity.Id, userId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));
        Assert.Contains("InvalidStatusTransition", ex.Message);

        // Verify status was NOT changed
        var refreshed = await db.ProcessingActivities.FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(refreshed);
        Assert.Equal(ProcessingActivityStatus.Draft, refreshed.Status);

        // Verify audit log was called with Blocked result (invalid state transition)
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Activate.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ActivateFirstVersion_NoError()
    {
        // Arrange: First version activation (no previous active version)
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = BuildReadyForApproval(tenantId, userId);
        activity.SubmitForReview(userId);
        activity.Approve(userId);

        await using var db = BuildProcessingContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        // Setup security context with TenantOwner role
        await using var securityDb = BuildSecurityContext();
        var tenantOwnerRole = Role.Create("TenantOwner", "Tenant owner", isSystemRole: true);
        securityDb.Roles.Add(tenantOwnerRole);
        await securityDb.SaveChangesAsync();

        var assignment = UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantId);
        securityDb.UserRoleAssignments.Add(assignment);
        await securityDb.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ActivateProcessingActivityCommandHandler(db, securityDb, auditService, httpAccessor);
        var cmd = new ActivateProcessingActivityCommand(tenantId, activity.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Active.ToString(), result.Status.ToString());

        // Verify activeVersionId was set
        var rootActivity = await db.ProcessingActivities.FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(rootActivity);
        Assert.Equal(activity.Id, rootActivity.ActiveVersionId);

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Activate.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }
}
