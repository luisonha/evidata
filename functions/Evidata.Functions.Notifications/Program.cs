using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.Notifications.Email;
using Evidata.Functions.Notifications.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();
builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry()
    .UseFunctionsWorkerDefaults()
    .UseAzureMonitorExporter();

// Email
builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();

// Handlers
builder.Services.AddScoped<GapNotificationHandler>();
builder.Services.AddScoped<RatNotificationHandler>();

builder.Build().Run();
