using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Application.Services;
using Evidata.Modules.Identity.Contracts;
using Evidata.Modules.Identity.Domain;
using NSubstitute;
using Xunit;

namespace Evidata.Tests.Unit.Identity;

public class AdminUsersServiceTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid TenantOwnerRoleId = Guid.NewGuid();
    private static readonly Guid OtherRoleId = Guid.NewGuid();

    private static AdminUsersService CreateService(
        IUserProfileRepository userRepo,
        IInvitationRepository invRepo,
        ISessionService? sessionService = null,
        IRoleNameResolver? roleResolver = null)
    {
        roleResolver ??= CreateResolverWithTenantOwner();
        sessionService ??= Substitute.For<ISessionService>();
        return new AdminUsersService(userRepo, invRepo, sessionService, roleResolver);
    }

    private static IRoleNameResolver CreateResolverWithTenantOwner()
    {
        var resolver = Substitute.For<IRoleNameResolver>();
        resolver.GetRoleIdByNameAsync("TenantOwner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Guid?>(TenantOwnerRoleId));
        resolver.GetRoleNamesAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var ids = ((IEnumerable<Guid>)ci[0]).ToList();
                return Task.FromResult<IReadOnlyList<string>>(
                    ids.Select(id => id == TenantOwnerRoleId ? "TenantOwner" : "OtherRole").ToList());
            });
        resolver.GetRoleIdsByNamesAsync(Arg.Any<IEnumerable<string>>(), Arg.Any<CancellationToken>())
            .Returns(ci =>
            {
                var names = ((IEnumerable<string>)ci[0]).ToList();
                var ids = names.Select(n => n == "TenantOwner" ? TenantOwnerRoleId : OtherRoleId).ToList();
                return Task.FromResult<IReadOnlyList<Guid>>(ids);
            });
        return resolver;
    }

    private static UserProfile CreateUserWithRole(Guid roleId, UserStatus status = UserStatus.Active)
    {
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "owner@test.com", "Owner", TenantId);
        user.SetRoles(new[] { roleId });
        user.Activate();
        if (status == UserStatus.Suspended)
        {
            user.Suspend();
        }
        return user;
    }

    [Fact]
    public async Task SuspendUserAsync_LastTenantOwner_ThrowsLastTenantOwnerBlocked()
    {
        var user = CreateUserWithRole(TenantOwnerRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));
        userRepo.CountActiveTenantOwnersAsync(TenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new SuspendUserCommand(TenantId, user.Id, Guid.NewGuid(), "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(() => service.SuspendUserAsync(cmd, CancellationToken.None));
        Assert.Equal(IdentityErrorCodes.LastTenantOwnerBlocked, ex.ErrorCode);
    }

    [Fact]
    public async Task SuspendUserAsync_NotLastTenantOwner_Succeeds()
    {
        var user = CreateUserWithRole(TenantOwnerRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));
        userRepo.CountActiveTenantOwnersAsync(TenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(2));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new SuspendUserCommand(TenantId, user.Id, Guid.NewGuid(), "test");

        var result = await service.SuspendUserAsync(cmd, CancellationToken.None);

        Assert.Equal(UserStatus.Suspended, result.NewStatus);
        await userRepo.Received(1).UpsertAsync(Arg.Is<UserProfile>(u => u.Status == UserStatus.Suspended), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuspendUserAsync_NonOwnerUser_DoesNotConsultOwnerCount()
    {
        var user = CreateUserWithRole(OtherRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new SuspendUserCommand(TenantId, user.Id, Guid.NewGuid(), "test");

        var result = await service.SuspendUserAsync(cmd, CancellationToken.None);

        Assert.Equal(UserStatus.Suspended, result.NewStatus);
        await userRepo.DidNotReceive().CountActiveTenantOwnersAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangeUserRolesAsync_RemovingLastTenantOwnerRole_ThrowsLastTenantOwnerBlocked()
    {
        var user = CreateUserWithRole(TenantOwnerRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));
        userRepo.CountActiveTenantOwnersAsync(TenantId, Arg.Any<CancellationToken>()).Returns(Task.FromResult(1));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new ChangeUserRolesCommand(TenantId, user.Id, Guid.NewGuid(), new[] { "OtherRole" }, "demote");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(() => service.ChangeUserRolesAsync(cmd, CancellationToken.None));
        Assert.Equal(IdentityErrorCodes.LastTenantOwnerBlocked, ex.ErrorCode);
    }

    [Fact]
    public async Task ChangeUserRolesAsync_ValidChange_ActuallyPersistsNewRoles()
    {
        var user = CreateUserWithRole(OtherRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new ChangeUserRolesCommand(TenantId, user.Id, Guid.NewGuid(), new[] { "TenantOwner" }, "promote");

        await service.ChangeUserRolesAsync(cmd, CancellationToken.None);

        // Real behavioral assertion: the in-memory user's role collection must actually change,
        // not just the response DTO (this is exactly the bug that was silently returning fake success).
        Assert.True(user.HasRole(TenantOwnerRoleId));
        Assert.False(user.HasRole(OtherRoleId));
        await userRepo.Received(1).UpsertAsync(Arg.Is<UserProfile>(u => u.HasRole(TenantOwnerRoleId)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResendInvitationAsync_LooksUpByEmail_NotByInvitationUserId()
    {
        // Regression test: Invitation.UserId stays null until the invitee accepts, so lookup
        // must go through email/tenant — looking up by UserId would always return null for a
        // genuinely pending invitation and make this endpoint permanently non-functional.
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "invitee@test.com", "Invitee", TenantId);
        var invitation = Invitation.Create(TenantId, "invitee@test.com", "OtherRole", Guid.NewGuid(), DateTime.UtcNow.AddDays(1));

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var invRepo = Substitute.For<IInvitationRepository>();
        invRepo.GetByUserIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<Invitation?>(null));
        invRepo.GetByEmailAndTenantAsync("invitee@test.com", TenantId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Invitation?>(invitation));

        var service = CreateService(userRepo, invRepo);
        var cmd = new ResendInvitationCommand(TenantId, user.Id, Guid.NewGuid());
        var originalExpiry = invitation.ExpiresAt;

        var result = await service.ResendInvitationAsync(cmd, CancellationToken.None);

        // Real behavioral assertion: the invitation's expiry must actually change and be persisted.
        Assert.True(invitation.ExpiresAt > originalExpiry);
        await invRepo.Received(1).UpsertAsync(Arg.Is<Invitation>(i => i.ExpiresAt == invitation.ExpiresAt), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SuspendUserAsync_TenantOwnerRoleUnresolvable_FailsClosedInsteadOfSilentlySkippingGuard()
    {
        // Regression test: if the role catalog can't resolve "TenantOwner" (data-integrity problem),
        // the guard must fail closed (throw), never silently behave as "user is not a TenantOwner".
        var user = CreateUserWithRole(TenantOwnerRoleId);
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var brokenResolver = Substitute.For<IRoleNameResolver>();
        brokenResolver.GetRoleIdByNameAsync("TenantOwner", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Guid?>(null));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>(), roleResolver: brokenResolver);
        var cmd = new SuspendUserCommand(TenantId, user.Id, Guid.NewGuid(), "test");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SuspendUserAsync(cmd, CancellationToken.None));
    }
}
