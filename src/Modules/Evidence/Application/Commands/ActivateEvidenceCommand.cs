namespace Evidata.Modules.Evidence.Application.Commands;

/// <summary>
/// Command to activate an Evidence from Draft state.
/// Implements AUD-ACT-001: audit logging of activation.
/// </summary>
public sealed record ActivateEvidenceCommand(
    Guid TenantId,
    Guid EvidenceId,
    Guid ActivatedBy);
