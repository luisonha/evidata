using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Application.Queries;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for GetAllPermissionsQuery and GetAllPermissionsQueryHandler.
/// 
/// Validates:
/// - GetAllPermissionsQuery returns all permissions with id, name, resource, action, and description
/// - Permissions are correctly mapped to PermissionDto
/// - Empty result when no permissions exist
/// - Permissions maintain resource and action attributes
/// </summary>
public class GetAllPermissionsQueryTests
{
    private static SecurityDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SecurityDbContext(opts);
    }

    private static Permission CreateAndSeedPermission(SecurityDbContext db, string resource, string action, string? description = null)
    {
        var permission = Permission.Create(resource, action, description);
        db.Permissions.Add(permission);
        db.SaveChanges();
        return permission;
    }

    /// <summary>
    /// GetAllPermissionsQuery returns all seeded permissions with correct properties.
    /// </summary>
    [Fact]
    public async Task GetAllPermissionsQuery_ReturnsAllPermissions_WithCorrectProperties()
    {
        // Arrange
        await using var db = CreateDb();
        
        var approvePerms = CreateAndSeedPermission(db, "processingActivity", "approve", "Approve a processing activity for review");
        var activatePerms = CreateAndSeedPermission(db, "processingActivity", "activate", "Activate an approved processing activity");
        var validatePerms = CreateAndSeedPermission(db, "evidence", "validate", "Validate evidence requirements");
        var downloadPerms = CreateAndSeedPermission(db, "evidence", "download", "Download evidence files");

        var permissionRepository = new PermissionRepository(db);
        var handler = new GetAllPermissionsQueryHandler(permissionRepository);
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Count);
        
        var permDtos = result.ToList();
        
        // Verify approve permission
        var approvePerm = permDtos.FirstOrDefault(p => p.Action == "approve");
        Assert.NotNull(approvePerm);
        Assert.Equal("processingActivity", approvePerm.Resource);
        Assert.Equal("Approve a processing activity for review", approvePerm.Description);
        
        // Verify download permission
        var downloadPerm = permDtos.FirstOrDefault(p => p.Action == "download");
        Assert.NotNull(downloadPerm);
        Assert.Equal("evidence", downloadPerm.Resource);
        Assert.Equal("Download evidence files", downloadPerm.Description);
    }

    /// <summary>
    /// GetAllPermissionsQuery correctly maps all permission properties to PermissionDto.
    /// </summary>
    [Fact]
    public async Task GetAllPermissionsQuery_CorrectlyMapsPermissionsToDto()
    {
        // Arrange
        await using var db = CreateDb();
        
        var permission = CreateAndSeedPermission(db, "testResource", "testAction", "A test permission");

        var permissionRepository = new PermissionRepository(db);
        var handler = new GetAllPermissionsQueryHandler(permissionRepository);
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.IsType<PermissionDto>(dto);
        Assert.Equal(permission.Id, dto.Id);
        Assert.Equal("testResource:testAction", dto.Name);
        Assert.Equal("testResource", dto.Resource);
        Assert.Equal("testAction", dto.Action);
        Assert.Equal("A test permission", dto.Description);
    }

    /// <summary>
    /// GetAllPermissionsQuery handles null descriptions gracefully.
    /// </summary>
    [Fact]
    public async Task GetAllPermissionsQuery_HandlesNullDescriptionGracefully()
    {
        // Arrange
        await using var db = CreateDb();
        
        var permission = Permission.Create("noDescResource", "noDescAction", description: null);
        db.Permissions.Add(permission);
        await db.SaveChangesAsync();

        var permissionRepository = new PermissionRepository(db);
        var handler = new GetAllPermissionsQueryHandler(permissionRepository);
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Single(result);
        var dto = result.First();
        Assert.Null(dto.Description);
    }

    /// <summary>
    /// GetAllPermissionsQuery returns empty list when no permissions exist.
    /// </summary>
    [Fact]
    public async Task GetAllPermissionsQuery_ReturnsEmptyList_WhenNoPermissionsExist()
    {
        // Arrange
        await using var db = CreateDb();
        var permissionRepository = new PermissionRepository(db);
        var handler = new GetAllPermissionsQueryHandler(permissionRepository);
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    /// <summary>
    /// GetAllPermissionsQuery correctly builds permission names from resource and action.
    /// </summary>
    [Fact]
    public async Task GetAllPermissionsQuery_BuildsPermissionNameFromResourceAndAction()
    {
        // Arrange
        await using var db = CreateDb();
        
        CreateAndSeedPermission(db, "export", "generate");
        CreateAndSeedPermission(db, "gap", "acceptWithRisk");

        var permissionRepository = new PermissionRepository(db);
        var handler = new GetAllPermissionsQueryHandler(permissionRepository);
        var query = new GetAllPermissionsQuery();

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Equal(2, result.Count);
        var names = result.Select(p => p.Name).OrderBy(n => n).ToList();
        Assert.Equal("export:generate", names[0]);
        Assert.Equal("gap:acceptWithRisk", names[1]);
    }
}
