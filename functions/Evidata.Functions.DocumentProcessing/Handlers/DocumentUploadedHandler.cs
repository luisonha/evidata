using Evidata.Functions.DocumentProcessing.Models;
using Evidata.Modules.Documents.Domain;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.DocumentProcessing.Handlers;

/// <summary>
/// Maneja el evento DocumentUploaded:
/// 1. Busca el Document en PostgreSQL
/// 2. Transiciona: Uploaded → Processing
/// 3. Ejecuta extracción de metadatos (stub extensible)
/// 4. Transiciona: Processing → Processed (o Failed si error)
///
/// El handler es idempotente: si el documento ya está en Processed, no hace nada.
/// </summary>
public class DocumentUploadedHandler
{
    private readonly DocumentDbContext _db;
    private readonly ILogger<DocumentUploadedHandler> _logger;

    // ID de sistema para operaciones internas (sin usuario humano)
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    public DocumentUploadedHandler(DocumentDbContext db, ILogger<DocumentUploadedHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(DocumentUploadedPayload payload, CancellationToken ct)
    {
        _logger.LogInformation(
            "Iniciando procesamiento documento {DocumentId} tenant={TenantId} archivo={FileName}",
            payload.DocumentId, payload.TenantId, payload.FileName);

        var document = await _db.Documents
            .IgnoreQueryFilters() // El global filter de soft-delete aplica, pero queremos tenant específico
            .Where(d => d.TenantId == payload.TenantId)
            .FirstOrDefaultAsync(d => d.Id == payload.DocumentId, ct);

        if (document is null)
        {
            _logger.LogWarning(
                "⚠ Documento {DocumentId} no encontrado en tenant {TenantId} — mensaje descartado",
                payload.DocumentId, payload.TenantId);
            return;
        }

        // Idempotencia: no reprocesar documentos ya procesados
        if (document.Status == DocumentStatus.Processed)
        {
            _logger.LogInformation(
                "Documento {DocumentId} ya está en estado Processed — omitiendo (idempotente)",
                payload.DocumentId);
            return;
        }

        // Transición: → Processing
        document.MarkProcessing(SystemUserId);
        await _db.SaveChangesAsync(ct);

        try
        {
            await ExtractMetadataAsync(document, payload, ct);

            // Transición: → Processed
            document.MarkProcessed(SystemUserId);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "✅ Documento {DocumentId} procesado correctamente. BlobPath={BlobPath}",
                document.Id, document.BlobPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "❌ Error procesando documento {DocumentId} — marcando como Failed",
                payload.DocumentId);

            document.MarkFailed(SystemUserId);
            await _db.SaveChangesAsync(ct);

            throw; // Re-lanzar para que Azure Functions gestione reintentos/poison
        }
    }

    /// <summary>
    /// Extracción de metadatos del documento.
    /// Actualmente registra info básica del blob.
    /// Fase 4 (Search) agregará OCR e indexación real.
    /// </summary>
    private async Task ExtractMetadataAsync(Document document, DocumentUploadedPayload payload, CancellationToken ct)
    {
        _logger.LogInformation(
            "Extrayendo metadatos: DocumentId={DocumentId} ContentType={ContentType} SizeBytes={Size}",
            document.Id, payload.ContentType, payload.SizeBytes);

        // Actualizar tamaño si no estaba confirmado
        if (document.SizeBytes is null && payload.SizeBytes > 0 && payload.BlobPath is not null)
        {
            document.SetBlobPath(payload.BlobPath, payload.SizeBytes, SystemUserId);
            await _db.SaveChangesAsync(ct);
        }

        // Stub para OCR / extracción de texto — implementado en f4-search-indexing
        await Task.CompletedTask;
    }
}
