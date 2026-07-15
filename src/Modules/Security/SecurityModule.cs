using Evidata.Modules.Security.Application.Abstractions;
using Evidata.Modules.Security.Application.Commands;
using Evidata.Modules.Security.Application.Queries;
using Evidata.Modules.Security.Domain;
using Evidata.Modules.Security.Infrastructure.Authorization;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Security;

public static class SecurityModule
{
    public static IServiceCollection AddRbac(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<SecurityDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(SecurityModule).Assembly.FullName)));

        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IUserRoleAssignmentRepository, UserRoleAssignmentRepository>();
        services.AddScoped<Application.Abstractions.IAuthorizationEvaluator, AuthorizationEvaluator>();
        services.AddScoped<IResourcePermissionsQueryService, ResourcePermissionsQueryService>();
        
        // Authorization handlers for fine-grained RBAC policies
        services.AddScoped<IAuthorizationHandler, TenantOwnerOrComplianceAdminHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddScoped<AssignRoleToUserCommandHandler>();
        services.AddScoped<RemoveRoleFromUserCommandHandler>();
        services.AddScoped<GetUserRolesQueryHandler>();
        services.AddScoped<GetAllRolesQueryHandler>();
        services.AddScoped<GetAllPermissionsQueryHandler>();

        return services;
    }
}
