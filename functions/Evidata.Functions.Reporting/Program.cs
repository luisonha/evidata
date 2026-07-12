using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.Reporting.Handlers;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Evidata.Modules.Documents.Infrastructure.Storage;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.Identity;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.Reporting;
using Evidata.Modules.Security;
using Evidata.Modules.TenantManagement;
using Evidata.Modules.Workflow;
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

// DI Registration order (must match Evidata.Api/Program.cs):
// 0. IDistributedCache — requerido por ReviewRequirementPolicyService (Workflow)
// 1. TenantManagement (foundation)
// 2. Identity (provides ICurrentUserContext for security & other modules)
// 3. RBAC/Security (requires ICurrentUserContext from Identity)
// 4. Other modules (Evidence requires IResourcePermissionsQueryService from Security)
// 5. Workflow — requerido por ProcessingActivityRiskAssessmentService (Reporting),
//    que consume IReviewSummaryQueryService (Workflow).
builder.Services.AddDistributedMemoryCache();
builder.Services.AddTenantManagement(builder.Configuration);
builder.Services.AddIdentityBridge(builder.Configuration, builder.Environment);
builder.Services.AddRbac(builder.Configuration);
builder.Services.AddEvidenceModule(builder.Configuration);
builder.Services.AddAudit(builder.Configuration);
builder.Services.AddProcessingInventoryModule(builder.Configuration);
// IOutboxWriter requerido por OutboxGapNotificationService (GapManagement) y
// por IReviewNotificationService (Workflow, vía OutboxReviewNotificationService).
// fn-reporting solo lee datos — NullOutboxWriter evita la dependencia del Outbox real.
builder.Services.AddScoped<IOutboxWriter, NullOutboxWriter>();
builder.Services.AddGapManagementModule(builder.Configuration);
builder.Services.AddWorkflowModule(builder.Configuration);
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
