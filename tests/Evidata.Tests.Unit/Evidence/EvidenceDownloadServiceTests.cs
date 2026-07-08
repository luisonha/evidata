using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Download;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
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

    // ── TC1: Download OK genera AccessLog ────────────────────────────────────
    [Fact]
    public async Task RequestDownload_ValidEvidence_CreatesAccessLog()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        var result = await svc.RequestDownloadAsync(tenantId, evidence.Id, userId);

        Assert.Contains("sas-token", result.DownloadUrl);
        Assert.NotEqual(Guid.Empty, result.AccessLogId);

        var log = await db.EvidenceAccessLogs.FindAsync(result.AccessLogId);
        Assert.NotNull(log);
        Assert.Equal(evidence.Id, log.EvidenceId);
        Assert.Equal(tenantId, log.TenantId);
        Assert.Equal(userId, log.AccessedBy);
    }

    // ── TC2: Sensitive sin reason → excepción ────────────────────────────────
    [Fact]
    public async Task RequestDownload_SensitiveWithoutReason_Throws()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity),
            EvidenceSensitivity.Sensitive);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, Guid.NewGuid(), reason: null));
    }

    // ── TC3: Evidence de otro tenant → KeyNotFoundException ──────────────────
    [Fact]
    public async Task RequestDownload_WrongTenant_Throws()
    {
        var ownerTenant = Guid.NewGuid();
        var evidence = BuildActiveEvidence(ownerTenant);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.RequestDownloadAsync(Guid.NewGuid(), evidence.Id, Guid.NewGuid()));
    }

    // ── TC4: Evidence sin BlobPath → excepción ───────────────────────────────
    [Fact]
    public async Task RequestDownload_NoBlobPath_Throws()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId, blobPath: null);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, Guid.NewGuid()));
    }

    // ── TC5: Evidence Deleted → excepción ────────────────────────────────────
    [Fact]
    public async Task RequestDownload_DeletedEvidence_Throws()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Status), EvidenceStatus.Deleted);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.RequestDownloadAsync(tenantId, evidence.Id, Guid.NewGuid()));
    }

    // ── TC6: Sensitive con reason → OK + log con reason ──────────────────────
    [Fact]
    public async Task RequestDownload_SensitiveWithReason_LogsReason()
    {
        var tenantId = Guid.NewGuid();
        var evidence = BuildActiveEvidence(tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity),
            EvidenceSensitivity.Sensitive);

        await using var db = BuildContext();
        db.Evidences.Add(evidence);
        await db.SaveChangesAsync();

        var svc = new EvidenceDownloadService(db, BuildBlobMock(),
            NullLogger<EvidenceDownloadService>.Instance);

        var result = await svc.RequestDownloadAsync(
            tenantId, evidence.Id, Guid.NewGuid(), reason: "Auditoría interna Q1");

        var log = await db.EvidenceAccessLogs.FindAsync(result.AccessLogId);
        Assert.Equal(EvidenceSensitivity.Sensitive, log!.SensitivityAtAccess);
        Assert.Equal("Auditoría interna Q1", log.Reason);
    }
}
