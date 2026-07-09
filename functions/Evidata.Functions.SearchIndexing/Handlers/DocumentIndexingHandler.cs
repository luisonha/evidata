using Evidata.Functions.SearchIndexing.Models;
using Evidata.Modules.Documents.Domain;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Evidata.Modules.Search.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Evidata.Functions.SearchIndexing.Handlers;

/// <summary>
/// Indexa un documento del tenant en el catálogo de búsqueda.
///
/// Flujo:
///   1. Verifica que el documento existe y pertenece al tenant
///   2. Solo indexa documentos en estado Processed (con contenido extraído)
///   3. Registra el evento de indexación en SearchQueryLog (source="system-index")
///   4. Producción: adicionalmente pushearía el documento a Azure AI Search
///
/// Idempotencia: re-indexar un documento no falla, solo actualiza el log.
/// </summary>
public class DocumentIndexingHandler
{
    private static readonly Guid SystemUserId = new("00000000-0000-0000-0000-000000000001");

    private readonly DocumentDbContext _db;
    private readonly ISearchQueryLogService _searchLog;
    private readonly ILogger<DocumentIndexingHandler> _logger;

    public DocumentIndexingHandler(
        DocumentDbContext db,
        ISearchQueryLogService searchLog,
        ILogger<DocumentIndexingHandler> logger)
    {
        _db = db;
        _searchLog = searchLog;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DocumentIndexPayload payload, CancellationToken ct)
    {
        var document = await _db.Documents
            .AsNoTracking()
            .Where(d => d.TenantId == payload.TenantId)
            .FirstOrDefaultAsync(d => d.Id == payload.DocumentId, ct);

        if (document is null)
        {
            _logger.LogWarning(
                "⚠ Documento {DocumentId} no encontrado en tenant {TenantId} — ignorando",
                payload.DocumentId, payload.TenantId);
            return false;
        }

        if (document.Status != DocumentStatus.Processed)
        {
            _logger.LogInformation(
                "Documento {DocumentId} en estado {Status} — aún no procesado, saltando indexación",
                payload.DocumentId, document.Status);
            return false;
        }

        // Construir término de búsqueda descriptivo para el log de indexación
        var searchTerm = BuildSearchTerm(document, payload);

        await _searchLog.RecordAsync(
            tenantId: payload.TenantId,
            userId: SystemUserId,
            query: searchTerm,
            source: "system-index",
            resultCount: 1,
            filtersJson: BuildFiltersJson(document),
            ct: ct);

        _logger.LogInformation(
            "✅ Documento indexado: {DocumentId} '{FileName}' ContentType={ContentType} Tenant={TenantId}",
            document.Id, document.OriginalFileName, document.ContentType, document.TenantId);

        return true;
    }

    private static string BuildSearchTerm(
        Evidata.Modules.Documents.Domain.Document document,
        DocumentIndexPayload payload)
    {
        // El término combina nombre de archivo + descripción para enriquecer el índice
        var parts = new List<string> { document.OriginalFileName };
        if (!string.IsNullOrWhiteSpace(document.Description))
            parts.Add(document.Description);
        return string.Join(" ", parts);
    }

    private static string BuildFiltersJson(
        Evidata.Modules.Documents.Domain.Document document)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            documentId = document.Id,
            contentType = document.ContentType,
            blobPath = document.BlobPath,
            entityType = "Document"
        });
    }
}
