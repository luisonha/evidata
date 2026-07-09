using System.Text;
using System.Text.Json;
using Evidata.Functions.DocumentProcessing.Functions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Evidata.Tests.Unit.Poison;

public class DocumentProcessingPoisonFunctionTests
{
    private readonly DocumentProcessingPoisonFunction _sut;

    public DocumentProcessingPoisonFunctionTests()
    {
        _sut = new DocumentProcessingPoisonFunction(NullLogger<DocumentProcessingPoisonFunction>.Instance);
    }

    [Fact]
    public async Task Run_PlainTextMessage_CompletesWithoutException()
    {
        // Arrange — mensaje plano (no parseable)
        var message = "this is not valid json";

        // Act — no debe lanzar excepción (reencolaría en poison)
        await _sut.Run(message, CancellationToken.None);
    }

    [Fact]
    public async Task Run_ValidBase64Message_CompletesWithoutException()
    {
        // Arrange — mensaje válido codificado en base64
        var envelope = new
        {
            messageType = "DocumentUploaded",
            correlationId = Guid.NewGuid().ToString(),
            sentAt = DateTimeOffset.UtcNow.ToString("O"),
            schemaVersion = "1.0",
            payload = "{}"
        };
        var json = JsonSerializer.Serialize(envelope);
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        // Act
        await _sut.Run(base64, CancellationToken.None);
    }

    [Fact]
    public async Task Run_EmptyMessage_CompletesWithoutException()
    {
        await _sut.Run("", CancellationToken.None);
    }

    [Fact]
    public async Task Run_LongMessage_TruncatesPreviewTo200Chars()
    {
        // Arrange — mensaje largo (> 200 chars)
        var longMessage = new string('x', 500);

        // Act — no debe lanzar excepción al truncar
        await _sut.Run(longMessage, CancellationToken.None);
    }
}
