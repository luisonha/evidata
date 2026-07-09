using Evidata.Modules.Documents.Application.Abstractions;
using Evidata.Modules.Documents.Application.Queries;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Evidata.Modules.Identity.Application.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Evidata.Modules.Documents.Api;

[ApiController]
[Route("api/documents")]
public class DocumentsController(
    ListDocumentsQueryHandler listHandler,
    IBlobStorageService blobStorage,
    DocumentDbContext db,
    ICurrentUserContext currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentDto>>> List(CancellationToken ct)
    {
        var result = await listHandler.HandleAsync(currentUser.TenantId, ct);
        return Ok(result);
    }

    [HttpPost("upload-url")]
    public async Task<ActionResult<UploadUrlResponse>> GenerateUploadUrl(
        [FromBody] UploadUrlRequest req, CancellationToken ct)
    {
        var sas = await blobStorage.GenerateUploadSasAsync(
            currentUser.TenantId, req.FileName, req.ContentType, ct: ct);

        var doc = Domain.Document.Create(
            currentUser.TenantId, req.FileName, req.ContentType,
            currentUser.UserId, req.Description);

        // El blobPath se asignará cuando el cliente confirme el upload completado.
        // Por ahora guardamos el documento en estado PendingUpload.

        db.Documents.Add(doc);
        await db.SaveChangesAsync(ct);

        return Ok(new UploadUrlResponse(doc.Id, sas.UploadUrl, sas.BlobPath, sas.ExpiresAt));
    }
}

public record UploadUrlRequest(string FileName, string ContentType, string? Description = null);
public record UploadUrlResponse(Guid DocumentId, string UploadUrl, string BlobPath, DateTimeOffset ExpiresAt);
