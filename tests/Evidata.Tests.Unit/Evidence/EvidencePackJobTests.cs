using Evidata.Modules.Evidence.Domain;

namespace Evidata.Tests.Unit.Evidence;

public class EvidencePackJobTests
{
    private static List<Guid> Ids(int count) =>
        Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToList();

    // ── TC1: Create OK ────────────────────────────────────────────────────────
    [Fact]
    public void Create_Valid_ReturnsPendingJob()
    {
        var tenantId = Guid.NewGuid();
        var job = EvidencePackJob.Create(tenantId, Guid.NewGuid(), Ids(3));

        Assert.Equal(EvidencePackStatus.Pending, job.Status);
        Assert.Equal(3, job.EvidenceIds.Count);
        Assert.Equal(tenantId, job.TenantId);
        Assert.NotEqual(Guid.Empty, job.Id);
    }

    // ── TC2: Cero evidencias → excepción ─────────────────────────────────────
    [Fact]
    public void Create_EmptyIds_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), []));
    }

    // ── TC3: Más de 50 evidencias → excepción ────────────────────────────────
    [Fact]
    public void Create_OverLimit_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(51)));
    }

    // ── TC4: Exactamente 50 → OK ──────────────────────────────────────────────
    [Fact]
    public void Create_MaxLimit_OK()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(50));
        Assert.Equal(50, job.EvidenceIds.Count);
    }

    // ── TC5: Dedup de IDs duplicados ──────────────────────────────────────────
    [Fact]
    public void Create_DuplicateIds_Deduped()
    {
        var id = Guid.NewGuid();
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), [id, id, id]);
        Assert.Single(job.EvidenceIds);
    }

    // ── TC6: FSM Pending → Processing → Completed ────────────────────────────
    [Fact]
    public void FSM_PendingToCompleted_OK()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(2));
        job.MarkProcessing();
        Assert.Equal(EvidencePackStatus.Processing, job.Status);
        Assert.NotNull(job.StartedAt);

        job.MarkCompleted("tenants/abc/packs/job.zip");
        Assert.Equal(EvidencePackStatus.Completed, job.Status);
        Assert.NotNull(job.ResultBlobPath);
        Assert.NotNull(job.CompletedAt);
        Assert.NotNull(job.ExpiresAt);
    }

    // ── TC7: FSM Pending → Failed ─────────────────────────────────────────────
    [Fact]
    public void FSM_PendingToFailed_OK()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(1));
        job.MarkProcessing();
        job.MarkFailed("Blob not found");

        Assert.Equal(EvidencePackStatus.Failed, job.Status);
        Assert.Equal("Blob not found", job.ErrorMessage);
    }

    // ── TC8: MarkProcessing en estado no-Pending → excepción ─────────────────
    [Fact]
    public void MarkProcessing_AlreadyProcessing_Throws()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(1));
        job.MarkProcessing();
        Assert.Throws<InvalidOperationException>(() => job.MarkProcessing());
    }

    // ── TC9: MarkCompleted en Pending → excepción ────────────────────────────
    [Fact]
    public void MarkCompleted_FromPending_Throws()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(1));
        Assert.Throws<InvalidOperationException>(() => job.MarkCompleted("path/zip"));
    }

    // ── TC10: ExpiresAt = CompletedAt + 7 días ────────────────────────────────
    [Fact]
    public void MarkCompleted_ExpiresAt_Is7DaysAfterCompletion()
    {
        var job = EvidencePackJob.Create(Guid.NewGuid(), Guid.NewGuid(), Ids(1));
        job.MarkProcessing();
        job.MarkCompleted("path/to/zip");

        var diff = (job.ExpiresAt!.Value - job.CompletedAt!.Value).TotalDays;
        Assert.Equal(EvidencePackJob.ZipTtlDays, (int)Math.Round(diff));
    }
}
