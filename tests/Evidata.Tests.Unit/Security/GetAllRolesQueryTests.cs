using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Application.Queries;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for GetAllRolesQuery and GetAllRolesQueryHandler.
/// 
/// Validates:
/// - GetAllRolesQuery returns all roles with id, name, and description
/// - Roles are correctly mapped to RoleCatalogDto
/// - Empty result when no roles exist
/// </summary>
public class GetAllRolesQueryTests
{
    private static SecurityDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SecurityDbContext(opts);
    }

    private static Role CreateAndSeedRole(SecurityDbContext db, string name, string? description = null)
    {
        var role = Role.Create(name, description, isSystemRole: true);
        db.Roles.Add(role);
        db.SaveChanges();
        return role;
    }

    /// <summary>
    /// GetAllRolesQuery returns all seeded roles with correct properties.
    /// </summary>
    [Fact]
    public async Task GetAllRolesQuery_ReturnsAllRoles_WithCorrectProperties()
    {
        // Arrange
        await using var db = CreateDb();
        
        var tenantOwner = CreateAndSeedRole(db, "TenantOwner", "Tenant owner - highest privilege");
        var complianceAdmin = CreateAndSeedRole(db, "ComplianceAdmin", "Compliance administrator");
        var processOwner = CreateAndSeedRole(db, "ProcessOwner", "Process owner - manages activities");
        var viewer = CreateAndSeedRole(db, "Viewer", "Viewer - read-only access");

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);
        var query = new GetAllRolesQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        
        var roleDtos = result.ToList();
        
        // Verify TenantOwner
        var tenantOwnerDto = roleDtos.FirstOrDefault(r => r.Name == "TenantOwner");
        Assert.NotNull(tenantOwnerDto);
        Assert.Equal("Tenant owner - highest privilege", tenantOwnerDto.Description);
        
        // Verify Viewer
        var viewerDto = roleDtos.FirstOrDefault(r => r.Name == "Viewer");
        Assert.NotNull(viewerDto);
        Assert.Equal("Viewer - read-only access", viewerDto.Description);
    }

    /// <summary>
    /// GetAllRolesQuery correctly maps all role properties to RoleCatalogDto.
    /// </summary>
    [Fact]
    public async Task GetAllRolesQuery_CorrectlyMapsRolesToDto()
    {
        // Arrange
        await using var db = CreateDb();
        
        var role = CreateAndSeedRole(db, "TestRole", "A test role");

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);
        var query = new GetAllRolesQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.IsType<RoleCatalogDto>(dto);
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal("TestRole", dto.Name);
        Assert.Equal("A test role", dto.Description);
    }

    /// <summary>
    /// GetAllRolesQuery handles null descriptions gracefully.
    /// </summary>
    [Fact]
    public async Task GetAllRolesQuery_HandlesNullDescriptionGracefully()
    {
        // Arrange
        await using var db = CreateDb();
        
        var role = Role.Create("NoDescriptionRole", description: null, isSystemRole: true);
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);
        var query = new GetAllRolesQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Null(dto.Description);
    }

    /// <summary>
    /// GetAllRolesQuery returns empty list when no roles exist.
    /// </summary>
    [Fact]
    public async Task GetAllRolesQuery_ReturnsEmptyList_WhenNoRolesExist()
    {
        // Arrange
        await using var db = CreateDb();
        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);
        var query = new GetAllRolesQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
