using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Evidata.Modules.Identity.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evidata.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityBridge(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? environment = null)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(IdentityModule).Assembly.FullName)));

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IInvitationRepository, InvitationRepository>();
        services.AddScoped<LinkExternalIdentityCommandHandler>();
        services.AddScoped<DeactivateUserCommandHandler>();
        services.AddScoped<GetUserProfileQueryHandler>();

        // OIDC and authentication services
        services.AddScoped<IOidcService, OidcService>();
        services.AddSingleton<IEntraIdTokenService, EntraIdTokenService>();
        services.AddScoped<IUserResolutionService, UserResolutionService>();
        
        // Auth command/query handlers
        services.AddScoped<CompleteLoginCommandHandler>();
        services.AddScoped<LogoutCommandHandler>();
        services.AddScoped<GetSessionStatusQueryHandler>();
        services.AddScoped<GetCurrentUserProfileQueryHandler>();
        services.AddScoped<GetCurrentUserPermissionsQueryHandler>();

        // Admin user management command handlers
        services.AddScoped<InviteUserCommandHandler>();
        services.AddScoped<ResendInvitationCommandHandler>();
        services.AddScoped<RevokeInvitationCommandHandler>();
        services.AddScoped<UpdateUserCommandHandler>();
        services.AddScoped<SuspendUserCommandHandler>();
        services.AddScoped<ReactivateUserCommandHandler>();
        services.AddScoped<DisableUserCommandHandler>();
        services.AddScoped<ChangeUserRolesCommandHandler>();

        // Admin user management query handlers
        services.AddScoped<ListUsersQueryHandler>();
        services.AddScoped<GetUserDetailQueryHandler>();

        // Session service
        services.AddScoped<ISessionService, SessionService>();

        // Dev auth service (for local/test environments)
        services.AddScoped<IDevAuthService, DevAuthService>();

        services.AddHttpContextAccessor();

        // LocalDev en Development, JWT en otros ambientes
        if (environment?.IsDevelopment() == true)
        {
            services.AddScoped<ICurrentUserContext, LocalDevCurrentUserContext>();
        }
        else
        {
            services.AddScoped<ICurrentUserContext, SessionResolverFactory>();
        }

        return services;
    }
}
