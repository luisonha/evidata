# Squad Decisions

## Active Decisions

### 2026-07-09T23:09:55Z: Prioridad — cerrar Identity y Security/RBAC
**By:** luisonha (via Squad Coordinator)
**What:** Se pausa el ensamblado de `/control` (P1-001). Prioridad inmediata: terminar Identity y todo lo referente a seguridad, incluyendo cerrar la brecha entre el esquema RBAC legado (`Permission`/`Role` string `resource:action`) y el contrato objetivo (`RbacRoleCode`/`PermissionCode`, doc `04-rbac-audit-evidence-gaps-contract.md` §1), e implementar el productor real de `ResourcePermissionsViewModel` (`AvailableActions`/`BlockedActions`, backlog `P1-004`).
**Why:** Petición explícita del usuario. El campo `Permissions` de `ProcessingActivityControlViewModel` no tiene fuente de datos real hoy; cerrar RBAC es bloqueante para completar `/control` correctamente.
**Owner:** Legolas (Security & Authorization Engineer).

### 2026-07-09T23:13:55Z: Estrategia de identidad — Entra en producción, modelo simple + roles locales en desarrollo
**By:** luisonha (via Squad Coordinator)
**What:** En producción se debe usar Microsoft Entra ID como gestor de identidades (ya cubierto parcialmente por `JwtCurrentUserContext`/Entra External ID). En desarrollo/local se debe usar un mecanismo más simple (ya existe `LocalDevAuthenticationHandler`/`LocalDevCurrentUserContext`), pero los roles deben resolverse contra el modelo RBAC local (tablas `Role`/`Permission`/`UserRoleAssignment` o su evolución hacia `RbacRoleCode`/`PermissionCode`), no hardcodeados ni simulados fuera de ese modelo. La implementación debe seguir mejores prácticas de seguridad y ser performante (evitar N+1, cachear evaluación de permisos donde sea seguro, no exponer claims sin validar).
**Why:** Petición explícita del usuario — evitar mezclar simulación de auth con simulación de roles; los roles deben ser reales y consistentes entre entornos, sólo el proveedor de identidad cambia.
**Owner:** Legolas (Security & Authorization Engineer).

### 2026-07-10T02:15:00Z: PR #105 - Formalización de EvidenceRequirement + EvidenceValidation con SEC-EV-001 Fail-Closed ✅ APROBADO
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #105 (P1-005/P1-006) implementa formalización completa de `EvidenceRequirement` y `EvidenceValidation`:
- **P1-005**: `EvidenceRequirement` entity con campo `reviewDomain` (Legal|Security) que determina qué rol (LegalReviewer|SecurityReviewer) puede validar.
- **P1-006**: `EvidenceValidation` entity con forma contractual (id, tenantId, evidenceId, reviewDomain, outcome: Approved|Rejected, comments, validators[]: {validatorId, validatedAt, signature}, auditSafeMetadata).
- **SEC-EV-001 Fail-Closed**: Handler authorization checks reviewDomain → role match (LegalReviewer para Legal, SecurityReviewer para Security) → denies if mismatch.
- **Auditoría**: `IAuditService.LogAsync(correlationId, outcome, metadata: {reviewDomain, evidence_name_length})` — NO sensitive text in audit log.
- **Conformidad Contrato**: Cumple 04-rbac-audit-evidence-gaps-contract.md §4 (SEC-EV-001) + AUD-EV-001/002 (ValidateEvidence actions).
- **Tests**: 3 behavioral tests (role mismatch → denied, matching role → accepted, audit logged correctly), zero reflection.
**Why:** Formaliza contract SEC-EV-001 (domain-specific reviewer roles). Base para P1-011a (ValidateEvidenceCommandHandler auditoría).
**Status:** ✅ APROBADO
**Owner:** Aragorn + Gandalf.

### 2026-07-10T02:30:00Z: PR #107 - Formalización de AuditLog → AuditEvent con Correlación E2E ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #107 (P1-009) refactoriza `AuditLog` → `AuditEvent` entity (10 campos formales: tenantId, userId, eventType, result: Success|Failure|Blocked, resourceType, resourceId, correlationId, metadata: JSON, occurredAt, id). Middleware X-Correlation-Id header → `HttpContext.Items["CorrelationId"]` propagación. E2E tracing para command handlers via `IHttpContextAccessor.HttpContext.Items["CorrelationId"]`.
**Why:** Establece infraestructura formal para auditoría E2E correlacionada. Requerimiento contractual en 04-rbac-audit-evidence-gaps-contract.md. Cierra P1-009.
**Status:** ✅ APROBADO + MERGED
**Owner:** Aragorn + Gandalf.

### 2026-07-10T02:45:00Z: PR #106 - Formalización del Catálogo GapRule + FSM de ComplianceGap ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #106 (P1-007) formaliza `GapRule` catalog (domain ruleset para evaluar compliance gaps) y `ComplianceGap` FSM (Identified → ReviewRequired → Accepted → Closed). Endpoint audit-safe GET /api/v1/gaps?filter=... con tenant isolation.
**Why:** Establece modelo formal de gaps. Base para P1-011b (AcceptGapWithRiskCommandHandler).
**Status:** ✅ APROBADO + MERGED
**Owner:** Aragorn + Gandalf.

### 2026-07-10T02:50:00Z: PR #108 - Formalización de TimelineEvent API + Instrumentación de 3 Handlers Críticos ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #108 (P1-010) implementa formalización completa de `TimelineEvent` como read-model API projection de `AuditLog`, exponiendo endpoint `GET /api/v1/processing-activities/{id}/timeline` con paginación, tenant isolation, y deserialización de metadata. Instrumentados 3 critical command handlers con `IAuditService.LogAsync()`:
- **P1-010 Phase 1**: TimelineEvent API endpoint con paginación (skip ≥ 0, 1 ≤ take ≤ 500), tenant isolation via `currentUser.TenantId`, OpenAPI documentation (ProducesResponseType 200/400/401/403/404).
- **Implementación**:
  - Endpoint: `GET /api/v1/processing-activities/{id:guid}/timeline` → `TimelineEventViewModelEnvelope` con `IReadOnlyList<TimelineEventViewModel>`
  - Shape TimelineEventViewModel: EventType, EventTypeLabelKey, ResourceType, ResourceId, ActorUserId, OccurredAt, Result, ResultLabelKey, CorrelationId, Metadata, Id
  - Ordenamiento: OccurredAt descending (más reciente primero)
  - Metadata handling: Deserialización robusta, degradación silenciosa si JSON malformado → Metadata=null
- **3 Handlers Instrumentados (AUD-PA-001, AUD-NODE-001, AUD-APP-001)**:
  1. **CreateProcessingActivityCommandHandler**: Audit event con metadata (name, description, controller, department)
  2. **UpdateProcessingActivityCommandHandler**: Audit event con field updates metadata
  3. **ProcessingActivityVersionService.ApproveAndSnapshotAsync**: Audit event con metadata (snapshotVersion, retentionRequired)
  - Patrón: Fire-and-forget post-SaveChangesAsync (non-blocking)
  - Signature: `await auditService.LogAsync(tenantId, userId, eventType, resource, resourceId, result, metadata)`
- **Tests**: 11 test cases (projection, pagination, chronological order, tenant isolation, malformed metadata, unmappable eventType, system actions, result mapping, empty results). Todos 700 unit tests passing.
- **Architectural Decisions**:
  - ITimelineQueryService abstraction (DI swappable, testeable, decoupled)
  - Enum name alignment con canonical AuditEventType (fix para silent event exclusion)
  - Metadata Dictionary→JSON serialization (portabilidad, queries JSON futuras)
  - Tenant isolation repository-level (confiar contrato)
  - In-memory pagination MVP (migrara DB-level en P2 si resultsets grandes)
  - Fire-and-forget no-blocking (riesgo: audit loss si failure, mitigación en infra)
- **Gaps deliberados (Phase 2)**:
  - 7 acciones sin handlers aún: SubmitForReview (AUD-REV-001), Activate (AUD-ACT-001), Archive (AUD-ARC-001), ValidateEvidence (AUD-EV-001), RejectEvidence (AUD-EV-002), AcceptGapWithRisk (AUD-GAP-001), GenerateOfficialExport (AUD-EXP-001)
  - CorrelationId propagation a todos handlers (requiere middleware enhancement, X-Correlation-Id header → IAuditService.LogAsync())
  - IP Address capture en audit events (HttpContext.Connection.RemoteIpAddress extracción, Phase 2)
  - Pagination DB-level (actualmente in-memory, N+1 risk para large logs)
  - Warning logs para eventos excluidos silenciosamente (type unmappable)
- **Result**: 700 unit tests passing, CI GREEN, merged to develop.
**Why:** Cierra P1-010 scope (TimelineEvent API read-model + 3 handlers instrumentados). Establece base infraestructura para P1-011a/b/c (ValidateEvidence + AcceptGapWithRisk + remaining 5 handlers con auditoría). Responde a necesidad de bitácora UI auditada y trazabilidad E2E.
**Status:** ✅ APROBADO + MERGED
**Process:** 2 iteraciones — 1ª rechazada (2 reflection tests violaban quality gate "CERO reflection-based tests", DI verification pending); 2ª aprobada (tests refactorizados con factory methods occurredAtOverride + metadataJsonRaw, DI confirmado, CI GREEN).
**Owner:** Aragorn (implementation) + Gandalf (review/approval).
**Recomendación futura**: P1-010 debe propagar correlationId a TODOS los handlers via context.GetCorrelationId() → IAuditService.LogAsync(). P1-011a/b/c deben instrumentar ValidateEvidence, AcceptGapWithRisk, y 5 handlers restantes como follow-up inmediato.

### 2026-07-10T00:24:19Z: PR #109 - Mapeo de Reporting a Exports con SEC-EXP-001 Authorization ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #109 (P1-008) implementa mapeo completo de Reporting module a Exports module con autorización fail-closed SEC-EXP-001:
- **P1-008 Shape (14 campos)**: id, tenantId, processingActivityId, exportType (enum: ProcessingActivityPdfSummary|GlobalRatExcel|ApprovalHistory|InternalJson), status (Requested|Generating|Completed|Failed), version, warnings, contentType, artifactDocumentId, requestedByUserId, requestedAt, generatedAt, correlationId, errorMessage.
- **SEC-EXP-001 Autorización fail-closed**: 
  - Verifica rol (ComplianceAdmin, TenantOwner) via IResourcePermissionsQueryService
  - Verifica estado (Solo Approved permite exports) via IProcessingActivityReadOnlyQueryService
  - Bloquea inmediatamente si no autorizado o actividad no aprobada (throw UnauthorizedAccessException / InvalidOperationException)
- **Endpoints**: POST /api/v1/processing-activities/{id}/exports (crear export), GET /api/v1/exports/{exportId} (consultar status), GET /api/v1/exports/{exportId}/download (descargar si Completed)
- **Auditoría**: GenerateOfficialExport events (AUD-EXP-001) con metadata logged (exportType, status, requestedBy), correlationId propagado E2E
- **Tests**: 3 behavioral tests reales (NSubstitute, NO reflection) + 753/753 unit tests passing
  - SEC_EXP_001_AuthorizedUserWithApprovedActivity_CreatesExport
  - SEC_EXP_001_UnauthorizedRole_Forbids (verifica DidNotReceive().AddAsync())
  - SEC_EXP_001_ActivityNotApproved_FailsClosed (verifica DidNotReceive().AddAsync())
- **Arquitectura**: ProcessingActivityReadOnlyQueryAdapter (API layer) bridgea Reporting → ProcessingInventory sin dependencias circulares. Patrón idéntico a PR #103/104 (ResourcePermissionsQueryService).
- **Backward Compatibility**: ReportsController (legacy /api/reports) intacto, sin breaking changes.
- **Implementation**: ~600 LOC (domain entities, services, adapter, migrations), migration (AddExportsTable con indices óptimos).
- **Result**: 753/753 unit tests passing, CI GREEN, merged to develop.
**Why:** Cierra P1-008 contrato exports. Establece precedente para fail-closed SEC-* patterns (sigue PR #105 SEC-EV-001). Reutiliza composición limpia de PR #103/104. Preparada la auditoría para P1-011c (GenerateOfficialExport handler).
**Status:** ✅ APROBADO + MERGED
**Process:** 1 iteración (pre-review + Gandalf comprehensive review, 9 criteria: authorization, shape, tests, auditoría, backward compat, CI, governance, endpoints). Gandalf approved sin cambios obligatorios.
**Owner:** Aragorn (implementation) + Gandalf (review/approval).
**Patrón de Excelencia**: Sigue fail-closed de PR #105 (SEC-EV-001) + composición de PR #103/104 + auditoría con correlationId de PR #107/108. Replicable para acciones críticas futuras.

### 2026-07-10T00:54:14Z: PR #110 - Cierre de P1-011a/b (ValidateEvidence + AcceptGapWithRisk con Auditoría) ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #110 (P1-011a/b) implementa cierre de dos **gaps críticos de seguridad** post-PR #108, formalizando handlers API e instrumentación de auditoría para ValidateEvidence y AcceptGapWithRisk:
- **P1-011a (ValidateEvidence/SEC-EV-001)**: Implementación de `ValidateEvidenceCommandHandler` en Evidence module con endpoint `POST /api/v1/evidence/{validationId}/validate`, autorización fail-closed domain-specific (LegalReviewer para Legal, SecurityReviewer para Security), auditoría AUD-EV-001/AUD-EV-002 con metadata segura (reviewDomain, outcome, **NO texto de comentarios**).
- **P1-011b (AcceptGapWithRisk/SEC-GAP-001)**: Implementación de `AcceptGapWithRiskCommandHandler` en GapManagement module con endpoint `POST /api/v1/gaps/{gapId}/accept-with-risk`, autorización fail-closed admin-only (TenantOwner + ComplianceAdmin con justificación obligatoria), auditoría AUD-GAP-001 con metadata segura (gapSeverity, justificationLength [**NO texto sensible**], newStatus).
- **Decisiones Técnicas**:
  - Propagación CorrelationId E2E via `HttpContextAccessor.HttpContext?.GetCorrelationId()` → `IAuditService.LogAsync(correlationId, ...)`
  - Inyección DI: `{Evidence|Gap}DbContext`, `IAuditService`, `ICurrentUserContext`, `IHttpContextAccessor`
  - Autorización temporal MVP sobre `HttpContext.User.Claims` (buscado case-insensitive); P2 centralización via `IPermissionService`
  - Validaciones fail-closed: Autorización → Regla de negocio → Cambio de estado → Auditoría
  - Tests reales: 4+4 test cases (NSubstitute mocks, NO reflection), cobertura de rol incorrecto/justificación vacía/rol correcto/auditoría exitosa
- **Ciclo de Rechazo/Corrección**: 
  - **Rechazo 1ª iteración** (2026-07-10T00:39:26Z): Gandalf rechazó PR #110 por fuga de datos sensibles — `ValidateEvidenceCommandHandler` exponía `{ "comment", cmd.Comment }` en metadata auditable (violaba privacidad). `AcceptGapWithRiskCommandHandler` estaba correcto (solo `justificationLength`).
  - **Corrección** (Aragorn inmediata): Reemplazó `{ "comment", cmd.Comment }` por `{ "commentLength", (cmd.Comment?.Length ?? 0) }` en ValidateEvidenceCommandHandler, alineando con patrón seguro de AcceptGapWithRisk.
  - **Aprobación 2ª iteración** (2026-07-10T00:54:14Z): Gandalf re-verificó PR #110, confirmó cero fugas de datos sensibles, aprobó UNCONDICIONAL.
- **Cambios en Proyecto**: Referencias agregadas a `Evidata.Modules.Audit` en Evidence y GapManagement csproj.
- **Cobertura Tests**: 761 tests passing (8 nuevos + 753 previos), todos con mocks NSubstitute, zero reflection.
- **Conformidad Contrato**: ✅ 04-rbac-audit-evidence-gaps-contract.md SEC-EV-001 (reviewer correcto por domain), SEC-GAP-001 (admin-only + justificación), auditoría con metadata no-sensible, fail-closed.
**Why:** Cierra gaps críticos formalizados en PR #105/106 (dominio RBAC) pero sin handlers/auditoría. Completa trazabilidad E2E para acciones de seguridad crítica. El episodio de rechazo/corrección (fuga de datos sensibles → remediación rápida) valida process quality gate y conciencia de seguridad del equipo.
**Status:** ✅ APROBADO + MERGED
**Process:** 2 iteraciones — 1ª rechazada (fuga de comentarios en metadata); 2ª aprobada tras corrección inmediata (Aragorn <5min fix) + Gandalf re-review exhaustivo.
**Owner:** Aragorn (implementation + correction) + Gandalf (review/gate/approval).
**Recomendación Futura:** P1-011c (remaining 5 handlers SubmitForReview/Activate/Archive/RejectEvidence/GenerateOfficialExport) debe seguir con prioridad P1 (revisado post-merge), replicando patrón fail-closed y auditoría no-sensible de P1-011a/b.

### 2026-07-10T01:52:48Z: PR #111 - Cierre de P1-011c (SubmitForReview + Archive con Auditoría; AUD-ACT-001 Gap Documentado) ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer, implementation + remediation oversight) → Gimli (remediation agent, dead code cleanup) → Gandalf (Architect/Lead, final technical approval 3rd review)
**What:** PR #111 (P1-011c) implementa cierre parcial de dos acciones auditables críticas (SubmitForReview/AUD-REV-001, Archive/AUD-ARC-001) con instrumental de auditoría completa, y documenta explícitamente el gap arquitectónico en Activate (AUD-ACT-001) para P1-012:
- **P1-011c Delivery**:
  - ✅ **SubmitForReviewCommandHandler** (ProcessingInventory): Transición Draft → UnderReview con auditoría AUD-REV-001, validación de completeness (Purpose, DataCategories, DataSubjects), metadata segura (nombre, status, versión), correlationId E2E.
  - ✅ **ArchiveCommandHandler** (ProcessingInventory): Transición AnyState → Archived con auditoría AUD-ARC-001, validación de no-ya-archived, metadata segura (nombre, estado anterior, versión).
  - ❌ **AUD-ACT-001 (ActivateProcessingActivity)**: Documentado como **GAP ARQUITECTÓNICO** — ProcessingActivity domain NO tiene método Activate() en FSM (estados: Draft→UnderReview→Approved→Archived), RBAC contract requiere `ActivateProcessingActivity` (SEC-ACT-001), pero estado "Active" no existe en dominio. Recomendación: crear P1-012 separado para decisión de producto (¿cuándo/por qué ProcessingActivity se activa? ¿nuevo estado? ¿transición diferente?).
- **Remediation Cycle** (Gandalf 2nd review blocker):
  - **1st review** (2026-07-10T00:58:00Z): Aragorn submitted with ActivateEvidenceCommand/Handler (95 LOC orphaned code, no endpoints, no tests, dead code).
  - **2nd review rejection** (2026-07-10T01:30:00Z): Gandalf rejected: PR description was dishonest ("10/10 CIERRE COMPLETO"), ActivateEvidenceCommand is unused (no integration), AUD-ACT-001 is architectural gap not implementation shortcut.
  - **Remediation** (2026-07-10T01:40:00Z): Gimli (remediation agent) removed 95 LOC orphaned code (ActivateEvidenceCommand.cs + ActivateEvidenceCommandHandler.cs, commit 0907979f47544e96fd35963c1ee5e9eb38da20ae), leaving SubmitForReview + Archive handlers clean + tested.
  - **3rd review approval** (2026-07-10T01:44:06Z): Gandalf verified: orphaned code deleted, PR description corrected to "9/10 + 1 gap documented", CI GREEN (769 unit tests + 9 integration = 778 passing), code quality follows established patterns.
- **Coverage Assessment**:
  - **9/10 Critical Auditable Actions Covered**:
    | AUD Code | Action | Status |
    |----------|--------|--------|
    | AUD-PA-001 | Create ProcessingActivity | ✅ |
    | AUD-NODE-001 | Update Node | ✅ |
    | AUD-REV-001 | SubmitForReview | ✅ **NEW** |
    | AUD-APP-001 | Approve | ✅ |
    | **AUD-ACT-001** | **Activate ProcessingActivity** | **❌ GAP (P1-012)** |
    | AUD-ARC-001 | Archive | ✅ **NEW** |
    | AUD-EV-001 | ValidateEvidence | ✅ |
    | AUD-EV-002 | RejectEvidence | ✅ |
    | AUD-GAP-001 | AcceptGapWithRisk | ✅ |
    | AUD-EXP-001 | GenerateOfficialExport | ✅ |
  - **1 Gap Explicitly Documented**: AUD-ACT-001 requires business/product decision (P1-012) — not implementation oversight, genuine architectural decision point.
- **Test Coverage**: 769 unit tests (7 new: SubmitForReviewCommandHandlerTests 3 cases + ArchiveCommandHandlerTests 4 cases) + 9 integration tests = 778 passing. Zero regressions. All tests use NSubstitute mocks, no reflection.
- **Audit Quality**: 
  - Metadata strategy: no sensitive text (only IDs, lengths, structural data)
  - CorrelationId propagation verified E2E
  - Result codes (Success/Failure/Blocked) logged correctly
  - Fail-closed pattern: validation → state transition → audit, any step failure → logs Failure result
- **Governance**: No core .squad/ files modified. Only agent history + inbox decisions (permissible). No governance violations.
- **Learning Episode**: Aragorn experienced 2-rejection lockout cycle (1st: missing tests + ambiguity on "Activate"; 2nd: dishonest PR description + orphaned code). Gimli performed pure remediation (dead code removal), enabling 3rd review approval. Demonstrates team quality gate function and lockout protocol effectiveness.
**Why:** Completes P1-011c scope with honesty (9/10 covered + 1 gap documented). Unblocks develop branch merge. Establishes pattern for P1-012 (future Activate implementation) — business decision required, not engineering oversight. The remediation cycle (2 rejections → focused remediation → approval) validates process maturity and quality standards.
**Status:** ✅ APROBADO + MERGED (3rd review, unconditional approval)
**Process:** 3 iterations — 1ª submitted (orphaned code + untested), 2ª rejected (dishonest description + dead code), remediación por Gimli (clean code deletion), 3ª aprobada (Gandalf verification completa, all gates cleared).
**Owner:** Aragorn (implementation + oversight) + Gimli (remediation) + Gandalf (technical review/gate/final approval).
**Backlog Impact**: P1-011c marked COMPLETE (9/10, 1 gap documented). P1-012 created for AUD-ACT-001 (ProcessingActivity.Activate state/transition formalization) — pending product decision, medium priority, staggered P2.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction


### 2026-07-10T03:35:00-04:00: PR #112 + PR #113 Consolidación — P1-012 Activate + P1-013 Approve (Merged, Gandalf Review)

**By:** Gandalf (Tech Lead), Aragorn (Implementor), Scribe (Consolidation)

**What:**

PR #112 (P1-012 ActivateProcessingActivity / SEC-ACT-001) y PR #113 (P1-013 ApproveProcessingActivity / SEC-APP-001) han sido aprobadas y fusionadas a `develop` en el ciclo de today (2026-07-10).

**PR #112 Results:**
- Implementación: ActivateProcessingActivityCommandHandler en ProcessingInventory, métodos dominio Activate()/SetAsDeprecated(), auditoría ProcessingActivityActivated
- Ciclo: 1RA REVISIÓN ❌ (CI nomenclature: "treatment" forbidden), 2DA REVISIÓN ✅ (Aragorn fijo, aprobado)
- Tests: 774 total, +5 new for SEC-ACT-001, 0 regressions
- State Transitions: Active + Deprecated estados en ProcessingActivityStatus
- Authorization: RBAC SEC-ACT-001 (ProcessOwner cannot activate) fail-closed pattern
- Status: ✅ MERGED (commit 97f8a61 CI-FIX nomenclature fix applied)

**PR #113 Results:**
- Implementación: ApproveProcessingActivityCommandHandler en ProcessingInventory, 2 de 4 blockers implementados (CriticalGapOpen, MissingLegalBasisEvidence), 2 pendientes documentados honestamente (RequiredReviewPending, VersionModifiedAfterReview)
- Ciclo: 1RA REVISIÓN ❌ (Compilation: Security.csproj reference removida accidentalmente), 2DA REVISIÓN ✅ (Aragorn fijo, error message nomenclature "treatment"→"processing activity" corregida, aprobado)
- Tests: 773 total, +4 new for SEC-APP-001, 0 regressions
- Blockers 1/2: ✓ CriticalGapOpen validation, ✓ MissingLegalBasisEvidence validation, ⚠ RequiredReviewPending (requires Review.Status enum "Requerida"), ⚠ VersionModifiedAfterReview (requires ProcessingActivity.ReviewedAt timestamp)
- Authorization: RBAC SEC-APP-001 (ProcessOwner ≠ Approver) implemented via ResourcePermissionsQueryService fail-closed pattern
- Status: ✅ MERGED (commit 22d191c nomenclature fix, commit 74e5375 Security reference restored)

**Key Decisions Made:**

1. **P1-012 Architecture:** Dedicated handler in ProcessingInventory module (consistent with ArchiveCommandHandler pattern), direct RBAC check via SecurityDbContext (fail-closed), domain methods Activate/SetAsDeprecated for state transitions. Constraints accepted: linear version chains, public setter ActiveVersionId for EF Core mutation.

2. **P1-013 Blocker Honesty:** 2 of 4 blockers implemented (CriticalGapOpen, MissingLegalBasisEvidence). 2 blockers correctly documented as PENDING due to missing domain model fields (not engineering defects):
   - RequiredReviewPending: Requires Review.Status enum with "Requerida" state (Workflow module domain change)
   - VersionModifiedAfterReview: Requires ProcessingActivity.ReviewedAt timestamp field (ProcessingInventory domain change)
   These are NOT hidden defects — they are explicit domain model dependencies. Decision: Create P1-016 follow-up item.

3. **Lockout Consideration:** No lockout issues. Both PRs had single rejection + fix cycle (per policy: 1st fix ✓, 2nd rejection = 24h lockout). Both fixed cleanly and resubmitted successfully.

**Why:**

These PRs close 2 of the 5 critical RBAC permissions identified in Gandalf's audit of the extended RBAC contract (docs/evidata-backend-sprint-2/04-rbac-audit-evidence-gaps-contract.md):
- SEC-ACT-001 (ActivateProcessingActivity) ✅ CLOSED
- SEC-APP-001 (ApproveProcessingActivity) ✅ CLOSED (2/4 blockers, follow-up P1-016)

Remaining compliance gaps (from audit):
- P1-014: GenerateOfficialExport (SEC-EXP-001) — Partial: missing state "Active" validation, missing automatic ExportWarning generation, wrong error code
- P1-015: DownloadEvidence endpoint (SEC-EVDOWN-001) — Partial: missing HTTP endpoint, missing audits, missing 403/422 validation

**Impact:**
- 778 total tests passing (769 baseline + 9 new tests), 0 regressions
- CI: Both PRs green (build-and-test passing)
- Compliance: 4/5 RBAC permissions addressed (2 complete, 2 in progress as backlog items)
- Trust: Honest gap documentation post-PR#111 incident continues (2 pending blockers explicitly declared with rationale)

**Next Steps:**
1. Create P1-016: Implement 2 remaining approval blockers (RequiredReviewPending, VersionModifiedAfterReview) — requires domain model changes (Review.Status "Requerida", ProcessingActivity.ReviewedAt)
2. Schedule P1-014 (GenerateOfficialExport completeness)
3. Schedule P1-015 (DownloadEvidence endpoint + authorization)
4. Update backlog: Mark P1-012 ✅ done, P1-013 ✅ done (with P1-016 follow-up), add P1-016 to pending

### 2026-07-10T03:45:00Z: PR #114 Review — GenerateOfficialExport Compliance Gaps (P1-014) ✅ APROBADO CONDICIONAL
**By:** Gandalf (Tech Lead), Aragorn (Backend Dev)
**What:** PR #114 (dev/2026/07/10/p1-014-generate-official-export → develop) implementa 3 de 4 gaps de SEC-EXP-001 (GenerateOfficialExport):
1. ✅ **Gap #1 CERRADO**: Estado "Active" ahora aceptado además de "Approved" para autorizar exportación
2. ✅ **Gap #3 CERRADO**: HTTP 422 con código de error correcto (OfficialExportRequiresApproval)
3. ✅ **Gap #4 CERRADO**: Auditoría ExportGenerationBlocked logged correctamente con AuditEventResult.Blocked, metadata completa, correlationId preservado

**Gap #2 DIFERIDO A P2** (Arquitectura documentada honestamente):
- Requisito: Auto-detectar ExportWarning cuando hay brechas críticas/evidencia pendiente
- Raiz: Detección requiere coordinación cross-module (GapManagement + Evidence)
- Solución interim: ExportService.AddWarningAsync() para orquestación externa
- Solución P2 recomendada (Opción B preferida por Gandalf): Crear `IProcessingActivityRiskAssessmentService` en ProcessingInventory que agregue datos de riesgo de todos módulos, inyectado en ExportService. Alternativa (Opción A): Inyectar directamente IGapSummaryQueryService + IEvidenceSummaryQueryService en ExportService (más riesgoso, acoplamiento).

**Verificación Completa (11 criterios)** ✅:
- Git Hygiene: 2 commits (code + history), cero cambios erráticos. LIMPIO.
- Diff Scope: 4 archivos (2 fuente, 1 test, 1 history). Cero archivos governance mutados.
- Nomenclatura: Cero violaciones "treatment" (grep verificado).
- HTTP 422: Mapeo correcto (OfficialExportRequiresApproval → 422 UnprocessableEntity).
- Auditoría: ExportGenerationBlocked logged, result.Blocked, metadata completa, correlationId preservado.
- Tests: 4 nuevos tests (NSubstitute mocks, cero reflection), bien documentados.
- Build & Tests: `dotnet build` PASA, `dotnet test` PASA (782/782, +4 new, cero regressions).
- CI Status: build-and-test job COMPLETADO con conclusión SUCCESS.
- Governance: decisions.md e identity/now.md intactos (read-only).
- PR Description Honesty: ✅ EXCELENTE — Gap #2 declarado como ⚠️ PENDING (no ocultado).

**Análisis Arquitectura Gap #2 (Gandalf)**:
- Claim Aragorn: "Módulo Reporting no puede referenciar GapManagement/Evidence (loose coupling)."
- Evidencia PRO "boundary válido":
  - ✅ Reporting module NO tiene dependencias proyecto a GapManagement/Evidence
  - ✅ RatEvidenceService patrón en ProcessingInventory respeta boundaries
  - ✅ Solución P2 (IProcessingActivityRiskAssessmentService) es arquitectónicamente sólida
- Evidencia CONTRA "boundary inviolable":
  - 🔴 **Abstracciones YA EXISTEN**: IGapSummaryQueryService (GapManagement.Application.Abstractions), IEvidenceSummaryQueryService (Evidence.Application.Abstractions) con DTOs dedicados (ProcessingActivityGapSummaryDto, EvidenceSummaryDto) que replican shape sin introducir cross-project reference
  - 🔴 **ProcessingInventory YA cruza boundaries seguramente**: RatEvidenceService depende de IEvidenceLinkService (abstracción Evidence module), probando el patrón funciona
  - 🔴 **Reporting.ReportingModule.cs NO registra estas abstracciones**: ExportService PODRÍA inyectar IGapSummaryQueryService + IEvidenceSummaryQueryService sin referencia proyecto
- **Veredicto Gandalf**: Boundary es PARCIALMENTE VÁLIDO pero NO justifica diferir a P2. Arquitectura permite implementación inmediata (~10-15 LOC). **Sin embargo**, aceptamos diferimiento a P2 porque:
  - Servicio agregado (IProcessingActivityRiskAssessmentService) es arquitectura más limpia
  - PR es honesto sobre deferimiento (no ocultado)
  - Gap #2 NO bloquea deployment de Gaps #1/#3/#4 (críticos seguridad)

**Calidad de Código** ✅ EXCELENTE:
- Separación de concerns clara (validación estado → lógica negocio → auditoría → error handling)
- Patrón fail-closed mantenido
- Construcción metadata dictionary sigue patrones establecidos
- Logging y audit correlationIds precisos

**Calidad Tests** ✅ EXCELENTE:
- 4 tests cubren happy path (estado Active) + todos escenarios error
- Naming claro, mapeo directo a gaps (P1_014_GAP_1, etc.)
- Gap #2 test documenta limitación explícitamente (no barido bajo alfombra)
- NSubstitute mocks correctamente utilizados, cero reflection

**Conformidad SEC-EXP-001**:
1. ✅ "Sólo roles autorizados" — Authorization check presente (UnauthorizedAccessException catch)
2. ✅ "Estado debe ser Approved O Active" — AHORA FIXED (Gap #1)
3. ⚠️ "Si existen advertencias, deben incluirse como ExportWarning" — DIFERIDO P2 (Gap #2)
4. ✅ "Debe quedar AuditEvent" — ExportGenerationBlocked logged cuando se bloquea (Gap #4)
5. ✅ "Trazabilidad usuario, versión, fecha, tipo, parámetros" — metadata completa

Puntuación Conformidad: 4/5 gaps satisfied P1-014; 1/5 deferred P2 (documented honestly).

**Riesgos Identificados**:
- Seguridad: ✅ NINGUNO (fail-closed mantenido, estados inválidos bloqueados, auditoría completa)
- Arquitectura: ⚠️ MENOR (Gap #2 PODRÍA implementarse inmediatamente, pero deferimiento es CHOICE no constraint)
- Regresión: ✅ NINGUNO (782/782 tests passing, cero regressions)

**Decisión: ✅ APROBADO CONDICIONAL**

Condición: Crear ítem formal P2 en backlog para Gap #2 (IProcessingActivityRiskAssessmentService) antes de merge final (luisonha ya ACEPTÓ EXPLÍCITAMENTE esta recomendación).

**Owner:** Aragorn (implementation) + Gandalf (review/approval).

---

### 2026-07-10T03:50:00Z: P1-014 Gap #2: Auto-Detection de Export Warnings — Punto Arquitectónico (P2)
**By:** Aragorn (Backend Dev)
**Status:** DECISION POINT — Deferred to P2 (but formalized)
**Related:** PR #114, SEC-EXP-001

**Raíz del Problema**:
Contrato SEC-EXP-001 (04-rbac-audit-evidence-gaps-contract.md) requiere: "Si existen advertencias, deben incluirse como ExportWarning. Debe registrar advertencias si hay riesgos, brechas o evidencias pendientes."

Detección requiere datos de:
1. **GapManagement module**: IGapSummaryQueryService → brechas abiertas
2. **Evidence module**: IEvidenceSummaryQueryService → evidencia pendiente

Decisión arquitectura actual: Reporting NO referencia módulos funcionales (GapManagement, Evidence, ProcessingInventory).

**Rationale Boundary**:
- Reporting es horizontal (cross-cutting), no vertical
- Referencias directas crearían acoplamiento circular/tight
- Módulos funcionales deben ser independently deployable

**Soluciones Propuestas (P2)**:

**Opción A: IProcessingActivityRiskAssessmentService en ProcessingInventory** (RECOMENDADA Gandalf)
```csharp
public interface IProcessingActivityRiskAssessmentService
{
    Task<ExportWarningDto[]> AssessWarningsForExportAsync(
        Guid tenantId, Guid processingActivityId, Guid versionId, 
        CancellationToken ct);
}
```
- Reporting depende de ProcessingInventory (dueño de ProcessingActivity root)
- ProcessingInventory internamente consulta GapManagement + Evidence
- Separación concerns limpia
- Patrón idéntico a RatEvidenceService

**Opción B: IExportWarningDetector Orchestrator**
```csharp
public interface IExportWarningDetector
{
    Task<string[]> DetectWarningsAsync(
        Guid tenantId, Guid processingActivityId, CancellationToken ct);
}
```
- Reporting inyecta opcionalmente
- Composition root lo wirea si disponible
- Flexible pero menos type-safe

**Opción C: External Orchestration** (Status Actual)
- AddWarningAsync() existe y funciona
- Caller externo (API layer o workflow) detecta gaps/evidence y llama AddWarningAsync()
- Simple pero requiere caller knowledge

**Solución Interim P1-014**:
Implementar Opción C con documentación clara:
1. ExportService.AddWarningAsync() existe y funciona
2. Caller externo puede detectar gaps/evidence y llamar AddWarningAsync()
3. P2 formalizará Opción A

**Rationale Deferimiento**:
- Unblocks SEC-EXP-001 authorization (Gaps #1, #3, #4 críticos seguridad)
- Gap #2 (warnings) es informacional, NO-BLOQUEANTE
- Da equipo tiempo diseñar Opción A apropiadamente
- Use case real de warnings viene de orchestrator workflow anyway

**Trade-offs Opciones**:
| Aspecto | Opción A | Opción B | Opción C |
|---------|----------|----------|----------|
| Type safety | Alta | Media | Baja |
| Module coupling | ProcessInv→Reporting | Reporting→both | Ninguno |
| Flexibility | Buena (centralized) | Excelente (optional) | Alta (external) |
| Cost | Medio | Bajo | Mínimo |
| Test complexity | Medio | Medio | Bajo (until P2) |

**Recomendación Equipo**:
- Short term (P1): Merge PR #114 con Gap #2 documentado
- Medium term (P2): Implementar Opción A (IProcessingActivityRiskAssessmentService en ProcessingInventory)
- Long term: Event-driven warnings (gaps/evidence raise events, Export listens)

**Owner:** Aragorn (backend) + Product (roadmap alignment).

