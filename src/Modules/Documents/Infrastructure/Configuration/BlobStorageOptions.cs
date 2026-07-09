namespace Evidata.Modules.Documents.Infrastructure.Configuration;

/// <summary>
/// Configuración para Azure Blob Storage.
/// Sección en appsettings: "BlobStorage"
/// </summary>
public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    /// <summary>
    /// Connection string a Azure Storage Account.
    /// En local, usar "UseDevelopmentStorage=true" (Azurite).
    /// </summary>
    public string ConnectionString { get; set; } = "UseDevelopmentStorage=true";

    /// <summary>
    /// Nombre del container donde se almacenan los documentos.
    /// Se crea automáticamente si no existe.
    /// </summary>
    public string ContainerName { get; init; } = "evidata-documents";

    /// <summary>
    /// Duración del SAS para upload (minutos). Default: 15.
    /// </summary>
    public int UploadSasExpirationMinutes { get; init; } = 15;

    /// <summary>
    /// Duración del SAS para download (minutos). Default: 60.
    /// </summary>
    public int DownloadSasExpirationMinutes { get; init; } = 60;
}
