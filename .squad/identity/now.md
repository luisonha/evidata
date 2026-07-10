---
updated_at: 2026-07-10T03:35:00-04:00
focus_area: P1-001 a P1-013 COMPLETADOS Y MERGEADOS a develop (778 tests passing, 0 regressions) + P1-014/P1-015/P1-016 PENDIENTES (RBAC compliance gaps) + follow-up administrativo (develop→main, Dependabot/CodeQL, P1-DEGRADATION)
active_issues: [P1-014 (GenerateOfficialExport state "Active" validation + auto ExportWarning), P1-015 (DownloadEvidence endpoint + 403/422 RBAC), P1-016 (2 blockers restantes de ApproveProcessingActivity: RequiredReviewPending, VersionModifiedAfterReview — domain model dependencies), develop→main reconciliation (pending), Dependabot/CodeQL enable (requires GitHub admin, pending), P1-DEGRADATION (pending)]
---

# What We're Focused On

## Current Sprint Status (P1 Pipeline)

- **P1-001** (Control Endpoint) — GetProcessingActivityControlQueryHandler + endpoint stub. Merged to develop.
- **P1-005/P1-006** (Evidence & Validation) — EvidenceRequirement + EvidenceValidation entities with SEC-EV-001 fail-closed authorization (domain: 500 LOC, tests: 29 state transitions + 8 cross-domain RBAC). 643 unit tests passing. **Merged to develop** (PR #105).
- **P1-007** (GapRule) — Formalización exhaustiva de 11 reglas de detección de brechas: 9 evaluables, 2 formalizadas para P2. ComplianceGap FSM (Open → InCorrection → Resolved → Closed/AutomaticReopen) + BlocksApproval simplificado (Critical && Open). 686 tests passing. **Merged to develop** (PR #106).
- **P1-009** (AuditEvent) — Formalized AuditLog → AuditEvent with 10-field contract (id, tenantId, eventType [enum], resourceType, resourceId, actorUserId, occurredAt, result [enum], correlationId, metadata [Dict→JSON]). CorrelationId propagated E2E via middleware. 40 new tests (11 AuditService + 19 Timeline + 10 more). **Merged to develop** (PR #107).
- **P1-010** (TimelineEvent) — TimelineEvent API endpoint (GET /api/v1/processing-activities/{id}/timeline) con paginación, tenant isolation. 3 critical handlers instrumentados: CreateProcessingActivity (AUD-PA-001), UpdateNode (AUD-NODE-001), Approve (AUD-APP-001). 700 unit tests passing. **Merged to develop** (PR #108).
- **P1-008** (Exports) — GenerateOfficialExport command + export formats (ProcessingActivityPdfSummary|GlobalRatExcel|ApprovalHistory|InternalJson) con SEC-EXP-001 fail-closed authorization. ProcessingActivityReadOnlyQueryAdapter (API layer) bridgea Reporting → ProcessingInventory. 3 behavioral tests reales (NSubstitute). 753/753 unit tests passing. **Merged to develop** (PR #109).
- **P1-011a/b (ValidateEvidence + AcceptGapWithRisk + Auditoría)** ✅ COMPLETADO Y MERGEADO — ValidateEvidenceCommandHandler (SEC-EV-001, fail-closed domain-specific RBAC) + AcceptGapWithRiskCommandHandler (SEC-GAP-001, admin-only + justificación obligatoria) con auditoría AUD-EV-001/AUD-EV-002/AUD-GAP-001, metadata segura (sin texto sensible), CorrelationId E2E. Ciclo: rechazo por fuga de datos sensibles → corrección inmediata Aragorn → re-aprobación Gandalf. 761 tests passing. **Merged to develop** (PR #110).

- **P1-011c (SubmitForReview + Archive + Auditoría)** ✅ COMPLETADO Y MERGEADO — SubmitForReviewCommandHandler (Draft → UnderReview, AUD-REV-001) + ArchiveCommandHandler (AnyState → Archived, AUD-ARC-001) con auditoría E2E, metadata segura, CorrelationId propagación. **Coverage: 9/10 critical auditable actions** (AUD-PA-001, AUD-NODE-001, AUD-REV-001, AUD-APP-001, AUD-ARC-001, AUD-EV-001, AUD-EV-002, AUD-GAP-001, AUD-EXP-001). **1 gap documented**: AUD-ACT-001 (ActivateProcessingActivity) architectural → P1-012 for product decision. Remediation cycle: Aragorn submitted → Gandalf rejected (dishonest description + orphaned code) → Gimli cleaned (95 LOC removed) → Gandalf approved unconditional (3rd review). 769 unit + 9 integration = 778 tests passing. **Merged to develop** (PR #111).

### ✅ COMPLETADO — P1-012: ActivateProcessingActivity / SEC-ACT-001
**P1-012 (Formalize ProcessingActivity.Activate State/Transition)** — Status: **✅ COMPLETADO Y MERGEADO A DEVELOP (2026-07-10)**. 

**Implementation** (PR #112):
- Added `ProcessingActivityStatus.Active` and `ProcessingActivityStatus.Deprecated` states
- Implemented `ProcessingActivity.Activate(Guid activatedBy)` and `ProcessingActivity.SetAsDeprecated(Guid modifiedBy)` domain methods
- Created `ActivateProcessingActivityCommandHandler` in ProcessingInventory module with SEC-ACT-001 RBAC (fail-closed: ProcessOwner cannot activate, only TenantOwner/ComplianceAdmin)
- Audit instrumentation: `ProcessingActivityActivated` event (Success/Blocked) with metadata
- 5 new tests (SEC-ACT-001 authorization, state transitions, deprecation logic), all passing
- CI: GREEN, tests: 774 total (769 baseline + 5 new)

**Decision Made**: Dedicated handler pattern (consistent with ArchiveCommandHandler), direct SecurityDbContext RBAC check (fail-closed semantics).

**Outcome**: AUD-ACT-001 gap closed, SEC-ACT-001 RBAC permission complete.

---

### ✅ COMPLETADO — P1-013: ApproveProcessingActivity / SEC-APP-001 (2/4 Blockers)
**P1-013 (Implement ApproveProcessingActivity Handler)** — Status: **✅ COMPLETADO Y MERGEADO A DEVELOP (2026-07-10)**.

**Implementation** (PR #113):
- Created `ApproveProcessingActivityCommandHandler` in ProcessingInventory module
- **Implemented 2 of 4 business blockers**:
  - ✓ CriticalGapOpen validation (blocks approval if critical gap open)
  - ✓ MissingLegalBasisEvidence validation (blocks approval if evidence missing)
- **Documented 2 of 4 blockers as PENDING** (domain model dependencies, not engineering defects):
  - ⚠ RequiredReviewPending: requires `Review.Status = "Requerida"` enum (Workflow module domain change) → **P1-016**
  - ⚠ VersionModifiedAfterReview: requires `ProcessingActivity.ReviewedAt` timestamp field → **P1-016**
- Authorization: SEC-APP-001 (ProcessOwner ≠ Approver via ResourcePermissionsQueryService fail-closed)
- Audit instrumentation: ProcessingActivityApproved / ApprovalBlocked (Success/Blocked/Denied)
- 4 new tests (SEC-APP-001 authorization, happy path, CriticalGapOpen blocker, state validation), all passing
- CI: GREEN, tests: 778 total (769 baseline + 9 from PR #112/#113)

**Decision Made**: Honest gap tracking (2 pending blockers explicitly documented with rationale). Matches team culture post-PR#111 incident.

**Outcome**: 2/4 blockers complete, 2/4 blockers tracked in P1-016 (domain model work), SEC-APP-001 RBAC permission complete.

---

### 🔄 PENDING — P1-014: GenerateOfficialExport Completeness (SEC-EXP-001)
**P1-014 (Complete GenerateOfficialExport Compliance)** — Status: **PENDIENTE**. Priority: **HIGH**. 

**Background**: Gandalf's RBAC audit (2026-07-10) identified compliance gaps in SEC-EXP-001 implementation.

**Gaps Found**:
1. State "Active" NOT validated — handler only accepts "Approved", but contract requires "Approved OR Active"
2. ExportWarning NOT generated automatically — handler should auto-warn if risks/gaps/evidence pending exist
3. HTTP error code incorrect — InvalidOperationException thrown instead of 422 UnprocessableEntity with code `OfficialExportRequiresApproval`

**Requirements**:
- Accept status `ProcessingActivityStatus.Active` in addition to `Approved`
- Implement automatic ExportWarning generation when:
  - Blocking evidence pending
  - Critical gaps open
  - Required reviews pending
- Return HTTP 422 with `OfficialExportRequiresApproval` code for invalid state
- Audit: `OfficialExportGenerated` (Success) / `ExportGenerationBlocked` (Denied)

**Implementation Path**:
- Update ExportService.RequestExportAsync() validation logic
- Add ExportWarning.CreateAutomatic() logic to detect riskFlags/gaps/evidence state
- Ensure 422 error code propagates to API layer
- Add test cases for Active state + automatic warning generation

**Blocked By**: None (ready to start).

---

### 🔄 PENDING — P1-015: DownloadEvidence Endpoint + Authorization (SEC-EVDOWN-001)
**P1-015 (Implement DownloadEvidence Endpoint & Authorization)** — Status: **PENDIENTE**. Priority: **HIGH**.

**Background**: Gandalf's RBAC audit (2026-07-10) identified critical gaps in SEC-EVDOWN-001 implementation.

**Gaps Found**:
1. HTTP endpoint NOT exposed — EvidenceController lacks `[HttpGet("{id:guid}/download")]` endpoint. Service exists (EvidenceDownloadService) but no API route.
2. Authorization validation NOT enforced — Service generates SAS before checking RBAC (should fail with 403 if Viewer tries Confidential/Sensitive evidence)
3. Audit events NOT implemented — No `AuditEventType.EvidenceDownloaded` (success) / `EvidenceAccessDenied` (denial)
4. HTTP error codes missing — Should return 403 SensitiveEvidenceRestricted or 422 ReasonRequiredForSensitive

**Requirements**:
- Implement `[HttpGet("{id:guid}/download")]` endpoint in EvidenceController
- Validate authorization BEFORE generating SAS:
  - Check user can read evidence + treatment
  - Block Viewer role for Confidential/Sensitive evidence (return 403)
  - Require `reason` field for Sensitive evidence (return 422 if missing)
- Audit results:
  - AuditEventType.EvidenceDownloaded (Success)
  - AuditEventType.EvidenceAccessDenied (Failure/Blocked)
- Return SAS URL only if authorized

**Implementation Path**:
- Add endpoint to EvidenceController
- Inject ResourcePermissionsQueryService for RBAC check
- Implement authorization validation logic with proper HTTP status codes
- Extend AuditLog → audit success/denial with proper event types
- Add test cases (authorization flows, error codes, audit trail)

**Blocked By**: None (ready to start).

---

### 🔄 PENDING — P1-016: ApproveProcessingActivity Remaining Blockers (Follow-up to P1-013)
**P1-016 (Implement 2 Remaining Approval Blockers)** — Status: **PENDIENTE**. Priority: **P1** (compliance gap).

**Background**: PR #113 (P1-013) completed with 2/4 blockers. Remaining 2 require domain model extensions.

**Blockers to Implement**:
1. **RequiredReviewPending** — Requires `Review.Status` enum with "Requerida" state in Workflow module
   - Precondition: Workflow.Review must support status = "Requerida"
   - Validation: ApproveProcessingActivityCommandHandler must check all required reviews are complete (status ≠ "Requerida")
   - Return 422 if any required review still pending

2. **VersionModifiedAfterReview** — Requires `ProcessingActivity.ReviewedAt` timestamp field
   - Precondition: ProcessingActivity domain must track when version was last reviewed
   - Validation: Compare ProcessingActivity.LastModifiedAt vs ReviewedAt; block approval if modified after review
   - Return 422 if version changed after review

**Requirements**:
- Add `Review.Status` enum state "Requerida" (Workflow module domain)
- Add `ProcessingActivity.ReviewedAt` timestamp field (ProcessingInventory domain)
- Update ApproveProcessingActivityCommandHandler to validate both blockers before approval
- Implement tests for both blockers (22 blockers checked, 2 new ones added)
- Audit: Blocked reason captures blocker code (RequiredReviewPending, VersionModifiedAfterReview)

**Implementation Path**:
1. Create/extend Review.Status enum in Workflow module (add "Requerida" state)
2. Add ProcessingActivity.ReviewedAt field (nullable DateTime) to ProcessingInventory domain
3. Update ApproveProcessingActivityCommandHandler validation logic:
   - Query reviews for this activity, check if any status = "Requerida"
   - Compare LastModifiedAt vs ReviewedAt for version change detection
4. Add comprehensive tests (both blockers, state transitions, audit trail)
5. Ensure no regressions (run full test suite)

**Blocked By**: None (ready to start immediately post-P1-013).

**Impact**: Achieves **4/4 blockers implemented** for SEC-APP-001, completing RBAC compliance for ApproveProcessingActivity.

### 🔄 FOLLOW-UP ADMINISTRATIVE (Secondary Priority)
- **develop → main reconciliation** — Pending: verify all P1 items stable (✅ done), run integration smoke tests, prepare release branch.
- **Dependabot + CodeQL enable** — Pending: requires GitHub admin permissions (luisonha). Repository settings → Security & analysis → Enable Dependabot (alerts, security updates, version updates), CodeQL (3-branch analysis workflow).
- **P1-DEGRADATION** (State Reconciliation at Runtime) — User requirement: handle schema migrations + stale cache at app start (from PR #104 partial degradation tests). Deferred; will schedule after P1-011a/b/c complete.

### ⏳ PREVIOUSLY PAUSED (No Change)
- **P1-004** (ResourcePermissionsViewModel Producer) — Depends on P1-007 (GapRule, now complete). Legolas (Security Engineer) will implement producer + performance optimization. **No bloqueante vs. P1-011a/b/c**.
- **P1-001 Full Composition** — Still paused pending P1-004 completion + architectural decision on circular dependencies.

## Team Capacity Notes
- **Aragorn** (Backend Core) — Available immediately post-PR #109 merge. **PRIORITY**: P1-011a (ValidateEvidence) + P1-011b (AcceptGapWithRisk) FIRST. Then P1-011c or P1-004.
- **Gandalf** (Architect/Lead) — Reviewing incoming PRs, owns P1 gate decisions.
- **Gimli** (Data Architect) — Available for P2 deferred items or support.
- **Legolas** (Security Engineer) — Owns P1-004 (producer) + RBAC strategy (Entra + local roles).

## Previous Priorities (Unchanged)
- **Identity Strategy** — Production: Entra ID (via JwtCurrentUserContext/Entra External ID). Development: LocalDevAuthenticationHandler with real RBAC (Role/Permission model, not simulated).
- **Close RBAC Gap** — Migrate legacy Permission/Role (string resource:action) to RbacRoleCode/PermissionCode per contract 04-rbac-audit-evidence-gaps-contract.md §1. Owner: Legolas.

