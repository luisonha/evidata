using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Application.Queries;

public sealed class GetEvidenceSummaryQueryHandler(EvidenceDbContext db) : IEvidenceSummaryQueryService
{
    public Task<EvidenceSummaryDto> GetSummaryAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default)
        => HandleAsync(tenantId, processingActivityId, versionId, ct);

    public async Task<EvidenceSummaryDto> HandleAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid versionId,
        CancellationToken ct = default)
    {
        var evidences = await db.EvidenceLinks
            .AsNoTracking()
            .Where(link =>
                link.TenantId == tenantId &&
                link.LinkedEntityType == LinkedEntityType.ProcessingActivity &&
                link.LinkedEntityId == processingActivityId &&
                link.Evidence.Status != EvidenceStatus.Superseded &&
                link.Evidence.Status != EvidenceStatus.Deleted)
            .Select(link => new EvidenceSummaryRow(
                link.EvidenceId,
                link.Evidence.Status,
                link.Evidence.BlobPath))
            .Distinct()
            .ToListAsync(ct);

        // TODO: EvidenceLink hoy sólo guarda ProcessingActivityId. No existe relación por VersionId,
        // así que este resumen agrega toda la evidencia activa del tratamiento sin diferenciar versión.
        // Se preserva versionId en el contrato para que /control no cambie cuando exista ese modelado.

        // TODO: El dominio actual no tiene una entidad separada de "requisito de evidencia" ni estados
        // de validación específicos para insufficient/rejected/blocking. Mientras se implemente esa capa,
        // usamos la aproximación más cercana con el lifecycle vigente (Draft/Active/Archived).
        // Superseded y Deleted se EXCLUYEN explícitamente del resumen: representan evidencia histórica ya
        // reemplazada o eliminada, no un requisito vigente, y NO deben contar como "rejected" ni bloquear
        // la aprobación del tratamiento (confirmado con el usuario — evidencia reemplazada ya fue validada
        // en su ciclo anterior, no es un rechazo).
        var totalRequirements = evidences.Count;
        var pendingCount = evidences.Count(evidence => evidence.Status == EvidenceStatus.Draft);
        var attachedCount = evidences.Count(evidence => !string.IsNullOrWhiteSpace(evidence.BlobPath));
        var validatedCount = evidences.Count(evidence => evidence.Status == EvidenceStatus.Active);
        var insufficientCount = evidences.Count(evidence => evidence.Status == EvidenceStatus.Archived);
        var rejectedCount = 0; // No hay estado de rechazo explícito en el dominio actual; ver TODO arriba.
        var blockingRequirementsCount = pendingCount + insufficientCount + rejectedCount;

        var completionPercentage = totalRequirements == 0
            ? 0m
            : Math.Round(validatedCount * 100m / totalRequirements, 2, MidpointRounding.AwayFromZero);

        return new EvidenceSummaryDto(
            processingActivityId,
            versionId,
            totalRequirements,
            pendingCount,
            attachedCount,
            validatedCount,
            insufficientCount,
            rejectedCount,
            blockingRequirementsCount,
            completionPercentage);
    }

    private sealed record EvidenceSummaryRow(
        Guid EvidenceId,
        EvidenceStatus Status,
        string? BlobPath);
}
