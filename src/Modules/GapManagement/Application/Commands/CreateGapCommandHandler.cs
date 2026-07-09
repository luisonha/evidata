using Evidata.Modules.GapManagement.Application.Queries;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;

namespace Evidata.Modules.GapManagement.Application.Commands;

public sealed record CreateGapCommand(
    Guid TenantId, string SourceModule, Guid SourceEntityId,
    string Title, string Description, string Severity,
    Guid CreatedBy, Guid? LegalObligationId = null, DateTimeOffset? DueAt = null);

public sealed class CreateGapCommandHandler(GapManagementDbContext db)
{
    public async Task<ComplianceGapDto> HandleAsync(CreateGapCommand cmd, CancellationToken ct = default)
    {
        if (!Enum.TryParse<GapSeverity>(cmd.Severity, ignoreCase: true, out var severity))
            throw new ArgumentException($"Severidad inválida: {cmd.Severity}");

        var gap = ComplianceGap.Create(
            cmd.TenantId, cmd.SourceModule, cmd.SourceEntityId,
            cmd.Title, cmd.Description, severity, cmd.CreatedBy,
            cmd.LegalObligationId, cmd.DueAt);

        db.ComplianceGaps.Add(gap);
        await db.SaveChangesAsync(ct);
        return ComplianceGapDto.From(gap);
    }
}
