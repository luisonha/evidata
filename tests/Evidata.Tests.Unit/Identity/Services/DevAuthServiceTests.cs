using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Evidata.Tests.Unit.Identity.Services;

/// <summary>
/// Tests for DevAuthService — the development-only authentication service.
/// 
/// SECURITY TESTS:
/// - Session expiration/revocation works
/// - Error handling for invalid inputs
/// 
/// These tests ensure that dev auth operations work as intended in Development environment.
/// </summary>
public class DevAuthServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private IdentityDbContext _dbContext = null!;
    private ISessionService _sessionService = null!;
    private ILogger<DevAuthService> _logger = null!;
    private DevAuthService _devAuthService = null!;

    public DevAuthServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new IdentityDbContext(_dbContextOptions);
        await _dbContext.Database.EnsureCreatedAsync();

        _sessionService = Substitute.For<ISessionService>();
        _logger = Substitute.For<ILogger<DevAuthService>>();

        _devAuthService = new DevAuthService(_dbContext, _sessionService, _logger);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    /// <summary>
    /// Test: ExpireCurrentSessionAsync returns false for empty session ID
    /// </summary>
    [Fact]
    public async Task ExpireCurrentSessionAsync_WithEmptySessionId_ReturnsFalse()
    {
        // Act
        var result = await _devAuthService.ExpireCurrentSessionAsync("", CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    /// <summary>
    /// Test: ExpireCurrentSessionAsync calls ISessionService.RevokeSessionAsync
    /// </summary>
    [Fact]
    public async Task ExpireCurrentSessionAsync_WithValidSessionId_CallsRevokeSession()
    {
        // Arrange
        var sessionId = "test-session-id";
        _sessionService.RevokeSessionAsync(sessionId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(true));

        // Act
        var result = await _devAuthService.ExpireCurrentSessionAsync(sessionId, CancellationToken.None);

        // Assert
        Assert.True(result);
        await _sessionService.Received(1).RevokeSessionAsync(sessionId, Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// Test: GetSeedUsersAsync returns empty list when no users exist
    /// </summary>
    [Fact]
    public async Task GetSeedUsersAsync_WithNoUsers_ReturnsEmptyList()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Act
        var users = await _devAuthService.GetSeedUsersAsync(tenantId, CancellationToken.None);

        // Assert
        Assert.Empty(users);
    }

    /// <summary>
    /// Test: ResetSeedUsersAsync returns 0 when no seed users exist
    /// </summary>
    [Fact]
    public async Task ResetSeedUsersAsync_WithNoUsers_ReturnsZero()
    {
        // Act
        var result = await _devAuthService.ResetSeedUsersAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, result);
    }

    /// <summary>
    /// Test: LoginWithEmailAsync returns error for empty email
    /// </summary>
    [Fact]
    public async Task LoginWithEmailAsync_WithEmptyEmail_ReturnsError()
    {
        // Act
        var (response, errorCode) = await _devAuthService.LoginWithEmailAsync(
            "",
            Guid.NewGuid(),
            CancellationToken.None);

        // Assert
        Assert.Null(response);
        Assert.NotNull(errorCode);
    }

    /// <summary>
    /// Test: SetScenarioAsync returns error for empty email
    /// </summary>
    [Fact]
    public async Task SetScenarioAsync_WithEmptyEmail_ReturnsError()
    {
        // Act
        var (success, errorCode) = await _devAuthService.SetScenarioAsync(
            "",
            "denied",
            CancellationToken.None);

        // Assert
        Assert.False(success);
        Assert.NotNull(errorCode);
    }
}
