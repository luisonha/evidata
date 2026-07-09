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
        
        // Seed RBAC roles from contract (RbacRoleCode enum)
        SeedRbacRoles(modelBuilder);
        
        // Seed permissions from contract (PermissionCode enum)
        SeedPermissions(modelBuilder);
        
        // Link permissions to roles
        SeedRolePermissions(modelBuilder);
        
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Seed the 7 standard RBAC roles from the contract.
    /// </summary>
    private static void SeedRbacRoles(ModelBuilder modelBuilder)
    {
        var roles = new[]
        {
            Role.Create("TenantOwner", "Tenant owner - highest privilege", isSystemRole: true),
            Role.Create("ComplianceAdmin", "Compliance administrator", isSystemRole: true),
            Role.Create("ProcessOwner", "Process owner - manages individual processing activities", isSystemRole: true),
            Role.Create("LegalReviewer", "Legal domain reviewer for evidence validation", isSystemRole: true),
            Role.Create("SecurityReviewer", "Security domain reviewer for evidence validation", isSystemRole: true),
            Role.Create("Auditor", "Auditor - read-only access with audit rights", isSystemRole: true),
            Role.Create("Viewer", "Viewer - read-only access to published information", isSystemRole: true)
        };

        modelBuilder.Entity<Role>().HasData(roles);
    }

    /// <summary>
    /// Seed the 6 critical permissions from the contract.
    /// </summary>
    private static void SeedPermissions(ModelBuilder modelBuilder)
    {
        var permissions = new[]
        {
            Permission.Create("processingActivity", "approve", "Approve a processing activity for review"),
            Permission.Create("processingActivity", "activate", "Activate an approved processing activity"),
            Permission.Create("evidence", "validate", "Validate evidence requirements"),
            Permission.Create("gap", "acceptWithRisk", "Accept gaps with risk justification"),
            Permission.Create("export", "generate", "Generate official exports"),
            Permission.Create("evidence", "download", "Download evidence files")
        };

        modelBuilder.Entity<Permission>().HasData(permissions);
    }

    /// <summary>
    /// Seed default role-permission mappings.
    /// Maps permissions to roles according to the contract.
    /// </summary>
    private static void SeedRolePermissions(ModelBuilder modelBuilder)
    {
        // Get the seed role IDs (deterministic based on seed order)
        // We'll use sequential GUIDs for seeding to ensure consistency
        var tenantOwnerId = new Guid("00000000-0000-0000-0000-000000000001");
        var complianceAdminId = new Guid("00000000-0000-0000-0000-000000000002");
        var processOwnerId = new Guid("00000000-0000-0000-0000-000000000003");
        var legalReviewerId = new Guid("00000000-0000-0000-0000-000000000004");
        var securityReviewerId = new Guid("00000000-0000-0000-0000-000000000005");
        var auditorId = new Guid("00000000-0000-0000-0000-000000000006");
        var viewerId = new Guid("00000000-0000-0000-0000-000000000007");

        var approvePermId = new Guid("00000001-0000-0000-0000-000000000001");
        var activatePermId = new Guid("00000001-0000-0000-0000-000000000002");
        var validateEvidencePermId = new Guid("00000001-0000-0000-0000-000000000003");
        var acceptGapPermId = new Guid("00000001-0000-0000-0000-000000000004");
        var generateExportPermId = new Guid("00000001-0000-0000-0000-000000000005");
        var downloadEvidencePermId = new Guid("00000001-0000-0000-0000-000000000006");

        var rolePermissions = new[]
        {
            // TenantOwner - all permissions
            new RolePermission(tenantOwnerId, approvePermId),
            new RolePermission(tenantOwnerId, activatePermId),
            new RolePermission(tenantOwnerId, validateEvidencePermId),
            new RolePermission(tenantOwnerId, acceptGapPermId),
            new RolePermission(tenantOwnerId, generateExportPermId),
            new RolePermission(tenantOwnerId, downloadEvidencePermId),

            // ComplianceAdmin - all permissions
            new RolePermission(complianceAdminId, approvePermId),
            new RolePermission(complianceAdminId, activatePermId),
            new RolePermission(complianceAdminId, validateEvidencePermId),
            new RolePermission(complianceAdminId, acceptGapPermId),
            new RolePermission(complianceAdminId, generateExportPermId),
            new RolePermission(complianceAdminId, downloadEvidencePermId),

            // ProcessOwner - approve, validate, export, download (not activate, not accept gap)
            new RolePermission(processOwnerId, approvePermId),
            new RolePermission(processOwnerId, validateEvidencePermId),
            new RolePermission(processOwnerId, generateExportPermId),
            new RolePermission(processOwnerId, downloadEvidencePermId),

            // LegalReviewer - validate evidence (legal domain)
            new RolePermission(legalReviewerId, validateEvidencePermId),
            new RolePermission(legalReviewerId, downloadEvidencePermId),

            // SecurityReviewer - validate evidence (security domain)
            new RolePermission(securityReviewerId, validateEvidencePermId),
            new RolePermission(securityReviewerId, downloadEvidencePermId),

            // Auditor - read-only access via GetPermissionsAsync
            new RolePermission(auditorId, validateEvidencePermId),
            new RolePermission(auditorId, downloadEvidencePermId),

            // Viewer - minimal access
            new RolePermission(viewerId, downloadEvidencePermId)
        };

        modelBuilder.Entity<RolePermission>().HasData(rolePermissions);
    }
}
