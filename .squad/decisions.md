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
- **P1-006**: `EvidenceValidation` state machine (Pending → Attached → Validated|Insufficient|Rejected) con auditoría preparada para handlers.
- **SEC-EV-001 Authorization**: Extensión de `ResourceContextData` + nuevo método `IsBlocked_ValidateEvidenceWrongDomain()` que valida domain-specific role authorization (LegalReviewer solo puede validar Legal, SecurityReviewer solo Security). Implementación **fail-closed**: si `ReviewDomain` es null/unknown, bloquea por defecto.
- Domain: ~500 LOC (entities, value objects, factory methods). DbContext config + indexes: ~200 LOC. Tests: 29 domain state transitions + 8 SEC-EV-001 cross-domain authorization tests (LegalReviewer ≠ Security, SecurityReviewer ≠ Legal).
- **Result**: All 643 unit tests passing, CI GREEN, merged to develop.
**Why:** Cierra contrato SEC-EV-001 (domain-specific RBAC para evidence validation). Permite que handlers posteriores (P1 Iteration 2) construyan commands sin tocar autorización. Responde a petición explícita del usuario sobre seguridad fail-closed.
**Status:** ✅ APROBADO + MERGED
**Process:** 2 iteraciones — 1ª rechazada (CS7036 compilation error + SEC-EV-001 fail-open + test gaps); 2ª aprobada (todos los issues corregidos, fail-closed, cross-domain tests).
**Owner:** Aragorn (implementation) + Gandalf (review/approval).

### 2026-07-10T02:30:00Z: PR #107 - Formalización de AuditLog → AuditEvent con Correlación E2E ✅ APROBADO + MERGED
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #107 (P1-009) implementa contrato de AuditEvent extendiendo `AuditLog` existente per **docs/evidata-backend-sprint-2/04-rbac-audit-evidence-gaps-contract.md** §2.1:
- **P1-009 Shape (10 campos)**: id, tenantId, eventType (enum, 10 valores AUD-PA-001..AUD-EXP-001), resourceType, resourceId, actorUserId, occurredAt (UTC), result (enum: Success|Failure|Blocked), correlationId, metadata.
- **Decisiones clave**:
  - Extender AuditLog vs. crear AuditEvent paralelo → menor complejidad migratoria, índices existentes reutilizados.
  - Metadata como Dictionary<string, object?> en código → JSON string en DB (portabilidad futura, queries JSON si es necesario).
  - CorrelationId propagado vía middleware global (X-Correlation-Id header o Guid generado) → disponible antes de autenticación, DRY.
  - Enum filtering: eventos con eventType no mapeables excluidos de timeline (no Unknown, catalog cerrado).
  - Backward compatibility: CreateLegacy() + LogLegacyAsync() para migración gradual de call-sites.
- **Gaps resueltos**: Result ausente → enum formal; CorrelationId null → header + middleware + persistido; Metadata string → Dictionary tipado; EventType strings libres → enum cerrado; UserId nullable → Guid.Empty placeholder en timeline.
- **Implementación**: 8 commits incrementales, ~400 LOC nuevas (tests + campos), migration 1 (FormalizAuditEvent: rename columns, add fields).
- **Tests**: 40 nuevos (11 AuditServiceTests + 19 TimelineQueryHandlerTests + 10 más). Comportamiento real: enum parsing, serialization, state transitions, no smoke tests. Todos 10 tipos AUD-* cubiertos.
- **Result**: 689 unit tests passing, CI GREEN, merged to develop.
**Why:** Cierra P1-009 contrato audit shape per especificación. Establece base para P1-010 (TimelineEvent, proyección UI). Correlación E2E crítica para trazabilidad y debugging distribuido.
**Status:** ✅ APROBADO + MERGED
**Process:** 1 iteración (pre-review + 1 revision cycle, Gandalf approval).
**Owner:** Aragorn (implementation) + Gandalf (review/approval).
**Recomendación futura:** P1-010 debe propagar correlationId a todos los handlers (ProcessingActivity, Evidence, GapManagement) via context.GetCorrelationId() → IAuditService.LogAsync().

### 2026-07-10T02:45:00Z: PR #106 - Formalización del Catálogo GapRule + FSM de ComplianceGap ✅ APROBADO + MERGED
**By:** Gimli (Data Architect/DB Models Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #106 (P1-007) implementa formalización exhaustiva del catálogo de **11 reglas mínimas de detección de brechas** definidas en contrato `04-rbac-audit-evidence-gaps-contract.md` §4:
- **P1-007 Resultado**: **9 de 11 reglas** completamente evaluables (82%), **2 de 11** formalizadas pero requieren captura de datos futura (18%, deuda técnica con path claro a P2).
- **Reglas Evaluables (9)**:
  - Grupo Transferencias: TRANSFER_WITHOUT_DESTINATION_COUNTRY, TRANSFER_WITHOUT_RECEIVER, TRANSFER_WITHOUT_SAFEGUARD (3 Critical)
  - Grupo Evidencias: TRANSFER_WITHOUT_BLOCKING_EVIDENCE, SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW (2 Critical)
  - Grupo Configuración Base: LEGAL_BASIS_MISSING, DATA_CATEGORIES_EMPTY, PURPOSE_UNDEFINED, DATA_SUBJECTS_EMPTY (4 campos simples)
- **Reglas P2 (2)**: RETENTION_UNDEFINED (requiere `ProcessingActivity.retentionPeriodDays`), SYSTEMS_WITHOUT_OWNER (requiere introspección RAT nodo↔propietario). Ambas marcadas `IsFullyImplemented=false` con documentación clara.
- **Entidades**: Nueva `GapRule` (Guid id, Guid tenantId, string ruleCode, GapSeverity, bool blocksApproval, bool isFullyImplemented, string implementationNotes). `ComplianceGap` actualizado con FSM: Open → InCorrection/AcceptedWithRisk/Dismissed, InCorrection → Resolved, Resolved → AutomaticReopen|Closed. 
- **BlocksApproval**: Simplificado a `Critical && Open` (reduce falsos positivos para gaps en corrección o aceptados con riesgo).
- **Reapertura Automática**: Transición Resolved → Open sin endpoint público, auditada con actor + timestamp.
- **Tests**: 686 todos pasando (11 catálogo + 40+ FSM + tenant isolation + reapertura automática con auditoría).
- **Implementación**: ~400 LOC (domain entities), ~200 LOC (DbContext config + indexes), migration (FormalizeGapRules).
- **Honestidad Técnica**: Gimli distingue claramente entre "evaluable" (9) y "formalizado pero no evaluable" (2) sin fingir completitud.
**Why:** Cierra P1-007 contrato gap rule catalog. Establece base para P1-004 (ResourcePermissionsViewModel producer, permisos bloqueados por gaps críticos). 9/11 evaluables es threshold aceptable para P1; 2 reglas no evaluables tienen plan claro a P2.
**Status:** ✅ APROBADO + MERGED
**Process:** 1 iteración (pre-review + Gandalf comprehensive review all 7 criteria: governance, CI/build, evaluability, FSM, tenant isolation, BlocksApproval simplification, tests). Gandalf approved "sin cambios obligatorios"; solo 3 recomendaciones P2 opcionales.
**Owner:** Gimli (implementation) + Gandalf (review/approval).
**Deuda Técnica Residual**: P2 agregar `retentionPeriodDays` a ProcessingActivity (RETENTION_UNDEFINED), P2 extender modelo RAT para vincular propietarios de nodos (SYSTEMS_WITHOUT_OWNER).

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
**Recomendación Futura:** P1-011c (remaining 5 handlers SubmitForReview/Activate/Archive/RejectEvidence/GenerateOfficialExport) debe seguir P2 con prioridad media, replicando patrón fail-closed y auditoría no-sensible de P1-011a/b.

### ⏳ PENDIENTE - P1-011c (Remaining Five Handlers + Auditoría, PRIORIDAD MEDIA/P2)
**Priority:** P2 · **Constraint Code:** SEC-HANDLERS-001  
**Scope**: Implementar + audit-instrument 5 handlers restantes (mismo patrón fail-closed que P1-011a/b):
1. **SubmitForReview** (AUD-REV-001): Metadata { processingActivityId, reviewedBy, submissionReason }
2. **Activate** (AUD-ACT-001): Metadata { processingActivityId, activatedBy, activationDate }
3. **Archive** (AUD-ARC-001): Metadata { processingActivityId, archivedBy, archiveReason }
4. **RejectEvidence** (AUD-EV-002): Metadata { evidenceId, rejectionReason [length only], rejectedBy }
5. **GenerateOfficialExport** (AUD-EXP-001): Metadata { exportFormat, exportScope, requestedBy, exportSize }
**Estimación**: 6-8 horas total (all 5 handlers + integration tests)  
**Status**: ⏳ PENDIENTE - Después de P1-011a/b completadas, stagger P2

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
