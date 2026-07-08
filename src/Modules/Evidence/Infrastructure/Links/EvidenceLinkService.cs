using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Evidence.Infrastructure.Links;

public class EvidenceLinkService : IEvidenceLinkService
{
    private readonly EvidenceDbContext _db;
    private readonly ILogger<EvidenceLinkService> _logger;

    public EvidenceLinkService(EvidenceDbContext db, ILogger<EvidenceLinkService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<AddLinkResult> AddLinkAsync(
        Guid tenantId,
        Guid evidenceId,
        LinkedEntityType linkedEntityType,
        Guid linkedEntityId,
        Guid createdBy,
        string? note = null,
        CancellationToken ct = default)
    {
        // Validar que la evidencia existe y pertenece al tenant
        var evidenceExists = await _db.Evidences
            .IgnoreQueryFilters()
            .AnyAsync(e => e.TenantId == tenantId && e.Id == evidenceId
                        && e.Status != EvidenceStatus.Deleted, ct);

        if (!evidenceExists)
            throw new KeyNotFoundException(
                $"Evidencia {evidenceId} no encontrada o está eliminada en tenant {tenantId}.");

        // Prevenir duplicado activo
        var duplicate = await _db.EvidenceLinks
            .AnyAsync(l => l.TenantId == tenantId
                        && l.EvidenceId == evidenceId
                        && l.LinkedEntityType == linkedEntityType
                        && l.LinkedEntityId == linkedEntityId
                        && l.DeletedAt == null, ct);

        if (duplicate)
            throw new InvalidOperationException(
                $"Ya existe un vínculo activo entre evidencia {evidenceId} y entidad {linkedEntityId} ({linkedEntityType}).");

        var link = EvidenceLink.Create(
            tenantId, evidenceId, linkedEntityType, linkedEntityId, createdBy, note);

        _db.EvidenceLinks.Add(link);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "EvidenceLink {LinkId} creado: evidence={EvidenceId} → {EntityType}/{EntityId}",
            link.Id, evidenceId, linkedEntityType, linkedEntityId);

        return new AddLinkResult(link.Id);
    }

    public async Task RemoveLinkAsync(
        Guid tenantId,
        Guid linkId,
        Guid deletedBy,
        CancellationToken ct = default)
    {
        var link = await _db.EvidenceLinks
            .Where(l => l.TenantId == tenantId && l.Id == linkId)
            .FirstOrDefaultAsync(ct);

        if (link is null)
            throw new KeyNotFoundException(
                $"EvidenceLink {linkId} no encontrado en tenant {tenantId}.");

        link.Delete(deletedBy);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("EvidenceLink {LinkId} eliminado por {UserId}", linkId, deletedBy);
    }

    public async Task<IReadOnlyList<EvidenceLinkDto>> GetLinksAsync(
        Guid tenantId,
        Guid evidenceId,
        CancellationToken ct = default)
    {
        return await _db.EvidenceLinks
            .Where(l => l.TenantId == tenantId
                     && l.EvidenceId == evidenceId
                     && l.DeletedAt == null)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new EvidenceLinkDto(
                l.Id,
                l.LinkedEntityType,
                l.LinkedEntityId,
                l.Note,
                l.CreatedBy,
                l.CreatedAt))
            .ToListAsync(ct);
    }
}
