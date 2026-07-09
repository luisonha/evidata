using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Authorization;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for TenantOwnerOrComplianceAdminHandler — the authorization handler that protects
/// role assignment/removal operations.
///
/// Validates:
/// - (a) TenantOwner/ComplianceAdmin CAN assign/remove roles
/// - (b) Other roles (Viewer, ProcessOwner, etc.) CANNOT (403)
/// - (c) TenantOwner of tenant A CANNOT assign roles in tenant B (cross-tenant isolation)
/// </summary>
public class RolesControllerAuthorizationTests
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

    private static async Task<bool> AuthorizeRequirementAsync(
        TenantOwnerOrComplianceAdminHandler handler,
        TenantOwnerOrComplianceAdminRequirement requirement)
    {
        return await handler.EvaluateAsync(requirement);
    }

    /// <summary>
    /// (a) TenantOwner CAN execute role assignment operations in their tenant.
    /// </summary>
    [Fact]
    public async Task TenantOwnerCanAssignRoles_SucceedsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var tenantOwnerRole = CreateAndSeedRole(db, "TenantOwner");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantId));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        currentUser.TenantId.Returns(tenantId);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// (a) ComplianceAdmin CAN execute role assignment operations in their tenant.
    /// </summary>
    [Fact]
    public async Task ComplianceAdminCanAssignRoles_SucceedsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var complianceAdminRole = CreateAndSeedRole(db, "ComplianceAdmin");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, complianceAdminRole.Id, tenantId));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        currentUser.TenantId.Returns(tenantId);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.True(result);
    }

    /// <summary>
    /// (b) Viewer CANNOT execute role assignment operations — fails authorization.
    /// </summary>
    [Fact]
    public async Task ViewerCannotAssignRoles_FailsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var viewerRole = CreateAndSeedRole(db, "Viewer");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, viewerRole.Id, tenantId));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        currentUser.TenantId.Returns(tenantId);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// (b) ProcessOwner CANNOT execute role assignment operations — fails authorization.
    /// </summary>
    [Fact]
    public async Task ProcessOwnerCannotAssignRoles_FailsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var processOwnerRole = CreateAndSeedRole(db, "ProcessOwner");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwnerRole.Id, tenantId));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        currentUser.TenantId.Returns(tenantId);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// (c) TenantOwner of tenant A CANNOT assign roles in tenant B — cross-tenant isolation enforced.
    /// When the authorization handler checks ICurrentUserContext.TenantId (which is B),
    /// it queries roles for the user in tenant B. Since the user only has roles in tenant A,
    /// the check fails.
    /// </summary>
    [Fact]
    public async Task TenantOwnerOfTenantACannnotAssignRolesInTenantB_CrossTenantIsolationEnforced()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var tenantOwnerRole = CreateAndSeedRole(db, "TenantOwner");
        
        // User has TenantOwner role ONLY in tenant A
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantA));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        // Simulate a request to tenant B (via ICurrentUserContext, which reflects the request's tenant)
        currentUser.TenantId.Returns(tenantB);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Unauthenticated user (IsAuthenticated = false) CANNOT execute role assignment operations.
    /// </summary>
    [Fact]
    public async Task UnauthenticatedUser_FailsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        
        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.IsAuthenticated.Returns(false);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// User with multiple roles where ONE of them is TenantOwner CAN assign roles.
    /// </summary>
    [Fact]
    public async Task UserWithMultipleRolesIncludingTenantOwner_SucceedsAuthorization()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        
        var tenantOwnerRole = CreateAndSeedRole(db, "TenantOwner");
        var viewerRole = CreateAndSeedRole(db, "Viewer");
        
        // User has both Viewer and TenantOwner roles
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, viewerRole.Id, tenantId));
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, tenantOwnerRole.Id, tenantId));
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserContext>();
        currentUser.UserId.Returns(userId);
        currentUser.TenantId.Returns(tenantId);
        currentUser.IsAuthenticated.Returns(true);

        var handler = new TenantOwnerOrComplianceAdminHandler(currentUser, db);
        var requirement = new TenantOwnerOrComplianceAdminRequirement();

        // Act
        var result = await AuthorizeRequirementAsync(handler, requirement);

        // Assert
        Assert.True(result);
    }
}

