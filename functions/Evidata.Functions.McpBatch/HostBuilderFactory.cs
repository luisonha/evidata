using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.McpBatch.Handlers;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.GapManagement;
using Evidata.Modules.LegalKnowledge;
using Evidata.Modules.Mcp;
using Evidata.Modules.ProcessingInventory;
using Evidata.Worker.Outbox.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace Evidata.Functions.McpBatch;

/// <summary>
/// Factory for building the fn-mcp-batch host with complete DI configuration.
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
        // IOutboxWriter requerido por OutboxGapNotificationService (GapManagement).
        // fn-mcp no envía notificaciones — NullOutboxWriter satisface la dependencia de DI.
        builder.Services.AddScoped<IOutboxWriter, NullOutboxWriter>();
        builder.Services.AddGapManagementModule(builder.Configuration);
        builder.Services.AddLegalKnowledge(builder.Configuration);
        builder.Services.AddMcpModule(builder.Configuration);

        builder.Services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults();

        // Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

        builder.Services.AddScoped<McpRetryHandler>();
        builder.Services.AddScoped<HitlEscalationHandler>();

        var services = builder.Services;
        var host = builder.Build();
        return (host, services);
    }
}
