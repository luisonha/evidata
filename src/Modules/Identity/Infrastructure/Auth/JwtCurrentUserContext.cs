using System.Security.Claims;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

public sealed class JwtCurrentUserContext : ICurrentUserContext
{
    private readonly Guid _userId;
    private readonly Guid _tenantId;
    private readonly string _email = string.Empty;
    private readonly bool _isAuthenticated;

    public JwtCurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        var principal = httpContextAccessor.HttpContext?.User;

        if (principal?.Identity?.IsAuthenticated != true)
            return;

        var userIdRaw = principal.FindFirstValue("userId");
        var tenantIdRaw = principal.FindFirstValue("tenantId");

        if (!Guid.TryParse(userIdRaw, out var userId) || !Guid.TryParse(tenantIdRaw, out var tenantId))
            return;

        _userId = userId;
        _tenantId = tenantId;
        _email = principal.FindFirstValue("email")
            ?? principal.FindFirstValue(ClaimTypes.Email)
            ?? string.Empty;
        _isAuthenticated = true;
    }

    public Guid UserId => _userId;
    public Guid TenantId => _tenantId;
    public string Email => _email;
    public bool IsAuthenticated => _isAuthenticated;
}
