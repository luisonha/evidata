using Evidata.Modules.TenantManagement.Application.DTOs;
using Evidata.Modules.TenantManagement.Domain;

namespace Evidata.Modules.TenantManagement.Application.Queries;

public record GetTenantQuery(Guid TenantId);

public class GetTenantQueryHandler(ITenantRepository repository)
{
    public async Task<TenantDto?> HandleAsync(GetTenantQuery query, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(query.TenantId, ct);
        return tenant?.ToDto();
    }
}
