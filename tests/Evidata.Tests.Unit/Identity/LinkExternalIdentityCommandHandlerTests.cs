using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Identity;

public class LinkExternalIdentityCommandHandlerTests
{
    private readonly IUserProfileRepository _repository = Substitute.For<IUserProfileRepository>();
    private readonly LinkExternalIdentityCommandHandler _handler;

    public LinkExternalIdentityCommandHandlerTests()
    {
        _handler = new LinkExternalIdentityCommandHandler(_repository);
    }

    [Fact]
    public async Task Handle_NewUser_CreatesUserProfile()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var command = new LinkExternalIdentityCommand("ext-123", "entra", "user@test.com", "Test User", tenantId);
        _repository.GetByExternalIdAsync("ext-123", "entra", tenantId).Returns((UserProfile?)null);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tenantId, result.TenantId);
        Assert.Equal("user@test.com", result.Email);
        Assert.Equal("Test User", result.DisplayName);
        Assert.Equal("entra", result.Provider);
        await _repository.Received(1).UpsertAsync(Arg.Any<UserProfile>());
    }

    [Fact]
    public async Task Handle_ExistingUser_UpdatesProfile()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var existing = UserProfile.Create("ext-123", "entra", "old@test.com", "Old Name", tenantId);
        var command = new LinkExternalIdentityCommand("ext-123", "entra", "new@test.com", "New Name", tenantId);
        _repository.GetByExternalIdAsync("ext-123", "entra", tenantId).Returns(existing);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal("new@test.com", result.Email);
        Assert.Equal("New Name", result.DisplayName);
        await _repository.Received(1).UpsertAsync(existing);
    }
}
