---
updated_at: 2026-07-10T00:56:16Z
focus_area: P1-001 a P1-011b COMPLETADOS y MERGEADOS a develop + P1-011c (5 handlers restantes de auditoría, PRIORIDAD MEDIA/P2) + pendientes follow-up administrativo (develop→main, Dependabot/CodeQL, P1-DEGRADATION)
active_issues: [P1-011c (Remaining 5 handlers: SubmitForReview/Activate/Archive/RejectEvidence/GenerateOfficialExport, MEDIUM PRIORITY/P2), develop→main reconciliation (pending), Dependabot/CodeQL enable (requires GitHub admin, pending), P1-DEGRADATION (partial degradation tests PR #104, pending)]
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

### 🔄 MEDIUM PRIORITY - P2 PIPELINE
**P1-011c (Remaining 5 Handlers + Audit)** — PRIORITY: P2 · Status: ⏳ PENDIENTE. Scope: SubmitForReview (AUD-REV-001), Activate (AUD-ACT-001), Archive (AUD-ARC-001), RejectEvidence (AUD-EV-002), GenerateOfficialExport (AUD-EXP-001) + auditoría + tests (mismo patrón fail-closed que P1-011a/b, metadata no-sensible). Estimación: 6-8 horas total.

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

