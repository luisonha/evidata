namespace Evidata.Modules.Identity.Application.Abstractions;

/// <summary>
/// Abstracción central del sistema: proporciona la identidad del usuario autenticado en el contexto actual.
/// Todos los módulos dependen de esta interfaz — nunca leen claims directamente.
/// </summary>
public interface ICurrentUserContext
{
    Guid UserId { get; }
    Guid TenantId { get; }
    string Email { get; }
    bool IsAuthenticated { get; }
}
