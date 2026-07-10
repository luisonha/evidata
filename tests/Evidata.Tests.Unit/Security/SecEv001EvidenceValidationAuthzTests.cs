using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for SEC-EV-001: Validate evidence — authorization based on EvidenceRequirement.reviewDomain
/// 
/// Rule: LegalReviewer can only validate Legal domain requirements.
///        SecurityReviewer can only validate Security domain requirements.
///        Reviewer cannot validate requirement outside their domain.
///        
/// Test cases:
///   - LegalReviewer validates Legal requirement → available
///   - LegalReviewer attempts to validate Security requirement → blocked
///   - SecurityReviewer validates Security requirement → available
///   - SecurityReviewer attempts to validate Legal requirement → blocked
///   - Non-reviewer role → blocked
///
/// Note: Domain-specific authorization is tested at unit level with context.ReviewDomain parameter.
/// Integration testing at application/handler layer will validate end-to-end flows.
/// </summary>
public class SecEv001EvidenceValidationAuthzTests
{
    private static SecurityDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SecurityDbContext(opts);
    }

    private static Role CreateAndSeedRole(SecurityDbContext db, string name)
    {
        var role = Role.Create(name, isSystemRole: true);
        db.Roles.Add(role);
        db.SaveChanges();
        return role;
    }

    private static Permission CreateAndSeedPermission(SecurityDbContext db, string resource, string action)
    {
        var perm = Permission.Create(resource, action);
        db.Permissions.Add(perm);
        db.SaveChanges();
        return perm;
    }

    [Fact]
    public async Task SEC_EV_001_LegalReviewerCanValidateEvidence_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var legalReviewer = CreateAndSeedRole(db, "LegalReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        legalReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(legalReviewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, legalReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act: LegalReviewer with Legal domain context (matching domain)
        var context = new ResourceContextData(ReviewDomain: ReviewDomain.Legal);
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid(), context);

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(available);
        Assert.Equal("permission.validateEvidence", available!.LabelKey);
    }

    [Fact]
    public async Task SEC_EV_001_SecurityReviewerCanValidateEvidence_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var securityReviewer = CreateAndSeedRole(db, "SecurityReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        securityReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(securityReviewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, securityReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act: SecurityReviewer with Security domain context (matching domain)
        var context = new ResourceContextData(ReviewDomain: ReviewDomain.Security);
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid(), context);

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(available);
        Assert.Equal("permission.validateEvidence", available!.LabelKey);
    }

    [Fact]
    public async Task SEC_EV_001_LegalReviewerCannotValidateSecurity_ReturnsBlocked()
    {
        // Arrange: LegalReviewer tries to validate a Security domain requirement
        await using var db = CreateDb();
        var legalReviewer = CreateAndSeedRole(db, "LegalReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        legalReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(legalReviewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, legalReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act: LegalReviewer with Security domain context
        var context = new ResourceContextData(ReviewDomain: ReviewDomain.Security);
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid(), context);

        // Assert: Must be blocked
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EV-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_EV_001_SecurityReviewerCannotValidateLegal_ReturnsBlocked()
    {
        // Arrange: SecurityReviewer tries to validate a Legal domain requirement
        await using var db = CreateDb();
        var securityReviewer = CreateAndSeedRole(db, "SecurityReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        securityReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(securityReviewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, securityReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act: SecurityReviewer with Legal domain context
        var context = new ResourceContextData(ReviewDomain: ReviewDomain.Legal);
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid(), context);

        // Assert: Must be blocked
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EV-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_EV_001_NonReviewerRoleCannotValidate_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var viewer = CreateAndSeedRole(db, "Viewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        // Viewer role DOES have validate permission technically
        // but will be blocked by SEC-EV-001 because Viewer is not a reviewer role
        viewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(viewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        // Assign only Viewer, not LegalReviewer or SecurityReviewer
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, viewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EV-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_EV_001_BothReviewersCanValidate()
    {
        // Arrange
        await using var db = CreateDb();
        var legalReviewer = CreateAndSeedRole(db, "LegalReviewer");
        var securityReviewer = CreateAndSeedRole(db, "SecurityReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");

        legalReviewer.AddPermission(validatePerm);
        securityReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(
            legalReviewer.Permissions.Concat(securityReviewer.Permissions));

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        // Assign both roles
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, legalReviewer.Id, tenantId));
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, securityReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act: User with both roles can validate Legal requirements
        var context = new ResourceContextData(ReviewDomain: ReviewDomain.Legal);
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "evidence", Guid.NewGuid(), context);

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(available);
        var roleCodes = result.RoleCodes;
        Assert.Contains("LegalReviewer", roleCodes);
        Assert.Contains("SecurityReviewer", roleCodes);
    }
}
