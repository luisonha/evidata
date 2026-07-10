namespace Evidata.Modules.Audit.Domain;

/// <summary>
/// Resultado de un evento auditable: solo Success, Failure, o Blocked.
/// </summary>
public enum AuditEventResult
{
    /// <summary>Acción completada exitosamente.</summary>
    Success = 1,

    /// <summary>Acción falló por error técnico o regla de negocio.</summary>
    Failure = 2,

    /// <summary>Acción fue bloqueada por autorización o restricción de seguridad.</summary>
    Blocked = 3
}
