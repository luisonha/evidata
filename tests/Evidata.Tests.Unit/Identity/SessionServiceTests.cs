using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Evidata.Tests.Unit.Identity;

public class SessionServiceTests : IAsyncLifetime
{
    private readonly DbContextOptions<IdentityDbContext> _dbContextOptions;
    private IdentityDbContext _dbContext = null!;
    private SessionService _sessionService = null!;

    public SessionServiceTests()
    {
        _dbContextOptions = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    public async Task InitializeAsync()
    {
        _dbContext = new IdentityDbContext(_dbContextOptions);
        await _dbContext.Database.EnsureCreatedAsync();
        _sessionService = new SessionService(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.Database.EnsureDeletedAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task CreateSessionAsync_WithValidParameters_ReturnsOpaqueSessionId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };

        // Act
        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        // Assert
        Assert.NotNull(sessionId);
        Assert.NotEmpty(sessionId);
        Assert.True(sessionId.Length > 20); // Base64 encoded random 32 bytes
    }

    [Fact]
    public async Task CreateSessionAsync_PersistsSessionInDatabase()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        // Act
        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        // Assert
        var session = await _dbContext.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId);
        Assert.NotNull(session);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(tenantId, session.TenantId);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithValidSessionId_ReturnsSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        // Act
        var session = await _sessionService.GetActiveSessionAsync(sessionId);

        // Assert
        Assert.NotNull(session);
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(tenantId, session.TenantId);
        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithInvalidSessionId_ReturnsNull()
    {
        // Act
        var session = await _sessionService.GetActiveSessionAsync("invalid-session-id");

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithExpiredSession_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1,
            expiryDuration: TimeSpan.FromSeconds(-1)); // Already expired

        // Act
        var session = await _sessionService.GetActiveSessionAsync(sessionId);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task GetActiveSessionAsync_UpdatesLastAccessedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        var firstAccess = await _dbContext.Sessions
            .AsNoTracking()
            .FirstAsync(s => s.Id == sessionId);

        // Act
        System.Threading.Thread.Sleep(100);
        await _sessionService.GetActiveSessionAsync(sessionId);

        // Assert
        var secondAccess = await _dbContext.Sessions
            .AsNoTracking()
            .FirstAsync(s => s.Id == sessionId);

        Assert.True(secondAccess.LastAccessedAt > firstAccess.LastAccessedAt);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithValidSessionId_RevokesSession()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        // Act
        var revoked = await _sessionService.RevokeSessionAsync(sessionId);

        // Assert
        Assert.True(revoked);

        var session = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId);
        Assert.NotNull(session.RevokedAt);
        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task RevokeSessionAsync_WithInvalidSessionId_ReturnsFalse()
    {
        // Act
        var revoked = await _sessionService.RevokeSessionAsync("invalid-id");

        // Assert
        Assert.False(revoked);
    }

    [Fact]
    public async Task GetActiveSessionAsync_WithRevokedSession_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId = await _sessionService.CreateSessionAsync(
            userId,
            tenantId,
            roleIds,
            permissionsVersion: 1);

        await _sessionService.RevokeSessionAsync(sessionId);

        // Act
        var session = await _sessionService.GetActiveSessionAsync(sessionId);

        // Assert
        Assert.Null(session);
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_RevokesAllUserSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId1 = await _sessionService.CreateSessionAsync(userId, tenantId, roleIds, permissionsVersion: 1);
        var sessionId2 = await _sessionService.CreateSessionAsync(userId, tenantId, roleIds, permissionsVersion: 1);
        var sessionId3 = await _sessionService.CreateSessionAsync(userId, tenantId, roleIds, permissionsVersion: 1);

        // Act
        var revokedCount = await _sessionService.RevokeAllSessionsForUserAsync(userId, tenantId);

        // Assert
        Assert.Equal(3, revokedCount);

        var session1 = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId1);
        var session2 = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId2);
        var session3 = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId3);

        Assert.NotNull(session1.RevokedAt);
        Assert.NotNull(session2.RevokedAt);
        Assert.NotNull(session3.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_WithDifferentTenant_DoesNotRevokeOtherTenantSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId1 = Guid.NewGuid();
        var tenantId2 = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid() };

        var sessionId1 = await _sessionService.CreateSessionAsync(userId, tenantId1, roleIds, permissionsVersion: 1);
        var sessionId2 = await _sessionService.CreateSessionAsync(userId, tenantId2, roleIds, permissionsVersion: 1);

        // Act
        var revokedCount = await _sessionService.RevokeAllSessionsForUserAsync(userId, tenantId1);

        // Assert
        Assert.Equal(1, revokedCount);

        var session1 = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId1);
        var session2 = await _dbContext.Sessions.FirstAsync(s => s.Id == sessionId2);

        Assert.NotNull(session1.RevokedAt);
        Assert.Null(session2.RevokedAt);
    }

    [Fact]
    public async Task RevokeAllSessionsForUserAsync_WithNoActiveSessions_ReturnsZero()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var revokedCount = await _sessionService.RevokeAllSessionsForUserAsync(userId, tenantId);

        // Assert
        Assert.Equal(0, revokedCount);
    }
}
