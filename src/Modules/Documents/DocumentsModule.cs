using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Documents.Application.Queries;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Evidata.Modules.Documents.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Evidata.Modules.Documents;

public static class DocumentsModule
{
    public static IServiceCollection AddDocumentsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<BlobStorageOptions>(
            configuration.GetSection(BlobStorageOptions.SectionName));

        // Aspire inyecta el connection string de Azurite en "ConnectionStrings:blobs"
        // (puerto dinámico según el contenedor). Tiene prioridad sobre el default estático.
        services.PostConfigure<BlobStorageOptions>(opts =>
        {
            var aspireBlobs = configuration.GetConnectionString("blobs");
            if (!string.IsNullOrWhiteSpace(aspireBlobs))
                opts.ConnectionString = aspireBlobs;
        });

        services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        services.AddScoped<ListDocumentsQueryHandler>();

        var connectionString = configuration.GetConnectionString("evidata-db")
            ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

        services.AddDbContext<DocumentDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(DocumentDbContextFactory).Assembly.FullName)));

        return services;
    }
}
