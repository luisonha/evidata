namespace Evidata.Modules.Evidence.Domain;

/// <summary>
/// Registro de auditoría de cada descarga de evidencia.
/// Append-only: nunca se modifica ni elimina.
///
/// Regla (doc 15, sec 8.5): evidencia Sensitive requiere motivo para descarga.
/// </summary>
public class EvidenceAccessLog
{
    private EvidenceAccessLog() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid EvidenceId { get; private set; }

    /// <summary>Usuario que realizó la descarga.</summary>
    public Guid AccessedBy { get; private set; }

    public DateTimeOffset AccessedAt { get; private set; }

    /// <summary>Motivo de la descarga (obligatorio para sensibilidad Sensitive).</summary>
    public string? Reason { get; private set; }

    /// <summary>IP del cliente (puede ser null si no se captura en el contexto).</summary>
    public string? ClientIp { get; private set; }

    /// <summary>User-Agent del cliente.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>Correlation ID para trazabilidad cross-servicio.</summary>
    public string? CorrelationId { get; private set; }

    /// <summary>Sensibilidad de la evidencia en el momento de la descarga (inmutable por diseño).</summary>
    public EvidenceSensitivity SensitivityAtAccess { get; private set; }

    // Navigation (read-only reference)
    public Evidence Evidence { get; private set; } = default!;

    // ── Factory ────────────────────────────────────────────────────────────────

    public static EvidenceAccessLog Record(
        Guid tenantId,
        Guid evidenceId,
        Guid accessedBy,
        EvidenceSensitivity sensitivity,
        string? reason = null,
        string? clientIp = null,
        string? userAgent = null,
        string? correlationId = null)
    {
        if (sensitivity == EvidenceSensitivity.Sensitive && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException(
                "El motivo de descarga es obligatorio para evidencia de sensibilidad Sensitive.");

        return new EvidenceAccessLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EvidenceId = evidenceId,
            AccessedBy = accessedBy,
            AccessedAt = DateTimeOffset.UtcNow,
            SensitivityAtAccess = sensitivity,
            Reason = reason?.Trim(),
            ClientIp = clientIp,
            UserAgent = userAgent?.Length > 500 ? userAgent[..500] : userAgent,
            CorrelationId = correlationId
        };
    }
}
