using Azure.Monitor.OpenTelemetry.Exporter;
using Evidata.Functions.Notifications.Email;
using Evidata.Functions.Notifications.Handlers;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;

namespace Evidata.Functions.Notifications;

/// <summary>
/// Factory for building the fn-notifications host with complete DI configuration.
/// Extracted from Program.cs to enable testable DI validation without running the host.
/// </summary>
public static class HostBuilderFactory
{
    /// <summary>
    /// Builds and returns an IHost with full DI configuration.
    /// Does NOT call host.Run() — that remains in Program.cs entrypoint.
    /// </summary>
    public static IHost Build(string[] args)
    {
        var builder = FunctionsApplication.CreateBuilder(args);

        builder.ConfigureFunctionsWebApplication();
        builder.AddServiceDefaults();

        builder.Services.AddOpenTelemetry()
            .UseFunctionsWorkerDefaults();

        // Azure Monitor solo en producción (requiere APPLICATIONINSIGHTS_CONNECTION_STRING)
        if (!string.IsNullOrWhiteSpace(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            builder.Services.AddOpenTelemetry().UseAzureMonitorExporter();

        builder.Services.AddSingleton<IEmailSender, MailKitEmailSender>();
        builder.Services.AddScoped<GapNotificationHandler>();
        builder.Services.AddScoped<RatNotificationHandler>();

        return builder.Build();
    }
}
