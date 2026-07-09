using System.Diagnostics.Metrics;

namespace Evidata.ServiceDefaults;

/// <summary>
/// Métricas de negocio Evidata expuestas como instrumentos OpenTelemetry.
///
/// Se registran automáticamente cuando se llama a <see cref="Extensions.ConfigureOpenTelemetry"/>.
/// Azure Monitor las recolecta vía el exportador OTLP configurado en producción.
///
/// Nomenclatura: evidata_{dominio}_{accion}_{unidad}
/// </summary>
public sealed class EvidataMetrics : IDisposable
{
    public const string MeterName = "Evidata";

    private readonly Meter _meter;

    // ── MCP ───────────────────────────────────────────────────────────────────
    private readonly Counter<long> _mcpQueriesTotal;
    private readonly Counter<long> _mcpErrorsTotal;
    private readonly Counter<long> _mcpHighRiskTotal;
    private readonly Counter<long> _mcpHitlTasksCreatedTotal;

    // ── Documentos ────────────────────────────────────────────────────────────
    private readonly Counter<long> _documentsProcessedTotal;
    private readonly Counter<long> _documentsIndexedTotal;
    private readonly Counter<long> _documentDownloadsTotal;

    // ── Reporting ─────────────────────────────────────────────────────────────
    private readonly Counter<long> _reportsGeneratedTotal;

    // ── RAT ───────────────────────────────────────────────────────────────────
    private readonly Counter<long> _ratApprovedTotal;
    private readonly Counter<long> _ratIndexedTotal;

    // ── Autorización ──────────────────────────────────────────────────────────
    private readonly Counter<long> _authorizationFailuresTotal;
    private readonly Counter<long> _crossTenantAttemptsTotal;

    public EvidataMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create(MeterName);

        _mcpQueriesTotal = _meter.CreateCounter<long>(
            "evidata_mcp_queries_total",
            description: "Total de consultas al asistente MCP");
        _mcpErrorsTotal = _meter.CreateCounter<long>(
            "evidata_mcp_errors_total",
            description: "Total de errores del asistente MCP");
        _mcpHighRiskTotal = _meter.CreateCounter<long>(
            "evidata_mcp_high_risk_total",
            description: "Consultas MCP clasificadas como riesgo alto");
        _mcpHitlTasksCreatedTotal = _meter.CreateCounter<long>(
            "evidata_mcp_hitl_tasks_created_total",
            description: "Tareas de revisión humana creadas por el sistema MCP");

        _documentsProcessedTotal = _meter.CreateCounter<long>(
            "evidata_documents_processed_total",
            description: "Documentos procesados (OCR/extracción completados)");
        _documentsIndexedTotal = _meter.CreateCounter<long>(
            "evidata_documents_indexed_total",
            description: "Documentos indexados en el catálogo de búsqueda");
        _documentDownloadsTotal = _meter.CreateCounter<long>(
            "evidata_document_downloads_total",
            description: "Descargas de evidencia/documentos");

        _reportsGeneratedTotal = _meter.CreateCounter<long>(
            "evidata_reports_generated_total",
            description: "Reportes Excel generados (RAT y Brechas)");

        _ratApprovedTotal = _meter.CreateCounter<long>(
            "evidata_rat_approved_total",
            description: "Tratamientos RAT aprobados");
        _ratIndexedTotal = _meter.CreateCounter<long>(
            "evidata_rat_indexed_total",
            description: "Tratamientos RAT indexados en búsqueda");

        _authorizationFailuresTotal = _meter.CreateCounter<long>(
            "evidata_authorization_failures_total",
            description: "Fallas de autorización (403/401)");
        _crossTenantAttemptsTotal = _meter.CreateCounter<long>(
            "evidata_cross_tenant_attempts_total",
            description: "Intentos de acceso cross-tenant detectados");
    }

    // ── MCP ───────────────────────────────────────────────────────────────────

    /// <param name="riskLevel">Low | Medium | High</param>
    public void McpQueryRecorded(Guid tenantId, string riskLevel)
    {
        _mcpQueriesTotal.Add(1,
            new KeyValuePair<string, object?>("risk_level", riskLevel));
        if (riskLevel is "High")
            _mcpHighRiskTotal.Add(1);
    }

    public void McpErrorRecorded(Guid tenantId) =>
        _mcpErrorsTotal.Add(1);

    public void McpHitlTaskCreated(Guid tenantId) =>
        _mcpHitlTasksCreatedTotal.Add(1);

    // ── Documentos ────────────────────────────────────────────────────────────

    public void DocumentProcessed(Guid tenantId) =>
        _documentsProcessedTotal.Add(1);

    public void DocumentIndexed(Guid tenantId) =>
        _documentsIndexedTotal.Add(1);

    public void DocumentDownloaded(Guid tenantId) =>
        _documentDownloadsTotal.Add(1);

    // ── Reporting ─────────────────────────────────────────────────────────────

    /// <param name="reportType">RAT | Gaps</param>
    public void ReportGenerated(Guid tenantId, string reportType) =>
        _reportsGeneratedTotal.Add(1,
            new KeyValuePair<string, object?>("report_type", reportType));

    // ── RAT ───────────────────────────────────────────────────────────────────

    public void RatApproved(Guid tenantId) =>
        _ratApprovedTotal.Add(1);

    public void RatIndexed(Guid tenantId) =>
        _ratIndexedTotal.Add(1);

    // ── Autorización ──────────────────────────────────────────────────────────

    public void AuthorizationFailure(Guid tenantId) =>
        _authorizationFailuresTotal.Add(1);

    public void CrossTenantAttempt(Guid tenantId) =>
        _crossTenantAttemptsTotal.Add(1);

    public void Dispose() => _meter.Dispose();
}
