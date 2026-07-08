using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Middleware;

/// <summary>
/// Middleware que valida aislamiento cross-tenant.
/// Rechaza requests donde el TenantId del recurso no coincide con el TenantId del usuario autenticado.
/// Solo activo cuando el usuario está autenticado (IsAuthenticated=true).
/// </summary>
public class TenantIsolationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TenantIsolationMiddleware> _logger;

    public TenantIsolationMiddleware(RequestDelegate next, ILogger<TenantIsolationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUserContext currentUser)
    {
        // Si no autenticado (LocalDev sin headers), pasar sin validar
        if (!currentUser.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        // Verificar header X-Tenant-Id si viene explícito en el request
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var headerTenantId))
        {
            if (Guid.TryParse(headerTenantId, out var requestedTenant) && requestedTenant != currentUser.TenantId)
            {
                _logger.LogWarning(
                    "Cross-tenant access attempt: user {UserId} (tenant {UserTenant}) requested tenant {RequestedTenant}",
                    currentUser.UserId, currentUser.TenantId, requestedTenant);

                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = "Cross-tenant access denied." });
                return;
            }
        }

        await _next(context);
    }
}

public static class TenantIsolationMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantIsolation(this IApplicationBuilder app)
        => app.UseMiddleware<TenantIsolationMiddleware>();
}
