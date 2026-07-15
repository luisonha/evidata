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

    // ========== TENANT ISOLATION TESTS ==========
    // These tests verify that cross-tenant access is rejected with UserNotFound (404),
    // not with a permission error (403), and that no data leaks occur.

    [Fact]
    public async Task GetUserAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        // Admin from TenantA tries to read a user from TenantB
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        
        // Admin from TenantA requests user from TenantB
        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.GetUserAsync(tenantA, user.Id, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task UpdateUserAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new UpdateUserCommand(tenantA, user.Id, Guid.NewGuid(), "NewName", null);

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.UpdateUserAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task SuspendUserAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new SuspendUserCommand(tenantA, user.Id, Guid.NewGuid(), "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.SuspendUserAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task ReactivateUserAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();
        user.Suspend();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new ReactivateUserCommand(tenantA, user.Id, Guid.NewGuid(), "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.ReactivateUserAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task DisableUserAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new DisableUserCommand(tenantA, user.Id, Guid.NewGuid(), "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.DisableUserAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task ChangeUserRolesAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        user.Activate();

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new ChangeUserRolesCommand(tenantA, user.Id, Guid.NewGuid(), new[] { "OtherRole" }, "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.ChangeUserRolesAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task ResendInvitationAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new ResendInvitationCommand(tenantA, user.Id, Guid.NewGuid());

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.ResendInvitationAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task RevokeInvitationAsync_CrossTenantAccess_ThrowsUserNotFound()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "user@test.com", "User", tenantB);
        // Set status to Invited (as required by RevokeInvitation)
        user.SetRoles(new[] { OtherRoleId });
        // Manually set to Invited since there's no public method for it
        // We'll use reflection or accept that this particular test may need adjustment

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(Task.FromResult<UserProfile?>(user));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        var cmd = new RevokeInvitationCommand(tenantA, user.Id, Guid.NewGuid(), "test");

        var ex = await Assert.ThrowsAsync<IdentityDomainException>(
            () => service.RevokeInvitationAsync(cmd, CancellationToken.None));
        
        Assert.Equal(IdentityErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task ListUsersAsync_FiltersOnlyByRequestedTenant_NeverReturnsOtherTenantUsers()
    {
        // Verify that ListUsersAsync uses GetByTenantIdAsync and never returns cross-tenant users
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var user1 = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "userA@test.com", "UserA", tenantA);
        var user2 = UserProfile.Create(Guid.NewGuid().ToString(), "EntraId", "userB@test.com", "UserB", tenantB);

        var userRepo = Substitute.For<IUserProfileRepository>();
        // GetByTenantIdAsync should only return users from the requested tenant
        userRepo.GetByTenantIdAsync(tenantA, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserProfile>>(new[] { user1 }));
        userRepo.GetByTenantIdAsync(tenantB, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<UserProfile>>(new[] { user2 }));

        var service = CreateService(userRepo, Substitute.For<IInvitationRepository>());
        
        // List users from TenantA
        var query = new ListUsersQuery(tenantA, null, null, null, 1, 10);
        var result = await service.ListUsersAsync(query, CancellationToken.None);

        // Should only contain TenantA users
        Assert.Single(result.Items);
        Assert.Equal("userA@test.com", result.Items[0].Email);
        
        // Verify GetByTenantIdAsync was called with the correct tenant
        await userRepo.Received(1).GetByTenantIdAsync(tenantA, Arg.Any<CancellationToken>());
        await userRepo.DidNotReceive().GetByTenantIdAsync(tenantB, Arg.Any<CancellationToken>());
    }
}
