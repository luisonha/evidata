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

        profile.Deactivate();
        await _repository.UpsertAsync(profile, ct);
    }
}
