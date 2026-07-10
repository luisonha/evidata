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

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
