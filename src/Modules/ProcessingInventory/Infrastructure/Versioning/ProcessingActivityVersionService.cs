using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Infrastructure.Versioning;

/// <summary>
/// Implementación del servicio de versionado del RAT.
/// Coordina la aprobación, snapshot y creación de nueva versión dentro de una
/// sola transacción para garantizar consistencia.
/// </summary>
public sealed class ProcessingActivityVersionService : IProcessingActivityVersionService
{
    private readonly ProcessingInventoryDbContext _db;

    public ProcessingActivityVersionService(ProcessingInventoryDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc/>
    public async Task<ProcessingActivitySnapshot> ApproveAndSnapshotAsync(
        Guid activityId,
        Guid approvedBy,
        bool retentionRequired = false,
        CancellationToken ct = default)
    {
        var activity = await _db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == activityId, ct)
            ?? throw new InvalidOperationException($"Tratamiento {activityId} no encontrado.");

        var snapshot = activity.Approve(approvedBy);

        _db.ProcessingActivitySnapshots.Add(snapshot);
        await _db.SaveChangesAsync(ct);

        return snapshot;
    }

    /// <inheritdoc/>
    public async Task<ProcessingActivity> CreateNewVersionAsync(
        Guid approvedActivityId,
        Guid createdBy,
        CancellationToken ct = default)
    {
        var approved = await _db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.Id == approvedActivityId, ct)
            ?? throw new InvalidOperationException($"Tratamiento {approvedActivityId} no encontrado.");

        var newVersion = approved.CreateNewVersion(createdBy);

        _db.ProcessingActivities.Add(newVersion);
        await _db.SaveChangesAsync(ct);

        return newVersion;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProcessingActivitySnapshot>> GetSnapshotsAsync(
        Guid activityId,
        Guid tenantId,
        CancellationToken ct = default)
    {
        return await _db.ProcessingActivitySnapshots
            .Where(s => s.ActivityId == activityId && s.TenantId == tenantId)
            .OrderByDescending(s => s.Version)
            .ToListAsync(ct);
    }
}
