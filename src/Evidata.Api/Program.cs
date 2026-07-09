using Evidata.Api.Infrastructure.HealthChecks;
using Evidata.Api.OpenApi;
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
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

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

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.DefaultAuthenticateScheme = LocalDevAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = LocalDevAuthenticationDefaults.AuthenticationScheme;
    }
    else
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }
});

authenticationBuilder.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    var issuer = builder.Configuration["Jwt:Issuer"]
        ?? throw new InvalidOperationException("JWT issuer not configured. Set Jwt:Issuer.");
    var audience = builder.Configuration["Jwt:Audience"]
        ?? throw new InvalidOperationException("JWT audience not configured. Set Jwt:Audience.");
    var signingKey = builder.Configuration["Jwt:SigningKey"]
        ?? throw new InvalidOperationException("JWT signing key not configured. Set Jwt:SigningKey.");

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };
});

if (builder.Environment.IsDevelopment())
{
    authenticationBuilder.AddScheme<AuthenticationSchemeOptions, LocalDevAuthenticationHandler>(
        LocalDevAuthenticationDefaults.AuthenticationScheme,
        _ => { });
}

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeDocumentTransformer>();
    options.AddDocumentTransformer<ApiErrorSchemaDocumentTransformer>();
    options.AddOperationTransformer<StableOperationIdTransformer>();
    options.AddOperationTransformer<BearerSecurityRequirementOperationTransformer>();
    options.AddOperationTransformer<ChangeStatusOperationTransformer>();
    options.AddOperationTransformer<ApiErrorResponsesOperationTransformer>();
});
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapEvidataHealthEndpoints();

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
app.UseLocalDevGuard();
app.UseAuthentication();
app.UseAuthorization();

// ── Log de versión al arranque ────────────────────────────────────────────────
var startupLogger = app.Services.GetRequiredService<ILogger<EvidataApiStartup>>();
var startupAsm    = Assembly.GetEntryAssembly()!;
var startupVer    = startupAsm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? startupAsm.GetName().Version?.ToString() ?? "unknown";
startupLogger.LogInformation(
    "Evidata API iniciada · versión {Version} · entorno {Environment}",
    startupVer,
    app.Environment.EnvironmentName);

app.UseTenantIsolation();
app.MapControllers();

app.Run();

// Marcador para categoría de log de arranque — evita ambigüedad CS0436
// con Evidata.Worker.Outbox que también tiene clase Program implícita.
internal sealed class EvidataApiStartup { }
