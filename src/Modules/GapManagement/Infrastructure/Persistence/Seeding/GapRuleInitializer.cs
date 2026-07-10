namespace Evidata.Modules.GapManagement.Infrastructure.Persistence.Seeding;

using Evidata.Modules.GapManagement.Domain;

/// <summary>
/// Inicializa el catálogo de 11 reglas mínimas de detección de brechas.
/// Se ejecuta una sola vez por tenant (idemopotente).
/// </summary>
public static class GapRuleInitializer
{
    /// <summary>
    /// Catálogo de 11 reglas de detección de brechas (contract section 04, lines 162-174).
    /// Cada regla especifica: RuleCode, Severity (Critical/High), BlocksApproval.
    /// IsFullyImplemented indica si la regla está completa en el motor de detección.
    /// </summary>
    public static IReadOnlyList<GapRuleSeed> GetDefaultRules()
        => new[]
        {
            new GapRuleSeed(
                "TRANSFER_WITHOUT_DESTINATION_COUNTRY",
                "Transferencia de datos sin país destino especificado",
                GapSeverity.Critical,
                true,
                "pa-transfer-no-country",
                true,
                "Evalúa si un nodo de transferencia internacional tiene especificado el país destino."),

            new GapRuleSeed(
                "TRANSFER_WITHOUT_RECEIVER",
                "Transferencia sin receptor identificado",
                GapSeverity.High,
                true,
                "pa-transfer-no-receiver",
                true,
                "Verifica que todo nodo de transferencia identifique un receptor o destinatario."),

            new GapRuleSeed(
                "TRANSFER_WITHOUT_SAFEGUARD",
                "Transferencia internacional sin salvaguarda especificada",
                GapSeverity.Critical,
                true,
                "pa-transfer-no-safeguard",
                true,
                "Controla que transferencias internacionales de terceros países cuenten con salvaguarda (Capítulo V RGPD)."),

            new GapRuleSeed(
                "TRANSFER_WITHOUT_BLOCKING_EVIDENCE",
                "Transferencia sin evidencia de aprobación o base legal",
                GapSeverity.Critical,
                true,
                "pa-transfer-no-evidence",
                true,
                "Valida que exista evidencia adjunta que apruebe o justifique la transferencia internacional."),

            new GapRuleSeed(
                "SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW",
                "Dato sensible sin revisión de seguridad",
                GapSeverity.Critical,
                true,
                "pa-sensitive-no-security-review",
                true,
                "Verifica que categorías de datos sensibles tengan validación de evidencia de tipo Security."),

            new GapRuleSeed(
                "LEGAL_BASIS_MISSING",
                "Base legal no especificada",
                GapSeverity.Critical,
                true,
                "pa-no-legal-basis",
                true,
                "Comprueba que todo tratamiento de datos cuente con base legal explícita (consentimiento, contrato, obligación legal, etc.)."),

            new GapRuleSeed(
                "RETENTION_UNDEFINED",
                "Período de retención no definido",
                GapSeverity.High,
                true,
                "pa-retention-undefined",
                false,
                "Requiere especificación de período de retención. Nota: El dominio aún no modela explícitamente 'retention_period_days' en ProcessingActivity."),

            new GapRuleSeed(
                "DATA_CATEGORIES_EMPTY",
                "Sin categorías de datos especificadas",
                GapSeverity.High,
                true,
                "pa-no-data-categories",
                true,
                "Verifica que al menos una categoría de dato esté vinculada al tratamiento."),

            new GapRuleSeed(
                "PURPOSE_UNDEFINED",
                "Propósito del tratamiento no definido",
                GapSeverity.Critical,
                true,
                "pa-purpose-undefined",
                true,
                "Asegura que se especifique claramente el propósito o finalidad del tratamiento de datos."),

            new GapRuleSeed(
                "DATA_SUBJECTS_EMPTY",
                "Sin categorías de interesados especificadas",
                GapSeverity.High,
                true,
                "pa-no-data-subjects",
                true,
                "Comprueba que al menos una categoría de interesado esté identificada en el tratamiento."),

            new GapRuleSeed(
                "SYSTEMS_WITHOUT_OWNER",
                "Sistemas sin propietario identificado",
                GapSeverity.High,
                true,
                "pa-systems-without-owner",
                false,
                "Verifica que todo sistema participante en el tratamiento tenga propietario asignado. Nota: Requiere introspección de nodos que el dominio aún no modeliza completamente.")
        };
}

/// <summary>
/// DTO para semilla de regla de brecha.
/// Se usa para inicializar el catálogo sin perder información contractual.
/// </summary>
public sealed record GapRuleSeed(
    string RuleCode,
    string Description,
    GapSeverity Severity,
    bool BlocksApproval,
    string TestFixtureName,
    bool IsFullyImplemented,
    string? ImplementationNotes);
