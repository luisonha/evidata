using Evidata.Modules.ProcessingInventory.Domain;

namespace Evidata.Modules.ProcessingInventory.Application.Queries;

public sealed record ProcessingActivityDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Description,
    string? Controller,
    string? Department,
    string Status,
    int Version,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastModifiedAt,
    Guid? ApprovedBy,
    DateTimeOffset? ApprovedAt)
{
    public static ProcessingActivityDto From(ProcessingActivity a) => new(
        a.Id, a.TenantId, a.Name, a.Description, a.Controller, a.Department,
        a.Status.ToString(), a.Version, a.CreatedBy, a.CreatedAt,
        a.LastModifiedAt, a.ApprovedBy, a.ApprovedAt);
}
