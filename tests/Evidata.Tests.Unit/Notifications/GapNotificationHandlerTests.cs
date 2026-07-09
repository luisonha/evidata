using Evidata.Functions.Notifications.Email;
using Evidata.Functions.Notifications.Handlers;
using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Evidata.Tests.Unit.Notifications;

public class GapNotificationHandlerTests
{
    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken ct = default) => Task.CompletedTask;
    }

    private readonly GapNotificationHandler _handler =
        new(new NoOpEmailSender(), NullLogger<GapNotificationHandler>.Instance);

    private static string BuildPayload(
        string? gapTitle = "Falta política de privacidad",
        GapSeverity severity = GapSeverity.High,
        GapStatus status = GapStatus.Open,
        Guid? ownerId = null,
        string? extra = null)
    {
        var payload = new GapEventPayload(
            GapId: Guid.NewGuid(),
            TenantId: Guid.NewGuid(),
            GapTitle: gapTitle ?? "Brecha test",
            Severity: severity,
            Status: status,
            SourceModule: "ProcessingInventory",
            SourceEntityId: Guid.NewGuid(),
            OwnerId: ownerId,
            OccurredAt: DateTimeOffset.UtcNow,
            ActorId: Guid.NewGuid(),
            Extra: extra);

        return JsonSerializer.Serialize(payload);
    }

    // ── Todos los tipos de evento devuelven true ───────────────────────────────

    [Theory]
    [InlineData(GapEventTypes.GapCreated)]
    [InlineData(GapEventTypes.GapAssigned)]
    [InlineData(GapEventTypes.GapProgressed)]
    [InlineData(GapEventTypes.GapBlocked)]
    [InlineData(GapEventTypes.GapResolved)]
    [InlineData(GapEventTypes.GapRiskAccepted)]
    [InlineData(GapEventTypes.GapClosed)]
    public async Task HandleAsync_KnownEventType_ReturnsTrue(string messageType)
    {
        var payload = BuildPayload();
        var result = await _handler.HandleAsync(messageType, payload, CancellationToken.None);
        Assert.True(result);
    }

    // ── Tipo desconocido devuelve false ───────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UnknownType_ReturnsFalse()
    {
        var result = await _handler.HandleAsync("gap.unknown.v99", BuildPayload(), CancellationToken.None);
        Assert.False(result);
    }

    // ── Payload nulo devuelve false ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_NullPayload_ReturnsFalse()
    {
        var result = await _handler.HandleAsync(GapEventTypes.GapCreated, "null", CancellationToken.None);
        Assert.False(result);
    }

    // ── GapCreated con OwnerId notifica propietario ───────────────────────────

    [Fact]
    public async Task HandleAsync_GapCreated_WithOwner_Succeeds()
    {
        var payload = BuildPayload(ownerId: Guid.NewGuid());
        var result = await _handler.HandleAsync(GapEventTypes.GapCreated, payload, CancellationToken.None);
        Assert.True(result);
    }

    // ── GapBlocked con severidad Critical ────────────────────────────────────

    [Fact]
    public async Task HandleAsync_GapBlocked_CriticalSeverity_Succeeds()
    {
        var payload = BuildPayload(severity: GapSeverity.Critical, extra: "Falta medida de seguridad");
        var result = await _handler.HandleAsync(GapEventTypes.GapBlocked, payload, CancellationToken.None);
        Assert.True(result);
    }

    // ── GapRiskAccepted con justificación ────────────────────────────────────

    [Fact]
    public async Task HandleAsync_GapRiskAccepted_WithJustification_Succeeds()
    {
        var payload = BuildPayload(extra: "Riesgo bajo, costo mitigación > impacto");
        var result = await _handler.HandleAsync(GapEventTypes.GapRiskAccepted, payload, CancellationToken.None);
        Assert.True(result);
    }

    // ── Payload JSON inválido lanza excepción ─────────────────────────────────

    [Fact]
    public async Task HandleAsync_InvalidJson_ThrowsJsonException()
    {
        await Assert.ThrowsAsync<JsonException>(
            () => _handler.HandleAsync(GapEventTypes.GapCreated, "{{invalid", CancellationToken.None));
    }
}
