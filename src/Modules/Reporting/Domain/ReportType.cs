namespace Evidata.Modules.Reporting.Domain;

/// <summary>
/// Tipos de reporte disponibles.
/// </summary>
public enum ReportType
{
    /// <summary>Registro de actividades de tratamiento.</summary>
    RAT,

    /// <summary>Brechas de cumplimiento detectadas.</summary>
    Gaps,

    /// <summary>Paquete de evidencias para auditoría.</summary>
    EvidencePack,

    /// <summary>Resumen ejecutivo de cumplimiento.</summary>
    ExecutiveSummary
}
