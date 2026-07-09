using Evidata.Modules.Security.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Security.Infrastructure.Persistence;

public class SecurityDbContext : DbContext
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("security");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoleConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
