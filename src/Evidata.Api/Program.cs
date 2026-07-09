using Evidata.Api.Infrastructure.HealthChecks;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.Identity;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.LegalKnowledge;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.Security;
using Evidata.Modules.TenantManagement;
using Evidata.Modules.Mcp;
using Evidata.Modules.Reporting;
using Evidata.Modules.Search;
using Evidata.Modules.Workflow;
using Evidata.Worker.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddEvidataHealthChecks(builder.Configuration);
builder.Services.AddTenantManagement(builder.Configuration);
builder.Services.AddIdentityBridge(builder.Configuration, builder.Environment);
builder.Services.AddRbac(builder.Configuration);
builder.Services.AddAudit(builder.Configuration);
builder.Services.AddDocumentsModule(builder.Configuration);
builder.Services.AddLegalKnowledge(builder.Configuration);
builder.Services.AddEvidenceModule(builder.Configuration);
builder.Services.AddProcessingInventoryModule(builder.Configuration);
builder.Services.AddGapManagementModule(builder.Configuration);

// Outbox — escritura desde la API (GapManagement notificaciones)
var outboxConn = builder.Configuration.GetConnectionString("evidata-db")
    ?? throw new InvalidOperationException("Connection string 'evidata-db' not found.");
builder.Services.AddDbContext<OutboxDbContext>(options =>
    options.UseNpgsql(outboxConn));
builder.Services.AddScoped<OutboxRepository>();
builder.Services.AddScoped<IOutboxWriter>(sp => sp.GetRequiredService<OutboxRepository>());

builder.Services.AddWorkflowModule(builder.Configuration);
builder.Services.AddReportingModule(builder.Configuration);
builder.Services.AddSearchModule(builder.Configuration);
builder.Services.AddMcpModule(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapEvidataHealthEndpoints();
app.UseLocalDevGuard();
app.UseTenantIsolation();
app.MapControllers();

// ── /api/version — versión del binario en ejecución ──────────────────────────
app.MapGet("/api/version", () =>
{
    var asm = Assembly.GetEntryAssembly()!;
    var infoVersion = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                      ?? asm.GetName().Version?.ToString() ?? "unknown";
    var fileVersion = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version ?? "unknown";
    return Results.Ok(new
    {
        version       = infoVersion,
        fileVersion   = fileVersion,
        assemblyName  = asm.GetName().Name,
        buildTime     = new FileInfo(asm.Location).LastWriteTimeUtc.ToString("o"),
        environment   = app.Environment.EnvironmentName
    });
})
.WithName("GetVersion")
.ExcludeFromDescription(); // no aparece en OpenAPI público

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ── Log de versión al arranque ────────────────────────────────────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<EvidataApiStartup>>();
var startupAsm    = Assembly.GetEntryAssembly()!;
var startupVer    = startupAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? startupAsm.GetName().Version?.ToString() ?? "unknown";
startupLogger.LogInformation(
    "Evidata API iniciada · versión {Version} · entorno {Environment}",
    startupVer,
    app.Environment.EnvironmentName);

app.Run();

// Marcador para categoría de log de arranque — evita ambigüedad CS0436
// con Evidata.Worker.Outbox que también tiene clase Program implícita.
internal sealed class EvidataApiStartup { }
