var builder = DistributedApplication.CreateBuilder(args);

// Infraestructura local
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("evidata-db");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs = storage.AddBlobs("blobs");
var queues = storage.AddQueues("queues");

var mailpit = builder.AddMailPit("mailpit");

// Proyectos
var api = builder.AddProject<Projects.Evidata_Api>("evidata-api")
    .WithReference(postgres)
    .WithReference(blobs)
    .WithReference(queues)
    .WaitFor(postgres);

builder.AddProject<Projects.Evidata_Worker_Outbox>("evidata-worker-outbox")
    .WithReference(postgres)
    .WithReference(queues)
    .WaitFor(postgres);

builder.Build().Run();
