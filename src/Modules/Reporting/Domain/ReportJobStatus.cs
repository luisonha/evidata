namespace Evidata.Modules.Reporting.Domain;

/// <summary>
/// Estados del ciclo de vida de un job de reporte.
/// Transiciones válidas:
///   Requested → Running (worker toma el job)
///   Running → Completed (artefacto generado)
///   Running → Failed (error irrecuperable)
///   Requested | Running → Expired (TTL superado sin completarse)
/// </summary>
public enum ReportJobStatus
{
    Requested,
    Running,
    Completed,
    Failed,
    Expired
}
