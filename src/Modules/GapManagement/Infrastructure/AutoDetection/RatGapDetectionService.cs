using Evidata.Modules.GapManagement.Application.Abstractions;
using Evidata.Modules.GapManagement.Domain;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.GapManagement.Infrastructure.AutoDetection;

public sealed class RatGapDetectionService : IRatGapDetectionService
{
    private readonly IRatFlagsProvider _flagsProvider;
    private readonly GapManagementDbContext _gapDb;
    private const string SourceModule = "ProcessingInventory";

    public RatGapDetectionService(IRatFlagsProvider flagsProvider, GapManagementDbContext gapDb)
    {
        _flagsProvider = flagsProvider;
        _gapDb = gapDb;
    }

    public async Task<GapDetectionResult> DetectAndCreateGapsAsync(
        Guid tenantId, Guid processingActivityId, Guid triggeredBy, CancellationToken ct = default)
    {
        var flags = await _flagsProvider.GetFlagsAsync(tenantId, processingActivityId, ct);
        var candidates = BuildCandidates(flags);

        var existingTitles = await _gapDb.ComplianceGaps
            .Where(g => g.TenantId == tenantId && g.SourceModule == SourceModule
                     && g.SourceEntityId == processingActivityId && g.Status != GapStatus.Closed)
            .Select(g => g.Title)
            .ToListAsync(ct);

        var created = new List<ComplianceGap>();
        var skipped = new List<string>();

        foreach (var (title, description, severity) in candidates)
        {
            if (existingTitles.Any(t => t == title)) { skipped.Add(title); continue; }
            var gap = ComplianceGap.Create(tenantId, SourceModule, processingActivityId, title, description, severity, triggeredBy);
            _gapDb.ComplianceGaps.Add(gap);
            created.Add(gap);
        }

        if (created.Count > 0) await _gapDb.SaveChangesAsync(ct);
        return new GapDetectionResult(created, skipped);
    }

    private static IReadOnlyList<(string, string, GapSeverity)> BuildCandidates(RatFlagsSnapshot f)
    {
        var list = new List<(string, string, GapSeverity)>();

        if (f.MissingSecurityMeasures)
            list.Add(("Faltan medidas de seguridad para datos sensibles",
                $"El tratamiento '{f.ActivityName}' incluye datos sensibles sin medidas de seguridad declaradas. Requerido por Art. 14 Ley 21.719.",
                GapSeverity.Critical));

        if (f.MissingLegalBasisEvidence)
            list.Add(("Falta evidencia de base de licitud (Consentimiento/Interés Legítimo)",
                $"El tratamiento '{f.ActivityName}' declara Consentimiento o Interés Legítimo sin evidencia documental.",
                GapSeverity.High));

        if (f.MissingRetention)
            list.Add(("Política de retención no declarada",
                $"El tratamiento '{f.ActivityName}' no define período de retención. Art. 14 letra d) Ley 21.719.",
                GapSeverity.Medium));

        if (f.ChildrenData)
            list.Add(("Tratamiento de datos de NNA sin revisión reforzada",
                $"El tratamiento '{f.ActivityName}' incluye datos de NNA. Requiere revisión reforzada.",
                GapSeverity.High));

        if (f.BiometricData)
            list.Add(("Tratamiento de datos biométricos — revisión obligatoria",
                $"El tratamiento '{f.ActivityName}' incluye datos biométricos. Requiere evaluación de impacto.",
                GapSeverity.High));

        if (f.InternationalTransfer)
            list.Add(("Transferencia internacional de datos sin garantías declaradas",
                $"El tratamiento '{f.ActivityName}' declara transferencias internacionales. Art. 25 Ley 21.719.",
                GapSeverity.Medium));

        if (f.AutomatedDecision)
            list.Add(("Decisión automatizada o perfilamiento sin salvaguardas declaradas",
                $"El tratamiento '{f.ActivityName}' incluye decisiones automatizadas sin salvaguardas.",
                GapSeverity.Medium));

        return list;
    }
}
