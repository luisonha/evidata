using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Evidata.Api.Infrastructure.HealthChecks;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddEvidataHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var pgConnection = configuration.GetConnectionString("evidata-db");
        var blobConnection = configuration.GetConnectionString("blobs");
        var queueConnection = configuration.GetConnectionString("queues");

        var builder = services.AddHealthChecks();

        if (!string.IsNullOrWhiteSpace(pgConnection))
        {
            builder.AddNpgSql(
                pgConnection,
                name: "postgres",
                tags: ["db", "ready"],
                failureStatus: HealthStatus.Degraded);
        }

        if (!string.IsNullOrWhiteSpace(blobConnection))
        {
            // v9 API: (clientFactory, optionsFactory, name, failureStatus, tags, timeout)
            var capturedBlob = blobConnection;
            builder.AddAzureBlobStorage(
                clientFactory: _ => new BlobServiceClient(capturedBlob),
                optionsFactory: null,
                name: "blob-storage",
                failureStatus: HealthStatus.Degraded,
                tags: ["storage", "ready"],
                timeout: null);
        }

        if (!string.IsNullOrWhiteSpace(queueConnection))
        {
            // v9 API: (clientFactory, optionsFactory, name, failureStatus, tags, timeout)
            var capturedQueue = queueConnection;
            builder.AddAzureQueueStorage(
                clientFactory: _ => new QueueServiceClient(capturedQueue),
                optionsFactory: null,
                name: "queue-storage",
                failureStatus: HealthStatus.Degraded,
                tags: ["queue", "ready"],
                timeout: null);
        }

        return services;
    }

    public static WebApplication MapEvidataHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("ready"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
            .AllowAnonymous();

        app.MapHealthChecks("/health/db", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("db"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
            .AllowAnonymous();

        app.MapHealthChecks("/health/storage", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("storage"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
            .AllowAnonymous();

        app.MapHealthChecks("/health/queue", new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("queue"),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
            .AllowAnonymous();

        return app;
    }
}
