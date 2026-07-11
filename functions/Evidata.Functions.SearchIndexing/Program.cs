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

builder.Build().Run();
