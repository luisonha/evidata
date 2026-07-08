namespace Evidata.Modules.Documents.Application.Abstractions;

/// <summary>
/// Resultado de una operación de SAS URL para upload directo desde cliente.
/// </summary>
/// <param name="UploadUrl">URL SAS con permiso de escritura (expira en <see cref="ExpiresAt"/>)</param>
/// <param name="BlobPath">Ruta del blob dentro del container (sin base URL)</param>
/// <param name="ExpiresAt">Momento de expiración del SAS token</param>
public sealed record SasUploadResult(
    string UploadUrl,
    string BlobPath,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Resultado de una URL SAS para descarga.
/// </summary>
/// <param name="DownloadUrl">URL SAS con permiso de lectura</param>
/// <param name="ExpiresAt">Momento de expiración del SAS token</param>
public sealed record SasDownloadResult(
    string DownloadUrl,
    DateTimeOffset ExpiresAt);

/// <summary>
/// Abstracción de almacenamiento de blobs para el módulo de documentos.
/// Permite upload directo desde cliente vía SAS y descarga controlada.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Genera una URL SAS con permiso de escritura para que el cliente
    /// suba un archivo directamente a Azure Blob Storage sin pasar por la API.
    /// </summary>
    /// <param name="tenantId">ID del tenant — usado como prefijo de path para aislamiento</param>
    /// <param name="fileName">Nombre original del archivo</param>
    /// <param name="contentType">MIME type del archivo</param>
    /// <param name="expiresIn">Ventana de validez del SAS (default: 15 minutos)</param>
    Task<SasUploadResult> GenerateUploadSasAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default);

    /// <summary>
    /// Genera una URL SAS con permiso de lectura para descargar un blob.
    /// </summary>
    /// <param name="blobPath">Ruta del blob dentro del container</param>
    /// <param name="expiresIn">Ventana de validez del SAS (default: 1 hora)</param>
    Task<SasDownloadResult> GenerateDownloadSasAsync(
        string blobPath,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default);

    /// <summary>
    /// Elimina un blob del storage. Operación no falla si el blob no existe.
    /// </summary>
    Task DeleteAsync(string blobPath, CancellationToken ct = default);

    /// <summary>
    /// Verifica si un blob existe. Usado para validar uploads completados.
    /// </summary>
    Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default);
}
