using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Domain;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Commands;

public sealed record CreateProcessingActivityCommand(
    Guid TenantId,
    string Name,
    string? Description,
    string? Controller,
    string? Department,
    Guid CreatedBy);

public sealed class CreateProcessingActivityCommandHandler(ProcessingInventoryDbContext db)
{
    public async Task<ProcessingActivityDto> HandleAsync(
        CreateProcessingActivityCommand cmd, CancellationToken ct = default)
    {
        var exists = await db.ProcessingActivities
            .AnyAsync(a => a.TenantId == cmd.TenantId &&
                           a.Name.ToLower() == cmd.Name.ToLower(), ct);

        if (exists)
            throw new InvalidOperationException(
                $"Ya existe un tratamiento con nombre '{cmd.Name}' en este tenant.");

        var activity = ProcessingActivity.Create(
            cmd.TenantId, cmd.Name, cmd.CreatedBy,
            cmd.Description, cmd.Controller, cmd.Department);

        db.ProcessingActivities.Add(activity);
        await db.SaveChangesAsync(ct);
        return ProcessingActivityDto.From(activity);
    }
}
