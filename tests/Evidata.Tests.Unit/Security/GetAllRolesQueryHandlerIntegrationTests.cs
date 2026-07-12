using Evidata.Modules.Security.Application.DTOs;
using Evidata.Modules.Security.Application.Queries;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for GetAllRolesQueryHandler integration.
/// Validates that the handler works end-to-end with the repository.
/// </summary>
public class GetAllRolesQueryHandlerIntegrationTests
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
    /// Handler returns all roles from repository with correct DTO structure.
    /// </summary>
    [Fact]
    public async Task HandleAsync_ReturnsAllRoles_WithExpectedCount()
    {
        // Arrange
        await using var db = CreateDb();
        
        CreateAndSeedRole(db, "Role1", "Description 1");
        CreateAndSeedRole(db, "Role2", "Description 2");
        CreateAndSeedRole(db, "Role3", null); // No description

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);

        // Act
        var result = await handler.HandleAsync();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.IsAssignableFrom<IReadOnlyList<RoleCatalogDto>>(result);
    }

    /// <summary>
    /// DTO structure matches expected format: Id, Name, Description.
    /// </summary>
    [Fact]
    public async Task HandleAsync_DtoContainsRequiredFields()
    {
        // Arrange
        await using var db = CreateDb();
        var role = CreateAndSeedRole(db, "AdminRole", "Admin description");

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);

        // Act
        var result = await handler.HandleAsync();

        // Assert
        var dto = result.First();
        Assert.Equal(role.Id, dto.Id);
        Assert.Equal("AdminRole", dto.Name);
        Assert.Equal("Admin description", dto.Description);
    }

    /// <summary>
    /// Handler properly handles CancellationToken.
    /// </summary>
    [Fact]
    public async Task HandleAsync_WithCancellationToken_Completes()
    {
        // Arrange
        await using var db = CreateDb();
        CreateAndSeedRole(db, "TestRole", "Test");

        var roleRepository = new RoleRepository(db);
        var handler = new GetAllRolesQueryHandler(roleRepository);
        var ct = CancellationToken.None;

        // Act
        var result = await handler.HandleAsync(ct);

        // Assert
        Assert.NotEmpty(result);
    }
}
