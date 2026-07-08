using Evidata.Worker.Outbox;
using Evidata.Worker.Outbox.Messaging;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

// Outbox messaging
builder.Services.AddSingleton<IDestinationResolver, DefaultDestinationResolver>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

var host = builder.Build();
host.Run();
