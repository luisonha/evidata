using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Security;

/// <summary>
/// Tests for the 6 critical RBAC permission rules from the contract.
///
/// Each rule has an associated test code (SEC-*-001):
/// - SEC-APP-001: ProcessOwner cannot approve their own processing activity
/// - SEC-ACT-001: Only TenantOwner/ComplianceAdmin can activate
/// - SEC-EV-001: Validator role depends on EvidenceRequirement.reviewDomain
/// - SEC-GAP-001: Only TenantOwner/ComplianceAdmin can accept gaps with risk
/// - SEC-EXP-001: Only authorized roles can generate official exports
/// - SEC-EVDOWN-001: Viewer cannot download sensitive evidence
/// </summary>
public class CriticalRbacRulesTests
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

    private static Permission CreateAndSeedPermission(SecurityDbContext db, string resource, string action)
    {
        var perm = Permission.Create(resource, action);
        db.Permissions.Add(perm);
        db.SaveChanges();
        return perm;
    }

    // ── SEC-APP-001: ProcessOwner cannot approve their own processing activity ──

    [Fact]
    public async Task SEC_APP_001_ProcessOwnerCannotApproveOwnActivity_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var processOwner = CreateAndSeedRole(db, "ProcessOwner");
        var approvePerm = CreateAndSeedPermission(db, "processingActivity", "approve");
        
        processOwner.AddPermission(approvePerm);
        db.RolePermissions.AddRange(processOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);
        var context = new ResourceContextData(ResourceOwnerId: userId); // Same as user = cannot approve

        // Act
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "processingActivity", Guid.NewGuid(), context);

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ApproveProcessingActivity");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-APP-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_APP_001_ProcessOwnerCanApproveOtherActivity_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var processOwner = CreateAndSeedRole(db, "ProcessOwner");
        var approvePerm = CreateAndSeedPermission(db, "processingActivity", "approve");
        
        processOwner.AddPermission(approvePerm);
        db.RolePermissions.AddRange(processOwner.Permissions);

        var userId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid(); // Different owner
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);
        var context = new ResourceContextData(ResourceOwnerId: otherOwnerId);

        // Act
        var result = await service.GetResourcePermissionsAsync(
            userId, tenantId, "processingActivity", Guid.NewGuid(), context);

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ApproveProcessingActivity");
        Assert.NotNull(available);
    }

    // ── SEC-ACT-001: Only TenantOwner/ComplianceAdmin can activate ──

    [Fact]
    public async Task SEC_ACT_001_ProcessOwnerCannotActivate_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var processOwner = CreateAndSeedRole(db, "ProcessOwner");
        var activatePerm = CreateAndSeedPermission(db, "processingActivity", "activate");
        
        processOwner.AddPermission(activatePerm);
        db.RolePermissions.AddRange(processOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "processingActivity", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ActivateProcessingActivity");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-ACT-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_ACT_001_TenantOwnerCanActivate_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantOwner = CreateAndSeedRole(db, "TenantOwner");
        var activatePerm = CreateAndSeedPermission(db, "processingActivity", "activate");
        
        tenantOwner.AddPermission(activatePerm);
        db.RolePermissions.AddRange(tenantOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, tenantOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "processingActivity", Guid.NewGuid());

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ActivateProcessingActivity");
        Assert.NotNull(available);
    }

    // ── SEC-EV-001: Validator role depends on evidence domain (Legal/Security) ──

    [Fact]
    public async Task SEC_EV_001_LegalReviewerCanValidate_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var legalReviewer = CreateAndSeedRole(db, "LegalReviewer");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");
        
        legalReviewer.AddPermission(validatePerm);
        db.RolePermissions.AddRange(legalReviewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, legalReviewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "evidence", Guid.NewGuid());

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(available);
    }

    [Fact]
    public async Task SEC_EV_001_ProcessOwnerCannotValidate_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var processOwner = CreateAndSeedRole(db, "ProcessOwner");
        var validatePerm = CreateAndSeedPermission(db, "evidence", "validate");
        
        processOwner.AddPermission(validatePerm);
        db.RolePermissions.AddRange(processOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "evidence", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "ValidateEvidence");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EV-001", blocked!.ReasonCode);
    }

    // ── SEC-GAP-001: Accept gap with risk - only TenantOwner/ComplianceAdmin ──

    [Fact]
    public async Task SEC_GAP_001_ProcessOwnerCannotAcceptGap_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var processOwner = CreateAndSeedRole(db, "ProcessOwner");
        var acceptGapPerm = CreateAndSeedPermission(db, "gap", "acceptWithRisk");
        
        processOwner.AddPermission(acceptGapPerm);
        db.RolePermissions.AddRange(processOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, processOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "gap", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "AcceptGapWithRisk");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-GAP-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_GAP_001_TenantOwnerCanAcceptGap_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var tenantOwner = CreateAndSeedRole(db, "TenantOwner");
        var acceptGapPerm = CreateAndSeedPermission(db, "gap", "acceptWithRisk");
        
        tenantOwner.AddPermission(acceptGapPerm);
        db.RolePermissions.AddRange(tenantOwner.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, tenantOwner.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "gap", Guid.NewGuid());

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "AcceptGapWithRisk");
        Assert.NotNull(available);
    }

    // ── SEC-EXP-001: Generate export - only authorized roles (not Viewer) ──

    [Fact]
    public async Task SEC_EXP_001_ViewerCannotGenerateExport_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var viewer = CreateAndSeedRole(db, "Viewer");
        var generateExportPerm = CreateAndSeedPermission(db, "export", "generate");
        
        viewer.AddPermission(generateExportPerm);
        db.RolePermissions.AddRange(viewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, viewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "export", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "GenerateOfficialExport");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EXP-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_EXP_001_ComplianceAdminCanGenerateExport_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var complianceAdmin = CreateAndSeedRole(db, "ComplianceAdmin");
        var generateExportPerm = CreateAndSeedPermission(db, "export", "generate");
        
        complianceAdmin.AddPermission(generateExportPerm);
        db.RolePermissions.AddRange(complianceAdmin.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, complianceAdmin.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "export", Guid.NewGuid());

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "GenerateOfficialExport");
        Assert.NotNull(available);
    }

    // ── SEC-EVDOWN-001: Download evidence - Viewer cannot download ──

    [Fact]
    public async Task SEC_EVDOWN_001_ViewerCannotDownload_ReturnsBlocked()
    {
        // Arrange
        await using var db = CreateDb();
        var viewer = CreateAndSeedRole(db, "Viewer");
        var downloadPerm = CreateAndSeedPermission(db, "evidence", "download");
        
        viewer.AddPermission(downloadPerm);
        db.RolePermissions.AddRange(viewer.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, viewer.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "evidence", Guid.NewGuid());

        // Assert
        var blocked = result.BlockedActions.FirstOrDefault(a => a.ActionCode == "DownloadEvidence");
        Assert.NotNull(blocked);
        Assert.Equal("SEC-EVDOWN-001", blocked!.ReasonCode);
    }

    [Fact]
    public async Task SEC_EVDOWN_001_AuditorCanDownload_ReturnsAvailable()
    {
        // Arrange
        await using var db = CreateDb();
        var auditor = CreateAndSeedRole(db, "Auditor");
        var downloadPerm = CreateAndSeedPermission(db, "evidence", "download");
        
        auditor.AddPermission(downloadPerm);
        db.RolePermissions.AddRange(auditor.Permissions);

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        db.UserRoleAssignments.Add(UserRoleAssignment.Create(userId, auditor.Id, tenantId));
        db.SaveChanges();

        var service = new ResourcePermissionsQueryService(db);

        // Act
        var result = await service.GetResourcePermissionsAsync(userId, tenantId, "evidence", Guid.NewGuid());

        // Assert
        var available = result.AvailableActions.FirstOrDefault(a => a.ActionCode == "DownloadEvidence");
        Assert.NotNull(available);
    }
}
