using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Tests.Unit.Evidence;

public class EvidenceSummaryQueryHandlerTests
{
    private static EvidenceDbContext BuildContext() =>
        new(new DbContextOptionsBuilder<EvidenceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Modules.Evidence.Domain.Evidence BuildEvidence(
        Guid tenantId,
        EvidenceStatus status,
        string? blobPath = null)
    {
        var evidence = (Modules.Evidence.Domain.Evidence)
            System.Runtime.CompilerServices.RuntimeHelpers
                .GetUninitializedObject(typeof(Modules.Evidence.Domain.Evidence));

        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Id), Guid.NewGuid());
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.TenantId), tenantId);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Status), status);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Sensitivity), EvidenceSensitivity.Internal);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Title), $"Evidence-{status}");
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.Type), EvidenceType.Policy);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.BlobPath), blobPath);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.CreatedAt), DateTimeOffset.UtcNow);
        Set(evidence, nameof(Modules.Evidence.Domain.Evidence.CreatedBy), Guid.NewGuid());

        return evidence;
    }

    private static void Set<T>(object target, string propertyName, T value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);

    [Fact]
    public async Task HandleAsync_ComputesSummaryFromLinkedEvidence()
    {
        var tenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var active = BuildEvidence(tenantId, EvidenceStatus.Active, "active.pdf");
        var draft = BuildEvidence(tenantId, EvidenceStatus.Draft);
        var archived = BuildEvidence(tenantId, EvidenceStatus.Archived, "archived.pdf");
        var superseded = BuildEvidence(tenantId, EvidenceStatus.Superseded, "superseded.pdf");

        await using var db = BuildContext();
        db.Evidences.AddRange(active, draft, archived, superseded);
        db.EvidenceLinks.AddRange(
            EvidenceLink.Create(tenantId, active.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId),
            EvidenceLink.Create(tenantId, draft.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId),
            EvidenceLink.Create(tenantId, archived.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId),
            EvidenceLink.Create(tenantId, superseded.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId));
        await db.SaveChangesAsync();

        var result = await new GetEvidenceSummaryQueryHandler(db)
            .HandleAsync(tenantId, processingActivityId, versionId);

        Assert.Equal(processingActivityId, result.ProcessingActivityId);
        Assert.Equal(versionId, result.VersionId);
        Assert.Equal(4, result.TotalRequirements);
        Assert.Equal(1, result.PendingCount);
        Assert.Equal(3, result.AttachedCount);
        Assert.Equal(1, result.ValidatedCount);
        Assert.Equal(1, result.InsufficientCount);
        Assert.Equal(1, result.RejectedCount);
        Assert.Equal(3, result.BlockingRequirementsCount);
        Assert.Equal(25m, result.CompletionPercentage);
    }

    [Fact]
    public async Task HandleAsync_FiltersByTenantAndProcessingActivity()
    {
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var processingActivityId = Guid.NewGuid();
        var otherProcessingActivityId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var included = BuildEvidence(tenantId, EvidenceStatus.Active, "included.pdf");
        var otherActivity = BuildEvidence(tenantId, EvidenceStatus.Active, "other-activity.pdf");
        var otherTenant = BuildEvidence(otherTenantId, EvidenceStatus.Active, "other-tenant.pdf");

        await using var db = BuildContext();
        db.Evidences.AddRange(included, otherActivity, otherTenant);
        db.EvidenceLinks.AddRange(
            EvidenceLink.Create(tenantId, included.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId),
            EvidenceLink.Create(tenantId, otherActivity.Id, LinkedEntityType.ProcessingActivity, otherProcessingActivityId, userId),
            EvidenceLink.Create(otherTenantId, otherTenant.Id, LinkedEntityType.ProcessingActivity, processingActivityId, userId));
        await db.SaveChangesAsync();

        var result = await new GetEvidenceSummaryQueryHandler(db)
            .HandleAsync(tenantId, processingActivityId, versionId);

        Assert.Equal(1, result.TotalRequirements);
        Assert.Equal(1, result.AttachedCount);
        Assert.Equal(1, result.ValidatedCount);
        Assert.Equal(100m, result.CompletionPercentage);
    }
}
