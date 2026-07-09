using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Infrastructure.Jobs;
using Evidata.Modules.Reporting.Infrastructure.Persistence;
using Evidata.Modules.Reporting.Infrastructure.Persistence.Factories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Reporting;

public static class ReportingModule
{
    public static IServiceCollection AddReportingModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<ReportingDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(ReportingDbContextFactory).Assembly.FullName)));

        services.AddScoped<IReportJobService, ReportJobService>();

        return services;
    }
}
