namespace Evidata.Modules.Reporting.Domain;

/// <summary>
/// Job de generación de reporte.
/// Representa la solicitud asíncrona y su ciclo de vida completo.
/// El artefacto final se referencia por documento id una vez completado.
/// </summary>
public sealed class ReportJob
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    public ReportType ReportType { get; private set; }

    /// <summary>Parámetros serializados en JSON (filtros, rango de fechas, etc.).</summary>
    public string Parameters { get; private set; } = default!;

    public ReportJobStatus Status { get; private set; }

    public Guid RequestedBy { get; private set; }

    /// <summary>Id del documento generado en el módulo Documents (null hasta completarse).</summary>
    public Guid? ArtifactDocumentId { get; private set; }

    /// <summary>Mensaje de error funcional (null si no hubo fallo).</summary>
    public string? ErrorMessage { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private ReportJob() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static ReportJob Create(
        Guid tenantId,
        ReportType reportType,
        string parameters,
        Guid requestedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters);

        return new ReportJob
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ReportType = reportType,
            Parameters = parameters.Trim(),
            Status = ReportJobStatus.Requested,
            RequestedBy = requestedBy,
            RequestedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Transiciones FSM ──────────────────────────────────────────────────────

    /// <summary>El worker toma el job. Requested → Running.</summary>
    public void Start()
    {
        if (Status != ReportJobStatus.Requested)
            throw new InvalidOperationException(
                $"Solo se puede iniciar un job en estado Requested. Estado actual: {Status}.");

        Status = ReportJobStatus.Running;
        StartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El worker completa el job con el artefacto generado. Running → Completed.</summary>
    public void Complete(Guid artifactDocumentId)
    {
        if (Status != ReportJobStatus.Running)
            throw new InvalidOperationException(
                $"Solo se puede completar un job en estado Running. Estado actual: {Status}.");

        ArtifactDocumentId = artifactDocumentId;
        Status = ReportJobStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>El worker reporta fallo. Running → Failed.</summary>
    public void Fail(string errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage);

        if (Status != ReportJobStatus.Running)
            throw new InvalidOperationException(
                $"Solo se puede fallar un job en estado Running. Estado actual: {Status}.");

        ErrorMessage = errorMessage.Trim();
        Status = ReportJobStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Job expiró sin procesarse. Requested | Running → Expired.</summary>
    public void Expire()
    {
        if (Status is ReportJobStatus.Completed or ReportJobStatus.Failed or ReportJobStatus.Expired)
            throw new InvalidOperationException(
                $"No se puede expirar un job en estado {Status}.");

        Status = ReportJobStatus.Expired;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public bool IsTerminal =>
        Status is ReportJobStatus.Completed or ReportJobStatus.Failed or ReportJobStatus.Expired;
}
