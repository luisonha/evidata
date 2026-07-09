using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Links;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evidata.Tests.Unit.Evidence;

public class EvidenceLinkServiceTests
{
    private static EvidenceDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<EvidenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Modules.Evidence.Domain.Evidence BuildEvidence(Guid tenantId)
    {
        var ev = (Modules.Evidence.Domain.Evidence)
            System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(Modules.Evidence.Domain.Evidence));

        void Set<T>(string name, T val) => ev.GetType().GetProperty(name)!.SetValue(ev, val);
        Set(nameof(Modules.Evidence.Domain.Evidence.Id), Guid.NewGuid());
        Set(nameof(Modules.Evidence.Domain.Evidence.TenantId), tenantId);
        Set(nameof(Modules.Evidence.Domain.Evidence.Status), EvidenceStatus.Active);
        Set(nameof(Modules.Evidence.Domain.Evidence.Sensitivity), EvidenceSensitivity.Internal);
        Set(nameof(Modules.Evidence.Domain.Evidence.Title), "Ev");
        Set(nameof(Modules.Evidence.Domain.Evidence.Type), EvidenceType.Policy);
        Set(nameof(Modules.Evidence.Domain.Evidence.CreatedAt), DateTimeOffset.UtcNow);
        Set(nameof(Modules.Evidence.Domain.Evidence.CreatedBy), Guid.NewGuid());
        return ev;
    }

    private static EvidenceLinkService Build(EvidenceDbContext db) =>
        new(db, NullLogger<EvidenceLinkService>.Instance);

    // ── TC1: Agregar link → retorna LinkId ───────────────────────────────────
    [Fact]
    public async Task AddLink_Valid_ReturnsLinkId()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var result = await Build(db).AddLinkAsync(
            tenantId, ev.Id, LinkedEntityType.LegalObligation, Guid.NewGuid(), Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, result.LinkId);
        Assert.Equal(1, await db.EvidenceLinks.CountAsync());
    }

    // ── TC2: Duplicado activo → excepción ────────────────────────────────────
    [Fact]
    public async Task AddLink_Duplicate_Throws()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        var entityId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var svc = Build(db);
        await svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.Document, entityId, userId);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.Document, entityId, userId));
    }

    // ── TC3: Evidence de otro tenant → KeyNotFoundException ──────────────────
    [Fact]
    public async Task AddLink_WrongTenant_Throws()
    {
        var ownerTenant = Guid.NewGuid();
        var ev = BuildEvidence(ownerTenant);

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Build(db).AddLinkAsync(
                Guid.NewGuid(), ev.Id, LinkedEntityType.Document, Guid.NewGuid(), Guid.NewGuid()));
    }

    // ── TC4: RemoveLink → soft delete ────────────────────────────────────────
    [Fact]
    public async Task RemoveLink_Existing_SoftDeletes()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        var userId = Guid.NewGuid();

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var svc = Build(db);
        var addResult = await svc.AddLinkAsync(
            tenantId, ev.Id, LinkedEntityType.AuditFinding, Guid.NewGuid(), userId);

        await svc.RemoveLinkAsync(tenantId, addResult.LinkId, userId);

        var link = await db.EvidenceLinks.FindAsync(addResult.LinkId);
        Assert.NotNull(link!.DeletedAt);
        Assert.Equal(userId, link.DeletedBy);
    }

    // ── TC5: RemoveLink link ya eliminado → excepción ─────────────────────────
    [Fact]
    public async Task RemoveLink_AlreadyDeleted_ThrowsNotFound()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        var userId = Guid.NewGuid();

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var svc = Build(db);
        var addResult = await svc.AddLinkAsync(
            tenantId, ev.Id, LinkedEntityType.AuditFinding, Guid.NewGuid(), userId);
        await svc.RemoveLinkAsync(tenantId, addResult.LinkId, userId);

        // Con el query filter DeletedAt == null, el link eliminado es invisible:
        // el servicio lanza KeyNotFoundException ("no encontrado") en vez de
        // InvalidOperationException ("ya eliminado").
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            svc.RemoveLinkAsync(tenantId, addResult.LinkId, userId));
    }

    // ── TC6: GetLinks → solo activos ──────────────────────────────────────────
    [Fact]
    public async Task GetLinks_ReturnsOnlyActive()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        var userId = Guid.NewGuid();

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var svc = Build(db);
        var r1 = await svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.Document, Guid.NewGuid(), userId);
        var r2 = await svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.LegalObligation, Guid.NewGuid(), userId);
        await svc.RemoveLinkAsync(tenantId, r2.LinkId, userId); // eliminar el segundo

        var links = await svc.GetLinksAsync(tenantId, ev.Id);

        Assert.Single(links);
        Assert.Equal(r1.LinkId, links[0].LinkId);
    }

    // ── TC7: Mismo entityId, distinto tipo → NO duplicado ────────────────────
    [Fact]
    public async Task AddLink_SameEntityDifferentType_Allowed()
    {
        var tenantId = Guid.NewGuid();
        var ev = BuildEvidence(tenantId);
        var entityId = Guid.NewGuid();

        await using var db = BuildContext();
        db.Evidences.Add(ev);
        await db.SaveChangesAsync();

        var svc = Build(db);
        await svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.Document, entityId, Guid.NewGuid());
        await svc.AddLinkAsync(tenantId, ev.Id, LinkedEntityType.LegalObligation, entityId, Guid.NewGuid());

        Assert.Equal(2, await db.EvidenceLinks.CountAsync());
    }
}
