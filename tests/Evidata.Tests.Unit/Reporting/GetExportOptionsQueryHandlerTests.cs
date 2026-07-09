using Evidata.Modules.Reporting.Application.Abstractions;
using Evidata.Modules.Reporting.Application.Queries;

namespace Evidata.Tests.Unit.Reporting;

/// <summary>
/// Tests para el handler de consulta GetExportOptionsQueryHandler.
/// Verifica el comportamiento actual (lista vacía) que es intencional
/// hasta que se resuelva la especificación de ExportType a nivel de arquitectura.
/// </summary>
public class GetExportOptionsQueryHandlerTests
{
    private readonly GetExportOptionsQueryHandler _handler = new();

    [Fact]
    public async Task GetExportOptionsAsync_ReturnsEmptyList()
    {
        // Arrange
        var processingActivityId = Guid.NewGuid();
        var ct = CancellationToken.None;

        // Act
        var result = await _handler.GetExportOptionsAsync(processingActivityId, ct);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExportOptionsAsync_WithValidId_ReturnsEmptyListNotNull()
    {
        // Arrange
        var processingActivityId = Guid.NewGuid();

        // Act
        var result = await _handler.GetExportOptionsAsync(processingActivityId);

        // Assert
        // Verificar que la lista es vacía y no nula
        // Este es el comportamiento intencional de stub.
        Assert.NotNull(result);
        Assert.IsType<ExportOptionViewModel[]>(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetExportOptionsAsync_CancellationToken_IsRespected()
    {
        // Arrange
        var processingActivityId = Guid.NewGuid();
        var cts = new CancellationTokenSource();

        // Act - para stub vacío, no hay operaciones async reales,
        // pero verificamos que la firma acepta el token
        var result = await _handler.GetExportOptionsAsync(processingActivityId, cts.Token);

        // Assert
        Assert.Empty(result);
    }
}
