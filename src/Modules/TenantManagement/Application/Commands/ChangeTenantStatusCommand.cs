using Evidata.Modules.TenantManagement.Application.DTOs;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.TenantManagement.Application.Commands;

public record ChangeTenantStatusCommand(Guid TenantId, TenantStatus NewStatus);

public class ChangeTenantStatusCommandHandler(ITenantRepository repository, ILogger<ChangeTenantStatusCommandHandler> logger)
{
    public async Task<TenantDto> HandleAsync(ChangeTenantStatusCommand command, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(command.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {command.TenantId} not found.");

        tenant.Status = command.NewStatus;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateAsync(tenant, ct);

        logger.LogInformation("Estado tenant cambiado. TenantId={TenantId} Status={Status}", tenant.Id, command.NewStatus);
        return tenant.ToDto();
    }
}
