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

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();

// PostgreSQL — DocumentDbContext
var connectionString = builder.Configuration["ConnectionStrings:evidata-db"]
    ?? "Host=localhost;Port=5432;Database=evidata;Username=evidata;Password=evidata_dev";

builder.Services.AddDbContext<DocumentDbContext>(options =>
    options.UseNpgsql(connectionString,
        b => b.MigrationsAssembly(typeof(DocumentDbContextFactory).Assembly.FullName)));

// Handlers
builder.Services.AddScoped<DocumentUploadedHandler>();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

builder.Build().Run();
