using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.SearchIndexing.Handlers;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.Search;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace Evidata.Functions.SearchIndexing;

/// <summary>
/// Factory for building the fn-search-indexing host with complete DI configuration.
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

        builder.Services.AddDocumentsModule(builder.Configuration);
        builder.Services.AddAudit(builder.Configuration);
        builder.Services.AddProcessingInventoryModule(builder.Configuration);
        builder.Services.AddSearchModule(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults();

        // Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

        builder.Services.AddScoped<DocumentIndexingHandler>();
        builder.Services.AddScoped<RatIndexingHandler>();

        var services = builder.Services;
        var host = builder.Build();
        return (host, services);
    }
}
