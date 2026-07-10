namespace Evidata.Modules.ProcessingInventory.Application.Commands;

/// <summary>
/// Command to archive a ProcessingActivity.
/// Implements AUD-ARC-001: audit logging of archive action.
/// </summary>
public sealed record ArchiveCommand(
    Guid TenantId,
    Guid ProcessingActivityId,
    Guid ArchivedBy);
