using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Identity.Api;

[ApiController]
[Route("api/users")]
public class UserProfileController : ControllerBase
{
    private readonly GetUserProfileQueryHandler _getProfile;
    private readonly LinkExternalIdentityCommandHandler _linkIdentity;
    private readonly DeactivateUserCommandHandler _deactivateUser;

    public UserProfileController(
        GetUserProfileQueryHandler getProfile,
        LinkExternalIdentityCommandHandler linkIdentity,
        DeactivateUserCommandHandler deactivateUser)
    {
        _getProfile = getProfile;
        _linkIdentity = linkIdentity;
        _deactivateUser = deactivateUser;
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var result = await _getProfile.HandleAsync(new GetUserProfileQuery(userId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("link")]
    public async Task<IActionResult> LinkIdentity([FromBody] LinkExternalIdentityCommand command, CancellationToken ct)
    {
        var result = await _linkIdentity.HandleAsync(command, ct);
        return CreatedAtAction(nameof(GetById), new { userId = result.UserId }, result);
    }

    [HttpDelete("{userId:guid}")]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        await _deactivateUser.HandleAsync(new DeactivateUserCommand(userId), ct);
        return NoContent();
    }
}
