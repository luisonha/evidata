namespace Evidata.Modules.ProcessingInventory.Domain;

/// <summary>
/// Estado del ciclo de vida de un tratamiento RAT (doc 15, sec 11).
/// Draft → UnderReview → Approved → Active → Archived / Deprecated
/// Draft también puede ir a Archived directamente (descarte).
/// Active es la versión vigente; Deprecated la reemplaza.
/// </summary>
public enum ProcessingActivityStatus
{
    Draft,
    UnderReview,
    Approved,
    Active,
    Deprecated,
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

    /// <summary>ID de la versión activa (vigente) de este tratamiento. Null si ninguna versión está activa aún.</summary>
    public Guid? ActiveVersionId { get; set; }

    // ── Secciones (owned types / JSONB) ────────────────────────────────────────

    /// <summary>Sección Finalidad y Base de Licitud (obligatoria para aprobación).</summary>
    public PurposeSection? Purpose { get; private set; }

    /// <summary>Categorías de datos tratados (al menos una requerida para aprobación).</summary>
    public IReadOnlyList<DataCategoryEntry> DataCategories => _dataCategories.AsReadOnly();
    private readonly List<DataCategoryEntry> _dataCategories = [];

    /// <summary>Tipos de titulares afectados (al menos uno requerido para aprobación).</summary>
    public IReadOnlyList<DataSubjectEntry> DataSubjects => _dataSubjects.AsReadOnly();
    private readonly List<DataSubjectEntry> _dataSubjects = [];

    /// <summary>Sistemas internos/externos involucrados.</summary>
    public IReadOnlyList<SystemEntry> Systems => _systems.AsReadOnly();
    private readonly List<SystemEntry> _systems = [];

    /// <summary>Proveedores / encargados de tratamiento.</summary>
    public IReadOnlyList<SupplierEntry> Suppliers => _suppliers.AsReadOnly();
    private readonly List<SupplierEntry> _suppliers = [];

    /// <summary>Política de retención (recomendada; puede bloquear aprobación según config).</summary>
    public RetentionSection? Retention { get; private set; }

    /// <summary>Medidas de seguridad declaradas (obligatorias si hay SpecialCategory).</summary>
    public IReadOnlyList<SecurityMeasureEntry> SecurityMeasures => _securityMeasures.AsReadOnly();
    private readonly List<SecurityMeasureEntry> _securityMeasures = [];

    /// <summary>
    /// Flags de riesgo regulatorio calculados automáticamente.
    /// Se recalculan en cada cambio de sección relevante.
    /// </summary>
    public RiskFlags Flags { get; private set; } = RiskFlags.Empty();

    /// <summary>¿Se declaran transferencias internacionales? (input manual, activa flag)</summary>
    public bool HasInternationalTransfer { get; private set; }

    /// <summary>¿Se declaran decisiones automatizadas o perfilamiento? (input manual, activa flag)</summary>
    public bool HasAutomatedDecision { get; private set; }

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

        var validation = ProcessingActivityValidator.ValidateForReview(this);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"El tratamiento no cumple los requisitos mínimos para revisión: {string.Join("; ", validation.Errors)}");

        Status = ProcessingActivityStatus.UnderReview;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    public void Approve(Guid approvedBy)
    {
        if (Status != ProcessingActivityStatus.UnderReview)
            throw new InvalidOperationException(
                $"Solo se puede aprobar desde UnderReview. Estado actual: {Status}.");

        var validation = ProcessingActivityValidator.ValidateForApproval(this);
        if (!validation.IsValid)
            throw new InvalidOperationException(
                $"El tratamiento no puede aprobarse: {string.Join("; ", validation.Errors)}");

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

    /// <summary>
    /// Crea una nueva versión editable (Draft) basada en este tratamiento aprobado.
    /// La nueva versión hereda todos los datos y tiene Version = this.Version + 1.
    /// </summary>
    public ProcessingActivity CreateNewVersion(Guid createdBy)
    {
        if (Status != ProcessingActivityStatus.Approved)
            throw new InvalidOperationException(
                "Solo se puede crear una nueva versión desde un tratamiento Aprobado.");

        var next = new ProcessingActivity
        {
            Id = Guid.NewGuid(),
            TenantId = TenantId,
            Name = Name,
            Description = Description,
            Controller = Controller,
            Department = Department,
            Status = ProcessingActivityStatus.Draft,
            Version = Version + 1,
            SupersedesId = Id,
            CreatedBy = createdBy,
            CreatedAt = DateTimeOffset.UtcNow,
            Purpose = Purpose
        };
        next._dataCategories.AddRange(_dataCategories);
        next._dataSubjects.AddRange(_dataSubjects);
        next._systems.AddRange(_systems);
        next._suppliers.AddRange(_suppliers);
        next._securityMeasures.AddRange(_securityMeasures);
        if (Retention is not null)
            next.Retention = Retention;
        return next;
    }

    public void Archive(Guid modifiedBy)
    {
        if (Status == ProcessingActivityStatus.Archived)
            throw new InvalidOperationException("El tratamiento ya está archivado.");

        Status = ProcessingActivityStatus.Archived;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Activates this version as the active version.
    /// Only valid when status is Approved.
    /// </summary>
    public void Activate(Guid activatedBy)
    {
        if (Status != ProcessingActivityStatus.Approved)
            throw new InvalidOperationException(
                $"Solo se puede activar desde estado Approved. Estado actual: {Status}.");

        Status = ProcessingActivityStatus.Active;
        LastModifiedBy = activatedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks this version as deprecated.
    /// Called when a new version becomes active.
    /// </summary>
    public void SetAsDeprecated(Guid modifiedBy)
    {
        if (Status != ProcessingActivityStatus.Active)
            throw new InvalidOperationException(
                $"Solo se puede deprecar un tratamiento Active. Estado actual: {Status}.");

        Status = ProcessingActivityStatus.Deprecated;
        LastModifiedBy = modifiedBy;
        LastModifiedAt = DateTimeOffset.UtcNow;
    }

    // ── Sección: Finalidad ─────────────────────────────────────────────────────

    public void SetPurpose(PurposeSection purpose, Guid modifiedBy)
    {
        GuardEditableState();
        ArgumentNullException.ThrowIfNull(purpose);
        Purpose = purpose;
        RecalculateFlags();
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
        RecalculateFlags();
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

    // ── Sección: Sistemas ──────────────────────────────────────────────────────

    public void SetSystems(IEnumerable<SystemEntry> entries, Guid modifiedBy)
    {
        GuardEditableState();
        _systems.Clear();
        _systems.AddRange(entries ?? []);
        Touch(modifiedBy);
    }

    // ── Sección: Proveedores ───────────────────────────────────────────────────

    public void SetSuppliers(IEnumerable<SupplierEntry> entries, Guid modifiedBy)
    {
        GuardEditableState();
        _suppliers.Clear();
        _suppliers.AddRange(entries ?? []);
        Touch(modifiedBy);
    }

    // ── Sección: Retención ─────────────────────────────────────────────────────

    public void SetRetention(RetentionSection retention, Guid modifiedBy)
    {
        GuardEditableState();
        ArgumentNullException.ThrowIfNull(retention);
        Retention = retention;
        RecalculateFlags();
        Touch(modifiedBy);
    }

    // ── Sección: Medidas de seguridad ──────────────────────────────────────────

    public void SetSecurityMeasures(IEnumerable<SecurityMeasureEntry> entries, Guid modifiedBy)
    {
        GuardEditableState();
        _securityMeasures.Clear();
        _securityMeasures.AddRange(entries ?? []);
        RecalculateFlags();
        Touch(modifiedBy);
    }

    // ── Flags de riesgo ────────────────────────────────────────────────────────

    /// <summary>
    /// Declara presencia de transferencias internacionales y/o decisiones automatizadas.
    /// Recalcula flags automáticamente.
    /// </summary>
    public void SetRiskInputs(
        bool hasInternationalTransfer,
        bool hasAutomatedDecision,
        Guid modifiedBy)
    {
        GuardEditableState();
        HasInternationalTransfer = hasInternationalTransfer;
        HasAutomatedDecision = hasAutomatedDecision;
        RecalculateFlags();
        Touch(modifiedBy);
    }

    /// <summary>
    /// Marca o limpia el flag CriticalGapOpen.
    /// Llamado por el Gap Management al crear/cerrar brechas críticas.
    /// </summary>
    public void SetCriticalGapFlag(bool hasCriticalGap, Guid modifiedBy)
    {
        // No requiere guard — puede llamarse desde estados no-Draft (gap puede llegar post-aprobación)
        Flags = RiskFlags.Calculate(
            _dataCategories, _securityMeasures, Retention, Purpose,
            HasInternationalTransfer, HasAutomatedDecision,
            hasCriticalGap);
        Touch(modifiedBy);
    }

    private void RecalculateFlags()
    {
        Flags = RiskFlags.Calculate(
            _dataCategories, _securityMeasures, Retention, Purpose,
            HasInternationalTransfer, HasAutomatedDecision,
            Flags.CriticalGapOpen);
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
