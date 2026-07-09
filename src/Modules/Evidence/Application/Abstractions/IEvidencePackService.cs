using Evidata.Modules.Evidence.Domain;

namespace Evidata.Modules.Evidence.Application.Abstractions;

/// <summary>Mensaje encolado para procesar un EvidencePackJob.</summary>
/// <param name="TenantId">Tenant del job</param>
/// <param name="JobId">ID del EvidencePackJob</param>
public sealed record EvidencePackMessage(Guid TenantId, Guid JobId);

/// <summary>Resultado de crear un pack job.</summary>
/// <param name="JobId">ID del job creado</param>
public sealed record CreatePackResult(Guid JobId);

/// <summary>Estado actual de un pack job.</summary>
public sealed record PackStatusResult(
    Guid JobId,
    EvidencePackStatus Status,
    string? DownloadUrl,
    DateTimeOffset? ExpiresAt,
    string? ErrorMessage);

/// <summary>
/// Servicio para crear y consultar trabajos de empaquetado de evidencias.
/// La ejecución real del pack ocorre en la Azure Function (EvidencePackFunction).
/// </summary>
public interface IEvidencePackService
{
    /// <summary>
    /// Crea un EvidencePackJob y encola el mensaje para procesamiento asíncrono.
    /// Valida que las evidencias existan, sean Active y pertenezcan al tenant.
    /// </summary>
    Task<CreatePackResult> RequestPackAsync(
        Guid tenantId,
        Guid requestedBy,
        IEnumerable<Guid> evidenceIds,
        CancellationToken ct = default);

    /// <summary>
    /// Retorna el estado actual de un job. Si está Completed, incluye URL SAS de descarga.
    /// </summary>
    Task<PackStatusResult> GetStatusAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken ct = default);
}
