using Evidata.Modules.Mcp.Application.CitationVerification;
using Evidata.Modules.Mcp.Domain;
using Evidata.Modules.Documents.Infrastructure.Persistence;
using Evidata.Modules.GapManagement.Infrastructure.Persistence;
using Evidata.Modules.LegalKnowledge.Infrastructure.Persistence;
using Evidata.Modules.ProcessingInventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Evidata.Modules.Mcp.Infrastructure.CitationVerification;

/// <summary>
/// Verifica citaciones consultando el DbContext del módulo correspondiente
/// según el <see cref="McpCitationSourceType"/>.
///
/// Reglas de acceso por tipo:
///   - LegalNorm/LegalSource: existe en LegalKnowledge (global, no requiere tenantId)
///   - TenantDocument: existe en Documents y pertenece al tenant
///   - ProcessingActivity: existe en ProcessingInventory y pertenece al tenant
///   - ComplianceGap: existe en GapManagement y pertenece al tenant
///   - Other: siempre Unverifiable
/// </summary>
public sealed class McpCitationVerifier : IMcpCitationVerifier
{
    private readonly DocumentDbContext _documents;
    private readonly GapManagementDbContext _gaps;
    private readonly LegalKnowledgeDbContext _legal;
    private readonly ProcessingInventoryDbContext _rat;

    public McpCitationVerifier(
        DocumentDbContext documents,
        GapManagementDbContext gaps,
        LegalKnowledgeDbContext legal,
        ProcessingInventoryDbContext rat)
    {
        _documents = documents;
        _gaps = gaps;
        _legal = legal;
        _rat = rat;
    }

    public async Task<CitationVerificationReport> VerifyAsync(
        Guid interactionId,
        Guid tenantId,
        IReadOnlyList<McpCitation> citations,
        CancellationToken ct = default)
    {
        var results = new List<CitationVerificationResult>(citations.Count);

        foreach (var citation in citations)
        {
            var result = await VerifySingleAsync(citation, tenantId, ct);
            results.Add(result);
        }

        return new CitationVerificationReport(interactionId, results);
    }

    private async Task<CitationVerificationResult> VerifySingleAsync(
        McpCitation citation, Guid tenantId, CancellationToken ct)
    {
        if (!Guid.TryParse(citation.SourceId, out var sourceGuid))
        {
            return new CitationVerificationResult(
                citation.Id,
                citation.SourceType,
                citation.SourceId,
                CitationVerificationStatus.NotFound,
                "El SourceId no es un GUID válido.");
        }

        return citation.SourceType switch
        {
            McpCitationSourceType.LegalNorm =>
                await VerifyLegalNormAsync(citation, sourceGuid, ct),

            McpCitationSourceType.TenantDocument =>
                await VerifyTenantDocumentAsync(citation, sourceGuid, tenantId, ct),

            McpCitationSourceType.ProcessingActivity =>
                await VerifyProcessingActivityAsync(citation, sourceGuid, tenantId, ct),

            McpCitationSourceType.ComplianceGap =>
                await VerifyComplianceGapAsync(citation, sourceGuid, tenantId, ct),

            McpCitationSourceType.Other or _ =>
                new CitationVerificationResult(
                    citation.Id,
                    citation.SourceType,
                    citation.SourceId,
                    CitationVerificationStatus.Unverifiable,
                    "Tipo de fuente 'Other' no puede verificarse automáticamente.")
        };
    }

    private async Task<CitationVerificationResult> VerifyLegalNormAsync(
        McpCitation citation, Guid sourceGuid, CancellationToken ct)
    {
        var exists = await _legal.LegalSources
            .AsNoTracking()
            .AnyAsync(s => s.Id == sourceGuid, ct);

        return exists
            ? Ok(citation)
            : NotFound(citation, "Norma legal no encontrada en el catálogo.");
    }

    private async Task<CitationVerificationResult> VerifyTenantDocumentAsync(
        McpCitation citation, Guid sourceGuid, Guid tenantId, CancellationToken ct)
    {
        var exists = await _documents.Documents
            .AsNoTracking()
            .AnyAsync(d => d.Id == sourceGuid && d.TenantId == tenantId, ct);

        return exists
            ? Ok(citation)
            : NotFound(citation, "Documento no encontrado o no pertenece al tenant.");
    }

    private async Task<CitationVerificationResult> VerifyProcessingActivityAsync(
        McpCitation citation, Guid sourceGuid, Guid tenantId, CancellationToken ct)
    {
        var exists = await _rat.ProcessingActivities
            .AsNoTracking()
            .AnyAsync(a => a.Id == sourceGuid && a.TenantId == tenantId, ct);

        return exists
            ? Ok(citation)
            : NotFound(citation, "Tratamiento RAT no encontrado o no pertenece al tenant.");
    }

    private async Task<CitationVerificationResult> VerifyComplianceGapAsync(
        McpCitation citation, Guid sourceGuid, Guid tenantId, CancellationToken ct)
    {
        var exists = await _gaps.ComplianceGaps
            .AsNoTracking()
            .AnyAsync(g => g.Id == sourceGuid && g.TenantId == tenantId, ct);

        return exists
            ? Ok(citation)
            : NotFound(citation, "Brecha de cumplimiento no encontrada o no pertenece al tenant.");
    }

    private static CitationVerificationResult Ok(McpCitation c) =>
        new(c.Id, c.SourceType, c.SourceId, CitationVerificationStatus.Verified, null);

    private static CitationVerificationResult NotFound(McpCitation c, string reason) =>
        new(c.Id, c.SourceType, c.SourceId, CitationVerificationStatus.NotFound, reason);
}
