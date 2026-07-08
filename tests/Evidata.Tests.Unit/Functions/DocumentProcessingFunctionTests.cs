using Evidata.Functions.DocumentProcessing.Models;
using System.Text.Json;

namespace Evidata.Tests.Unit.Functions;

public class DocumentProcessingFunctionTests
{
    [Fact]
    public void QueueMessageEnvelope_Deserializes_Correctly()
    {
        var json = """
            {
                "messageType": "DocumentUploaded",
                "payload": "{\"documentId\":\"abc123\"}",
                "correlationId": "corr-1",
                "sentAt": "2026-07-08T09:00:00Z",
                "schemaVersion": 1
            }
            """;

        var envelope = JsonSerializer.Deserialize<QueueMessageEnvelope>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(envelope);
        Assert.Equal("DocumentUploaded", envelope.MessageType);
        Assert.Equal("corr-1", envelope.CorrelationId);
        Assert.Equal(1, envelope.SchemaVersion);
    }

    [Fact]
    public void DocumentUploadedPayload_Deserializes_Correctly()
    {
        var docId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var json = $$"""
            {
                "documentId": "{{docId}}",
                "tenantId": "{{tenantId}}",
                "blobPath": "evidata/doc-1.pdf",
                "fileName": "contrato.pdf",
                "contentType": "application/pdf",
                "uploadedBy": "{{userId}}"
            }
            """;

        var payload = JsonSerializer.Deserialize<DocumentUploadedPayload>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(payload);
        Assert.Equal(docId, payload.DocumentId);
        Assert.Equal("application/pdf", payload.ContentType);
    }

    [Theory]
    [InlineData("SGVsbG8gV29ybGQ=", true)]   // "Hello World" en Base64
    [InlineData("{\"messageType\":\"test\"}", false)]  // JSON plano
    [InlineData("", false)]
    public void IsBase64_DetectsCorrectly(string input, bool expected)
    {
        bool IsBase64(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length % 4 != 0) return false;
            try { Convert.FromBase64String(value); return true; }
            catch { return false; }
        }

        Assert.Equal(expected, IsBase64(input));
    }
}
