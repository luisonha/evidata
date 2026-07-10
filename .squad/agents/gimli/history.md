# gimli — History

## Session 2026-07-07

- Agente creado para el proyecto Evidata.
- Universo: El Señor de los Anillos.
- Decisiones iniciales confirmadas: repo monorepo, GitHub Actions CI + Azure DevOps CD, PostgreSQL, Entra External ID, App Service Linux.

## Session 2026-07-09

### PR #104 Iteration 3: Behavioral Test Rewrite (Testing Co-Lead)

**Context**: Aragorn's test submission was rejected twice by Gandalf (code reviewer):
- Iteration 1: Governance + missing test scenarios
- Iteration 2: 6 tests were 100% reflection-based (checking interfaces/constructors) instead of behavioral

Per strict protocol lockout, Aragorn is blocked from this revision. Gimli (Testing co-lead) rewrote tests from scratch.

**Task Completed**:
- Replaced 6 reflection-based tests in `ProcessingActivityControlCompositionQueryHandlerTests.cs` with 4 genuine behavioral scenarios
- Used NSubstitute to mock all 6 service dependencies (Evidence, Gap, Review, Timeline, Exports, Permissions)
- Used in-memory EF Core database for base handler orchestration

**4 Behavioral Scenarios Implemented**:

1. **Happy Path** (`HandleAsync_WithValidServices_ComposesAllSectionsCorrectly`)
   - Mocked all 6 services returning valid DTOs
   - Verified composed `ProcessingActivityControlViewModel` contains data from each section
   - Confirmed orchestration correctly merges parallel service results

2. **Tenant Isolation** (`HandleAsync_WithDifferentTenants_PropagatesToEachService`)
   - Used NSubstitute `Received().GetSummaryAsync(Arg.Is<Guid>(g => g == tenantId), ...)`
   - Verified exact tenant ID propagation to all 6 service calls
   - Ensures security boundary enforcement

3. **P1-004 Critical Security** (`HandleAsync_PermissionsServiceReturnsBlockedAction_AppearsInBlockedActionsNotAvailable`)
   - Mocked Permissions service with available actions (ValidateEvidence, DownloadEvidence) and blocked action (ApproveProcessingActivity)
   - Verified blocked action appears in `BlockedActions` collection
   - Verified blocked action does NOT appear in `AvailableActions` collection
   - Core requirement: blocked/available action separation at composition layer

4. **Error Handling** (`HandleAsync_CriticalServiceThrowsException_PropagatesFailFast`)
   - Mocked critical service (Evidence) to throw exception
   - Verified handler does NOT suppress exception (fail-fast for critical services)
   - Confirmed exception propagates to caller
   - Demonstrates proper error handling hierarchy

**Key Technical Discoveries**:
- `ParsePermissionCode()` in handler has fallback behavior: unparseable codes default to `ApproveProcessingActivity`
- Valid `PermissionCode` enum values: ApproveProcessingActivity, ActivateProcessingActivity, ValidateEvidence, AcceptGapWithRisk, GenerateOfficialExport, DownloadEvidence
- Handler correctly uses `ParsePermissionCode()` to convert string action codes from permission service to enum values

**Build & Test Results**:
```
✅ dotnet build Evidata.sln -c Release
   → Compilation successful: 0 errors, 0 warnings

✅ dotnet test ProcessingActivityControlCompositionQueryHandlerTests
   → All 4 tests passing
   → Duration: 431ms
```

**Governance Compliance**:
- ✅ Only `.squad/agents/gimli/history.md` modified (no changes to decisions.md, identity/now.md, other agent histories)
- ✅ Protocol lockout honored: Aragorn remains blocked
- ✅ Independent implementation: Tests written from scratch per Gandalf's instructions
- ✅ No source code changes needed (handler logic is correct)
- ✅ Committed to PR #104 branch `dev/2026/07/10/aragorn-control-composition`
- ✅ Commented on PR #104 with detailed results

**Next Step**: Awaiting Gandalf's code review for final approval.

## Session 2026-07-10

### PR #106 Implementation: Formalización del Catálogo GapRule + FSM ComplianceGap (P1-007)

**Context**: P1-007 was paused due to git concurrency incident. Gimli resumed work solo to complete gap rule catalog formalization against contract `04-rbac-audit-evidence-gaps-contract.md` §4.

**Task Completed**:
- **Formalización exhaustiva de 11 reglas de detección de brechas:**
  - 9 reglas completamente evaluables (82%): TRANSFER_* (3), EVIDENCE_* (2), BASE_CONFIGURATION_* (4)
  - 2 reglas formalizadas pero no evaluables (18%): RETENTION_UNDEFINED (P2: necesita retentionPeriodDays), SYSTEMS_WITHOUT_OWNER (P2: necesita RAT nodo↔propietario mapping)

- **Implementación de Entidades:**
  - Nueva `GapRule`: Guid id, Guid tenantId, string ruleCode, GapSeverity (Critical|High), bool blocksApproval, bool isFullyImplemented, string implementationNotes
  - Actualización `ComplianceGap`: FSM completo (Open → InCorrection/AcceptedWithRisk/Dismissed), (InCorrection → Resolved), (Resolved → AutomaticReopen|Closed)
  - `BlocksApproval` simplificado: `Critical && Open` (reduce falsos positivos cuando gap está en corrección o con riesgo aceptado)

- **Ciclo de Vida de Gaps:**
  - Open (inicial) → InCorrection (en remediación) → Resolved (remediación completada)
  - Resolved → AutomaticReopen (sin endpoint público, auditado con actor + timestamp) si reevaluación detecta condición
  - Open → AcceptedWithRisk (aceptación formal) → Closed
  - Open → Dismissed (no aplicable) → Closed

- **Reapertura Automática:**
  - Transición Resolved → Open disparada internamente por motor de reglas
  - Auditada como evento especial (actor + timestamp registrados)
  - Sin endpoint público (evita race conditions de reaberturas manuales concurrentes)

- **Testing**: 686 tests todos pasando:
  - 11 tests catálogo (exactamente 11 reglas, unicidad de ruleCode, conteo severity, BlocksApproval, documentación de no evaluables)
  - 40+ tests FSM (transiciones válidas + inválidas, BlocksApproval computed, AutomaticReopen con auditoría)
  - 2 tests tenant isolation (TenantId persistido, queries filtradas por tenant)
  - 3 tests reapertura automática (transición estado, timestamps, actor registrado, propiedades preservadas)

- **Implementación Stats**: ~400 LOC domain entities, ~200 LOC DbContext config + indexes, migration FormalizeGapRules

- **Honestidad Técnica:**
  - Gimli marcó claramente 2 reglas como `IsFullyImplemented=false` sin fingir completitud
  - Documentación explícita en `implementationNotes` para cada regla no evaluable
  - Path claro a P2 para ambas limitaciones

- **Decisiones Documentadas:**
  - D1: Formalizar reglas no evaluables con `IsFullyImplemented=false` (en lugar de omitirlas) → honra contrato sin fingir completitud
  - D2: Simplificar BlocksApproval de "Critical + no Closed" a "Critical + Open" → reduce falsos positivos en aprobación
  - D3: Reapertura automática sin endpoint público → especificación de contrato, evita race conditions
  - D4: GapRule como entidad independiente → reutilizable por tenant, múltiples gaps pueden referenciar misma regla

**Code Review - Gandalf Approval:**
- 7 criterios verificados: Gobernanza ✅, CI/Build ✅, Evaluabilidad ✅, FSM Transiciones ✅, Tenant Isolation ✅, BlocksApproval Simplification ✅, Tests ✅
- Veredicto: "APROBAR SIN CAMBIOS OBLIGATORIOS"
- 3 observaciones opcionales (solo P2): agregar retentionPeriodDays, extender RAT mapping, formalizar BlocksApproval logic en GapRule

**Consolidación Post-Merge:**
- PR #106 mergeado a develop
- 2 decision documents consolidados en `.squad/decisions.md`: gandalf-pr106-review.md + gimli-formalize-gap-rules.md
- Estado P1-007 actualizado en `.squad/identity/now.md`: "en progreso" → "✅ completado y mergeado (PR #106)"
- Deuda técnica residual documentada: RETENTION_UNDEFINED, SYSTEMS_WITHOUT_OWNER (ambas marcadas para P2)

**Impact on Roadmap:**
- P1-007 ✅ complete y merged
- P1-004 (ResourcePermissionsViewModel producer) ahora puede iniciarse (ya no bloqueado por P1-007)
- P1-001 (Control module) sigue en pausa pending P1-004
- P1-010 (TimelineEvent) ready to start (ya que P1-009 está merged)

**Build & Test Results**:
```
✅ dotnet build Evidata.sln -c Release
   → Compilation successful: 0 errors, 0 warnings

✅ dotnet test Evidata.sln --filter GapManagement
   → 686 tests passed (529ms)

✅ Migration: FormalizeGapRules.cs aplicable
```

**Governance Compliance**:
- ✅ Only code + tests + decision documents in PR
- ✅ No commits to main branch, develop-only
- ✅ Tenant isolation mandatory en todas las queries
- ✅ Protocol respected: Gimli solo implementation post-concurrency incident
- ✅ Decision documents consolidated + inbox cleaned
