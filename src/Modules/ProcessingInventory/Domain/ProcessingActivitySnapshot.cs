using System.Text.Json;

namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Copia inmutable de un <see cref="ProcessingActivity"/> en el momento de su aprobación.
///
/// Reglas (doc 15, sec 12 — Versionado):
/// - Se crea automáticamente cuando un tratamiento pasa a Approved.
/// - Nunca se modifica después de creada.
/// - Sirve como registro de auditoría y base para comparar versiones futuras.
/// - El campo <see cref="Payload"/> almacena el estado completo serializado en JSON.
/// </summary>
public sealed class ProcessingActivitySnapshot
{
    private ProcessingActivitySnapshot() { } // EF Core

    public Guid Id { get; private init; }
    public Guid TenantId { get; private init; }

    /// <summary>ID del tratamiento origen.</summary>
    public Guid ActivityId { get; private init; }

    /// <summary>Número de versión del tratamiento al momento del snapshot.</summary>
    public int Version { get; private init; }

    public Guid ApprovedBy { get; private init; }
    public DateTimeOffset ApprovedAt { get; private init; }

    /// <summary>
    /// Estado completo del tratamiento en JSON en el momento de la aprobación.
    /// Inmutable — nunca modificado post-creación.
    /// </summary>
    public string Payload { get; private init; } = default!;

    // ── Factory ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Crea un snapshot a partir del estado actual de un <see cref="ProcessingActivity"/> Approved.
    /// </summary>
    public static ProcessingActivitySnapshot TakeFrom(ProcessingActivity activity)
    {
        ArgumentNullException.ThrowIfNull(activity);

        if (activity.ApprovedBy is null || activity.ApprovedAt is null)
            throw new InvalidOperationException(
                "Solo se puede crear un snapshot de un tratamiento aprobado.");

        return new ProcessingActivitySnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = activity.TenantId,
            ActivityId = activity.Id,
            Version = activity.Version,
            ApprovedBy = activity.ApprovedBy.Value,
            ApprovedAt = activity.ApprovedAt.Value,
            Payload = JsonSerializer.Serialize(SnapshotPayload.From(activity))
        };
    }
}

/// <summary>
/// DTO interno que define qué campos se serializar en el payload del snapshot.
/// </summary>
internal sealed record SnapshotPayload(
    Guid ActivityId,
    Guid TenantId,
    int Version,
    string Name,
    string? Description,
    string? Controller,
    string? Department,
    object? Purpose,
    IReadOnlyList<DataCategoryEntry> DataCategories,
    IReadOnlyList<DataSubjectEntry> DataSubjects,
    IReadOnlyList<SystemEntry> Systems,
    IReadOnlyList<SupplierEntry> Suppliers,
    object? Retention,
    IReadOnlyList<SecurityMeasureEntry> SecurityMeasures,
    bool HasInternationalTransfer,
    bool HasAutomatedDecision,
    object Flags,
    Guid? SupersedesId,
    DateTimeOffset ApprovedAt,
    Guid ApprovedBy)
{
    internal static SnapshotPayload From(ProcessingActivity a) => new(
        a.Id, a.TenantId, a.Version, a.Name, a.Description, a.Controller, a.Department,
        a.Purpose, a.DataCategories, a.DataSubjects, a.Systems, a.Suppliers,
        a.Retention, a.SecurityMeasures,
        a.HasInternationalTransfer, a.HasAutomatedDecision,
        a.Flags, a.SupersedesId, a.ApprovedAt!.Value, a.ApprovedBy!.Value);
}
