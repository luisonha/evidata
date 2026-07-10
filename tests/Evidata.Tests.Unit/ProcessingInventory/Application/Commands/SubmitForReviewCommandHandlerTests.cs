using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.ProcessingInventory.Application.Commands;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Evidata.Tests.Unit.ProcessingInventory.Application.Commands;

/// <summary>
/// P1-011c: Tests para SubmitForReviewCommandHandler.
/// Verifica auditoría de SubmitForReview (AUD-REV-001).
/// </summary>
public class SubmitForReviewCommandHandlerTests
{
    private static ProcessingInventoryDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<ProcessingInventoryDbContext>()
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

    private static ProcessingActivity CreateValidActivityForReview(Guid tenantId, Guid userId)
    {
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId, "Test Description");

        // Add required sections to pass validation for SubmitForReview
        var purpose = PurposeSection.Create(
            "Test Purpose", 
            LegalBasis.LegalObligation,  // Use LegalObligation to avoid evidence requirement
            "Regulatory compliance");
        activity.SetPurpose(purpose, userId);

        var categories = new[] {
            DataCategoryEntry.Create(Guid.NewGuid(), DataSensitivityLevel.Ordinary, "Personal Identifiers")
        };
        activity.SetDataCategories(categories, userId);

        var subjects = new[] {
            DataSubjectEntry.Create(DataSubjectType.Customers, "Individual customers", 100)
        };
        activity.SetDataSubjects(subjects, userId);

        return activity;
    }

    [Fact]
    public async Task HandleAsync_SubmitForReview_Draft_LogsAuditEventSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = CreateValidActivityForReview(tenantId, userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new SubmitForReviewCommandHandler(db, auditService, httpAccessor);
        var cmd = new SubmitForReviewCommand(tenantId, activity.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.UnderReview.ToString(), result.Status.ToString());

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.SubmitForReview.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SubmitForReview_NotInDraft_LogsAuditEventFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity and manually move to UnderReview (not Draft)
        var activity = CreateValidActivityForReview(tenantId, userId);
        activity.SubmitForReview(userId); // Move to UnderReview

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new SubmitForReviewCommandHandler(db, auditService, httpAccessor);
        var cmd = new SubmitForReviewCommand(tenantId, activity.Id, userId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Failure result
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.SubmitForReview.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Failure,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SubmitForReview_IncompleteActivity_LogsAuditEventFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create an INCOMPLETE activity (missing required sections)
        var activity = ProcessingActivity.Create(tenantId, "Incomplete Activity", userId);
        // Not setting Purpose, DataCategories, DataSubjects - intentionally invalid

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new SubmitForReviewCommandHandler(db, auditService, httpAccessor);
        var cmd = new SubmitForReviewCommand(tenantId, activity.Id, userId);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Failure result (validation failed)
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.SubmitForReview.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Failure,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_SubmitForReview_CorrelationIdPropagated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = "test-correlation-id-12345";

        var activity = CreateValidActivityForReview(tenantId, userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new SubmitForReviewCommandHandler(db, auditService, httpAccessor);
        var cmd = new SubmitForReviewCommand(tenantId, activity.Id, userId);

        // Act
        await handler.HandleAsync(cmd);

        // Assert - verify correlationId was propagated to audit service
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.SubmitForReview.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }
}
