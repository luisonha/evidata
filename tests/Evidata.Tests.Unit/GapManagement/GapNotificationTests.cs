using Evidata.Modules.GapManagement.Application.Notifications;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Notifications;
using Evidata.Worker.Outbox.Persistence;
using NSubstitute;

namespace Evidata.Tests.Unit.GapManagement;

public class GapNotificationTests
{
    private static readonly Guid _tenantId = Guid.NewGuid();
    private static readonly Guid _actorId = Guid.NewGuid();

    private static (IGapNotificationService svc, IOutboxWriter outbox) Build()
    {
        var outbox = Substitute.For<IOutboxWriter>();
        return (new OutboxGapNotificationService(outbox), outbox);
    }

    private static ComplianceGap BuildOpen() =>
        ComplianceGap.Create(_tenantId, "ProcessingInventory", Guid.NewGuid(),
            "Falta medida de seguridad", "Descripción del gap.", GapSeverity.Critical, _actorId);

    // ── Created ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyCreated_EnqueuesGapCreatedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();

        await svc.NotifyCreatedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            gap.TenantId.ToString(),
            "notification-queue",
            GapEventTypes.GapCreated,
            Arg.Any<string>(),
            gap.Id.ToString(),
            Arg.Any<CancellationToken>());
    }

    // ── Assigned ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyAssigned_EnqueuesGapAssignedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();
        gap.Assign(Guid.NewGuid(), _actorId);

        await svc.NotifyAssignedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapAssigned,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Progressed ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyProgressed_EnqueuesGapProgressedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();

        await svc.NotifyProgressedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapProgressed,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Blocked ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyBlocked_EnqueuesGapBlockedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();

        await svc.NotifyBlockedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapBlocked,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Resolved ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyResolved_EnqueuesGapResolvedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();

        await svc.NotifyResolvedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapResolved,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── RiskAccepted ──────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyRiskAccepted_EnqueuesGapRiskAcceptedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();
        gap.AcceptRisk("Justificación formal aprobada por DPO.", _actorId);

        await svc.NotifyRiskAcceptedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapRiskAccepted,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Closed ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task NotifyClosed_EnqueuesGapClosedEvent()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();
        gap.AcceptRisk("Aceptado", _actorId);
        gap.Close(_actorId);

        await svc.NotifyClosedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(),
            GapEventTypes.GapClosed,
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    // ── Payload contiene TenantId y GapId ─────────────────────────────────────

    [Fact]
    public async Task NotifyCreated_PayloadContainsTenantAndGapId()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();
        string? capturedPayload = null;
        await outbox.EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Do<string>(p => capturedPayload = p),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());

        await svc.NotifyCreatedAsync(gap, _actorId);

        Assert.NotNull(capturedPayload);
        Assert.Contains(gap.Id.ToString(), capturedPayload);
        Assert.Contains(gap.TenantId.ToString(), capturedPayload);
    }

    // ── CorrelationId es el GapId ─────────────────────────────────────────────

    [Fact]
    public async Task NotifyCreated_CorrelationIdIsGapId()
    {
        var (svc, outbox) = Build();
        var gap = BuildOpen();

        await svc.NotifyCreatedAsync(gap, _actorId);

        await outbox.Received(1).EnqueueAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), gap.Id.ToString(), Arg.Any<CancellationToken>());
    }
}
