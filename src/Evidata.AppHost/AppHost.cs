var builder = DistributedApplication.CreateBuilder(args);

// ── Infraestructura local ────────────────────────────────────────────────────
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("evidata-db");

var storage = builder.AddAzureStorage("storage")
    .RunAsEmulator();

var blobs   = storage.AddBlobs("blobs");
var queues  = storage.AddQueues("queues");

var mailpit = builder.AddMailPit("mailpit");

// ── Servicios principales ────────────────────────────────────────────────────
var api = builder.AddProject<Projects.Evidata_Api>("evidata-api")
    .WithReference(postgres)
    .WithReference(blobs)
    .WithReference(queues)
    .WaitFor(postgres);

builder.AddProject<Projects.Evidata_Worker_Outbox>("evidata-worker-outbox")
    .WithReference(postgres)
    .WithReference(queues)
    .WaitFor(postgres);

// ── Azure Functions ──────────────────────────────────────────────────────────
// fn-notifications: consume cola de eventos RAT/Gap y envía emails vía MailPit (local)
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_Notifications>("fn-notifications")
    .WithReference(queues)
    .WithReference(mailpit)
    .WaitFor(queues);

// fn-documents: procesa documentos subidos a Blob Storage
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_DocumentProcessing>("fn-documents")
    .WithReference(queues)
    .WithReference(blobs)
    .WithReference(postgres)
    .WaitFor(queues)
    .WaitFor(postgres);

// fn-maintenance: tareas programadas (limpieza, housekeeping)
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_Maintenance>("fn-maintenance")
    .WithReference(queues)
    .WithReference(postgres)
    .WaitFor(postgres);

// fn-mcp-batch: procesamiento por lotes de ítems MCP
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_McpBatch>("fn-mcp-batch")
    .WithReference(queues)
    .WithReference(postgres)
    .WaitFor(postgres);

// fn-reporting: generación de reportes Excel/PDF bajo demanda
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_Reporting>("fn-reporting")
    .WithReference(queues)
    .WithReference(blobs)
    .WithReference(postgres)
    .WaitFor(postgres);

// fn-search-indexing: indexación de documentos y actividades para búsqueda
builder.AddAzureFunctionsProject<Projects.Evidata_Functions_SearchIndexing>("fn-search-indexing")
    .WithReference(queues)
    .WithReference(blobs)
    .WithReference(postgres)
    .WaitFor(postgres);

builder.Build().Run();
