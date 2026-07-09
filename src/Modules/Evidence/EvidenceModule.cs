using Azure.Storage.Queues;
using Evidata.Modules.Evidence.Application.Abstractions;
using Evidata.Modules.Evidence.Application.Commands;
using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Evidence.Infrastructure.Download;
using Evidata.Modules.Evidence.Infrastructure.Links;
using Evidata.Modules.Evidence.Infrastructure.Pack;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Evidence;

public static class EvidenceModule
{
    public static IServiceCollection AddEvidenceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<EvidenceDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(EvidenceDbContextFactory).Assembly.FullName)));

        services.AddScoped<IEvidenceDownloadService, EvidenceDownloadService>();
        services.AddScoped<IEvidenceLinkService, EvidenceLinkService>();
        services.AddScoped<IEvidenceSummaryQueryService, GetEvidenceSummaryQueryHandler>();
        services.AddScoped<ListEvidenceQueryHandler>();
        services.AddScoped<GetEvidenceQueryHandler>();
        services.AddScoped<GetEvidenceSummaryQueryHandler>();
        services.AddScoped<CreateEvidenceCommandHandler>();

        // EvidencePackService requiere QueueClient — solo disponible si está configurado
        var storageConn = configuration.GetValue<string>("AzureWebJobsStorage");
        if (!string.IsNullOrWhiteSpace(storageConn))
        {
            services.AddSingleton(_ => new QueueClient(storageConn, "evidence-pack-jobs",
                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.None }));
            services.AddScoped<IEvidencePackService, EvidencePackService>();
        }

        return services;
    }
}
