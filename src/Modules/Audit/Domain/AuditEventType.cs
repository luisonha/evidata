namespace Evidata.Modules.Audit.Domain;

/// <summary>
/// Catálogo cerrado de tipos de eventos auditables.
/// Alineado con tabla de "Acciones críticas auditables" en el contrato.
/// </summary>
public enum AuditEventType
{
    /// <summary>Crear ProcessingActivity (AUD-PA-001)</summary>
    CreateProcessingActivity = 1,

    /// <summary>Actualizar nodo (AUD-NODE-001)</summary>
    UpdateNode = 2,

    /// <summary>Enviar a revisión (AUD-REV-001)</summary>
    SubmitForReview = 3,

    /// <summary>Aprobar (AUD-APP-001)</summary>
    Approve = 4,

    /// <summary>Activar (AUD-ACT-001)</summary>
    Activate = 5,

    /// <summary>Archivar (AUD-ARC-001)</summary>
    Archive = 6,

    /// <summary>Validar evidencia (AUD-EV-001)</summary>
    ValidateEvidence = 7,

    /// <summary>Rechazar evidencia (AUD-EV-002)</summary>
    RejectEvidence = 8,

    /// <summary>Aceptar brecha con riesgo (AUD-GAP-001)</summary>
    AcceptGapWithRisk = 9,

    /// <summary>Generar export oficial (AUD-EXP-001)</summary>
    GenerateOfficialExport = 10
}
