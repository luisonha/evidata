using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests de las reglas de autorización RBAC.
///
/// Verifica: ausencia de permiso, permisos múltiples, separación de acciones
/// y que un permiso de un recurso no otorga acceso a otro.
/// </summary>
public class AuthorizationEvaluatorTests
{
    private static SecurityDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SecurityDbContext(opts);
    }

    // ── Sin asignación ────────────────────────────────────────────────────────

    [Fact]
    public async Task HasPermission_NoAssignment_ReturnsFalse()
    {
        await using var db = CreateDb();
        var evaluator = new AuthorizationEvaluator(db);

        var result = await evaluator.HasPermissionAsync(
            Guid.NewGuid(), Guid.NewGuid(), "documents", "read");

        Assert.False(result);
    }

    [Fact]
    public async Task GetPermissions_NoAssignment_ReturnsEmpty()
    {
        await using var db = CreateDb();
        var evaluator = new AuthorizationEvaluator(db);

        var result = await evaluator.GetPermissionsAsync(Guid.NewGuid(), Guid.NewGuid());

        Assert.Empty(result);
    }

    // ── Permiso correcto concede acceso ───────────────────────────────────────

    [Fact]
    public async Task HasPermission_ExactPermission_ReturnsTrue()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var perm = Permission.Create("rat", "read");
        var role = Role.Create("rat-reader");
        role.AddPermission(perm);

        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "read"));
    }

    // ── Acción diferente no concede acceso ────────────────────────────────────

    [Fact]
    public async Task HasPermission_DifferentAction_ReturnsFalse()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var perm = Permission.Create("rat", "read");
        var role = Role.Create("rat-reader");
        role.AddPermission(perm);

        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        // read concedido, write NO
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "read"));
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "write"));
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "delete"));
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "approve"));
    }

    // ── Recurso diferente no concede acceso ───────────────────────────────────

    [Fact]
    public async Task HasPermission_DifferentResource_ReturnsFalse()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var perm = Permission.Create("documents", "read");
        var role = Role.Create("doc-reader");
        role.AddPermission(perm);

        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "documents", "read"));
        // rat:read no debe ser visible por tener documents:read
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantId, "rat", "read"));
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantId, "gaps", "read"));
    }

    // ── Rol con múltiples permisos ────────────────────────────────────────────

    [Fact]
    public async Task HasPermission_RoleWithMultiplePermissions_AllGranted()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var permRead = Permission.Create("gaps", "read");
        var permWrite = Permission.Create("gaps", "write");
        var permManage = Permission.Create("gaps", "manage");
        var role = Role.Create("gap-manager");
        role.AddPermission(permRead);
        role.AddPermission(permWrite);
        role.AddPermission(permManage);

        db.Permissions.AddRange(permRead, permWrite, permManage);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "gaps", "read"));
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "gaps", "write"));
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, "gaps", "manage"));
    }

    // ── GetPermissions retorna lista completa sin duplicados ──────────────────

    [Fact]
    public async Task GetPermissions_MultipleRoles_ReturnsDistinctPermissions()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var permRead = Permission.Create("documents", "read");
        var permWrite = Permission.Create("documents", "write");
        var role1 = Role.Create("doc-reader");
        var role2 = Role.Create("doc-writer");
        role1.AddPermission(permRead);
        role2.AddPermission(permRead);  // duplicado intencionado
        role2.AddPermission(permWrite);

        db.Permissions.AddRange(permRead, permWrite);
        db.Roles.AddRange(role1, role2);
        db.RolePermissions.AddRange(role1.Permissions);
        db.RolePermissions.AddRange(role2.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role1.Id, tenantId));
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role2.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);
        var perms = await evaluator.GetPermissionsAsync(userId, tenantId);

        // No debe haber duplicados
        Assert.Equal(perms.Count, perms.Distinct().Count());
        Assert.Contains("documents:read", perms);
        Assert.Contains("documents:write", perms);
    }

    // ── Permisos de compliance Ley 21.719 ─────────────────────────────────────

    [Theory]
    [InlineData("rat", "read")]
    [InlineData("rat", "write")]
    [InlineData("rat", "approve")]
    [InlineData("gaps", "read")]
    [InlineData("gaps", "manage")]
    [InlineData("documents", "read")]
    [InlineData("documents", "write")]
    [InlineData("mcp", "query")]
    [InlineData("evidence", "read")]
    [InlineData("reports", "generate")]
    public async Task HasPermission_CompliancePermissions_CanBeGranted(string resource, string action)
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var perm = Permission.Create(resource, action);
        var role = Role.Create($"test-{resource}-{action}");
        role.AddPermission(perm);

        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantId, resource, action));
    }
}
