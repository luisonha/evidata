using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Identity.Application.Queries;

/// <summary>
/// Query to get the current session status.
/// Returns whether user is authenticated, their ID, tenant, and session expiry.
/// </summary>
public record GetSessionStatusQuery;

/// <summary>
/// Handler for GetSessionStatusQuery.
/// </summary>
public class GetSessionStatusQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;

    public GetSessionStatusQueryHandler(ICurrentUserContext currentUserContext)
    {
        _currentUserContext = currentUserContext;
    }

    public Task<SessionStatusDto> HandleAsync(CancellationToken ct = default)
    {
        // If not authenticated, return unauthenticated status
        if (!_currentUserContext.IsAuthenticated)
        {
            return Task.FromResult(new SessionStatusDto(
                IsAuthenticated: false,
                UserId: null,
                TenantId: null,
                ExpiresAt: null));
        }

        // Return authenticated session status
        // Note: Session expiry comes from the session entity, which would need to be passed
        // through the current user context. For now, we return null.
        // TODO: Pass session expiry through ICurrentUserContext
        return Task.FromResult(new SessionStatusDto(
            IsAuthenticated: true,
            UserId: _currentUserContext.UserId,
            TenantId: _currentUserContext.TenantId,
            ExpiresAt: null)); // Would be set if SessionCurrentUserContext exposed it
    }
}

/// <summary>
/// Query to get the current authenticated user's profile.
/// Returns id, email, displayName, roles, and tenant.
/// Only valid for authenticated users.
/// </summary>
public record GetCurrentUserProfileQuery;

/// <summary>
/// Handler for GetCurrentUserProfileQuery.
/// </summary>
public class GetCurrentUserProfileQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserProfileRepository _repository;

    public GetCurrentUserProfileQueryHandler(
        ICurrentUserContext currentUserContext,
        IUserProfileRepository repository)
    {
        _currentUserContext = currentUserContext;
        _repository = repository;
    }

    public async Task<UserProfileResponseDto> HandleAsync(CancellationToken ct = default)
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new InvalidOperationException("User not authenticated");

        var profile = await _repository.GetByIdAsync(_currentUserContext.UserId, ct);
        if (profile is null)
            throw new InvalidOperationException("User profile not found");

        var roleIds = profile.GetRoleIds().ToList();

        return new UserProfileResponseDto(
            profile.Id,
            profile.TenantId,
            profile.Email,
            profile.DisplayName,
            roleIds.AsReadOnly());
    }
}

/// <summary>
/// Query to get the current authenticated user's effective permissions.
/// Derives permissions from the user's roles.
/// Only valid for authenticated users.
/// </summary>
public record GetCurrentUserPermissionsQuery;

/// <summary>
/// Handler for GetCurrentUserPermissionsQuery.
/// Note: This is a placeholder. In a full implementation, this would integrate with
/// the Security module's permission resolution service to compute effective permissions
/// from the user's roles. For now, returns an empty list.
/// TODO: Integrate with Security module's IResourcePermissionsQueryService or equivalent.
/// </summary>
public class GetCurrentUserPermissionsQueryHandler
{
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IUserProfileRepository _repository;

    public GetCurrentUserPermissionsQueryHandler(
        ICurrentUserContext currentUserContext,
        IUserProfileRepository repository)
    {
        _currentUserContext = currentUserContext;
        _repository = repository;
    }

    public async Task<UserPermissionsResponseDto> HandleAsync(CancellationToken ct = default)
    {
        if (!_currentUserContext.IsAuthenticated)
            throw new InvalidOperationException("User not authenticated");

        var profile = await _repository.GetByIdAsync(_currentUserContext.UserId, ct);
        if (profile is null)
            throw new InvalidOperationException("User profile not found");

        // TODO: Compute effective permissions from roles via Security module
        // For now, return empty permissions
        // This will be implemented in a subsequent iteration with proper Security module integration
        var permissions = new List<string>();

        return new UserPermissionsResponseDto(permissions.AsReadOnly());
    }
}
