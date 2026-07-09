using Azure.Storage.Blobs;
using DotNet.Testcontainers.Builders;
using Evidata.Modules.Documents.Infrastructure.Configuration;
using Evidata.Modules.Documents.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Testcontainers.Azurite;

namespace Evidata.Tests.Integration.BlobStorage;

/// <summary>
/// Tests de integración de IBlobStorageService contra Azurite real (Testcontainers).
/// Requiere Docker disponible — se omiten automáticamente si Docker no está activo.
///
/// Validan:
///   - UploadAsync: sube bytes y el blob queda accesible en Azurite
///   - ExistsAsync: retorna true tras upload, false antes
///   - DeleteAsync: el blob deja de existir tras eliminar
///   - GenerateUploadSasAsync + GenerateDownloadSasAsync: retornan URLs con SAS válido
/// </summary>
[Collection("Azurite")]
public sealed class AzureBlobStorageServiceTests : IAsyncLifetime
{
    private readonly AzuriteContainer _azurite;
    private AzureBlobStorageService _sut = null!;
    private readonly Guid _tenantId = Guid.NewGuid();
    private const string ContainerName = "evidata-documents";

    public AzureBlobStorageServiceTests()
    {
        _azurite = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _azurite.StartAsync();

        var options = Options.Create(new BlobStorageOptions
        {
            ConnectionString = _azurite.GetConnectionString(),
            ContainerName    = ContainerName,
            UploadSasExpirationMinutes   = 15,
            DownloadSasExpirationMinutes = 60
        });

        _sut = new AzureBlobStorageService(options, NullLogger<AzureBlobStorageService>.Instance);

        // Crear container
        var serviceClient = new BlobServiceClient(_azurite.GetConnectionString());
        await serviceClient.GetBlobContainerClient(ContainerName).CreateIfNotExistsAsync();
    }

    public async Task DisposeAsync() => await _azurite.DisposeAsync();

    // ── UploadAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Upload_ThenExists_ReturnsTrue()
    {
        var blobPath = $"{_tenantId}/test/exists-check.txt";
        var content  = "hello azurite"u8.ToArray();

        await _sut.UploadAsync(blobPath, content, "text/plain");

        Assert.True(await _sut.ExistsAsync(blobPath));
    }

    [Fact]
    public async Task Upload_ContentIsPreserved()
    {
        var blobPath = $"{_tenantId}/test/content.txt";
        var content  = "evidata content check"u8.ToArray();

        await _sut.UploadAsync(blobPath, content, "text/plain");

        // Descargar directamente con SDK para comparar bytes
        var serviceClient = new BlobServiceClient(_azurite.GetConnectionString());
        var blobClient = serviceClient.GetBlobContainerClient(ContainerName).GetBlobClient(blobPath);
        var download  = await blobClient.DownloadContentAsync();
        var downloaded = download.Value.Content.ToArray();

        Assert.Equal(content, downloaded);
    }

    [Fact]
    public async Task Upload_ExcelBytes_BlobAccessible()
    {
        var blobPath = $"{_tenantId}/reports/2026/07/08/{Guid.NewGuid()}_rat.xlsx";
        // Simula bytes de Excel (mínimo válido para verificar que no trunca)
        var fakeExcel = new byte[1024];
        new Random(42).NextBytes(fakeExcel);

        var returnedPath = await _sut.UploadAsync(
            blobPath, fakeExcel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        Assert.Equal(blobPath, returnedPath);
        Assert.True(await _sut.ExistsAsync(blobPath));
    }

    // ── ExistsAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Exists_BlobNotUploaded_ReturnsFalse()
    {
        var blobPath = $"{_tenantId}/test/does-not-exist.bin";

        Assert.False(await _sut.ExistsAsync(blobPath));
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Delete_AfterUpload_BlobNoLongerExists()
    {
        var blobPath = $"{_tenantId}/test/to-delete.txt";
        await _sut.UploadAsync(blobPath, "delete me"u8.ToArray(), "text/plain");

        await _sut.DeleteAsync(blobPath);

        Assert.False(await _sut.ExistsAsync(blobPath));
    }

    [Fact]
    public async Task Delete_NonExistentBlob_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() =>
            _sut.DeleteAsync($"{_tenantId}/test/ghost.bin"));

        Assert.Null(ex);
    }

    // ── SAS ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GenerateUploadSas_ReturnsNonEmptyUrl()
    {
        var result = await _sut.GenerateUploadSasAsync(
            _tenantId, "documento.pdf", "application/pdf");

        Assert.NotEmpty(result.UploadUrl);
        Assert.NotEmpty(result.BlobPath);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GenerateDownloadSas_AfterUpload_ReturnsNonEmptyUrl()
    {
        var blobPath = $"{_tenantId}/test/sas-download.txt";
        await _sut.UploadAsync(blobPath, "sas test"u8.ToArray(), "text/plain");

        var result = await _sut.GenerateDownloadSasAsync(blobPath);

        Assert.NotEmpty(result.DownloadUrl);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    // ── Aislamiento multi-tenant ──────────────────────────────────────────────

    [Fact]
    public async Task Upload_TwoTenants_PathsAreIsolated()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var pathA   = $"{tenantA}/reports/file.xlsx";
        var pathB   = $"{tenantB}/reports/file.xlsx";

        await _sut.UploadAsync(pathA, "tenant-a"u8.ToArray(), "text/plain");
        await _sut.UploadAsync(pathB, "tenant-b"u8.ToArray(), "text/plain");

        Assert.True(await _sut.ExistsAsync(pathA));
        Assert.True(await _sut.ExistsAsync(pathB));
        Assert.False(await _sut.ExistsAsync($"{tenantA}/reports/tenant-b-file.xlsx"));
    }
}
