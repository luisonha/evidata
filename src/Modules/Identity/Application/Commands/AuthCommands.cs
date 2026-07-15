using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Identity.Application.Commands;

/// <summary>
/// Command to complete the login flow after successful Entra ID callback.
/// Creates a session, activates user if needed, records login time.
/// </summary>
public record CompleteLoginCommand(
    Guid TenantId,
    UserProfile User,
    Invitation? Invitation,
    IEnumerable<Guid> RoleIds
);

/// <summary>
/// Handler for CompleteLoginCommand.
/// Creates a new session and applies state transitions (activate if invited).
/// </summary>
public class CompleteLoginCommandHandler
{
    private readonly IdentityDbContext _context;
    private readonly ISessionService _sessionService;

    public CompleteLoginCommandHandler(IdentityDbContext context, ISessionService sessionService)
    {
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<LoginCallbackResponseDto> HandleAsync(CompleteLoginCommand command, CancellationToken ct = default)
    {
        var user = command.User;
        var roleIds = command.RoleIds.ToList();

        // If user is Invited and has a matching invitation, activate the user
        if (user.Status == UserStatus.Invited && command.Invitation is not null)
        {
            user.Activate();
            command.Invitation.Accept(user.Id);
        }

        // Record login timestamp
        user.RecordLogin();

        // Link Entra OID if not already linked
        // (The OID comes from the JWT claims passed through the command)
        // Note: This is done in the controller before calling this handler

        // Create session
        var sessionId = await _sessionService.CreateSessionAsync(
            user.Id,
            command.TenantId,
            roleIds,
            permissionsVersion: 1, // TODO: Get from permissions service when available
            ct: ct);

        // Persist user and invitation updates
        _context.UserProfiles.Update(user);
        if (command.Invitation is not null)
        {
            _context.Invitations.Update(command.Invitation);
        }
        await _context.SaveChangesAsync(ct);

        return new LoginCallbackResponseDto(
            user.Id,
            command.TenantId,
            user.Email,
            user.DisplayName,
            roleIds.AsReadOnly(),
            sessionId,
            DateTime.UtcNow.AddHours(24)); // Session expires in 24 hours
    }
}

/// <summary>
/// Command to logout (revoke session).
/// </summary>
public record LogoutCommand(string SessionId);

/// <summary>
/// Handler for LogoutCommand.
/// Revokes the user's session.
/// </summary>
public class LogoutCommandHandler
{
    private readonly ISessionService _sessionService;

    public LogoutCommandHandler(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    public async Task<LogoutResponseDto> HandleAsync(LogoutCommand command, CancellationToken ct = default)
    {
        var revoked = await _sessionService.RevokeSessionAsync(command.SessionId, ct);
        
        return new LogoutResponseDto(
            Success: revoked,
            Message: revoked ? "Session revoked successfully" : "Session not found");
    }
}
