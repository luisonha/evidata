using System.Text.Json.Serialization;

namespace Evidata.Functions.DocumentProcessing.Models;

/// <summary>Payload del mensaje DocumentUploaded.</summary>
public record DocumentUploadedPayload(
    [property: JsonPropertyName("documentId")] string DocumentId,
    [property: JsonPropertyName("tenantId")] string TenantId,
    [property: JsonPropertyName("blobPath")] string BlobPath,
    [property: JsonPropertyName("fileName")] string FileName,
    [property: JsonPropertyName("contentType")] string ContentType,
    [property: JsonPropertyName("uploadedBy")] string UploadedBy
);
