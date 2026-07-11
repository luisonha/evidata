namespace Evidata.Modules.Workflow.Domain;

/// <summary>
/// Tipos de revisión que pueden ser configurables por tenant y entidad.
/// Mapeado desde ProcessingInventory.ReviewDomain (Legal, Security) para uso genérico
/// en la política de requerimientos de revisión.
/// </summary>
public enum ReviewType
{
    Legal = 0,
    Security = 1
}
