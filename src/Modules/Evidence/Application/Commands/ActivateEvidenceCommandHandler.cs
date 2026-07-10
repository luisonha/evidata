using Evidata.Modules.Evidence.Domain;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Audit.Application.Abstractions;
using Evidata.Modules.Audit.Domain;
using Evidata.Modules.Identity.Infrastructure.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Application.Commands;

/// <summary>
/// Handler for ActivateEvidenceCommand.
/// 
/// P1-011c: Instruments audit logging for Activate action (AUD-ACT-001).
/// - Validates the Evidence exists and is in Draft state
/// - Transitions from Draft to Active
/// - Logs audit event with correlationId, result (Success/Failure), and structured metadata
/// - No sensitive data in metadata (only IDs and types)
/// </summary>
public sealed class ActivateEvidenceCommandHandler(
    EvidenceDbContext db,
    IAuditService auditService,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<EvidenceDto> HandleAsync(
        ActivateEvidenceCommand cmd, CancellationToken ct = default)
    {
        var evidence = await db.Evidences
            .FirstOrDefaultAsync(e => e.TenantId == cmd.TenantId && e.Id == cmd.EvidenceId, ct)
            ?? throw new InvalidOperationException(
                $"Evidence {cmd.EvidenceId} not found in tenant {cmd.TenantId}");

        var correlationId = httpContextAccessor.HttpContext?.GetCorrelationId();
        var metadata = new Dictionary<string, object?>
        {
            { "evidenceType", evidence.Type.ToString() },
            { "evidenceSensitivity", evidence.Sensitivity.ToString() },
            { "currentStatus", evidence.Status.ToString() },
            { "hasBlobPath", !string.IsNullOrEmpty(evidence.BlobPath) }
        };

        try
        {
            // Attempt to activate
            // This will throw if not in Draft state
            evidence.Activate(cmd.ActivatedBy);

            db.Evidences.Update(evidence);
            await db.SaveChangesAsync(ct);

            // Audit: Activate (AUD-ACT-001) — Success
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ActivatedBy,
                AuditEventType.Activate.ToString(),
                "Evidence",
                cmd.EvidenceId,
                AuditEventResult.Success,
                correlationId,
                metadata,
                ct: ct);

            return EvidenceDto.From(evidence);
        }
        catch (InvalidOperationException ex)
        {
            // Activation failed (e.g., not in Draft state)
            metadata["errorMessage"] = ex.Message;
            
            await auditService.LogAsync(
                cmd.TenantId,
                cmd.ActivatedBy,
                AuditEventType.Activate.ToString(),
                "Evidence",
                cmd.EvidenceId,
                AuditEventResult.Failure,
                correlationId,
                metadata,
                ct: ct);

            throw;
        }
    }
}
