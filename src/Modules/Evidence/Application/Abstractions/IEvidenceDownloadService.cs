namespace Evidata.Modules.Evidence.Application.Abstractions;

/// <summary>
/// Resultado de una solicitud de descarga de evidencia.
/// </summary>
/// <param name="DownloadUrl">URL SAS con permiso de lectura (expira en <see cref="ExpiresAt"/>)</param>
/// <param name="FileName">Nombre sugerido para el archivo descargado</param>
/// <param name="ContentType">MIME type del archivo</param>
/// <param name="ExpiresAt">Momento de expiración del SAS token</param>
/// <param name="AccessLogId">ID del registro de auditoría generado</param>
public sealed record EvidenceDownloadResult(
    string DownloadUrl,
    string FileName,
    string ContentType,
    DateTimeOffset ExpiresAt,
    Guid AccessLogId);

/// <summary>
/// Abstracción del servicio de descarga de evidencia.
/// Genera una URL SAS de lectura y registra la auditoría de acceso.
///
/// Reglas:
/// - Evidencia Sensitive requiere reason no vacío.
/// - Evidencia sin BlobPath lanza excepción (no hay archivo adjunto).
/// - Siempre registra EvidenceAccessLog antes de devolver la URL.
/// </summary>
public interface IEvidenceDownloadService
{
    /// <summary>
    /// Genera URL SAS de descarga y registra auditoría.
    /// </summary>
    /// <param name="tenantId">Tenant del solicitante (validación cross-tenant)</param>
    /// <param name="evidenceId">ID de la evidencia a descargar</param>
    /// <param name="requestedBy">ID del usuario solicitante</param>
    /// <param name="reason">Motivo de descarga (obligatorio para Sensitive)</param>
    /// <param name="clientIp">IP del cliente (para auditoría)</param>
    /// <param name="userAgent">User-Agent del cliente (para auditoría)</param>
    /// <param name="correlationId">ID de correlación de la request HTTP</param>
    Task<EvidenceDownloadResult> RequestDownloadAsync(
        Guid tenantId,
        Guid evidenceId,
        Guid requestedBy,
        string? reason = null,
        string? clientIp = null,
        string? userAgent = null,
        string? correlationId = null,
        CancellationToken ct = default);
}
