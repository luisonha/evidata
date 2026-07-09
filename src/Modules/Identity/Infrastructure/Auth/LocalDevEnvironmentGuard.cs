using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Middleware que bloquea el modo LocalDev en entornos no-Development.
/// Si los headers X-Evidata-Dev-* aparecen fuera de Development → 403.
/// </summary>
public class LocalDevEnvironmentGuard
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _env;
    private readonly ILogger<LocalDevEnvironmentGuard> _logger;

    public LocalDevEnvironmentGuard(RequestDelegate next, IHostEnvironment env, ILogger<LocalDevEnvironmentGuard> logger)
    {
        _next = next;
        _env = env;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var hasDevHeaders =
            context.Request.Headers.ContainsKey(LocalDevCurrentUserContext.UserIdHeader) ||
            context.Request.Headers.ContainsKey(LocalDevCurrentUserContext.TenantIdHeader);

        if (hasDevHeaders && !_env.IsDevelopment())
        {
            _logger.LogCritical(
                "🚨 Intento de uso de headers LocalDev en ambiente {Environment}. IP: {IP}",
                _env.EnvironmentName,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "LocalDev mode not allowed in this environment." });
            return;
        }

        await _next(context);
    }
}

public static class LocalDevEnvironmentGuardExtensions
{
    public static IApplicationBuilder UseLocalDevGuard(this IApplicationBuilder app)
        => app.UseMiddleware<LocalDevEnvironmentGuard>();
}
