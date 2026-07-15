using Evidata.Modules.Identity.Application.DTOs;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Services;
using Evidata.Modules.TenantManagement.Domain;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Evidata.Tests.Unit.Identity.Services;

/// <summary>
/// Tests for UserResolutionService login callback scenarios from doc02.
/// Covers the priority order of user resolution and error handling.
/// </summary>
public class UserResolutionServiceTests : IDisposable
{
    private readonly IdentityDbContext _context;
    private readonly MockTenantRepository _tenantRepository;
    private readonly UserResolutionService _service;
    private readonly Guid _tenantId = Guid.NewGuid();

    public UserResolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new IdentityDbContext(options);
        _tenantRepository = new MockTenantRepository(_tenantId);
        _service = new UserResolutionService(_context, _tenantRepository, new NullLogger<UserResolutionService>());
    }

    #region Resolution Priority Tests

    [Fact]
    public async Task ResolveUserAsync_FoundByOid_ReturnsUserProfile()
    {
        // Arrange
        var user = UserProfile.Create("entra-oid-123", "EntraId", "user@example.com", "User Name", _tenantId);
        _context.UserProfiles.Add(user);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid-123",
            Email: "user@example.com",
            DisplayName: "User Name",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, invitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.NotNull(resolvedUser);
        Assert.Equal(user.Id, resolvedUser.Id);
        Assert.Null(invitation);
        Assert.Null(errorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_FoundByEmail_ReturnsUserProfile()
    {
        // Arrange
        var user = UserProfile.Create("old-provider-id", "OldProvider", "user@example.com", "User Name", _tenantId);
        _context.UserProfiles.Add(user);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid-new",
            Email: "user@example.com",
            DisplayName: "User Name",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, invitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.NotNull(resolvedUser);
        Assert.Equal(user.Id, resolvedUser.Id);
        Assert.Null(invitation);
        Assert.Null(errorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_FoundByPendingInvitation_ReturnsInvitation()
    {
        // Arrange
        var invitation = Invitation.Create(
            _tenantId,
            "newuser@example.com",
            "ProcessOwner",
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(7));
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid-new",
            Email: "newuser@example.com",
            DisplayName: "New User",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, resolvedInvitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.Null(resolvedUser);
        Assert.NotNull(resolvedInvitation);
        Assert.Equal(invitation.Id, resolvedInvitation.Id);
        Assert.Null(errorCode);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ResolveUserAsync_TenantNotActive_ReturnsTenantUnavailable()
    {
        // Arrange
        var inactiveTenantId = Guid.NewGuid();
        _tenantRepository.SetInactiveTenant(inactiveTenantId);

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid",
            Email: "user@example.com",
            DisplayName: "User",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, invitation, errorCode) = await _service.ResolveUserAsync(claims, inactiveTenantId);

        // Assert
        Assert.Null(resolvedUser);
        Assert.Null(invitation);
        Assert.Equal(IdentityErrorCodes.TenantUnavailable, errorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_UserNotFound_ReturnsUserNotProvisioned()
    {
        // Arrange
        var claims = new EntraIdTokenClaimsDto(
            Oid: "unknown-oid",
            Email: "unknown@example.com",
            DisplayName: "Unknown",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, invitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.Null(resolvedUser);
        Assert.Null(invitation);
        Assert.Equal(IdentityErrorCodes.UserNotProvisioned, errorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_UserSuspended_ReturnsSuspendedError()
    {
        // Arrange
        var user = UserProfile.Create("entra-oid", "EntraId", "user@example.com", "User", _tenantId);
        user.Activate(); // Must activate first before suspending
        user.Suspend(); // User is suspended
        _context.UserProfiles.Add(user);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid",
            Email: "user@example.com",
            DisplayName: "User",
            EntaTenantId: "entra-tenant");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IdentityDomainException>(
            () => _service.ResolveUserAsync(claims, _tenantId));

        Assert.Equal(IdentityErrorCodes.UserSuspended, exception.ErrorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_UserDisabled_ReturnsDisabledError()
    {
        // Arrange
        var user = UserProfile.Create("entra-oid", "EntraId", "user@example.com", "User", _tenantId);
        user.Activate(); // Must activate first before disabling
        user.Disable(); // User is disabled
        _context.UserProfiles.Add(user);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid",
            Email: "user@example.com",
            DisplayName: "User",
            EntaTenantId: "entra-tenant");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IdentityDomainException>(
            () => _service.ResolveUserAsync(claims, _tenantId));

        Assert.Equal(IdentityErrorCodes.UserDisabled, exception.ErrorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_InvitationExpired_ReturnsInvitationExpired()
    {
        // Arrange
        var invitation = Invitation.Create(
            _tenantId,
            "newuser@example.com",
            "ProcessOwner",
            Guid.NewGuid(),
            DateTime.UtcNow.AddHours(-1)); // Expired
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid-new",
            Email: "newuser@example.com",
            DisplayName: "New User",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, resolvedInvitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.Null(resolvedUser);
        Assert.Null(resolvedInvitation);
        Assert.Equal(IdentityErrorCodes.InvitationExpired, errorCode);
    }

    [Fact]
    public async Task ResolveUserAsync_InvitationRevoked_ReturnsInvitationRevoked()
    {
        // Arrange
        var invitation = Invitation.Create(
            _tenantId,
            "inviteduser@example.com",
            "ProcessOwner",
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(7)); // Not expired
        invitation.Revoke(); // Revoke the invitation
        _context.Invitations.Add(invitation);
        await _context.SaveChangesAsync();

        var claims = new EntraIdTokenClaimsDto(
            Oid: "entra-oid-new",
            Email: "inviteduser@example.com",
            DisplayName: "New User",
            EntaTenantId: "entra-tenant");

        // Act
        var (resolvedUser, resolvedInvitation, errorCode) = await _service.ResolveUserAsync(claims, _tenantId);

        // Assert
        Assert.Null(resolvedUser);
        Assert.Null(resolvedInvitation);
        Assert.Equal(IdentityErrorCodes.InvitationRevoked, errorCode);
    }

    #endregion

    public void Dispose()
    {
        _context?.Dispose();
    }
}

/// <summary>
/// Mock tenant repository for testing.
/// </summary>
public class MockTenantRepository : Evidata.Modules.TenantManagement.Domain.ITenantRepository
{
    private readonly Guid _activeTenantId;
    private Guid? _inactiveTenantId;

    public MockTenantRepository(Guid activeTenantId)
    {
        _activeTenantId = activeTenantId;
    }

    public void SetInactiveTenant(Guid tenantId)
    {
        _inactiveTenantId = tenantId;
    }

    public Task<Tenant?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        if (id == _activeTenantId)
        {
            return Task.FromResult<Tenant?>(
                Tenant.Create(_activeTenantId.ToString()[..8], "Active Tenant"));
        }

        if (id == _inactiveTenantId)
        {
            var tenant = Tenant.Create(id.ToString()[..8], "Inactive Tenant");
            tenant.Status = TenantStatus.Suspended;
            return Task.FromResult<Tenant?>(tenant);
        }

        return Task.FromResult<Tenant?>(null);
    }

    public Task<Tenant?> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task AddAsync(Tenant tenant, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(Tenant tenant, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Null logger for testing.
/// </summary>
public class NullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) { }
}
