using System.Text.Json.Serialization;

namespace Evidata.Functions.DocumentProcessing.Models;

/// <summary>Payload del mensaje DocumentUploaded.</summary>
public record DocumentUploadedPayload(
    [property: JsonPropertyName("documentId")] Guid DocumentId,
    [property: JsonPropertyName("tenantId")] Guid TenantId,
    [property: JsonPropertyName("blobPath")] string? BlobPath,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("uploadedBy")] Guid UploadedBy,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes = 0
);
