using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Application.Commands;
using Evidata.Modules.Identity.Application.Queries;
using Evidata.Modules.Identity.Domain;
using Evidata.Modules.Identity.Infrastructure;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Persistence;
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
        services.AddScoped<LinkExternalIdentityCommandHandler>();
        services.AddScoped<DeactivateUserCommandHandler>();
        services.AddScoped<GetUserProfileQueryHandler>();

        // LocalDev en Development, Null en otros ambientes
        if (environment?.IsDevelopment() == true)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserContext, LocalDevCurrentUserContext>();
        }
        else
        {
            services.AddScoped<ICurrentUserContext, NullCurrentUserContext>();
        }

        return services;
    }
}
