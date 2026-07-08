using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

/// <summary>
/// Implementación de ICurrentUserContext para ambiente de desarrollo local.
/// Lee los headers X-Evidata-Dev-User y X-Evidata-Dev-Tenant inyectados por el cliente.
/// NUNCA debe activarse en Staging o Producción — el guard LocalDevEnvironmentGuard lo bloquea.
/// </summary>
public class LocalDevCurrentUserContext : ICurrentUserContext
{
    public const string UserIdHeader = "X-Evidata-Dev-User";
    public const string TenantIdHeader = "X-Evidata-Dev-Tenant";
    public const string EmailHeader = "X-Evidata-Dev-Email";

    private readonly Guid _userId;
    private readonly Guid _tenantId;
    private readonly string _email = string.Empty;
    private readonly bool _isAuthenticated;

    public LocalDevCurrentUserContext(IHttpContextAccessor httpContextAccessor, ILogger<LocalDevCurrentUserContext> logger)
    {
        var context = httpContextAccessor.HttpContext;

        if (context is null)
        {
            _isAuthenticated = false;
            return;
        }

        var userIdRaw = context.Request.Headers[UserIdHeader].FirstOrDefault();
        var tenantIdRaw = context.Request.Headers[TenantIdHeader].FirstOrDefault();
        _email = context.Request.Headers[EmailHeader].FirstOrDefault() ?? string.Empty;

        if (Guid.TryParse(userIdRaw, out var userId) && Guid.TryParse(tenantIdRaw, out var tenantId))
        {
            _userId = userId;
            _tenantId = tenantId;
            _isAuthenticated = true;
        }
        else
        {
            if (userIdRaw is not null || tenantIdRaw is not null)
                logger.LogWarning(
                    "⚠️ LocalDev headers presentes pero inválidos. UserId={UserId} TenantId={TenantId}",
                    userIdRaw, tenantIdRaw);

            _isAuthenticated = false;
        }
    }

    public Guid UserId => _userId;
    public Guid TenantId => _tenantId;
    public string Email => _email;
    public bool IsAuthenticated => _isAuthenticated;
}
