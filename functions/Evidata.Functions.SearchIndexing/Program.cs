using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.SearchIndexing.Handlers;
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
builder.Services.AddProcessingInventoryModule(builder.Configuration);
builder.Services.AddSearchModule(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Services.AddScoped<DocumentIndexingHandler>();
builder.Services.AddScoped<RatIndexingHandler>();

builder.Build().Run();
