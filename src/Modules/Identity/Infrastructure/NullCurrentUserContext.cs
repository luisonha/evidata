using Evidata.Modules.Identity.Application.Abstractions;

namespace Evidata.Modules.Identity.Infrastructure;

/// <summary>
/// Implementación vacía de ICurrentUserContext usada como default en DI
/// hasta que el módulo LocalAuth (f1-local-auth) provea la implementación real.
/// </summary>
public class NullCurrentUserContext : ICurrentUserContext
{
    public Guid UserId => Guid.Empty;
    public Guid TenantId => Guid.Empty;
    public string Email => string.Empty;
    public bool IsAuthenticated => false;
}
