namespace Evidata.Modules.Workflow.Application.Commands;

/// <summary>
/// Comando para crear o actualizar un requerimiento de revisión para un tenant.
/// </summary>
public sealed record SetReviewRequirementCommand(
    Guid TenantId,
    int ReviewType, // 0 = Legal, 1 = Security
    string EntityType,
    bool IsRequired);
