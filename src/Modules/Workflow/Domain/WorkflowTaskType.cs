namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Tipo de tarea de cumplimiento.
/// </summary>
public enum WorkflowTaskType
{
    Review,
    Approval,
    Remediation,
    HumanReview,
    Other
}
