using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.GapManagement.Application.Commands;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Evidata.Tests.Unit.GapManagement;

/// <summary>
/// P1-011b: Tests para AcceptGapWithRiskCommandHandler.
/// Verifica auditoría, autorización (SEC-GAP-001), y justificación obligatoria.
/// </summary>
public class AcceptGapWithRiskCommandHandlerTests
{
    private static GapManagementDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<GapManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static IHttpContextAccessor BuildHttpContextAccessor(
        string? correlationId = null,
        params string[] userRoles)
    {
        var claims = userRoles
            .Select(role => new Claim("roles", role))
            .ToList();

        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = principal };

        if (!string.IsNullOrEmpty(correlationId))
            httpContext.Items["CorrelationId"] = correlationId;

        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        return accessor;
    }

    [Fact]
    public async Task HandleAsync_AcceptGapWithRisk_WithCorrectRoleAndJustification_LogsAuditEventSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var justification = "Accepted risk due to business continuity requirements";

        var gap = ComplianceGap.Create(
            tenantId,
            "ProcessingInventory",
            Guid.NewGuid(),
            "Transfer without safeguard",
            "Transfer to third country without data processing agreement",
            GapSeverity.Critical,
            userId);

        await using var db = BuildContext();
        db.ComplianceGaps.Add(gap);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "TenantOwner");
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);

        var handler = new AcceptGapWithRiskCommandHandler(db, auditService, currentUser, httpAccessor);
        var cmd = new AcceptGapWithRiskCommand(tenantId, gap.Id, justification, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(gap.Id, result.Id);
        Assert.Equal("AcceptedWithRisk", result.Status);
        Assert.Equal(justification, result.RiskAcceptanceJustification);

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.AcceptGapWithRisk.ToString(),
            "ComplianceGap",
            gap.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            severity: AuditSeverity.Critical,
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AcceptGapWithRisk_WithWrongRole_LogsAuditEventBlocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var justification = "Accepted risk due to business continuity requirements";

        var gap = ComplianceGap.Create(
            tenantId,
            "ProcessingInventory",
            Guid.NewGuid(),
            "Transfer without safeguard",
            "Transfer to third country without data processing agreement",
            GapSeverity.Critical,
            userId);

        await using var db = BuildContext();
        db.ComplianceGaps.Add(gap);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "ProcessOwner"); // Wrong role
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);

        var handler = new AcceptGapWithRiskCommandHandler(db, auditService, currentUser, httpAccessor);
        var cmd = new AcceptGapWithRiskCommand(tenantId, gap.Id, justification, userId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.AcceptGapWithRisk.ToString(),
            "ComplianceGap",
            gap.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            severity: AuditSeverity.Warning,
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AcceptGapWithRisk_WithEmptyJustification_LogsAuditEventBlocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var gap = ComplianceGap.Create(
            tenantId,
            "ProcessingInventory",
            Guid.NewGuid(),
            "Transfer without safeguard",
            "Transfer to third country without data processing agreement",
            GapSeverity.Critical,
            userId);

        await using var db = BuildContext();
        db.ComplianceGaps.Add(gap);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "ComplianceAdmin");
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);

        var handler = new AcceptGapWithRiskCommandHandler(db, auditService, currentUser, httpAccessor);
        var cmd = new AcceptGapWithRiskCommand(tenantId, gap.Id, "", userId); // Empty justification

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.AcceptGapWithRisk.ToString(),
            "ComplianceGap",
            gap.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            severity: AuditSeverity.Warning,
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AcceptGapWithRisk_WithComplianceAdminRole_LogsAuditEventSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");
        var justification = "Compliance team reviewed and accepted the risk";

        var gap = ComplianceGap.Create(
            tenantId,
            "ProcessingInventory",
            Guid.NewGuid(),
            "Sensitive data without security review",
            "Processing sensitive data requires security review",
            GapSeverity.Critical,
            userId);

        await using var db = BuildContext();
        db.ComplianceGaps.Add(gap);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "ComplianceAdmin");
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.TenantId.Returns(tenantId);
        currentUser.UserId.Returns(userId);

        var handler = new AcceptGapWithRiskCommandHandler(db, auditService, currentUser, httpAccessor);
        var cmd = new AcceptGapWithRiskCommand(tenantId, gap.Id, justification, userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(gap.Id, result.Id);
        Assert.Equal("AcceptedWithRisk", result.Status);

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.AcceptGapWithRisk.ToString(),
            "ComplianceGap",
            gap.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            severity: AuditSeverity.Critical,
            ct: Arg.Any<CancellationToken>());
    }
}
