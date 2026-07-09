using Evidata.Modules.Security.Application.Commands;
using Evidata.Modules.Security.Domain;
using NSubstitute;

namespace Evidata.Tests.Unit.Security;

public class AssignRoleToUserCommandHandlerTests
{
    private readonly IUserRoleAssignmentRepository _assignments = Substitute.For<IUserRoleAssignmentRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserCommandHandlerTests()
    {
        _handler = new AssignRoleToUserCommandHandler(_assignments, _roles);
    }

    [Fact]
    public async Task Handle_ValidAssignment_ReturnsDto()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var role = Role.Create("admin", "Administrator");
        var command = new AssignRoleToUserCommand(userId, role.Id, tenantId);

        _roles.GetByIdAsync(role.Id).Returns(role);
        _assignments.GetByUserAsync(userId, tenantId).Returns([]);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(userId, result.UserId);
        Assert.Equal("admin", result.RoleName);
        await _assignments.Received(1).AssignAsync(Arg.Any<UserRoleAssignment>());
    }

    [Fact]
    public async Task Handle_DuplicateAssignment_ReturnsExistingAssignment()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var role = Role.Create("admin");
        var existing = UserRoleAssignment.Create(userId, role.Id, tenantId);
        var command = new AssignRoleToUserCommand(userId, role.Id, tenantId);

        _roles.GetByIdAsync(role.Id).Returns(role);
        _assignments.GetByUserAsync(userId, tenantId).Returns([existing]);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.Equal(role.Name, result.RoleName);
        await _assignments.DidNotReceive().AssignAsync(Arg.Any<UserRoleAssignment>());
    }

    [Fact]
    public async Task Handle_RoleNotFound_ThrowsException()
    {
        // Arrange
        var command = new AssignRoleToUserCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        _roles.GetByIdAsync(command.RoleId).Returns((Role?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.HandleAsync(command));
    }
}
