using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Evidences;

/// <summary>
/// Implementación de <see cref="IRatEvidenceService"/>.
/// Delega completamente en <see cref="IEvidenceLinkService"/> usando
/// <see cref="LinkedEntityType.ProcessingActivity"/> como discriminador.
/// </summary>
public sealed class RatEvidenceService : IRatEvidenceService
{
    private readonly IEvidenceLinkService _linkService;

    public RatEvidenceService(IEvidenceLinkService linkService)
    {
        _linkService = linkService;
    }

    /// <inheritdoc/>
    public async Task<Guid> AddEvidenceToRatAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid evidenceId,
        Guid createdBy,
        string? note = null,
        CancellationToken ct = default)
    {
        var result = await _linkService.AddLinkAsync(
            tenantId,
            evidenceId,
            LinkedEntityType.ProcessingActivity,
            processingActivityId,
            createdBy,
            note,
            ct);

        return result.LinkId;
    }

    /// <inheritdoc/>
    public Task RemoveEvidenceFromRatAsync(
        Guid tenantId,
        Guid linkId,
        Guid deletedBy,
        CancellationToken ct = default)
        => _linkService.RemoveLinkAsync(tenantId, linkId, deletedBy, ct);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<RatEvidenceLinkDto>> GetEvidencesForRatAsync(
        Guid tenantId,
        Guid processingActivityId,
        CancellationToken ct = default)
    {
        // IEvidenceLinkService.GetLinksAsync obtiene por evidenceId.
        // Para obtener evidencias de un RAT necesitamos la consulta inversa:
        // todos los links donde LinkedEntityType=ProcessingActivity y LinkedEntityId=activityId.
        // Delegamos al método extendido de bajo nivel via IEvidenceLinkService.
        var links = await _linkService.GetLinksByEntityAsync(
            tenantId,
            LinkedEntityType.ProcessingActivity,
            processingActivityId,
            ct);

        return links
            .Select(l => new RatEvidenceLinkDto(l.LinkId, l.LinkedEntityId, l.Note, l.CreatedBy, l.CreatedAt))
            .ToList();
    }
}
