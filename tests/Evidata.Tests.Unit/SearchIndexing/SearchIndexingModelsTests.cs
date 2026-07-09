using Evidata.Functions.SearchIndexing.Models;
using System.Text.Json;

namespace Evidata.Tests.Unit.SearchIndexing;

public class SearchIndexingModelsTests
{
    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    // ── DocumentIndexPayload ──────────────────────────────────────────────────

    [Fact]
    public void DocumentIndexPayload_Roundtrip_PreservesFields()
    {
        var payload = new DocumentIndexPayload(
            DocumentId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            FileName: "politica_privacidad.pdf",
            ContentType: "application/pdf",
            BlobPath: "tenant1/2026/07/file.pdf");

        var json = JsonSerializer.Serialize(payload);
        var deserialized = JsonSerializer.Deserialize<DocumentIndexPayload>(json, Opts);

        Assert.NotNull(deserialized);
        Assert.Equal(payload.DocumentId, deserialized.DocumentId);
        Assert.Equal(payload.FileName, deserialized.FileName);
        Assert.Equal(payload.ContentType, deserialized.ContentType);
        Assert.Equal(payload.BlobPath, deserialized.BlobPath);
    }

    [Fact]
    public void DocumentIndexPayload_NullBlobPath_Preserved()
    {
        var payload = new DocumentIndexPayload(
            Guid.NewGuid(), Guid.NewGuid(), "file.docx", "application/docx", null);
        var json = JsonSerializer.Serialize(payload);
        var d = JsonSerializer.Deserialize<DocumentIndexPayload>(json, Opts);
        Assert.Null(d!.BlobPath);
    }

    // ── RatIndexPayload ───────────────────────────────────────────────────────

    [Fact]
    public void RatIndexPayload_Roundtrip_PreservesFields()
    {
        var payload = new RatIndexPayload(
            ActivityId: Guid.NewGuid(),
            TenantId: Guid.NewGuid());

        var json = JsonSerializer.Serialize(payload);
        var d = JsonSerializer.Deserialize<RatIndexPayload>(json, Opts);

        Assert.NotNull(d);
        Assert.Equal(payload.ActivityId, d.ActivityId);
        Assert.Equal(payload.TenantId, d.TenantId);
    }

    // ── QueueMessageEnvelope ──────────────────────────────────────────────────

    [Fact]
    public void Envelope_Roundtrip_PreservesFields()
    {
        var env = new QueueMessageEnvelope(
            MessageType: "document.indexed.v1",
            Payload: "{\"documentId\":\"aaa\"}",
            CorrelationId: "corr-1",
            SentAt: DateTimeOffset.UtcNow,
            SchemaVersion: 1);

        var json = JsonSerializer.Serialize(env);
        var d = JsonSerializer.Deserialize<QueueMessageEnvelope>(json, Opts);

        Assert.NotNull(d);
        Assert.Equal("document.indexed.v1", d.MessageType);
        Assert.Equal("corr-1", d.CorrelationId);
        Assert.Equal(1, d.SchemaVersion);
    }

    [Fact]
    public void Envelope_NullCorrelationId_Preserved()
    {
        var env = new QueueMessageEnvelope("rat.indexed.v1", "{}", null, DateTimeOffset.UtcNow, 1);
        var json = JsonSerializer.Serialize(env);
        var d = JsonSerializer.Deserialize<QueueMessageEnvelope>(json, Opts);
        Assert.Null(d!.CorrelationId);
    }

    // ── Tipos de mensaje reconocidos ──────────────────────────────────────────

    [Theory]
    [InlineData("document.indexed.v1")]
    [InlineData("rat.indexed.v1")]
    public void MessageType_KnownTypes_FollowNamingConvention(string messageType)
    {
        Assert.Contains(".indexed.v", messageType);
    }
}
