using Evidata.Modules.Mcp.Application.RatContext;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Mcp.Infrastructure.RatContext;

/// <summary>
/// Obtiene una proyección mínima de los tratamientos RAT aprobados del tenant.
/// Consulta directamente el schema <c>rat</c> sin cargar las colecciones JSONB completas.
/// </summary>
public sealed class RatContextProvider : IRatContextProvider
{
    private readonly ProcessingInventoryDbContext _db;

    public RatContextProvider(ProcessingInventoryDbContext db)
    {
        _db = db;
    }

    public async Task<RatContextSnapshot> GetSnapshotAsync(
        Guid tenantId, CancellationToken ct = default)
    {
        var activities = await _db.ProcessingActivities
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId
                     && a.Status == ProcessingActivityStatus.Approved)
            .ToListAsync(ct);

        var summaries = activities.Select(a => new RatActivitySummary(
            ActivityId: a.Id,
            Name: a.Name,
            Department: a.Department,
            LegalBasis: a.Purpose?.LegalBasis.ToString(),
            DataCategoryNames: a.DataCategories.Select(d => d.LocalName ?? d.DataCategoryId.ToString()).ToList(),
            HasSensitiveData: a.Flags.SensitiveData,
            HasInternationalTransfer: a.Flags.InternationalTransfer,
            HasAutomatedDecision: a.Flags.AutomatedDecision,
            RequiresEnhancedReview: a.Flags.RequiresEnhancedReview
        )).ToList();

        return new RatContextSnapshot(tenantId, summaries);
    }
}
