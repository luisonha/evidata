using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure;
using Evidata.Modules.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityBridge(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(IdentityModule).Assembly.FullName)));

        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<LinkExternalIdentityCommandHandler>();
        services.AddScoped<DeactivateUserCommandHandler>();
        services.AddScoped<GetUserProfileQueryHandler>();

        // NullCurrentUserContext como default — LocalAuth (f1-local-auth) lo reemplaza
        services.AddScoped<ICurrentUserContext, NullCurrentUserContext>();

        return services;
    }
}
