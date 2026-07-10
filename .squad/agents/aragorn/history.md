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

## 2026-07-10 · P1-010 TimelineEvent API + 3 Critical Handlers Audit Instrumentation (PR #108)

### What
Implementación formal de P1-010: TimelineEvent API read-model endpoint exposing AuditLog projection + audit instrumentation para 3 critical command handlers (CreateProcessingActivity, UpdateNode, Approve).

### Scope
- **TimelineEvent API Endpoint**: `GET /api/v1/processing-activities/{id:guid}/timeline`
  - Paginación: skip ≥ 0, 1 ≤ take ≤ 500
  - Tenant isolation via currentUser.TenantId
  - OpenAPI documentation (ProducesResponseType 200/400/401/403/404)
  - Returns TimelineEventViewModelEnvelope con IReadOnlyList<TimelineEventViewModel>
  - Shape: EventType, EventTypeLabelKey, ResourceType, ResourceId, ActorUserId, OccurredAt, Result, ResultLabelKey, CorrelationId, Metadata, Id
  - Ordenamiento: OccurredAt descending (más reciente primero)
  - Metadata handling: Robust deserialization, silent degradation si JSON malformado

- **3 Handlers Instrumentados**:
  1. **CreateProcessingActivityCommandHandler** (AUD-PA-001)
    - Audit logging post-SaveChangesAsync (fire-and-forget, non-blocking)
    - Metadata: name, description, controller, department
  2. **UpdateProcessingActivityCommandHandler** (AUD-NODE-001)
    - Constructor nuevo requires IAuditService injection
    - Metadata: field updates (name, description, controller, department changes)
    - Fire-and-forget pattern
  3. **ProcessingActivityVersionService.ApproveAndSnapshotAsync** (AUD-APP-001)
    - Constructor nuevo requires IAuditService injection
    - Metadata: snapshotVersion, retentionRequired
    - Fire-and-forget pattern

- **Architectural Decisions**:
  - ITimelineQueryService abstraction (DI swappable)
  - Enum name alignment fix (UpdateNode vs UpdateProcessingActivityNode, etc.)
  - Metadata Dictionary→JSON serialization
  - Tenant isolation at repository level
  - In-memory pagination (MVP, migrará DB-level en P2 si large resultsets)
  - Fire-and-forget (no-blocking, riesgo audit loss si failure)

### Changes
1. **Nuevos archivos**:
   - `tests/Evidata.Tests.Unit/Api/Queries/GetProcessingActivityTimelineQueryHandlerTests.cs` (11 test cases)

2. **Modificados**:
   - `src/Modules/ProcessingInventory/Api/ProcessingActivitiesController.cs` (added GetTimeline endpoint)
   - `src/Modules/ProcessingInventory/Evidata.Modules.ProcessingInventory.csproj` (added Audit reference)
   - `src/Modules/Audit/Application/DTOs/TimelineEventViewModel.cs` (added envelope)
   - `src/Modules/ProcessingInventory/Application/Commands/CreateProcessingActivityCommandHandler.cs` (added audit logging)
   - `src/Modules/ProcessingInventory/Application/Commands/UpdateProcessingActivityCommandHandler.cs` (added audit logging + IAuditService injection)
   - `src/Modules/ProcessingInventory/Infrastructure/Versioning/ProcessingActivityVersionService.cs` (added audit logging + IAuditService injection)
   - `src/Modules/Audit/Application/Queries/GetProcessingActivityTimelineQueryHandler.cs` (fixed enum names)
   - `tests/Evidata.Tests.Unit/Audit/TimelineQueryHandlerTests.cs` (updated test event names)

### Why (Contrato)
- Cierra P1-010 scope (TimelineEvent API read-model + 3 handlers instrumentados)
- Establece base infraestructura para P1-011a/b/c (ValidateEvidence + AcceptGapWithRisk + remaining 5 handlers con auditoría)
- Responde a necesidad de bitácora UI auditada y trazabilidad E2E

### Status (Pre-Corrections)
- ✓ Endpoint structure correct: paginación, tenant isolation, OpenAPI doc
- ✓ 3 handlers audit-instrumented correctly
- ✓ Tests: 11 cases covering projection, pagination, chronological order, tenant isolation, metadata handling
- ⚠️ ISSUE 1: 2 tests violated "CERO reflection-based tests" quality gate
  - ChronologicalOrder_ShouldBeMostRecentFirst: used `typeof(AuditLog).GetProperty("OccurredAt").SetValue(...)`
  - MalformedMetadata_ShouldNotBreakTimeline: used `typeof(AuditLog).GetProperty("Metadata").SetValue(...)`
- ⚠️ ISSUE 2: DI verification needed for 2 new dependencies (UpdateProcessingActivityCommandHandler, ProcessingActivityVersionService)
- ⏳ CI: Pending (build-and-test jobs not complete at initial review)

### Status (Post-Corrections)
- ✅ Reflection tests FIXED: ChronologicalOrder now uses `occurredAtOverride` factory param, MalformedMetadata now uses `metadataJsonRaw` factory param
- ✅ CI GREEN: build-and-test checks passed
- ✅ DI VERIFIED: All three handlers correctly registered with IAuditService dependency
- ✅ Governance: No .squad/decisions.md or .squad/identity/now.md modifications
- ✅ Follow-up DOCUMENTED: aragorn-timeline-followup.md registers all three post-merge items (P1-011a/b/c)
- ✅ **APPROVED AND MERGED to develop** (PR #108)

### Test Results
```
dotnet test tests/Evidata.Tests.Unit/Evidata.Tests.Unit.csproj --no-build

Result:
  Con error:     0
  Superado:    700
  Omitido:     0
  Total:       700
  Duración: 520 ms
```

### Known Limitations / Phase 2 Work
1. **CorrelationId propagation**: Contract specifies X-Correlation-Id header. Currently not captured in endpoint or command handlers. Requires middleware enhancement (Phase 2).
2. **IP Address capture**: AuditService accepts `ipAddress` parameter; currently not passed from API layer. Requires HttpContext.Connection.RemoteIpAddress extraction (Phase 2).
3. **Pagination performance**: Skip/take applied in-memory. For large audit logs, should migrate to DB-level paging (Phase 2+).
4. **Silent event exclusion**: Unmappable event types excluded from timeline without warning. Should log warning for debugging (Phase 2).
5. **Fire-and-forget audit loss**: If IAuditService fails, audit is lost without retry. Mitigation depends on infrastructure (queue/retry pattern) outside scope (Phase 2+).

### Post-Merge Backlog (CRITICAL)
- **P1-011a**: Implement ValidateEvidence handler + audit instrumentation (SEC-EV-001). PRIORITY: P1, BLOCKING: YES.
- **P1-011b**: Implement AcceptGapWithRisk handler + audit instrumentation (SEC-GAP-001). PRIORITY: P1, BLOCKING: YES.
- **P1-011c**: Implement remaining 5 handlers (SubmitForReview, Activate, Archive, RejectEvidence, GenerateOfficialExport) + audit + tests. PRIORITY: P2, BLOCKING: DEFER.
- All registered explicitly in `.squad/decisions/inbox/aragorn-timeline-followup.md` (consolidated to decisions.md by Scribe).

### Commits (Incremental)
1. `84fb1bd` – feat(timeline): expose GET /api/v1/processing-activities/{id}/timeline endpoint
2. `0015059` – feat(audit): implement audit logging in command handlers
3. `f35b881` – test(timeline): comprehensive tests for P1-010 TimelineEvent projection
4. `7971909` – fix(test): update enum names in timeline tests to match canonical domain enum
5. (Post-corrections) – fix(test): refactor reflection-based tests to use factory methods with override params
6. (Post-corrections) – verify(di): confirm IAuditService DI registration for all new handler dependencies

### Quality Checklist
| Criterion | Status | Note |
|---|---|---|
| CERO reflection-based tests | ✅ | All tests refactored to use factory methods |
| CERO smoke tests | ✅ | Each test validates specific projection/pagination/isolation feature |
| Tenant isolation explicit | ✅ | Test case verifies two tenants cannot leak events |
| Pagination tested | ✅ | Skip/take validation in endpoint; in-memory apply verified |
| Metadata handling | ✅ | Malformed JSON handled gracefully |
| No manual decisions edits | ✅ | Decision written to `.squad/decisions/inbox/` (consolidated by Scribe) |
| Build passes | ✅ | `dotnet build Evidata.sln --no-restore` → 0 errors, 25 warnings (pre-existing) |
| All tests pass | ✅ | 700/700 unit tests passing |
| CI GREEN | ✅ | AWS CodeBuild checks passed |
| DI verified | ✅ | All IAuditService dependencies correctly registered |

### Next Steps (Aragorn — PRIORITY POST-MERGE)
1. **PRIORITY: Start P1-011a immediately** — ValidateEvidence + audit instrumentation (3-4 hours, BLOCKING)
2. **Follow-up: P1-011b** — AcceptGapWithRisk + audit instrumentation (3-4 hours, BLOCKING)
3. **Then: P1-011c** — Remaining 5 handlers + audit + tests (6-8 hours, P2 but recommended immediate)
4. **Parallel: P1-008** — Exports handler (no blocker, lower priority vs. security-critical items)