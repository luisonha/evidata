using Evidata.Api.Infrastructure.HealthChecks;
using Evidata.Api.OpenApi;
using Evidata.Api.Queries;
using Evidata.Modules.Audit;
using Evidata.Modules.Documents;
using Evidata.Modules.Evidence;
using Evidata.Modules.GapManagement;
using Evidata.Modules.Identity;
using Evidata.Modules.Identity.Application.Abstractions;
using Evidata.Modules.Identity.Infrastructure.Auth;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Evidata.Modules.Identity.Infrastructure.Services;
using Evidata.Modules.LegalKnowledge;
using Evidata.Modules.ProcessingInventory;
using Evidata.Modules.ProcessingInventory.Application.Abstractions;
using Evidata.Modules.Security;
using Evidata.Modules.Security.Infrastructure.Authorization;
using Evidata.Modules.Security.Infrastructure.Persistence;
using Evidata.Modules.TenantManagement;
using Evidata.Modules.Mcp;
using Evidata.Modules.Reporting;
using Evidata.Modules.Search;
using Evidata.Modules.Workflow;
using Evidata.Worker.Outbox.Persistence;
using Evidata.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Text;

namespace Evidata.Api;

/// <summary>
/// Factory for building the WebApplication host with complete DI configuration.
/// Extracted from Program.cs to enable testable DI validation without running the host.
/// </summary>
public static class HostBuilderFactory
{
    /// <summary>
    /// Builds and returns a WebApplication with full DI configuration.
    /// Does NOT call app.Run() — that remains in Program.cs entrypoint.
    /// </summary>
    public static WebApplication Build(string[] args) => BuildForValidation(args).App;

    /// <summary>
    /// Builds the app AND exposes the raw IServiceCollection used to build it,
    /// so tests can resolve every registered service descriptor and force full
    /// DI graph validation.
    /// </summary>
    public static (WebApplication App, IServiceCollection Services) BuildForValidation(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddMemoryCache();  // Add IMemoryCache for RoleNameResolver
        builder.Services.AddEvidataHealthChecks(builder.Configuration);
        builder.Services.AddTenantManagement(builder.Configuration);
        builder.Services.AddIdentityBridge(builder.Configuration, builder.Environment);
        builder.Services.AddRbac(builder.Configuration);
        
        // Register RoleNameResolver for admin user management (after both Identity and Security modules are configured)
        builder.Services.AddScoped<IRoleNameResolver>(sp =>
        {
            var securityDbContext = sp.GetRequiredService<SecurityDbContext>();
            var cache = sp.GetRequiredService<IMemoryCache>();
            return new RoleNameResolver(securityDbContext, cache);
        });
        
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

        // P1-FULL-COMPOSITION: Register composition query handler for /control endpoint
        // This replaces the stub implementation with full cross-module composition.
        // The composition handler is registered to the IProcessingActivityControlQueryService interface,
        // which is injected into the ProcessingActivitiesController in the module.
        builder.Services.AddScoped<ProcessingActivityControlCompositionQueryHandler>();
        builder.Services.AddScoped<IProcessingActivityControlQueryService>(sp =>
            sp.GetRequiredService<ProcessingActivityControlCompositionQueryHandler>());

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

        builder.Services.AddAuthorization(options =>
        {
            // FallbackPolicy: por defecto, todo endpoint requiere autenticación.
            // Los endpoints públicos legítimos (health, swagger, version) deben marcar explícitamente [AllowAnonymous].
            // Esto evita que nuevos controllers queden desprotegidos por omisión.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();

            // Fine-grained policy for role management operations (assign/remove roles)
            // Requires user to have TenantOwner or ComplianceAdmin role in the current tenant
            options.AddPolicy("TenantOwnerOrComplianceAdmin", policy =>
                policy.AddRequirements(new TenantOwnerOrComplianceAdminRequirement()));
        });

        // Register the custom IAuthorizationPolicyProvider for dynamic permission-based policies
        // This enables policies like [Authorize(Policy = "HasPermission:Admin.ReadUsers")]
        // to be dynamically constructed from permission codes stored in the database
        builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

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
        
        // Configure Antiforgery (used for CSRF protection on form submissions)
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = "__Host-evidata.csrf";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
        });

        var services = builder.Services;
        var app = builder.Build();

        app.MapDefaultEndpoints();
        app.MapEvidataHealthEndpoints();

        // ── /api/version — versión del binario en ejecución ──────────────────────────
        app.MapGet("/api/version", () =>
        {
            var versionInfo = VersionHelper.GetVersionInfo();
            return Results.Ok(versionInfo.ToResponse(app.Environment.EnvironmentName));
        })
        .WithName("GetVersion")
        .AllowAnonymous()
        .ExcludeFromDescription(); // no aparece en OpenAPI público

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // AllowAnonymous es necesario: el FallbackPolicy global exige
            // autenticación para todo endpoint que no la excluya explícitamente.
            app.MapOpenApi().AllowAnonymous();
        }

        app.UseHttpsRedirection();
        app.UseLocalDevGuard();
        app.UseCorrelationId();
        
        // Session resolution middleware must come before authentication/authorization
        app.UseMiddleware<SessionResolutionMiddleware>();
        
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

        return (app, services);
    }
}

// Marcador para categoría de log de arranque — evita ambigüedad CS0436
// con Evidata.Worker.Outbox que también tiene clase Program implícita.
internal sealed class EvidataApiStartup { }
