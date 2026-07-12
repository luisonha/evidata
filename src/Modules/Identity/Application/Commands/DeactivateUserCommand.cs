using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Commands;

public record DeactivateUserCommand(Guid UserId);

public class DeactivateUserCommandHandler
{
    private readonly IUserProfileRepository _repository;

    public DeactivateUserCommandHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task HandleAsync(DeactivateUserCommand command, CancellationToken ct = default)
    {
        var profile = await _repository.GetByIdAsync(command.UserId, ct)
            ?? throw new InvalidOperationException($"UserProfile {command.UserId} not found.");

        // For now, map deactivation to Disable (permanent during Sprint 3)
        // This may evolve to Suspend (temporary) based on admin action intent
        if (profile.Status == UserStatus.Active || profile.Status == UserStatus.Suspended)
        {
            profile.Disable();
        }

        await _repository.UpsertAsync(profile, ct);
    }
}
