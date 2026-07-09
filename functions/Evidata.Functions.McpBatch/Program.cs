using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.McpBatch.Handlers;
using Evidata.Modules.Mcp;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();

builder.Services.AddMcpModule(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

// Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

builder.Services.AddScoped<McpRetryHandler>();
builder.Services.AddScoped<HitlEscalationHandler>();

builder.Build().Run();
