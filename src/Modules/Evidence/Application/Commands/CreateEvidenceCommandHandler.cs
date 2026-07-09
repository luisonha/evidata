using Evidata.Modules.Evidence.Application.Queries;
using Evidata.Modules.Evidence.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Evidence.Application.Commands;

public sealed record CreateEvidenceCommand(
    Guid TenantId, string Title, string Type, string Sensitivity,
    string? Description, string? Tags, Guid CreatedBy);

public sealed class CreateEvidenceCommandHandler(EvidenceDbContext db)
{
    public async Task<EvidenceDto> HandleAsync(CreateEvidenceCommand cmd, CancellationToken ct = default)
    {
        if (!Enum.TryParse<Domain.EvidenceType>(cmd.Type, ignoreCase: true, out var type))
            throw new ArgumentException($"Tipo de evidencia inválido: {cmd.Type}");
        if (!Enum.TryParse<Domain.EvidenceSensitivity>(cmd.Sensitivity, ignoreCase: true, out var sensitivity))
            throw new ArgumentException($"Sensibilidad inválida: {cmd.Sensitivity}");

        var evidence = Domain.Evidence.Create(
            cmd.TenantId, cmd.Title, type, sensitivity, cmd.CreatedBy,
            cmd.Description, cmd.Tags);

        db.Evidences.Add(evidence);
        await db.SaveChangesAsync(ct);
        return EvidenceDto.From(evidence);
    }
}
