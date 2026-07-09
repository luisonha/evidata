using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Infrastructure.AutoDetection;

/// <summary>
/// Lee los flags de riesgo del módulo ProcessingInventory vía EF.
/// Solo proyecta las columnas escalares necesarias — no carga JSONB.
/// </summary>
public sealed class EfRatFlagsProvider : IRatFlagsProvider
{
    private readonly ProcessingInventoryDbContext _db;

    public EfRatFlagsProvider(ProcessingInventoryDbContext db)
    {
        _db = db;
    }

    public async Task<RatFlagsSnapshot> GetFlagsAsync(
        Guid tenantId,
        Guid processingActivityId,
        CancellationToken ct = default)
    {
        var row = await _db.ProcessingActivities
            .Where(a => a.Id == processingActivityId && a.TenantId == tenantId)
            .Select(a => new RatFlagsSnapshot(
                a.Id,
                a.TenantId,
                a.Name,
                a.Flags.MissingSecurityMeasures,
                a.Flags.MissingLegalBasisEvidence,
                a.Flags.MissingRetention,
                a.Flags.ChildrenData,
                a.Flags.BiometricData,
                a.HasInternationalTransfer,
                a.HasAutomatedDecision))
            .FirstOrDefaultAsync(ct);

        return row ?? throw new InvalidOperationException(
            $"Tratamiento {processingActivityId} no encontrado en tenant {tenantId}.");
    }
}
