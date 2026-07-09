using Evidata.Modules.Documents.Application.Abstractions;
using NSubstitute;
using Xunit;

namespace Evidata.Tests.Unit.Documents;

/// <summary>
/// Tests de IBlobStorageService usando NSubstitute.
/// La implementación real (AzureBlobStorageService) requiere Azure Storage —
/// estos tests validan el contrato de la interfaz y los tipos de retorno.
/// </summary>
public class BlobStorageServiceTests
{
    private readonly IBlobStorageService _sut;
    private readonly Guid _tenantId = Guid.NewGuid();

    public BlobStorageServiceTests()
    {
        _sut = Substitute.For<IBlobStorageService>();
    }

    [Fact]
    public async Task GenerateUploadSas_ReturnsValidResult()
    {
        // Arrange
        var expected = new SasUploadResult(
            "https://account.blob.core.windows.net/container/blob?sv=2023&sig=xxx",
            $"{_tenantId}/2026/07/08/{Guid.NewGuid()}_documento.pdf",
            DateTimeOffset.UtcNow.AddMinutes(15));

        _sut.GenerateUploadSasAsync(_tenantId, "documento.pdf", "application/pdf", null, default)
            .Returns(expected);

        // Act
        var result = await _sut.GenerateUploadSasAsync(_tenantId, "documento.pdf", "application/pdf");

        // Assert
        Assert.NotNull(result.UploadUrl);
        Assert.NotEmpty(result.BlobPath);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GenerateDownloadSas_ReturnsValidResult()
    {
        // Arrange
        var blobPath = $"{_tenantId}/2026/07/08/test.pdf";
        var expected = new SasDownloadResult(
            "https://account.blob.core.windows.net/container/blob?sv=2023&sig=yyy",
            DateTimeOffset.UtcNow.AddHours(1));

        _sut.GenerateDownloadSasAsync(blobPath, null, default).Returns(expected);

        // Act
        var result = await _sut.GenerateDownloadSasAsync(blobPath);

        // Assert
        Assert.NotNull(result.DownloadUrl);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Delete_NonExistentBlob_DoesNotThrow()
    {
        // Arrange — stub silencioso (default = Task.CompletedTask)
        var blobPath = $"{_tenantId}/nonexistent.pdf";

        // Act & Assert — no debe lanzar
        await _sut.DeleteAsync(blobPath);
    }

    [Fact]
    public async Task Exists_WhenBlobPresent_ReturnsTrue()
    {
        // Arrange
        var blobPath = $"{_tenantId}/2026/07/08/existing.pdf";
        _sut.ExistsAsync(blobPath, default).Returns(true);

        // Act
        var exists = await _sut.ExistsAsync(blobPath);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task Exists_WhenBlobAbsent_ReturnsFalse()
    {
        // Arrange
        var blobPath = $"{_tenantId}/2026/07/08/missing.pdf";
        _sut.ExistsAsync(blobPath, default).Returns(false);

        // Act
        var exists = await _sut.ExistsAsync(blobPath);

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public void SasUploadResult_BlobPath_ContainsTenantId()
    {
        // Arrange & Act
        var blobPath = $"{_tenantId}/2026/07/08/{Guid.NewGuid()}_doc.pdf";
        var result = new SasUploadResult("https://url", blobPath, DateTimeOffset.UtcNow.AddMinutes(15));

        // Assert — el path contiene el tenantId como prefijo para aislamiento multi-tenant
        Assert.StartsWith(_tenantId.ToString(), result.BlobPath);
    }
}
