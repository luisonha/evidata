using Evidata.Modules.Evidence.Domain;
using Xunit;

namespace Evidata.Tests.Unit.Evidence;

public class EvidenceDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    private static Modules.Evidence.Domain.Evidence CreateSample(
        EvidenceSensitivity sensitivity = EvidenceSensitivity.Internal) =>
        Modules.Evidence.Domain.Evidence.Create(
            TenantId, "Política de privacidad v2.1",
            EvidenceType.Policy, sensitivity, UserId,
            description: "Política actualizada conforme Ley 21.719",
            tags: "privacidad,politica,2024");

    [Fact]
    public void Create_ValidParams_StatusIsDraft()
    {
        var ev = CreateSample();

        Assert.Equal(EvidenceStatus.Draft, ev.Status);
        Assert.Equal(TenantId, ev.TenantId);
        Assert.Equal(EvidenceType.Policy, ev.Type);
        Assert.Null(ev.BlobPath);
        Assert.NotEqual(Guid.Empty, ev.Id);
    }

    [Fact]
    public void AttachBlob_SetsPathAndContentType()
    {
        var ev = CreateSample();

        ev.AttachBlob($"{TenantId}/2026/07/08/guid_politica.pdf", "application/pdf", 204800, UserId);

        Assert.NotNull(ev.BlobPath);
        Assert.Equal("application/pdf", ev.ContentType);
        Assert.Equal(204800, ev.SizeBytes);
        Assert.NotNull(ev.UpdatedAt);
    }

    [Fact]
    public void Activate_FromDraft_TransitionsToActive()
    {
        var ev = CreateSample();

        ev.Activate(UserId);

        Assert.Equal(EvidenceStatus.Active, ev.Status);
    }

    [Fact]
    public void Activate_FromActive_Throws()
    {
        var ev = CreateSample();
        ev.Activate(UserId);

        Assert.Throws<InvalidOperationException>(() => ev.Activate(UserId));
    }

    [Fact]
    public void SoftDelete_RequiresReason()
    {
        var ev = CreateSample();

        Assert.Throws<ArgumentException>(() => ev.SoftDelete("", UserId));
    }

    [Fact]
    public void SoftDelete_SetsStatusDeletedAndReason()
    {
        var ev = CreateSample();
        const string reason = "Política reemplazada por v2.2";

        ev.SoftDelete(reason, UserId);

        Assert.Equal(EvidenceStatus.Deleted, ev.Status);
        Assert.Equal(reason, ev.DeletionReason);
    }

    [Fact]
    public void Supersede_ChangesStatusToSuperseded()
    {
        var ev = CreateSample();
        ev.Activate(UserId);

        ev.Supersede(Guid.NewGuid(), UserId);

        Assert.Equal(EvidenceStatus.Superseded, ev.Status);
    }

    [Fact]
    public void Archive_ChangesStatusToArchived()
    {
        var ev = CreateSample();

        ev.Archive(UserId);

        Assert.Equal(EvidenceStatus.Archived, ev.Status);
    }

    [Fact]
    public void MarkSupersedes_SetsSupersedesId()
    {
        var ev = CreateSample();
        var oldId = Guid.NewGuid();

        ev.MarkSupersedes(oldId, UserId);

        Assert.Equal(oldId, ev.SupersedesEvidenceId);
    }

    [Fact]
    public void Create_EmptyTitle_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Modules.Evidence.Domain.Evidence.Create(
                TenantId, "", EvidenceType.Other, EvidenceSensitivity.Internal, UserId));
    }

    [Fact]
    public void AttachBlob_NegativeSize_Throws()
    {
        var ev = CreateSample();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ev.AttachBlob("path/file.pdf", "application/pdf", -1, UserId));
    }

    [Fact]
    public void UpdateTags_SetsTags()
    {
        var ev = CreateSample();

        ev.UpdateTags("nuevo,tag", UserId);

        Assert.Equal("nuevo,tag", ev.Tags);
        Assert.NotNull(ev.UpdatedAt);
    }
}
