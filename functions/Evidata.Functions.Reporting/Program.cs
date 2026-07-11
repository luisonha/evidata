using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.Reporting.Handlers;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Evidata.Modules.Documents.Infrastructure.Storage;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.Reporting;
using Evidata.Worker.Outbox.Persistence;
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
builder.Services.AddAudit(builder.Configuration);
builder.Services.AddProcessingInventoryModule(builder.Configuration);
// IOutboxWriter requerido por OutboxGapNotificationService (GapManagement).
// fn-reporting solo lee datos — NullOutboxWriter evita la dependencia del Outbox real.
builder.Services.AddScoped<IOutboxWriter, NullOutboxWriter>();
builder.Services.AddGapManagementModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);

// Blob Storage — usa Azurite en local (AzureWebJobsStorage=UseDevelopmentStorage=true)
builder.Services.Configure<BlobStorageOptions>(
    builder.Configuration.GetSection(BlobStorageOptions.SectionName));
builder.Services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults();

// Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
    builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

builder.Services.AddScoped<RatReportHandler>();
builder.Services.AddScoped<GapReportHandler>();

builder.Build().Run();
