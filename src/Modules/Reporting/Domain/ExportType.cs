namespace Evidata.Modules.Reporting.Domain;

/// <summary>
/// Tipos de exportación públicos (API contract - P1-008).
/// 
/// Mapeo con tipos internos (ReportType):
/// - ProcessingActivityPdfSummary ← ReportType.RAT + ReportType.ExecutiveSummary (PDF output)
/// - GlobalRatExcel ← ReportType.RAT (Excel output)
/// - ApprovalHistory ← ReportType.RAT (incluye auditoría)
/// - InternalJson ← ReportType.EvidencePack, ReportType.Gaps (formato técnico)
/// 
/// Decisión de diseño: Se mantiene enum público separado (ExportType) porque
/// la semántica de "tipo de reporte interno" (RAT, Gaps, EvidencePack) es ortogonal
/// al "formato/visibilidad de exportación" (PDF, Excel, JSON, con auditoría).
/// El mapeo es N:1 (múltiples ReportTypes → un ExportType según formato solicitado).
/// </summary>
public enum ExportType
{
    /// <summary>Resumen PDF de actividad de tratamiento. UserVisible. Puede generarse con warnings.</summary>
    ProcessingActivityPdfSummary = 1,

    /// <summary>RAT en formato Excel con datos completos. UserVisible. Requiere permisos GenerateOfficialExport.</summary>
    GlobalRatExcel = 2,

    /// <summary>Historial de aprobaciones + auditoría relevante. UserVisible. Requiere permisos.</summary>
    ApprovalHistory = 3,

    /// <summary>Paquete técnico JSON (evidencias, brechas). TechnicalOnly. No visible en UI MVP.</summary>
    InternalJson = 4
}
