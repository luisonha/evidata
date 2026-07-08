using Evidata.Modules.Reporting.Domain;

namespace Evidata.Modules.Reporting.Application.Abstractions;

/// <summary>
/// Servicio de orquestación de jobs de reporte.
/// </summary>
public interface IReportJobService
{
    /// <summary>Solicita la generación de un reporte. Retorna el job creado.</summary>
    Task<ReportJob> RequestAsync(
        Guid tenantId,
        ReportType reportType,
        string parameters,
        Guid requestedBy,
        CancellationToken ct = default);

    /// <summary>El worker marca el job como iniciado.</summary>
    Task StartAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>El worker completa el job con el documento generado.</summary>
    Task CompleteAsync(Guid jobId, Guid artifactDocumentId, CancellationToken ct = default);

    /// <summary>El worker reporta un fallo.</summary>
    Task FailAsync(Guid jobId, string errorMessage, CancellationToken ct = default);

    /// <summary>Expira jobs que superaron el TTL sin completarse.</summary>
    Task ExpireStaleJobsAsync(DateTimeOffset cutoff, CancellationToken ct = default);

    /// <summary>Obtiene un job por Id.</summary>
    Task<ReportJob?> GetByIdAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Lista los jobs recientes de un tenant (más recientes primero).</summary>
    Task<IReadOnlyList<ReportJob>> GetByTenantAsync(
        Guid tenantId, int limit = 20, CancellationToken ct = default);
}
