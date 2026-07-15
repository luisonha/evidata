using Evidata.Modules.Identity.Domain;
using Xunit;

namespace Evidata.Tests.Unit.Identity;

public class SessionTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsActiveSession()
    {
        // Arrange
        var sessionId = "test-session-id-123";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var roleIds = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var rolesSnapshot = string.Join(",", roleIds);

        // Act
        var session = Session.Create(
            sessionId,
            userId,
            tenantId,
            rolesSnapshot,
            permissionsVersion: 1);

        // Assert
        Assert.Equal(sessionId, session.Id);
        Assert.Equal(userId, session.UserId);
        Assert.Equal(tenantId, session.TenantId);
        Assert.Equal(rolesSnapshot, session.RolesSnapshot);
        Assert.Equal(1, session.PermissionsVersion);
        Assert.True(session.IsActive);
        Assert.Null(session.RevokedAt);
        Assert.True(session.CreatedAt <= DateTime.UtcNow);
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void Create_WithCustomExpiryDuration_SetsCorrectExpiration()
    {
        // Arrange
        var sessionId = "test-session-id";
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var rolesSnapshot = Guid.NewGuid().ToString();
        var customExpiry = TimeSpan.FromHours(1);

        // Act
        var session = Session.Create(
            sessionId,
            userId,
            tenantId,
            rolesSnapshot,
            permissionsVersion: 1,
            expiryDuration: customExpiry);

        // Assert
        var expectedExpiration = DateTime.UtcNow.Add(customExpiry);
        Assert.True(session.ExpiresAt <= expectedExpiration.AddSeconds(1));
        Assert.True(session.ExpiresAt >= expectedExpiration.AddSeconds(-1));
    }

    [Fact]
    public void Revoke_MarksSessionAsNotActive()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1);

        // Act
        session.Revoke();

        // Assert
        Assert.False(session.IsActive);
        Assert.NotNull(session.RevokedAt);
        Assert.True(session.RevokedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void IsActive_ReturnsFalseWhenExpired()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1,
            expiryDuration: TimeSpan.FromSeconds(-1)); // Expired already

        // Act & Assert
        Assert.False(session.IsActive);
    }

    [Fact]
    public void IsActive_ReturnsFalseWhenRevoked()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1,
            expiryDuration: TimeSpan.FromHours(24));

        session.Revoke();

        // Act & Assert
        Assert.False(session.IsActive);
    }

    [Fact]
    public void Touch_UpdatesLastAccessedAtAndExtendsExpiration()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1,
            expiryDuration: TimeSpan.FromHours(1));

        var originalExpiresAt = session.ExpiresAt;
        var originalLastAccessedAt = session.LastAccessedAt;

        // Act
        System.Threading.Thread.Sleep(100); // Small delay
        session.Touch();

        // Assert
        Assert.True(session.LastAccessedAt > originalLastAccessedAt);
        Assert.True(session.ExpiresAt > originalExpiresAt);
    }

    [Fact]
    public void Touch_DoesNothingWhenRevoked()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1);

        session.Revoke();
        var revokedAt = session.RevokedAt;

        // Act
        session.Touch();

        // Assert
        Assert.Equal(revokedAt, session.RevokedAt); // Unchanged
    }

    [Fact]
    public void PermissionsHaveChanged_ReturnsTrueWhenVersionDiffers()
    {
        // Arrange
        var session = Session.Create(
            "test-id",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid().ToString(),
            permissionsVersion: 1);

        // Act & Assert
        Assert.True(session.PermissionsHaveChanged(2));
        Assert.False(session.PermissionsHaveChanged(1));
    }
}
