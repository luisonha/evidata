using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Infrastructure.Evidences;
using NSubstitute;

namespace Evidata.Tests.Unit.ProcessingInventory;

public class RatEvidenciasTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _activityId = Guid.NewGuid();
    private static readonly Guid _evidenceId = Guid.NewGuid();
    private static readonly Guid _userId = Guid.NewGuid();

    private static (IRatEvidenceService svc, IEvidenceLinkService mock) Build()
    {
        var mock = Substitute.For<IEvidenceLinkService>();
        return (new RatEvidenceService(mock), mock);
    }

    // ── AddEvidenceToRat ──────────────────────────────────────────────────────

    [Fact]
    public async Task AddEvidence_DelegatesToLinkService_WithProcessingActivityType()
    {
        var (svc, mock) = Build();
        var linkId = Guid.NewGuid();
        mock.AddLinkAsync(_tenantId, _evidenceId, LinkedEntityType.ProcessingActivity,
            _activityId, _userId, null, Arg.Any<CancellationToken>())
            .Returns(new AddLinkResult(linkId));

        var result = await svc.AddEvidenceToRatAsync(_tenantId, _activityId, _evidenceId, _userId);

        Assert.Equal(linkId, result);
        await mock.Received(1).AddLinkAsync(
            _tenantId, _evidenceId, LinkedEntityType.ProcessingActivity,
            _activityId, _userId, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvidence_ForwardsNote()
    {
        var (svc, mock) = Build();
        var linkId = Guid.NewGuid();
        mock.AddLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkedEntityType>(),
            Arg.Any<Guid>(), Arg.Any<Guid>(), "Nota importante", Arg.Any<CancellationToken>())
            .Returns(new AddLinkResult(linkId));

        await svc.AddEvidenceToRatAsync(_tenantId, _activityId, _evidenceId, _userId, "Nota importante");

        await mock.Received(1).AddLinkAsync(
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkedEntityType>(),
            Arg.Any<Guid>(), Arg.Any<Guid>(), "Nota importante", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddEvidence_PropagatesException_OnDuplicate()
    {
        var (svc, mock) = Build();
        mock.AddLinkAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<LinkedEntityType>(),
            Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<AddLinkResult>(_ => throw new InvalidOperationException("Ya existe un vínculo activo."));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.AddEvidenceToRatAsync(_tenantId, _activityId, _evidenceId, _userId));
    }

    // ── RemoveEvidenceFromRat ─────────────────────────────────────────────────

    [Fact]
    public async Task RemoveEvidence_DelegatesToRemoveLink()
    {
        var (svc, mock) = Build();
        var linkId = Guid.NewGuid();

        await svc.RemoveEvidenceFromRatAsync(_tenantId, linkId, _userId);

        await mock.Received(1).RemoveLinkAsync(_tenantId, linkId, _userId, Arg.Any<CancellationToken>());
    }

    // ── GetEvidencesForRat ────────────────────────────────────────────────────

    [Fact]
    public async Task GetEvidences_CallsGetLinksByEntity_WithProcessingActivityType()
    {
        var (svc, mock) = Build();
        var evidenceId2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        mock.GetLinksByEntityAsync(_tenantId, LinkedEntityType.ProcessingActivity, _activityId,
            Arg.Any<CancellationToken>())
            .Returns(new List<EvidenceLinkDto>
            {
                new(Guid.NewGuid(), LinkedEntityType.ProcessingActivity, _evidenceId, "Nota1", _userId, now),
                new(Guid.NewGuid(), LinkedEntityType.ProcessingActivity, evidenceId2, null, _userId, now)
            });

        var result = await svc.GetEvidencesForRatAsync(_tenantId, _activityId);

        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.NotEqual(Guid.Empty, r.EvidenceId));
    }

    [Fact]
    public async Task GetEvidences_MapsEvidenceIdFromLinkedEntityId()
    {
        var (svc, mock) = Build();
        var now = DateTimeOffset.UtcNow;
        var specificEvidenceId = Guid.NewGuid();

        mock.GetLinksByEntityAsync(Arg.Any<Guid>(), Arg.Any<LinkedEntityType>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<EvidenceLinkDto>
            {
                new(Guid.NewGuid(), LinkedEntityType.ProcessingActivity, specificEvidenceId, "nota", _userId, now)
            });

        var result = await svc.GetEvidencesForRatAsync(_tenantId, _activityId);

        Assert.Single(result);
        Assert.Equal(specificEvidenceId, result[0].EvidenceId);
        Assert.Equal("nota", result[0].Note);
    }

    [Fact]
    public async Task GetEvidences_ReturnsEmpty_WhenNoLinks()
    {
        var (svc, mock) = Build();
        mock.GetLinksByEntityAsync(Arg.Any<Guid>(), Arg.Any<LinkedEntityType>(), Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new List<EvidenceLinkDto>());

        var result = await svc.GetEvidencesForRatAsync(_tenantId, _activityId);

        Assert.Empty(result);
    }
}
