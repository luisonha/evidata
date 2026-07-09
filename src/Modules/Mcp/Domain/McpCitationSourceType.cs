namespace Evidata.Modules.Mcp.Domain;

/// <summary>
/// Fuente de una citación MCP.
/// </summary>
public enum McpCitationSourceType
{
    /// <summary>Norma o regulación legal (ej: Ley 21.719).</summary>
    LegalNorm,

    /// <summary>Documento interno del tenant.</summary>
    TenantDocument,

    /// <summary>Registro de tratamiento (RAT).</summary>
    ProcessingActivity,

    /// <summary>Brecha de cumplimiento.</summary>
    ComplianceGap,

    /// <summary>Otra fuente no categorizada.</summary>
    Other
}
