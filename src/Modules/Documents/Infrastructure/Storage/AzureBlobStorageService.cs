using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Evidata.Modules.Documents.Infrastructure.Storage;

/// <summary>
/// Implementación de IBlobStorageService usando Azure Blob Storage.
/// En desarrollo usa Azurite (UseDevelopmentStorage=true).
/// 
/// Path convention: {tenantId}/{yyyy/MM/dd}/{guid}_{sanitizedFileName}
/// Garantiza aislamiento multi-tenant por prefijo de path.
/// </summary>
public sealed class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _container;
    private readonly BlobStorageOptions _options;
    private readonly StorageSharedKeyCredential? _sharedKeyCredential;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(
        IOptions<BlobStorageOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;

        var serviceClient = new BlobServiceClient(_options.ConnectionString);
        _container = serviceClient.GetBlobContainerClient(_options.ContainerName);

        // Para SAS con shared key — solo disponible con connection string real (no Azurite managed identity)
        _sharedKeyCredential = TryExtractSharedKeyCredential(_options.ConnectionString);
    }

    public async Task<string> UploadAsync(
        string blobPath,
        byte[] content,
        string contentType,
        CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(ct);

        var blobClient = _container.GetBlobClient(blobPath);
        using var stream = new MemoryStream(content, writable: false);

        await blobClient.UploadAsync(stream, overwrite: true, cancellationToken: ct);
        await blobClient.SetHttpHeadersAsync(
            new Azure.Storage.Blobs.Models.BlobHttpHeaders { ContentType = contentType },
            cancellationToken: ct);

        _logger.LogInformation(
            "Blob subido: {BlobPath} ({Bytes} bytes, {ContentType})",
            blobPath, content.Length, contentType);

        return blobPath;
    }

    public async Task<SasUploadResult> GenerateUploadSasAsync(
        Guid tenantId,
        string fileName,
        string contentType,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(ct);

        var expiration = expiresIn ?? TimeSpan.FromMinutes(_options.UploadSasExpirationMinutes);
        var blobPath = BuildBlobPath(tenantId, fileName);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiration);

        var blobClient = _container.GetBlobClient(blobPath);
        var sasUri = GenerateSasUri(blobClient, BlobSasPermissions.Write | BlobSasPermissions.Create, expiresAt, contentType);

        _logger.LogDebug(
            "SAS upload generado para tenant {TenantId}, blob {BlobPath}, expira {ExpiresAt}",
            tenantId, blobPath, expiresAt);

        return new SasUploadResult(sasUri, blobPath, expiresAt);
    }

    public async Task<SasDownloadResult> GenerateDownloadSasAsync(
        string blobPath,
        TimeSpan? expiresIn = null,
        CancellationToken ct = default)
    {
        await EnsureContainerExistsAsync(ct);

        var expiration = expiresIn ?? TimeSpan.FromMinutes(_options.DownloadSasExpirationMinutes);
        var expiresAt = DateTimeOffset.UtcNow.Add(expiration);

        var blobClient = _container.GetBlobClient(blobPath);
        var sasUri = GenerateSasUri(blobClient, BlobSasPermissions.Read, expiresAt);

        _logger.LogDebug(
            "SAS download generado para blob {BlobPath}, expira {ExpiresAt}",
            blobPath, expiresAt);

        return new SasDownloadResult(sasUri, expiresAt);
    }

    public async Task DeleteAsync(string blobPath, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var deleted = await blobClient.DeleteIfExistsAsync(cancellationToken: ct);

        if (deleted)
            _logger.LogInformation("Blob eliminado: {BlobPath}", blobPath);
        else
            _logger.LogDebug("DeleteAsync: blob no encontrado (no-op): {BlobPath}", blobPath);
    }

    public async Task<bool> ExistsAsync(string blobPath, CancellationToken ct = default)
    {
        var blobClient = _container.GetBlobClient(blobPath);
        var response = await blobClient.ExistsAsync(ct);
        return response.Value;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static string BuildBlobPath(Guid tenantId, string fileName)
    {
        var sanitized = SanitizeFileName(fileName);
        var date = DateTimeOffset.UtcNow;
        return $"{tenantId}/{date:yyyy/MM/dd}/{Guid.NewGuid()}_{sanitized}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        // Eliminar caracteres no seguros para blob path
        var safe = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
        return string.IsNullOrEmpty(safe) ? $"file{ext}" : $"{safe}{ext}";
    }

    private string GenerateSasUri(BlobClient blobClient, BlobSasPermissions permissions, DateTimeOffset expiresAt, string? contentType = null)
    {
        // Azurite soporta SAS vía shared key embebida en connection string
        if (_sharedKeyCredential is not null)
        {
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _options.ContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = expiresAt
            };
            if (contentType is not null)
                sasBuilder.ContentType = contentType;
            sasBuilder.SetPermissions(permissions);

            var sasToken = sasBuilder.ToSasQueryParameters(_sharedKeyCredential).ToString();
            return $"{blobClient.Uri}?{sasToken}";
        }

        // Managed Identity / user delegation SAS (producción sin shared key)
        // Fallback: retornar URL del blob sin SAS (requiere acceso público o token AAD externo)
        _logger.LogWarning(
            "No se pudo generar SAS con shared key para {BlobPath}. " +
            "En producción configure AccountName/AccountKey o use Managed Identity con User Delegation SAS.",
            blobClient.Name);

        return blobClient.Uri.ToString();
    }

    private bool _containerEnsured;

    private async Task EnsureContainerExistsAsync(CancellationToken ct)
    {
        if (_containerEnsured) return;
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);
        _containerEnsured = true;
    }

    private static StorageSharedKeyCredential? TryExtractSharedKeyCredential(string connectionString)
    {
        // Connection string format: "AccountName=xxx;AccountKey=yyy;..."
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .ToDictionary(p => p[0].Trim(), p => p[1].Trim(), StringComparer.OrdinalIgnoreCase);

        if (parts.TryGetValue("AccountName", out var name) && parts.TryGetValue("AccountKey", out var key))
            return new StorageSharedKeyCredential(name, key);

        // Azurite development connection string
        if (connectionString.Contains("UseDevelopmentStorage=true", StringComparison.OrdinalIgnoreCase))
            return new StorageSharedKeyCredential("devstoreaccount1",
                "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");

        return null;
    }
}
