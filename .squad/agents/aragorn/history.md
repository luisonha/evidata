# aragorn — History

## Session 2026-07-07

- Agente creado para el proyecto Evidata.
- Universo: El Señor de los Anillos.
- Decisiones iniciales confirmadas: repo monorepo, GitHub Actions CI + Azure DevOps CD, PostgreSQL, Entra External ID, App Service Linux.

## 2026-07-10 · P1-001 Control Endpoint Implementation (Stub)

### What
- Created `GetProcessingActivityControlQueryHandler` in ProcessingInventory module
- Added endpoint `/processing-activities/{id}/control` to ProcessingActivitiesController
- Registered handler in DI via ProcessingInventoryModule

### Why
- P1-001 requires a /control endpoint returning ProcessingActivityControlViewModel
- Full composition blocked by circular module dependencies (GapManagement→ProcessingInventory, but handler needs GapManagement, Workflow, Reporting, Security, Audit interfaces)
- Implemented as stub to resolve compile-time circular dependency; full composition deferred to P1-FULL-COMPOSITION task

### Status
- ✓ Endpoint structure in place (returns 200 OK with stub data)
- ✓ Handler registered and DI configured
- ✓ Build succeeds (dotnet build)
- ✓ All 597 unit tests pass
- ⏳ Full composition pending (P1-FULL-COMPOSITION): requires cross-module service composition without circular references

### Architecture Decision
Due to GapManagement module dependency on ProcessingInventory, direct composition in ProcessingInventory creates circular reference. Solution:
- ProcessingInventory contains stub handler returning empty/placeholder summaries
- Separate composition layer (TBD: API-level wrapper or dedicated composition service) will inject real summaries at runtime
- Prevents compile-time failures while maintaining clean API contract

### Blockers for Full Implementation
1. Module circular dependency: GapManagement uses ProcessingInventory types
2. Need abstract service layer or composition at API level to break the cycle
3. Architectural decision needed: where should cross-module composition live?

### Next Steps (P1-FULL-COMPOSITION)
1. Define composition strategy (API layer wrapper vs. dedicated service)
2. Implement full summaries: Evidence, Gaps, Reviews, Timeline, Exports
3. Implement P1-004: availableActions/blockedActions via ResourcePermissionsQueryService
4. Full integration tests
5. Update OpenAPI contract

## 2026-07-05/06/07 · P1-005 & P1-006 Evidence Requirement and Validation

### What
- **P1-005**: Formalizado `EvidenceRequirement` con campo `reviewDomain` (Legal | Security)
- **P1-006**: Formalizado `EvidenceValidation` con estado machine (Pending → Attached → Validated/Insufficient/Rejected)
- Integración SEC-EV-001 con reviewDomain en ResourcePermissionsQueryService
- EF Core entities, migrations, y tests de comportamiento real

### Cambios
1. **Nuevos archivos de dominio**:
   - `src/Modules/Evidence/Domain/EvidenceRequirement.cs` (97 líneas, reviewDomain crítico)
   - `src/Modules/Evidence/Domain/EvidenceValidation.cs` (201 líneas, máquina de estados)

2. **Actualización EF Core**:
   - `EvidenceDbContext`: agregados DbSets + configuración (73 líneas)
   - Migration automática: `AddEvidenceRequirementAndValidation` (generada vía `dotnet ef`)

3. **Seguridad (RBAC)**:
   - `ResourceContextData`: agregado campo `ReviewDomain?` (backward compatible)
   - `ResourcePermissionsQueryService.IsBlocked_ValidateEvidenceWrongDomain()`: valida reviewer role vs. domain
   - SEC-EV-001 ahora diferencia LegalReviewer (para Legal) vs. SecurityReviewer (para Security)

4. **Tests** (398 líneas):
   - `EvidenceRequirementAndValidationDomainTests.cs`: 29 test cases
     - Creación, mutaciones, validación de campos obligatorios
   - `SecEv001EvidenceValidationAuthzTests.cs`: 5 test cases
     - LegalReviewer puede validar Legal, no Security
     - SecurityReviewer puede validar Security, no Legal

### Why (Contrato)
- **02-domain-implementation-contract.md**: define EvidenceRequirement como entidad de dominio con reviewDomain
- **04-rbac-audit-evidence-gaps-contract.md**: SEC-EV-001 must validate against reviewDomain, NOT file name/MIME/text

### Status
- ✓ Dominio: creado con máquina de estados correcta
- ✓ EF Core: mapeo de entidades y relaciones (DbContext + migration)
- ✓ RBAC: ResourcePermissionsQueryService extendido; backward compatible
- ✓ Tests unitarios: 34 casos, todas las transiciones de estado cubiertas
- ✓ Tenant isolation: multi-tenant en todas las entidades
- ✓ Commit: `fd8da15` (11 files, 1705 insertions)

### Arquitectura
- Ningún cambio de API pública (handlers/endpoints son P1 en siguiente ciclo)
- Auditoría: integración delegada a handlers (AUD-EV-001, AUD-EV-002)
- Waived state: excluido del MVP (será P2 si se añade)

### Decision Document
- Creado: `.squad/decisions/inbox/aragorn-evidence-requirement-validation.md`
- Documenta contexto, decisiones, riesgos, impacto, y responsabilidades fuera de scope

### Próximos Pasos (P1 posterior)
1. ValidateEvidenceCommandHandler (con AuditService integration)
2. Query handlers para EvidenceRequirements y EvidenceValidations
3. API endpoints (POST /requirements, POST /validations/{id}/validate, etc.)
4. Integration tests de autorización (LegalReviewer vs SecurityReviewer)

## 2026-07-10 · P1-009 AuditEvent Contract Formalization (PR #107)

### What
Implementación formal del contrato AuditEvent (P1-009) extendiendo `AuditLog` per **docs/evidata-backend-sprint-2/04-rbac-audit-evidence-gaps-contract.md** §2.1:
- **Shape de AuditEvent (10 campos)**: id, tenantId, eventType (enum: 10 valores AUD-PA-001..AUD-EXP-001), resourceType, resourceId, actorUserId, occurredAt (UTC), result (enum: Success|Failure|Blocked), correlationId, metadata (Dictionary<string,object?> → JSON).
- **Decisiones clave**:
  - Extender AuditLog vs. crear AuditEvent paralelo → menor complejidad migratoria, reutilización de índices.
  - Metadata como Dictionary tipada en código → JSON string en DB (portabilidad futura, JSON queries).
  - CorrelationId propagado vía middleware global (header X-Correlation-Id o Guid.NewGuid().ToString("N")) → disponible antes de autenticación.
  - Enum filtering: eventos con eventType no mapeables excluidos de timeline (catálogo cerrado, no Unknown).
  - Backward compatibility: CreateLegacy() + LogLegacyAsync() para migración gradual.
- **Gaps cerrados**: Result (null → enum), CorrelationId (null → middleware+persistido), Metadata (string → Dictionary), EventType (strings libres → enum cerrado), UserId (nullable confuso → Guid.Empty placeholder).

### Cambios
1. **Nuevos archivos de dominio**:
   - `src/Modules/Audit/Domain/AuditLog.cs` (extendido con nuevos campos)
   - `src/Modules/Audit/Domain/AuditEventType.cs` (enum, 10 valores)
   - `src/Modules/Audit/Domain/AuditEventResult.cs` (enum: Success=1, Failure=2, Blocked=3)

2. **Middleware**:
   - `src/Middleware/CorrelationIdMiddleware.cs`: Acepta header X-Correlation-Id o genera Guid; almacena en context.Items; propaga a respuesta.
   - Extension method: `GetCorrelationId()` para retrieval en handlers.

3. **Service**:
   - `src/Modules/Audit/Application/AuditService.cs`: Signature actualizada LogAsync(..., correlationId, ...).

4. **API**:
   - `src/Modules/Audit/API/AuditController.cs` (DTO incluye correlationId).

5. **Query Handler**:
   - `GetProcessingActivityTimelineQueryHandler`: Mapea AuditLog → TimelineEventDto con todos los nuevos campos.

6. **Migration**:
   - `20260710032536_FormalizAuditEvent.cs`: RenameColumn (Details→Metadata, Action→EventType), AddColumn (CorrelationId varchar(256) nullable, Result int default=0), CreateIndex (IX_audit_logs_CorrelationId).

7. **Tests** (40 nuevos):
   - `AuditServiceTests.cs` (11 tests): LogAsync_WithFormalEventType, WithFailureResult, WithBlockedResult, WithMetadata (serialización JSON), GetMetadata (deserialization), severity compat, empty metadata, null userId.
   - `TimelineQueryHandlerTests.cs` (19 tests): EventWithValidEventType_MapsCorrectly, WithFailureResult_MapsFailure, WithBlockedResult_MapsBlocked, WithUnmatchableEventType_ExcludedFromTimeline, WithNullUserId_UsesGuidEmpty, pagination, ordering, metadata deserialization.
   - Total: 689 unit tests passing.

### Why (Contrato)
- Cierra P1-009 shape contract completo.
- Establece base para P1-010 (TimelineEvent, UI projection).
- Correlación E2E crítica para trazabilidad y debugging distribuido.
- Responde a necesidad de auditoría formal y RBAC enforcement.

### Status
- ✓ Dominio: forma formal con enums y backward compatibility
- ✓ Middleware: correlationId propagado E2E (header → service → domain → API)
- ✓ Tests: comportamiento real (enum parsing, serialization, state transitions), 40 nuevos
- ✓ CI: 689 unit tests passing en 506ms
- ✓ Commits: 8 incrementales (~400 LOC nuevas)
- ✓ Migration: RenameColumn safe, AddColumn con defaults sensatos, legacy methods para compat
- ✓ **MERGED to develop** (PR #107)

### Trabajo Futuro (P1-010, P2)
1. **P1-010 (TimelineEvent)**: Proyección UI derivada de auditoría. Propagar correlationId a todos handlers.
2. **P2 refinements**:
   - Definir SYSTEM constant para userId=null
   - Agregar Unknown enum si es necesario
   - Step-up para acciones críticas
3. Cross-module: Asegurar todos los LogAsync() nuevos pasen correlationId.



