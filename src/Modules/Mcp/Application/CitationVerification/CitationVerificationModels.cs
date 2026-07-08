using Evidata.Modules.Mcp.Domain;

namespace Evidata.Modules.Mcp.Application.CitationVerification;

/// <summary>Estado de la verificación de una citación.</summary>
public enum CitationVerificationStatus
{
    /// <summary>La fuente existe y pertenece al tenant.</summary>
    Verified,

    /// <summary>La fuente no fue encontrada (ID inválido o no pertenece al tenant).</summary>
    NotFound,

    /// <summary>El tipo de fuente no puede verificarse automáticamente.</summary>
    Unverifiable
}

/// <summary>Resultado de la verificación de una citación individual.</summary>
public sealed record CitationVerificationResult(
    Guid CitationId,
    McpCitationSourceType SourceType,
    string SourceId,
    CitationVerificationStatus Status,
    string? Reason);

/// <summary>Reporte completo de verificación de las citaciones de una interacción.</summary>
public sealed record CitationVerificationReport(
    Guid InteractionId,
    IReadOnlyList<CitationVerificationResult> Results)
{
    public int VerifiedCount => Results.Count(r => r.Status == CitationVerificationStatus.Verified);
    public int NotFoundCount => Results.Count(r => r.Status == CitationVerificationStatus.NotFound);
    public int UnverifiableCount => Results.Count(r => r.Status == CitationVerificationStatus.Unverifiable);

    /// <summary>True si todas las citaciones verificables están verificadas (ninguna NotFound).</summary>
    public bool IsFullyVerified => NotFoundCount == 0;
}
