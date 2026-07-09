using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Application.Queries;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Audit.Infrastructure;
using Evidata.Modules.Audit.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Audit;

public static class AuditModule
{
    public static IServiceCollection AddAudit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<AuditDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(AuditModule).Assembly.FullName)));

        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ITimelineQueryService, TimelineQueryService>();

        return services;
    }
}
