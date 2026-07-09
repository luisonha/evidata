namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Estados del ciclo de vida de una revisión humana.
/// Transiciones válidas:
///   Open → InProgress (al asignar revisor)
///   InProgress → Approved (revisor aprueba)
///   InProgress → ChangesRequested (revisor solicita cambios)
///   Open | InProgress → Cancelled
/// </summary>
public enum ReviewStatus
{
    Open,
    InProgress,
    Approved,
    ChangesRequested,
    Cancelled
}
