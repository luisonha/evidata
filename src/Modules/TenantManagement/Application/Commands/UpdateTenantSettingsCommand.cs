using Evidata.Modules.TenantManagement.Application.DTOs;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.TenantManagement.Application.Commands;

public record UpdateTenantSettingsCommand(Guid TenantId, string? TimeZone, string? Locale, int? MaxUsers, bool? MfaRequired);

public class UpdateTenantSettingsCommandHandler(ITenantRepository repository, ILogger<UpdateTenantSettingsCommandHandler> logger)
{
    public async Task<TenantDto> HandleAsync(UpdateTenantSettingsCommand command, CancellationToken ct = default)
    {
        var tenant = await repository.GetByIdAsync(command.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {command.TenantId} not found.");

        if (command.TimeZone is not null) tenant.Settings.TimeZone = command.TimeZone;
        if (command.Locale is not null) tenant.Settings.Locale = command.Locale;
        if (command.MaxUsers.HasValue) tenant.Settings.MaxUsers = command.MaxUsers.Value;
        if (command.MfaRequired.HasValue) tenant.Settings.MfaRequired = command.MfaRequired.Value;
        tenant.UpdatedAt = DateTimeOffset.UtcNow;

        await repository.UpdateAsync(tenant, ct);
        logger.LogInformation("Settings actualizados. TenantId={TenantId}", tenant.Id);
        return tenant.ToDto();
    }
}
