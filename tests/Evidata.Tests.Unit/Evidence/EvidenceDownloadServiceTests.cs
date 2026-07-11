using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Download;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Evidata.Modules.Security.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Evidata.Tests.Unit.Evidence;

public class EvidenceDownloadServiceTests
{
    private static EvidenceDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<EvidenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EvidenceDbContext(options);
    }

    private static Modules.Evidence.Domain.Evidence BuildActiveEvidence(
        Guid tenantId, string? blobPath = "tenants/abc/2024/01/01/guid_file.pdf")
    {
        var ev = (Modules.Evidence.Domain.Evidence)
            System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(Modules.Evidence.Domain.Evidence));

        Set(ev, nameof(Modules.Evidence.Domain.Evidence.Id), Guid.NewGuid());
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.TenantId), tenantId);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.Status), EvidenceStatus.Active);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.Sensitivity), EvidenceSensitivity.Internal);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.BlobPath), blobPath);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.ContentType), "application/pdf");
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.Title), "Test Evidence");
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.Type), EvidenceType.Policy);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.CreatedAt), DateTimeOffset.UtcNow);
        Set(ev, nameof(Modules.Evidence.Domain.Evidence.CreatedBy), Guid.NewGuid());

        return ev;
    }

    private static void Set<T>(object obj, string propName, T value) =>
        obj.GetType().GetProperty(propName)!.SetValue(obj, value);

    private static IBlobStorageService BuildBlobMock()
    {
        var blob = Substitute.For<IBlobStorageService>();
        blob.GenerateDownloadSasAsync(
                Arg.Any<string>(),
                Arg.Any<TimeSpan?>(),
                Arg.Any<CancellationToken>())
            .Returns(new SasDownloadResult(
                "https://storage.azure.com/sas-token",
                DateTimeOffset.UtcNow.AddMinutes(30)));
        return blob;
    }

    private static IResourcePermissionsQueryService BuildPermissionsServiceMock(
        bool isBlocked = false)
    {
        var permissions = Substitute.For<IResourcePermissionsQueryService>();
        
        var blockedActions = isBlocked
            ? new List<BlockedActionResult>
            {
                new("DownloadEvidence", "permission.downloadEvidence",
                    "SEC-EVDOWN-001", "block.downloadSensitive",
                    "High", "severity.high", "Evidence", "node.evidence")
            }
            : new List<BlockedActionResult>();

        var availableActions = !isBlocked
            ? new List<AvailableActionResult>
            {
                new("DownloadEvidence", "permission.downloadEvidence")
            }
            : new List<AvailableActionResult>();

        permissions.GetResourcePermissionsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(),
                Arg.Any<Guid>(), Arg.Any<ResourceContextData>(),
                Arg.Any<CancellationToken>())
            .Returns(new ResourcePermissionsResult(
                new List<string> { "Viewer" },
                availableActions,
                false,
                blockedActions));

        return permissions;
    }

    private static IAuditService BuildAuditServiceMock()
    {
        var audit = Substitute.For<IAuditService>();
        audit.LogAsync(
                Arg.Any<Guid>(), Arg.Any<Guid?>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<AuditEventResult>(),
                Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(),
                Arg.Any<string>(), Arg.Any<AuditSeverity>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return audit;
    }

    // ── TC1: Download OK genera AccessLog y AuditEvent.EvidenceDownloaded ──────
    [Fact]
    public async Task RequestDownload_ValidEvidence_CreatesAccessLogAndAudit()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var auditSvc = BuildAuditServiceMock();
        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            auditSvc, NullLogger<EvidenceDownloadService>.Instance);

        var result = await svc.RequestDownloadAsync(tenantId, evidence.Id, userId);

        Assert.Contains("sas-token", result.DownloadUrl);
        Assert.NotEqual(Guid.Empty, result.AccessLogId);

        var log = await db.EvidenceAccessLogs.FindAsync(result.AccessLogId);
        Assert.NotNull(log);
        Assert.Equal(evidence.Id, log.EvidenceId);
        Assert.Equal(tenantId, log.TenantId);
        Assert.Equal(userId, log.AccessedBy);

        // Verificar que se registró auditoría de éxito
        await auditSvc.Received(1).LogAsync(
            tenantId, userId, "EvidenceDownloaded", "Evidence",
            evidence.Id, AuditEventResult.Success,
            Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<string>(), AuditSeverity.Info, Arg.Any<CancellationToken>());
    }

    // ── TC2: SEC-EVDOWN-001: Viewer + Sensitive → 403 SensitiveEvidenceRestricted ──
    [Fact]
    public async Task RequestDownload_ViewerDownloadingSensitive_ThrowsAndAudits()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity),
            EvidenceSensitivity.Sensitive);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        // Mock: Viewer role con Sensitive evidence → bloqueado
        var auditSvc = BuildAuditServiceMock();
        var permSvc = BuildPermissionsServiceMock(isBlocked: true);

        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), permSvc,
            auditSvc, NullLogger<EvidenceDownloadService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, userId));

        Assert.Contains("SensitiveEvidenceRestricted", ex.Message);

        // Verificar que se registró auditoría de denegación
        await auditSvc.Received(1).LogAsync(
            tenantId, userId, "EvidenceAccessDenied", "Evidence",
            evidence.Id, AuditEventResult.Blocked,
            Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<string>(), AuditSeverity.Critical, Arg.Any<CancellationToken>());

        // Verificar que NO se creó AccessLog (excepción lanzada antes)
        var logs = db.EvidenceAccessLogs
            .Where(l => l.EvidenceId == evidence.Id)
            .ToList();
        Assert.Empty(logs);
    }

    // ── TC3: Sensitive sin reason → excepción y auditoría denegación ────────
    [Fact]
    public async Task RequestDownload_SensitiveWithoutReason_AuditsAndThrows()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity),
            EvidenceSensitivity.Sensitive);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var auditSvc = BuildAuditServiceMock();
        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            auditSvc, NullLogger<EvidenceDownloadService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, userId, reason: null));

        Assert.Contains("Missing required reason", ex.Message);

        // Verificar que se registró auditoría de denegación
        await auditSvc.Received(1).LogAsync(
            tenantId, userId, "EvidenceAccessDenied", "Evidence",
            evidence.Id, AuditEventResult.Blocked,
            Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<string>(), AuditSeverity.Critical, Arg.Any<CancellationToken>());

        // Verificar que NO se creó AccessLog
        var logs = db.EvidenceAccessLogs
            .Where(l => l.EvidenceId == evidence.Id)
            .ToList();
        Assert.Empty(logs);
    }

    // ── TC4: Evidence de otro tenant → KeyNotFoundException ──────────────────
    [Fact]
    public async Task RequestDownload_WrongTenant_Throws()
    {
        var ownerTenant = Guid.NewGuid();
        var evidence = BuildActiveEvidence(ownerTenant);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            BuildAuditServiceMock(), NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.RequestDownloadAsync(Guid.NewGuid(), evidence.Id, Guid.NewGuid()));
    }

    // ── TC5: Evidence sin BlobPath → excepción ───────────────────────────────
    [Fact]
    public async Task RequestDownload_NoBlobPath_Throws()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId, blobPath: null);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            BuildAuditServiceMock(), NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, Guid.NewGuid()));
    }

    // ── TC6: Evidence Deleted → excepción ────────────────────────────────────
    [Fact]
    public async Task RequestDownload_DeletedEvidence_Throws()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Status), EvidenceStatus.Deleted);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            BuildAuditServiceMock(), NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, Guid.NewGuid()));
    }

    // ── TC7: Sensitive con reason → OK + log con reason ──────────────────────
    [Fact]
    public async Task RequestDownload_SensitiveWithReason_LogsReasonAndAudits()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity),
            EvidenceSensitivity.Sensitive);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var auditSvc = BuildAuditServiceMock();
        var svc = new EvidenceDownloadService(
            db, BuildBlobMock(), BuildPermissionsServiceMock(),
            auditSvc, NullLogger<EvidenceDownloadService>.Instance);

        var result = await svc.RequestDownloadAsync(
            tenantId, evidence.Id, userId, reason: "Auditoría interna Q1");

        var log = await db.EvidenceAccessLogs.FindAsync(result.AccessLogId);
        Assert.Equal(EvidenceSensitivity.Sensitive, log!.SensitivityAtAccess);
        Assert.Equal("Auditoría interna Q1", log.Reason);

        // Verificar que se registró auditoría de éxito
        await auditSvc.Received(1).LogAsync(
            tenantId, userId, "EvidenceDownloaded", "Evidence",
            evidence.Id, AuditEventResult.Success,
            Arg.Any<string>(), Arg.Any<Dictionary<string, object?>>(),
            Arg.Any<string>(), AuditSeverity.Info, Arg.Any<CancellationToken>());
    }
}
