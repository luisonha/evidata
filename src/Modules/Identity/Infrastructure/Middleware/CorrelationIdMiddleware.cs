using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Middleware;

/// <summary>
/// Middleware que propaga correlationId a través de la request.
/// - Acepta X-Correlation-Id header si viene del cliente.
/// - Si no viene, genera uno automáticamente.
/// - Lo almacena en HttpContext.Items["CorrelationId"] para acceso desde handlers/services.
/// - Lo agrega al HttpContext.TraceIdentifier para facilitar logging distribuido.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeaderName = "X-Correlation-Id";
    private const string CorrelationIdItemKey = "CorrelationId";
    
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Leer correlationId del header si viene; si no, generar uno
        string correlationId;
        
        if (context.Request.Headers.TryGetValue(CorrelationIdHeaderName, out var headerValue))
        {
            correlationId = headerValue.ToString();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = Guid.NewGuid().ToString("N");
            }
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        // Almacenar en HttpContext para acceso durante la request
        context.Items[CorrelationIdItemKey] = correlationId;
        
        // Usar como TraceIdentifier para logging distribuido
        context.TraceIdentifier = correlationId;

        _logger.LogInformation(
            "Request iniciada con CorrelationId: {CorrelationId} | Método: {Method} | Ruta: {Path}",
            correlationId, context.Request.Method, context.Request.Path);

        // Agregar header a la respuesta para que el cliente pueda rastrear
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        await _next(context);
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>
    /// Extrae el correlationId del contexto HTTP actual.
    /// Retorna el correlationId generado por CorrelationIdMiddleware o null si no se encuentra.
    /// </summary>
    public static string? GetCorrelationId(this HttpContext context)
    {
        if (context?.Items.TryGetValue("CorrelationId", out var value) == true)
        {
            return value as string;
        }
        return null;
    }
}
