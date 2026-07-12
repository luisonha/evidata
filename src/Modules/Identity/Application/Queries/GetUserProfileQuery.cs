using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Domain;

namespace Evidata.Modules.Identity.Application.Queries;

public record GetUserProfileQuery(Guid UserId);

public class GetUserProfileQueryHandler
{
    private readonly IUserProfileRepository _repository;

    public GetUserProfileQueryHandler(IUserProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserProfileDto?> HandleAsync(GetUserProfileQuery query, CancellationToken ct = default)
    {
        var profile = await _repository.GetByIdAsync(query.UserId, ct);
        if (profile is null) return null;

        return new UserProfileDto(profile.Id, profile.TenantId, profile.Email, profile.DisplayName, profile.Provider, profile.Status);
    }
}
