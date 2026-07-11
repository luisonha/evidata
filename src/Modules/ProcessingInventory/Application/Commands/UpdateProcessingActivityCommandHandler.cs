using Evidata.Modules.ProcessingInventory.Application.Queries;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.ProcessingInventory.Application.Commands;

public sealed record UpdateProcessingActivityCommand(
    Guid TenantId,
    Guid ProcessingActivityId,
    Guid ModifiedBy,
    string? Name = null,
    string? Description = null,
    string? Controller = null,
    string? Department = null);

public enum UpdateProcessingActivityOutcome
{
    Success,
    NotFound,
    Conflict,
    UnprocessableEntity
}

public sealed record UpdateProcessingActivityError(
    string Code,
    string LabelKey,
    string? Message);

public sealed record UpdateProcessingActivityResult(
    UpdateProcessingActivityOutcome Outcome,
    ProcessingActivityDto? Activity = null,
    UpdateProcessingActivityError? Error = null)
{
    public static UpdateProcessingActivityResult Success(ProcessingActivityDto activity) =>
        new(UpdateProcessingActivityOutcome.Success, activity);

    public static UpdateProcessingActivityResult NotFound(Guid id) =>
        new(
            UpdateProcessingActivityOutcome.NotFound,
            Error: new UpdateProcessingActivityError(
                "ProcessingActivityNotFound",
                "processingActivity.notFound",
                $"No existe un tratamiento con id '{id}' para este tenant."));

    public static UpdateProcessingActivityResult Conflict(string message) =>
        new(
            UpdateProcessingActivityOutcome.Conflict,
            Error: new UpdateProcessingActivityError(
                "ProcessingActivityNotEditable",
                "processingActivity.notEditable",
                message));

    public static UpdateProcessingActivityResult Unprocessable(string code, string labelKey, string message) =>
        new(
            UpdateProcessingActivityOutcome.UnprocessableEntity,
            Error: new UpdateProcessingActivityError(code, labelKey, message));
}

public sealed class UpdateProcessingActivityCommandHandler(
    ProcessingInventoryDbContext db,
    IAuditService auditService)
{
    public async Task<UpdateProcessingActivityResult> HandleAsync(
        UpdateProcessingActivityCommand cmd,
        CancellationToken ct = default)
    {
        var activity = await db.ProcessingActivities
            .FirstOrDefaultAsync(a => a.TenantId == cmd.TenantId && a.Id == cmd.ProcessingActivityId, ct);

        if (activity is null)
        {
            return UpdateProcessingActivityResult.NotFound(cmd.ProcessingActivityId);
        }

        var name = cmd.Name ?? activity.Name;
        var description = cmd.Description ?? activity.Description;
        var controller = cmd.Controller ?? activity.Controller;
        var department = cmd.Department ?? activity.Department;

        if (cmd.Name is not null)
        {
            var normalizedName = cmd.Name.Trim();
            var nameChanged = !string.Equals(activity.Name, normalizedName, StringComparison.OrdinalIgnoreCase);

            if (nameChanged)
            {
                var exists = await db.ProcessingActivities
                    .AnyAsync(a => a.TenantId == cmd.TenantId &&
                                   a.Id != cmd.ProcessingActivityId &&
                                   a.Name.ToLower() == normalizedName.ToLower(), ct);

                if (exists)
                {
                    return UpdateProcessingActivityResult.Unprocessable(
                        "ProcessingActivityDuplicateName",
                        "processingActivity.duplicateName",
                        $"Ya existe un tratamiento con nombre '{normalizedName}' en este tenant.");
                }
            }
        }

        try
        {
            activity.Update(name, cmd.ModifiedBy, description, controller, department);
        }
        catch (InvalidOperationException ex)
        {
            return UpdateProcessingActivityResult.Conflict(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return UpdateProcessingActivityResult.Unprocessable(
                "ProcessingActivityValidationFailed",
                "processingActivity.validationFailed",
                ex.Message);
        }

        await db.SaveChangesAsync(ct);

        // Audit: UpdateNode (AUD-NODE-001)
        await auditService.LogAsync(
            tenantId: cmd.TenantId,
            userId: cmd.ModifiedBy,
            eventType: AuditEventType.UpdateNode.ToString(),
            resource: "ProcessingActivity",
            resourceId: activity.Id,
            result: AuditEventResult.Success,
            metadata: new Dictionary<string, object?>
            {
                { "name", cmd.Name },
                { "description", cmd.Description },
                { "controller", cmd.Controller },
                { "department", cmd.Department }
            },
            ct: ct);

        return UpdateProcessingActivityResult.Success(ProcessingActivityDto.From(activity));
    }
}
