using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Evidata.Modules.Identity.Infrastructure.Auth;

public static class LocalDevAuthenticationDefaults
{
    public const string AuthenticationScheme = "LocalDev";
}

public sealed class LocalDevAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public LocalDevAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userIdRaw = Request.Headers[LocalDevCurrentUserContext.UserIdHeader].FirstOrDefault();
        var tenantIdRaw = Request.Headers[LocalDevCurrentUserContext.TenantIdHeader].FirstOrDefault();

        if (userIdRaw is null && tenantIdRaw is null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!Guid.TryParse(userIdRaw, out var userId) || !Guid.TryParse(tenantIdRaw, out var tenantId))
            return Task.FromResult(AuthenticateResult.Fail("LocalDev headers are invalid."));

        var email = Request.Headers[LocalDevCurrentUserContext.EmailHeader].FirstOrDefault();
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new("tenantId", tenantId.ToString())
        };

        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim("email", email));
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
