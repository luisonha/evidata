using Evidata.Modules.Security.Domain;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests de las reglas del dominio de seguridad.
///
/// Verifica: convención de nombres de permisos, creación de roles,
/// y que no se duplican asignaciones de permisos.
/// </summary>
public class SecurityDomainTests
{
    // ── Permission naming convention ──────────────────────────────────────────

    [Theory]
    [InlineData("documents", "read", "documents:read")]
    [InlineData("rat", "approve", "rat:approve")]
    [InlineData("gaps", "manage", "gaps:manage")]
    [InlineData("mcp", "query", "mcp:query")]
    [InlineData("reports", "generate", "reports:generate")]
    [InlineData("evidence", "write", "evidence:write")]
    public void Permission_Name_FollowsResourceActionConvention(
        string resource, string action, string expectedName)
    {
        var perm = Permission.Create(resource, action);

        Assert.Equal(expectedName, perm.Name);
        Assert.Equal(resource, perm.Resource);
        Assert.Equal(action, perm.Action);
    }

    [Fact]
    public void Permission_HasUniqueId()
    {
        var p1 = Permission.Create("documents", "read");
        var p2 = Permission.Create("documents", "read");

        Assert.NotEqual(p1.Id, p2.Id);
    }

    // ── Role ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Role_Create_HasEmptyPermissions()
    {
        var role = Role.Create("admin");
        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Role_AddPermission_AddsToCollection()
    {
        var perm = Permission.Create("rat", "read");
        var role = Role.Create("rat-reader");
        role.AddPermission(perm);

        Assert.Single(role.Permissions);
        Assert.Equal(perm.Id, role.Permissions[0].PermissionId);
    }

    [Fact]
    public void Role_AddPermission_NoDuplicates()
    {
        var perm = Permission.Create("rat", "read");
        var role = Role.Create("rat-reader");
        role.AddPermission(perm);
        role.AddPermission(perm); // duplicado intencional

        Assert.Single(role.Permissions);
    }

    [Fact]
    public void Role_RemovePermission_RemovesFromCollection()
    {
        var perm = Permission.Create("gaps", "manage");
        var role = Role.Create("gap-manager");
        role.AddPermission(perm);
        role.RemovePermission(perm.Id);

        Assert.Empty(role.Permissions);
    }

    [Fact]
    public void Role_RemovePermission_NonExistent_NoException()
    {
        var role = Role.Create("test");
        var ex = Record.Exception(() => role.RemovePermission(Guid.NewGuid()));
        Assert.Null(ex);
    }

    // ── UserRoleAssignment ────────────────────────────────────────────────────

    [Fact]
    public void UserRoleAssignment_Create_SetsAllFields()
    {
        var userId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var assignment = UserRoleAssignment.Create(userId, roleId, tenantId);

        Assert.Equal(userId, assignment.UserId);
        Assert.Equal(roleId, assignment.RoleId);
        Assert.Equal(tenantId, assignment.TenantId);
        Assert.NotEqual(Guid.Empty, assignment.Id);
    }

    [Fact]
    public void UserRoleAssignment_Create_AssignedAtIsUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var assignment = UserRoleAssignment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var after = DateTime.UtcNow.AddSeconds(1);

        Assert.InRange(assignment.AssignedAt, before, after);
    }

    // ── Convención de recursos de cumplimiento (Ley 21.719) ───────────────────

    [Theory]
    [InlineData("rat")]
    [InlineData("gaps")]
    [InlineData("documents")]
    [InlineData("evidence")]
    [InlineData("mcp")]
    [InlineData("reports")]
    [InlineData("legal")]
    [InlineData("tenants")]
    [InlineData("identity")]
    public void Permission_KnownComplianceResources_CanBeCreated(string resource)
    {
        var actions = new[] { "read", "write", "delete", "manage" };
        foreach (var action in actions)
        {
            var perm = Permission.Create(resource, action);
            Assert.Equal($"{resource}:{action}", perm.Name);
        }
    }

    // ── SystemRole flag ───────────────────────────────────────────────────────

    [Fact]
    public void Role_SystemRole_FlagIsSet()
    {
        var role = Role.Create("SuperAdmin", isSystemRole: true);
        Assert.True(role.IsSystemRole);
    }

    [Fact]
    public void Role_NonSystemRole_FlagIsFalse()
    {
        var role = Role.Create("TenantAdmin");
        Assert.False(role.IsSystemRole);
    }
}
