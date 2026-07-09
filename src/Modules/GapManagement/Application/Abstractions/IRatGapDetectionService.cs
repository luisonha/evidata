using Evidata.Modules.GapManagement.Domain;

namespace Evidata.Modules.GapManagement.Application.Abstractions;

/// <summary>
/// Resultado del análisis automático de brechas de un tratamiento RAT.
/// </summary>
/// <param name="CreatedGaps">Brechas nuevas creadas en esta ejecución.</param>
/// <param name="SkippedFlags">Flags detectados pero cuya brecha ya existía (dedup).</param>
public sealed record GapDetectionResult(
    IReadOnlyList<ComplianceGap> CreatedGaps,
    IReadOnlyList<string> SkippedFlags);

/// <summary>
/// Detecta automáticamente brechas de cumplimiento a partir de los flags
/// de riesgo de un tratamiento RAT (<see cref="ProcessingInventory"/>).
///
/// Reglas:
/// - Se analiza cada flag bloqueante / de riesgo elevado.
/// - Solo se crea una brecha nueva si no existe ya una activa para el mismo
///   flag + tratamiento (dedup por SourceModule + SourceEntityId + Title prefix).
/// - La detección es idempotente: ejecutarla varias veces no duplica brechas.
/// </summary>
public interface IRatGapDetectionService
{
    /// <summary>
    /// Analiza los flags de un tratamiento y crea las brechas que falten.
    /// </summary>
    Task<GapDetectionResult> DetectAndCreateGapsAsync(
        Guid tenantId,
        Guid processingActivityId,
        Guid triggeredBy,
        CancellationToken ct = default);
}
