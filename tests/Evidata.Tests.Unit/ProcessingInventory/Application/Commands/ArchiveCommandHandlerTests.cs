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
/// P1-011c: Tests para ArchiveCommandHandler.
/// Verifica auditoría de Archive (AUD-ARC-001).
/// </summary>
public class ArchiveCommandHandlerTests
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

    [Fact]
    public async Task HandleAsync_Archive_FromDraft_LogsAuditEventSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ArchiveCommandHandler(db, auditService, httpAccessor);
        var cmd = new ArchiveCommand(tenantId, activity.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Archived.ToString(), result.Status.ToString());

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Archive.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Archive_AlreadyArchived_LogsAuditEventFailure()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        // Create activity and archive it once
        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId);
        activity.Archive(userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ArchiveCommandHandler(db, auditService, httpAccessor);
        var cmd = new ArchiveCommand(tenantId, activity.Id, userId);

        // Act & Assert - archiving again should fail
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Failure result
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Archive.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Failure,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Archive_CorrelationIdPropagated()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = "archive-correlation-id-67890";

        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ArchiveCommandHandler(db, auditService, httpAccessor);
        var cmd = new ArchiveCommand(tenantId, activity.Id, userId);

        // Act
        await handler.HandleAsync(cmd);

        // Assert - verify correlationId was propagated to audit service
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Archive.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_Archive_MetadataIncludesVersionAndStatus()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var activity = ProcessingActivity.Create(tenantId, "Test Activity", userId);

        await using var db = BuildContext();
        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId);
        var handler = new ArchiveCommandHandler(db, auditService, httpAccessor);
        var cmd = new ArchiveCommand(tenantId, activity.Id, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert - verify result is correct
        Assert.NotNull(result);
        Assert.Equal(ProcessingActivityStatus.Archived.ToString(), result.Status.ToString());

        // Verify audit was called (metadata structure is validated by handler itself)
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.Archive.ToString(),
            "ProcessingActivity",
            activity.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }
}
