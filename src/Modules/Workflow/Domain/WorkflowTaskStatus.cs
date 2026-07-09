namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Estados del ciclo de vida de una tarea de cumplimiento.
/// Transiciones válidas:
///   Open → InProgress (al iniciar trabajo)
///   InProgress → Completed (al completar)
///   Open | InProgress → Cancelled
///   Open | InProgress → Overdue (vence sin completarse)
/// </summary>
public enum WorkflowTaskStatus
{
    Open,
    InProgress,
    Completed,
    Cancelled,
    Overdue
}
