using Evidata.Worker.Outbox;
using Evidata.Worker.Outbox.Messaging;
using Evidata.Worker.Outbox.Persistence;
using Evidata.Worker.Outbox.Poison;
using Evidata.Worker.Outbox.Storage;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();

// PostgreSQL — Outbox
var connectionString = builder.Configuration.GetConnectionString("evidata-db")
    ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");

builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseNpgsql(connectionString,
        b => b.MigrationsAssembly(typeof(OutboxDbContextFactory).Assembly.FullName)));

builder.Services.AddScoped<OutboxRepository>();
builder.Services.AddScoped<IOutboxRepository>(sp => sp.GetRequiredService<OutboxRepository>());
builder.Services.AddScoped<IOutboxWriter>(sp => sp.GetRequiredService<OutboxRepository>());

// Messaging
builder.Services.AddSingleton<IDestinationResolver, DefaultDestinationResolver>();

// Azure Storage Queue publisher (usa Azurite en local via Aspire)
var storageConn = builder.Configuration.GetConnectionString("blob")
    ?? builder.Configuration.GetConnectionString("azurite");

if (!string.IsNullOrEmpty(storageConn))
    builder.Services.AddScoped<IMessagePublisher, AzureStorageQueuePublisher>();
else
    builder.Services.AddScoped<IMessagePublisher, InMemoryMessagePublisher>();

builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<DeadLetterMonitorWorker>();

var host = builder.Build();
host.Run();
