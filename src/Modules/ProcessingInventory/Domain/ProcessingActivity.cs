namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Estado del ciclo de vida de un tratamiento RAT (doc 15, sec 11).
/// Draft → UnderReview → Approved → Archived
/// Draft también puede ir a Archived directamente (descarte).
/// </summary>
public enum ProcessingActivityStatus
{
    Draft,
    UnderReview,
    Approved,
    Archived
}

/// <summary>
/// Registro de Actividad de Tratamiento (RAT) — entidad central de la Fase 5.
///
/// Modela un tratamiento de datos personales conforme a Ley 21.719 (Chile)
/// y al Artículo 30 GDPR (compatibilidad con estándares internacionales).
///
/// Este objeto cubre la estructura base + ciclo de vida. Las secciones de detalle
/// (finalidad, base de licitud, categorías, sistemas, etc.) se agregan en tareas posteriores
/// como value objects embebidos o entidades relacionadas.
///
/// Reglas:
/// - Solo el tenant propietario puede ver/editar su RAT.
/// - La aprobación requiere completitud (validada antes de pasar a UnderReview).
/// - Los RAT aprobados son inmutables; para cambios se crea una nueva versión (f5-rat-versionado).
/// - Nombre único por tenant (case-insensitive).
/// </summary>
public class ProcessingActivity
{
    private ProcessingActivity() { } // EF Core

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }

    /// <summary>Nombre descriptivo del tratamiento (único por tenant).</summary>
    public string Name { get; private set; } = default!;

    /// <summary>Descripción general del tratamiento.</summary>
    public string? Description { get; private set; }

    /// <summary>Responsable del tratamiento dentro de la organización.</summary>
    public string? Controller { get; private set; }

    /// <summary>Departamento u área responsable.</summary>
    public string? Department { get; private set; }

    public ProcessingActivityStatus Status { get; private set; }

    /// <summary>Número de versión — 1 para nuevo, incrementa con cada re-aprobación.</summary>
    public int Version { get; private set; }

    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Guid? LastModifiedBy { get; private set; }
    public DateTimeOffset? LastModifiedAt { get; private set; }

    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }

    /// <summary>ID del tratamiento anterior al que esta versión reemplaza (null si es la primera).</summary>
    public Guid? SupersedesId { get; private set; }

    // ── Secciones (owned types / JSONB) ────────────────────────────────────────

    /// <summary>Sección Finalidad y Base de Licitud (obligatoria para aprobación).</summary>
    public PurposeSection? Purpose { get; private set; }

    /// <summary>Categorías de datos tratados (al menos una requerida para aprobación).</summary>
    public IReadOnlyList<DataCategoryEntry> DataCategories => _dataCategories.AsReadOnly();
    private readonly List<DataCategoryEntry> _dataCategories = [];

    /// <summary>Tipos de titulares afectados (al menos uno requerido para aprobación).</summary>
    public IReadOnlyList<DataSubjectEntry> DataSubjects => _dataSubjects.AsReadOnly();
    private readonly List<DataSubjectEntry> _dataSubjects = [];

    // ── Factory ────────────────────────────────────────────────────────────────

    public static ProcessingActivity Create(
        Guid tenantId,
        string name,
        Guid createdBy,
        string? description = null,
        string? controller = null,
        string? department = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del tratamiento no puede ser vacío.", nameof(name));
        if (name.Length > 300)
            throw new ArgumentException("El nombre no puede superar 300 caracteres.", nameof(name));

        return new ProcessingActivity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            Controller = controller?.Trim(),
            Department = department?.Trim(),
            Status = ProcessingActivityStatus.Draft,
            Version = 1,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    // ── Edición ────────────────────────────────────────────────────────────────

    /// <summary>Actualiza campos editables. Solo válido en estado Draft.</summary>
    public void Update(
        string name,
        Guid modifiedBy,
        string? description = null,
        string? controller = null,
        string? department = null)
    {
        GuardEditableState();

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre no puede ser vacío.", nameof(name));
        if (name.Length > 300)
            throw new ArgumentException("El nombre no puede superar 300 caracteres.", nameof(name));

        Name = name.Trim();
        Description = description?.Trim();
        Controller = controller?.Trim();
        Department = department?.Trim();
        Touch(modifiedBy);
    }

    // ── FSM ────────────────────────────────────────────────────────────────────

    public void SubmitForReview(Guid modifiedBy)
    {
        if (Status != ProcessingActivityStatus.Draft)
            throw new InvalidOperationException(
                $"Solo se puede enviar a revisión desde Draft. Estado actual: {Status}.");

        Status = ProcessingActivityStatus.UnderReview;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid approvedBy)
    {
        if (Status != ProcessingActivityStatus.UnderReview)
            throw new InvalidOperationException(
                $"Solo se puede aprobar desde UnderReview. Estado actual: {Status}.");

        Status = ProcessingActivityStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAt = DateTimeOffset.UtcNow;
    }

    public void ReturnToDraft(Guid modifiedBy)
    {
        if (Status != ProcessingActivityStatus.UnderReview)
            throw new InvalidOperationException(
                $"Solo se puede devolver a Draft desde UnderReview. Estado actual: {Status}.");

        Status = ProcessingActivityStatus.Draft;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Archive(Guid modifiedBy)
    {
        if (Status == ProcessingActivityStatus.Archived)
            throw new InvalidOperationException("El tratamiento ya está archivado.");

        Status = ProcessingActivityStatus.Archived;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    // ── Sección: Finalidad ─────────────────────────────────────────────────────

    public void SetPurpose(PurposeSection purpose, Guid modifiedBy)
    {
        GuardEditableState();
        ArgumentNullException.ThrowIfNull(purpose);
        Purpose = purpose;
        Touch(modifiedBy);
    }

    // ── Sección: Categorías de datos ───────────────────────────────────────────

    public void SetDataCategories(IEnumerable<DataCategoryEntry> entries, Guid modifiedBy)
    {
        GuardEditableState();
        var list = entries?.ToList() ?? [];
        if (list.Count == 0)
            throw new ArgumentException("Debe indicarse al menos una categoría de datos.", nameof(entries));

        _dataCategories.Clear();
        _dataCategories.AddRange(list);
        Touch(modifiedBy);
    }

    // ── Sección: Titulares ─────────────────────────────────────────────────────

    public void SetDataSubjects(IEnumerable<DataSubjectEntry> entries, Guid modifiedBy)
    {
        GuardEditableState();
        var list = entries?.ToList() ?? [];
        if (list.Count == 0)
            throw new ArgumentException("Debe indicarse al menos un tipo de titular.", nameof(entries));

        _dataSubjects.Clear();
        _dataSubjects.AddRange(list);
        Touch(modifiedBy);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void Touch(Guid modifiedBy)
    {
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    private void GuardEditableState()
    {
        if (Status == ProcessingActivityStatus.Approved)
            throw new InvalidOperationException(
                "Un tratamiento aprobado no puede modificarse. Cree una nueva versión.");
        if (Status == ProcessingActivityStatus.Archived)
            throw new InvalidOperationException("Un tratamiento archivado no puede modificarse.");
    }

    public bool IsEditable =>
        Status is ProcessingActivityStatus.Draft or ProcessingActivityStatus.UnderReview;
}
