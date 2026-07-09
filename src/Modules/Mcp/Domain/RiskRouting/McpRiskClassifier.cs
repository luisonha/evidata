namespace Evidata.Modules.Mcp.Domain.RiskRouting;

/// <summary>
/// Clasificador de riesgo para consultas MCP basado en señales de texto.
///
/// Reglas (en orden de evaluación — el primer match gana):
///   1. Señales de abstención → High + abstain (consultas que el asistente no debe responder)
///   2. Señales High → High sin abstención (responder con revisión HITL)
///   3. Señales Medium → Medium (responder con advertencia)
///   4. Sin señales → Low
///
/// Las señales son case-insensitive y se aplican sobre la pregunta completa.
/// Esta implementación es determinista y no requiere llamada a modelo externo.
/// </summary>
public sealed class McpRiskClassifier
{
    // ── Señales de abstención ─────────────────────────────────────────────────
    // Consultas que implican asesoría legal concreta, decisiones judiciales o
    // datos sensibles de terceros que el asistente no puede ni debe emitir.

    private static readonly string[] AbstentionSignals =
    [
        "demanda", "juicio", "querella", "acción judicial", "recurso de amparo",
        "recurso de protección", "denuncia penal", "sanción específica",
        "multa exacta", "cuánto me multarán", "me pueden multar",
        "contraseña", "password", "credencial", "token de acceso",
        "datos de otro usuario", "datos de tercero", "información personal de",
        "rut de", "cédula de", "datos bancarios"
    ];

    // ── Señales de riesgo alto ────────────────────────────────────────────────
    // Consultas sobre obligaciones legales específicas, transferencias internacionales
    // o brechas críticas que requieren revisión humana antes de actuar.

    private static readonly string[] HighRiskSignals =
    [
        "transferencia internacional", "transferencia de datos", "país tercero",
        "encargado de tratamiento", "subencargado",
        "brecha de seguridad", "incidente de datos", "notificar al cmf",
        "notificar a la cmf", "notificar autoridad", "plazo de notificación",
        "datos sensibles", "dato sensible", "categoría especial",
        "salud", "origen étnico", "orientación sexual", "biometría",
        "menor de edad", "datos de niños", "menores"
    ];

    // ── Señales de riesgo medio ───────────────────────────────────────────────
    // Consultas interpretativas que pueden inducir a error pero no tienen
    // consecuencias inmediatas críticas.

    private static readonly string[] MediumRiskSignals =
    [
        "plazo", "período de retención", "retención de datos", "cuánto tiempo",
        "tiempo máximo", "base legal", "consentimiento", "interés legítimo",
        "derecho arco", "derecho de acceso", "derecho de supresión",
        "titular de datos", "responsable del tratamiento",
        "política de privacidad", "aviso de privacidad",
        "ley 21719", "ley 19628", "rgpd", "gdpr"
    ];

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Clasifica una consulta y retorna el nivel de riesgo con señales detectadas.</summary>
    public RiskClassificationResult Classify(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
            return RiskClassificationResult.Low();

        var lower = question.ToLowerInvariant();

        var abstentionMatches = AbstentionSignals.Where(s => lower.Contains(s)).ToList();
        if (abstentionMatches.Count > 0)
            return RiskClassificationResult.Abstain(
                "La consulta requiere asesoría legal especializada que el asistente no puede proporcionar.",
                abstentionMatches);

        var highMatches = HighRiskSignals.Where(s => lower.Contains(s)).ToList();
        if (highMatches.Count > 0)
            return RiskClassificationResult.High(highMatches);

        var mediumMatches = MediumRiskSignals.Where(s => lower.Contains(s)).ToList();
        if (mediumMatches.Count > 0)
            return RiskClassificationResult.Medium(mediumMatches);

        return RiskClassificationResult.Low();
    }
}
