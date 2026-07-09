using Evidata.Functions.McpBatch.Handlers;
using Evidata.Functions.McpBatch.Models;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Mcp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Evidata.Tests.Unit.McpBatch;

/// <summary>
/// Tests para McpRetryHandler.
/// El handler crea McpReviewTask para interacciones Failed sin tarea asignada.
/// </summary>
public class McpRetryHandlerTests
{
    private static McpDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new McpDbContext(opts);
    }

    private static McpInteraction FailedInteraction(Guid tenantId) =>
        McpInteraction.RecordFailed(tenantId, Guid.NewGuid(), "¿Cómo proceso datos personales?");

    // ── Happy path ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_FailedInteraction_CreatesReviewTask()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        db.McpInteractions.Add(FailedInteraction(tenantId));
        await db.SaveChangesAsync();

        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(new McpRetryPayload(tenantId), CancellationToken.None);

        Assert.Equal(1, result.Elevated);

        var task = await db.McpReviewTasks.FirstOrDefaultAsync();
        Assert.NotNull(task);
        Assert.Equal(tenantId, task.TenantId);
        Assert.Equal(McpReviewTaskStatus.Open, task.Status);
    }

    [Fact]
    public async Task Handle_FailedInteraction_ElevatesCount()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        db.McpInteractions.Add(FailedInteraction(tenantId));
        await db.SaveChangesAsync();

        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(new McpRetryPayload(tenantId), CancellationToken.None);

        Assert.Equal(1, result.Elevated);
        Assert.Equal(0, result.Skipped);
    }

    // ── Idempotencia — no crear tarea si ya existe ────────────────────────────

    [Fact]
    public async Task Handle_InteractionAlreadyHasTask_IsIdempotent()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        var interaction = FailedInteraction(tenantId);
        db.McpInteractions.Add(interaction);
        await db.SaveChangesAsync();

        // Crear la tarea manualmente antes del handler
        db.McpReviewTasks.Add(McpReviewTask.Create(interaction.Id, tenantId));
        await db.SaveChangesAsync();

        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(new McpRetryPayload(tenantId), CancellationToken.None);

        Assert.Equal(0, result.Elevated);
        Assert.Equal(1, await db.McpReviewTasks.CountAsync());
    }

    // ── Filtro por tenant ─────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_OtherTenantInteraction_NotElevated()
    {
        await using var db = CreateDb();
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        db.McpInteractions.Add(FailedInteraction(tenantB));
        await db.SaveChangesAsync();

        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(new McpRetryPayload(tenantA), CancellationToken.None);

        Assert.Equal(0, result.Elevated);
    }

    // ── No hay fallidas ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoFailed_ReturnsZeros()
    {
        await using var db = CreateDb();
        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(new McpRetryPayload(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(0, result.Elevated);
        Assert.Equal(0, result.Skipped);
    }

    // ── BatchSize respetado ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BatchSize1_OnlyCreatesOneTask()
    {
        await using var db = CreateDb();
        var tenantId = Guid.NewGuid();

        db.McpInteractions.AddRange(FailedInteraction(tenantId), FailedInteraction(tenantId));
        await db.SaveChangesAsync();

        var handler = new McpRetryHandler(db, NullLogger<McpRetryHandler>.Instance);
        var result = await handler.HandleAsync(
            new McpRetryPayload(tenantId, BatchSize: 1), CancellationToken.None);

        Assert.Equal(1, result.Elevated);
        Assert.Equal(1, await db.McpReviewTasks.CountAsync());
    }
}
