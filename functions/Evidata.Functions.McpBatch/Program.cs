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
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Services.AddScoped<McpRetryHandler>();
builder.Services.AddScoped<HitlEscalationHandler>();

builder.Build().Run();
