using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Queries;

public sealed class ListProcessingActivitiesQueryHandler(ProcessingInventoryDbContext db)
{
    public async Task<IReadOnlyList<ProcessingActivityDto>> HandleAsync(
        Guid tenantId,
        ProcessingActivityStatus? status = null,
        CancellationToken ct = default)
    {
        var query = db.ProcessingActivities
            .Where(a => a.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var items = await query
            .OrderByDescending(a => a.LastModifiedAt ?? a.CreatedAt)
            .ToListAsync(ct);

        return items.Select(ProcessingActivityDto.From).ToList();
    }
}
