using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Identity.Infrastructure.Persistence;

public class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options) { }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<UserProfileRole> UserProfileRoles => Set<UserProfileRole>();
    public DbSet<Invitation> Invitations => Set<Invitation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("identity");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserProfileConfiguration).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
