using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Evidence.Application.Commands;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using System.Security.Claims;
using Xunit;

namespace Evidata.Tests.Unit.Evidence;

/// <summary>
/// P1-011a: Tests para ValidateEvidenceCommandHandler.
/// Verifica auditoría y autorización (SEC-EV-001).
/// </summary>
public class ValidateEvidenceCommandHandlerTests
{
    private static EvidenceDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<EvidenceDbContext>()
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
    public async Task HandleAsync_ValidateEvidence_WithCorrectRole_LogsAuditEventSuccess()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var requirement = EvidenceRequirement.Create(
            tenantId, processingActivityId, ReviewDomain.Legal, "Legal review", userId);
        
        var validation = EvidenceValidation.Create(tenantId, requirement.Id, userId);
        var evidenceId = Guid.NewGuid();
        validation.AttachEvidence(evidenceId, userId);

        await using var db = BuildContext();
        db.EvidenceRequirements.Add(requirement);
        db.EvidenceValidations.Add(validation);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "LegalReviewer");

        var handler = new ValidateEvidenceCommandHandler(db, auditService, httpAccessor);
        var cmd = new ValidateEvidenceCommand(tenantId, validation.Id, "Validate", "Policy document is compliant", userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validation.Id, result.Id);
        Assert.Equal("Validated", result.Status);

        // Verify audit log was called with success
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.ValidateEvidence.ToString(),
            "EvidenceValidation",
            validation.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidateEvidence_WithWrongRole_LogsAuditEventBlocked()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var requirement = EvidenceRequirement.Create(
            tenantId, processingActivityId, ReviewDomain.Legal, "Legal review", userId);
        
        var validation = EvidenceValidation.Create(tenantId, requirement.Id, userId);
        var evidenceId = Guid.NewGuid();
        validation.AttachEvidence(evidenceId, userId);

        await using var db = BuildContext();
        db.EvidenceRequirements.Add(requirement);
        db.EvidenceValidations.Add(validation);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "SecurityReviewer"); // Wrong role

        var handler = new ValidateEvidenceCommandHandler(db, auditService, httpAccessor);
        var cmd = new ValidateEvidenceCommand(tenantId, validation.Id, "Validate", "Policy document is compliant", userId);

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.HandleAsync(cmd));

        // Verify audit log was called with Blocked result
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.ValidateEvidence.ToString(),
            "EvidenceValidation",
            validation.Id,
            AuditEventResult.Blocked,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_RejectEvidence_WithCorrectRole_LogsAuditEventWithRejectType()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var requirement = EvidenceRequirement.Create(
            tenantId, processingActivityId, ReviewDomain.Security, "Security review", userId);
        
        var validation = EvidenceValidation.Create(tenantId, requirement.Id, userId);
        var evidenceId = Guid.NewGuid();
        validation.AttachEvidence(evidenceId, userId);

        await using var db = BuildContext();
        db.EvidenceRequirements.Add(requirement);
        db.EvidenceValidations.Add(validation);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "SecurityReviewer");

        var handler = new ValidateEvidenceCommandHandler(db, auditService, httpAccessor);
        var cmd = new ValidateEvidenceCommand(tenantId, validation.Id, "Reject", "Missing required encryption details", userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Rejected", result.Status);

        // Verify audit log was called with RejectEvidence type
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.RejectEvidence.ToString(),
            "EvidenceValidation",
            validation.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_MarkInsufficient_LogsAuditEventWithRejectType()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var correlationId = Guid.NewGuid().ToString("N");

        var requirement = EvidenceRequirement.Create(
            tenantId, processingActivityId, ReviewDomain.Legal, "Legal review", userId);
        
        var validation = EvidenceValidation.Create(tenantId, requirement.Id, userId);
        var evidenceId = Guid.NewGuid();
        validation.AttachEvidence(evidenceId, userId);

        await using var db = BuildContext();
        db.EvidenceRequirements.Add(requirement);
        db.EvidenceValidations.Add(validation);
        await db.SaveChangesAsync();

        var auditService = Substitute.For<IAuditService>();
        var httpAccessor = BuildHttpContextAccessor(correlationId, "LegalReviewer");

        var handler = new ValidateEvidenceCommandHandler(db, auditService, httpAccessor);
        var cmd = new ValidateEvidenceCommand(tenantId, validation.Id, "MarkInsufficient", "Incomplete policy details", userId);

        // Act
        var result = await handler.HandleAsync(cmd);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Insufficient", result.Status);

        // Verify audit log was called with RejectEvidence type (Insufficient is a form of rejection)
        await auditService.Received(1).LogAsync(
            tenantId,
            userId,
            AuditEventType.RejectEvidence.ToString(),
            "EvidenceValidation",
            validation.Id,
            AuditEventResult.Success,
            correlationId,
            Arg.Any<Dictionary<string, object?>>(),
            ct: Arg.Any<CancellationToken>());
    }
}
