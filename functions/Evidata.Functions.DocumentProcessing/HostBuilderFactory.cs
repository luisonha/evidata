using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.DocumentProcessing.Handlers;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace Evidata.Functions.DocumentProcessing;

/// <summary>
/// Factory for building the fn-documents host with complete DI configuration.
/// Extracted from Program.cs to enable testable DI validation without running the host.
/// </summary>
public static class HostBuilderFactory
{
    /// <summary>
    /// Builds and returns an IHost with full DI configuration.
    /// Does NOT call host.Run() — that remains in Program.cs entrypoint.
    /// </summary>
    public static IHost Build(string[] args) => BuildForValidation(args).Host;

    /// <summary>
    /// Builds the host AND exposes the raw IServiceCollection used to build it,
    /// so tests can resolve every registered service descriptor and force full
    /// DI graph validation.
    /// </summary>
    public static (IHost Host, IServiceCollection Services) BuildForValidation(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);

        builder.ConfigureFunctionsWebApplication();
        builder.AddServiceDefaults();

        // PostgreSQL — DocumentDbContext
        var connectionString = builder.Configuration["ConnectionStrings:evidata-db"]
            ?? "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=password";

        builder.Services.AddDbContext<DocumentDbContext>(options =>
            options.UseNpgsql(connectionString,
                b => b.MigrationsAssembly(typeof(DocumentDbContextFactory).Assembly.FullName)));

        // Handlers
        builder.Services.AddScoped<DocumentUploadedHandler>();

        builder.Services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults();

        // Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

        var services = builder.Services;
        var host = builder.Build();
        return (host, services);
    }
}
