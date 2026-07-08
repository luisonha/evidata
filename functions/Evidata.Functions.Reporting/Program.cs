using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Modules.Evidence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();

builder.Services.AddEvidenceModule(builder.Configuration);

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();
