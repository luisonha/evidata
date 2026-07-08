using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Commands;

public record LinkExternalIdentityCommand(
    string ExternalId,
    string Provider,
    string Email,
    string DisplayName,
    Guid TenantId
);

public class LinkExternalIdentityCommandHandler
{
    private readonly IUserProfileRepository _repository;

    public LinkExternalIdentityCommandHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserProfileDto> HandleAsync(LinkExternalIdentityCommand command, CancellationToken ct = default)
    {
        var existing = await _repository.GetByExternalIdAsync(command.ExternalId, command.Provider, command.TenantId, ct);

        if (existing is not null)
        {
            existing.UpdateProfile(command.Email, command.DisplayName);
            await _repository.UpsertAsync(existing, ct);
            return MapToDto(existing);
        }

        var profile = UserProfile.Create(command.ExternalId, command.Provider, command.Email, command.DisplayName, command.TenantId);
        await _repository.UpsertAsync(profile, ct);
        return MapToDto(profile);
    }

    private static UserProfileDto MapToDto(UserProfile p) =>
        new(p.Id, p.TenantId, p.Email, p.DisplayName, p.Provider, p.IsActive);
}
