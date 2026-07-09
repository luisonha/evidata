using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests de aislamiento cross-tenant para AuthorizationEvaluator.
///
/// Verifica que los permisos de un tenant NO son visibles desde otro tenant,
/// y que la combinación userId+tenantId se trata siempre como par atómico.
/// </summary>
public class CrossTenantIsolationTests
{
    private static SecurityDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<SecurityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new SecurityDbContext(opts);
    }

    private static (Role role, Permission perm) SeedRoleWithPermission(
        SecurityDbContext db, string resource, string action)
    {
        var perm = Permission.Create(resource, action);
        var role = Role.Create($"role-{resource}-{action}");
        role.AddPermission(perm);

        db.Permissions.Add(perm);
        db.Roles.Add(role);
        db.RolePermissions.AddRange(role.Permissions);
        db.SaveChanges();
        return (role, perm);
    }

    // ── Acceso legítimo ───────────────────────────────────────────────────────

    [Fact]
    public async Task HasPermission_UserWithRoleInTenant_ReturnsTrue()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (role, _) = SeedRoleWithPermission(db, "documents", "read");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);
        var result = await evaluator.HasPermissionAsync(userId, tenantId, "documents", "read");

        Assert.True(result);
    }

    // ── Cross-tenant: usuario con rol en TenantA NO accede a TenantB ─────────

    [Fact]
    public async Task HasPermission_UserInTenantA_CannotAccessTenantB()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (role, _) = SeedRoleWithPermission(db, "documents", "read");
        // Asignar al usuario SOLO en TenantA
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, tenantA));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        // Acceso a TenantA: debe funcionar
        Assert.True(await evaluator.HasPermissionAsync(userId, tenantA, "documents", "read"));

        // Acceso a TenantB: DEBE FALLAR — aislamiento cross-tenant
        Assert.False(await evaluator.HasPermissionAsync(userId, tenantB, "documents", "read"));
    }

    // ── Cross-tenant: distintos usuarios en distintos tenants ────────────────

    [Fact]
    public async Task HasPermission_DifferentUsersInDifferentTenants_AreIsolated()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var (roleA, _) = SeedRoleWithPermission(db, "rat", "approve");
        var (roleB, _) = SeedRoleWithPermission(db, "gaps", "manage");

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userA, roleA.Id, tenantA));
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userB, roleB.Id, tenantB));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        // userA tiene acceso en su tenant
        Assert.True(await evaluator.HasPermissionAsync(userA, tenantA, "rat", "approve"));

        // userA NO tiene acceso cross-tenant a TenantB
        Assert.False(await evaluator.HasPermissionAsync(userA, tenantB, "rat", "approve"));
        Assert.False(await evaluator.HasPermissionAsync(userA, tenantB, "gaps", "manage"));

        // userB NO tiene acceso a TenantA
        Assert.False(await evaluator.HasPermissionAsync(userB, tenantA, "gaps", "manage"));
        Assert.False(await evaluator.HasPermissionAsync(userB, tenantA, "rat", "approve"));
    }

    // ── TenantId no puede ser sustituido por Guid.Empty ──────────────────────

    [Fact]
    public async Task HasPermission_EmptyTenantId_ReturnsFalse()
    {
        await using var db = CreateDb();
        var realTenant = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var (role, _) = SeedRoleWithPermission(db, "documents", "write");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, role.Id, realTenant));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);

        // Con tenantId vacío: NUNCA debe tener acceso
        Assert.False(await evaluator.HasPermissionAsync(userId, Guid.Empty, "documents", "write"));
    }

    // ── UserId vacío no tiene permisos ────────────────────────────────────────

    [Fact]
    public async Task HasPermission_EmptyUserId_ReturnsFalse()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var (role, _) = SeedRoleWithPermission(db, "documents", "delete");
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(Guid.NewGuid(), role.Id, tenantId));
        await db.SaveChangesAsync();

        var evaluator = new AuthorizationEvaluator(db);
        Assert.False(await evaluator.HasPermissionAsync(Guid.Empty, tenantId, "documents", "delete"));
    }
}
