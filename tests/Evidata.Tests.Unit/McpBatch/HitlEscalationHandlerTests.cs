using Evidata.Functions.McpBatch.Handlers;
using Evidata.Functions.McpBatch.Models;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evidata.Tests.Unit.McpBatch;

/// <summary>Tests para HitlEscalationHandler — detecta tareas HITL estancadas.</summary>
public class HitlEscalationHandlerTests
{
    private static McpDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new McpDbContext(opts);
    }

    private static McpReviewTask StaleTask(Guid tenantId, int minutesAgo = 180)
    {
        // Creamos la tarea con Create, luego la "envejecemos" usando reflexión mínima.
        // No hay método público para setear CreatedAt — usamos EF shadow state trick:
        // simplemente creamos la tarea y ajustamos directamente (en tests es aceptable).
        var task = McpReviewTask.Create(Guid.NewGuid(), tenantId);
        // McpReviewTask.CreatedAt es private set — accedemos via reflexión solo en tests
        var prop = typeof(McpReviewTask).GetProperty("CreatedAt")!;
        prop.SetValue(task, DateTimeOffset.UtcNow.AddMinutes(-minutesAgo));
        return task;
    }

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_StaleTask_DetectsIt()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        db.McpReviewTasks.Add(StaleTask(tenantId, minutesAgo: 180));
        await db.SaveChangesAsync();

        var handler = new HitlEscalationHandler(db, NullLogger<HitlEscalationHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpHitlEscalationPayload(tenantId, StaleThresholdMinutes: 120),
            CancellationToken.None);

        Assert.Equal(1, result.StaleTasksDetected);
    }

    // ── Tarea reciente no debe escalarse ──────────────────────────────────────

    [Fact]
    public async Task Handle_FreshTask_NotDetected()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        // Tarea creada hace solo 30 min — no supera el umbral de 120 min
        db.McpReviewTasks.Add(StaleTask(tenantId, minutesAgo: 30));
        await db.SaveChangesAsync();

        var handler = new HitlEscalationHandler(db, NullLogger<HitlEscalationHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpHitlEscalationPayload(tenantId, StaleThresholdMinutes: 120),
            CancellationToken.None);

        Assert.Equal(0, result.StaleTasksDetected);
    }

    // ── Filtro por tenant opcional ────────────────────────────────────────────

    [Fact]
    public async Task Handle_NullTenantId_ScansAllTenants()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        db.McpReviewTasks.AddRange(
            StaleTask(tenantA, 180),
            StaleTask(tenantB, 180));
        await db.SaveChangesAsync();

        var handler = new HitlEscalationHandler(db, NullLogger<HitlEscalationHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpHitlEscalationPayload(TenantId: null, StaleThresholdMinutes: 120),
            CancellationToken.None);

        Assert.Equal(2, result.StaleTasksDetected);
    }

    // ── Tarea InProgress no debe escalarse ────────────────────────────────────

    [Fact]
    public async Task Handle_InProgressTask_NotDetected()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var task = StaleTask(tenantId, minutesAgo: 300);
        task.Start(Guid.NewGuid()); // → InProgress
        db.McpReviewTasks.Add(task);
        await db.SaveChangesAsync();

        var handler = new HitlEscalationHandler(db, NullLogger<HitlEscalationHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpHitlEscalationPayload(tenantId, StaleThresholdMinutes: 120),
            CancellationToken.None);

        // InProgress significa que YA tiene asignado a alguien, no es stale
        Assert.Equal(0, result.StaleTasksDetected);
    }

    // ── No hay tareas ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoTasks_ReturnsZero()
    {
        await using var db = CreateDb();
        var handler = new HitlEscalationHandler(db, NullLogger<HitlEscalationHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpHitlEscalationPayload(Guid.NewGuid()),
            CancellationToken.None);
        Assert.Equal(0, result.StaleTasksDetected);
    }
}
