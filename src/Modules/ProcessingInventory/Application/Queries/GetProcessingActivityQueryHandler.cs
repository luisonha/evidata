using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Queries;

public sealed class GetProcessingActivityQueryHandler(ProcessingInventoryDbContext db)
{
    public async Task<ProcessingActivityDto?> HandleAsync(
        Guid tenantId, Guid id, CancellationToken ct = default)
    {
        var item = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.Id == id, ct);

        return item is null ? null : ProcessingActivityDto.From(item);
    }
}
