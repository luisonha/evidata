using Evidata.Functions.DocumentProcessing.Handlers;
using Evidata.Functions.DocumentProcessing.Models;
using Evidata.Modules.Documents.Domain;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Evidata.Tests.Unit.Functions;

public class DocumentUploadedHandlerTests : IDisposable
{
    private readonly DocumentDbContext _db;
    private readonly DocumentUploadedHandler _sut;
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId = Guid.NewGuid();

    public DocumentUploadedHandlerTests()
    {
        var options = new DbContextOptionsBuilder<DocumentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new DocumentDbContext(options);
        _sut = new DocumentUploadedHandler(_db, NullLogger<DocumentUploadedHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ValidDocument_TransitionsToProcessed()
    {
        // Arrange
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);
        doc.SetBlobPath($"{TenantId}/2026/07/08/guid_contrato.pdf", 102400, UserId);
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var payload = new DocumentUploadedPayload(
            DocumentId: doc.Id,
            TenantId: TenantId,
            BlobPath: doc.BlobPath,
            FileName: "contrato.pdf",
            ContentType: "application/pdf",
            UploadedBy: UserId,
            SizeBytes: 102400);

        // Act
        await _sut.HandleAsync(payload, CancellationToken.None);

        // Assert
        var updated = await _db.Documents.IgnoreQueryFilters().FirstAsync(d => d.Id == doc.Id);
        Assert.Equal(DocumentStatus.Processed, updated.Status);
    }

    [Fact]
    public async Task HandleAsync_DocumentNotFound_ReturnsWithoutException()
    {
        // Arrange — documento que no existe en BD
        var payload = new DocumentUploadedPayload(
            DocumentId: Guid.NewGuid(),
            TenantId: TenantId,
            BlobPath: "path/notexist.pdf",
            FileName: "notexist.pdf",
            ContentType: "application/pdf",
            UploadedBy: UserId);

        // Act & Assert — no debe lanzar excepción
        await _sut.HandleAsync(payload, CancellationToken.None);
    }

    [Fact]
    public async Task HandleAsync_AlreadyProcessed_IsIdempotent()
    {
        // Arrange — documento ya procesado
        var doc = Document.Create(TenantId, "contrato.pdf", "application/pdf", UserId);
        doc.SetBlobPath("path/blob.pdf", 1024, UserId);
        doc.MarkProcessing(UserId);
        doc.MarkProcessed(UserId);
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        var payload = new DocumentUploadedPayload(
            DocumentId: doc.Id,
            TenantId: TenantId,
            BlobPath: doc.BlobPath,
            FileName: "contrato.pdf",
            ContentType: "application/pdf",
            UploadedBy: UserId);

        // Act — procesar dos veces
        await _sut.HandleAsync(payload, CancellationToken.None);
        await _sut.HandleAsync(payload, CancellationToken.None);

        // Assert — sigue en Processed (no retrocede)
        var updated = await _db.Documents.IgnoreQueryFilters().FirstAsync(d => d.Id == doc.Id);
        Assert.Equal(DocumentStatus.Processed, updated.Status);
    }

    [Fact]
    public async Task HandleAsync_WrongTenant_DocumentNotFound()
    {
        // Arrange — documento de otro tenant
        var otherTenant = Guid.NewGuid();
        var doc = Document.Create(otherTenant, "doc.pdf", "application/pdf", UserId);
        doc.SetBlobPath("path/blob.pdf", 1024, UserId);
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync();

        // Payload con el TenantId equivocado (cross-tenant)
        var payload = new DocumentUploadedPayload(
            DocumentId: doc.Id,
            TenantId: TenantId, // ← tenant diferente
            BlobPath: "path/blob.pdf",
            FileName: "doc.pdf",
            ContentType: "application/pdf",
            UploadedBy: UserId);

        // Act — el handler no debe encontrarlo
        await _sut.HandleAsync(payload, CancellationToken.None);

        // Assert — documento original no fue modificado
        var original = await _db.Documents.IgnoreQueryFilters().FirstAsync(d => d.Id == doc.Id);
        Assert.Equal(DocumentStatus.Uploaded, original.Status);
    }

    public void Dispose() => _db.Dispose();
}
