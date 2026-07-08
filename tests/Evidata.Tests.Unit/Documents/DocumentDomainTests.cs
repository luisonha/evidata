using Evidata.Modules.Documents.Domain;
using Xunit;

namespace Evidata.Tests.Unit.Documents;

public class DocumentDomainTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    [Fact]
    public void Create_ValidParams_SetsStatusPendingUpload()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);

        Assert.Equal(DocumentStatus.PendingUpload, doc.Status);
        Assert.Equal(TenantId, doc.TenantId);
        Assert.Equal("contrato.pdf", doc.OriginalFileName);
        Assert.Null(doc.BlobPath);
        Assert.Null(doc.DeletedAt);
    }

    [Fact]
    public void SetBlobPath_TransitionsToUploaded()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);

        doc.SetBlobPath($"{TenantId}/2026/07/08/guid_contrato.pdf", 102400, UserId);

        Assert.Equal(DocumentStatus.Uploaded, doc.Status);
        Assert.NotNull(doc.BlobPath);
        Assert.Equal(102400, doc.SizeBytes);
        Assert.NotNull(doc.UpdatedAt);
    }

    [Fact]
    public void MarkProcessing_TransitionsToProcessing()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);
        doc.SetBlobPath("path/blob.pdf", 1024, UserId);

        doc.MarkProcessing(UserId);

        Assert.Equal(DocumentStatus.Processing, doc.Status);
    }

    [Fact]
    public void MarkProcessed_TransitionsToProcessed()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);
        doc.SetBlobPath("path/blob.pdf", 1024, UserId);
        doc.MarkProcessing(UserId);

        doc.MarkProcessed(UserId);

        Assert.Equal(DocumentStatus.Processed, doc.Status);
    }

    [Fact]
    public void MarkFailed_TransitionsToFailed()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);
        doc.SetBlobPath("path/blob.pdf", 1024, UserId);
        doc.MarkProcessing(UserId);

        doc.MarkFailed(UserId);

        Assert.Equal(DocumentStatus.Failed, doc.Status);
    }

    [Fact]
    public void SoftDelete_SetsDeletedAt()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);

        doc.SoftDelete(UserId);

        Assert.NotNull(doc.DeletedAt);
        Assert.Equal(UserId, doc.DeletedBy);
    }

    [Fact]
    public void AddVersion_AddsVersionToCollection()
    {
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);

        var v1 = doc.AddVersion("path/v1.pdf", 1024, 1, UserId);
        var v2 = doc.AddVersion("path/v2.pdf", 2048, 2, UserId);

        Assert.Equal(2, doc.Versions.Count);
        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(2, v2.VersionNumber);
        Assert.Equal(doc.TenantId, v1.TenantId);
    }

    [Fact]
    public void Create_EmptyFileName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Document.Create(TenantId, "", "application/pdf", UserId));
    }

    [Fact]
    public void Create_NegativeSizeBytes_ThrowsOnVersion()
    {
        var doc = Document.Create(TenantId, "file.pdf", "application/pdf", UserId);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            doc.AddVersion("path/v1.pdf", -1, 1, UserId));
    }
}
