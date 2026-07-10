namespace Evidata.Modules.Reporting.Domain;

/// <summary>
/// Estado del ciclo de vida de una exportación.
/// Transiciones válidas:
///   Requested → Generating (worker toma la tarea)
///   Generating → Completed (artefacto generado)
///   Generating → Failed (error irrecuperable)
/// </summary>
public enum ExportStatus
{
    Requested,
    Generating,
    Completed,
    Failed
}

/// <summary>
/// Agregado Export — mapeo formal de reportes a exportaciones con contrato P1-008.
/// 
/// Ciclo de vida:
///   Requested → Generating → Completed | Failed
///
/// Campos contractuales (sección 5 "Exportaciones"):
///   - id: UUID único del export
///   - processingActivityId: referencia a actividad de tratamiento
///   - exportType: ProcessingActivityPdfSummary, GlobalRatExcel, ApprovalHistory, InternalJson
///   - status: Requested, Generating, Completed, Failed
///   - version: incremental por (processingActivityId, exportType)
///   - warnings: array opcional de warnings funcionales no bloqueantes
///   - contentType: ej. application/pdf, application/vnd.ms-excel
///   - artifactDocumentId: referencia al documento generado
///   - requestedByUserId: usuario que solicitó
///   - generatedAt: timestamp UTC cuando se completó (nullable hasta Completed)
///   - correlationId: auditable, para trazabilidad de solicitud/generación
/// 
/// SEC-EXP-001: GenerateOfficialExport solo para roles autorizados + actividad aprobada/activa.
/// </summary>
public sealed class Export
{
    private readonly List<string> _warnings = [];

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Referencia a la actividad de tratamiento propietaria.</summary>
    public Guid ProcessingActivityId { get; private set; }

    public ExportType ExportType { get; private set; }
    public ExportStatus Status { get; private set; }

    /// <summary>Versión incremental por (processingActivityId, exportType).</summary>
    public int Version { get; private set; }

    /// <summary>Warnings funcionales no bloqueantes (ej. "Datos incompletos en sección X").</summary>
    public IReadOnlyList<string> Warnings => _warnings.AsReadOnly();

    /// <summary>Content-Type del artefacto (ej. application/pdf, application/json).</summary>
    public string ContentType { get; private set; } = default!;

    /// <summary>ID del documento generado en Documents module (null hasta Completed).</summary>
    public Guid? ArtifactDocumentId { get; private set; }

    public Guid RequestedByUserId { get; private set; }

    /// <summary>Timestamp UTC de solicitud.</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Timestamp UTC cuando completó (Completed | Failed). Null mientras esté en Generating.</summary>
    public DateTimeOffset? GeneratedAt { get; private set; }

    /// <summary>Correlation ID para trazabilidad auditable (propagado a IAuditService).</summary>
    public string CorrelationId { get; private set; } = default!;

    /// <summary>Mensaje de error si falló (null si no hubo fallo).</summary>
    public string? ErrorMessage { get; private set; }

    private Export() { }

    // ── Factory ──────────────────────────────────────────────────────────────

    public static Export Create(
        Guid tenantId,
        Guid processingActivityId,
        ExportType exportType,
        string contentType,
        int version,
        Guid requestedByUserId,
        string correlationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);

        return new Export
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProcessingActivityId = processingActivityId,
            ExportType = exportType,
            ContentType = contentType.Trim(),
            Version = version,
            Status = ExportStatus.Requested,
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTimeOffset.UtcNow,
            CorrelationId = correlationId.Trim()
        };
    }

    // ── Transiciones FSM ─────────────────────────────────────────────────────

    /// <summary>Worker toma la tarea. Requested → Generating.</summary>
    public void Start()
    {
        if (Status != ExportStatus.Requested)
            throw new InvalidOperationException(
                $"Solo se puede iniciar un export en estado Requested. Estado actual: {Status}.");

        Status = ExportStatus.Generating;
    }

    /// <summary>Worker completa el export con el artefacto generado. Generating → Completed.</summary>
    public void Complete(Guid artifactDocumentId)
    {
        if (Status != ExportStatus.Generating)
            throw new InvalidOperationException(
                $"Solo se puede completar un export en estado Generating. Estado actual: {Status}.");

        ArtifactDocumentId = artifactDocumentId;
        Status = ExportStatus.Completed;
        GeneratedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Worker reporta fallo. Generating → Failed.</summary>
    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (Status != ExportStatus.Generating)
            throw new InvalidOperationException(
                $"Solo se puede fallar un export en estado Generating. Estado actual: {Status}.");

        ErrorMessage = errorMessage.Trim();
        Status = ExportStatus.Failed;
        GeneratedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Agregar un warning funcional (no bloqueante).</summary>
    public void AddWarning(string warning)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(warning);
        if (!_warnings.Contains(warning.Trim()))
            _warnings.Add(warning.Trim());
    }

    // ── Queries ──────────────────────────────────────────────────────────────

    public bool IsTerminal => Status is ExportStatus.Completed or ExportStatus.Failed;
}
