using Evidata.Modules.TenantManagement.Application.DTOs;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.TenantManagement.Application.Commands;

public record CreateTenantCommand(string Slug, string Name);

public class CreateTenantCommandHandler(ITenantRepository repository, ILogger<CreateTenantCommandHandler> logger)
{
    public async Task<TenantDto> HandleAsync(CreateTenantCommand command, CancellationToken ct = default)
    {
        var existing = await repository.GetBySlugAsync(command.Slug, ct);
        if (existing is not null)
        {
            logger.LogInformation("Tenant ya existe. Slug={Slug} Id={TenantId}", command.Slug, existing.Id);
            return existing.ToDto();
        }

        var tenant = Tenant.Create(command.Slug, command.Name);
        await repository.AddAsync(tenant, ct);

        logger.LogInformation("Tenant creado. Id={TenantId} Slug={Slug}", tenant.Id, tenant.Slug);
        return tenant.ToDto();
    }
}
