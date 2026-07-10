---
updated_at: 2026-07-10T02:35:00Z
focus_area: P1-009 completed + P1-010 (TimelineEvent) pending + P1-008 (Exports) pending
active_issues: [P1-010 (TimelineEvent, depends on P1-009 AuditEvent), P1-008 (Exports), develop→main reconciliation (pending)]
---

# What We're Focused On

## Current Sprint Status (P1 Pipeline)

### ✅ COMPLETED
- **P1-005/P1-006** (Evidence & Validation) — EvidenceRequirement + EvidenceValidation entities with SEC-EV-001 fail-closed authorization. Domain: ~500 LOC. Tests: 29 state transitions + 8 cross-domain authorization tests (all 643 unit tests passing). **Merged to develop** (PR #105, 2 iterations: reject → fix → approve).
- **P1-009** (AuditEvent) — Formalized AuditLog → AuditEvent with 10-field contract (id, tenantId, eventType [enum], resourceType, resourceId, actorUserId, occurredAt, result [enum], correlationId, metadata [Dict→JSON]). CorrelationId propagated E2E via middleware. Backward compatibility via CreateLegacy/LogLegacyAsync. Tests: 40 new (11 AuditService + 19 Timeline + 10 more, all behavior-driven). **Merged to develop** (PR #107, 1 iteration: preReview+Gandalf approval).

### 🔄 IN PROGRESS
- **P1-008** (Exports) — GenerateOfficialExport command + export format. Coordinates with Workflow/Reporting modules. Gate: must not break audit trail (AUD-EXP-001 event type already defined in P1-009 enum).

### ✅ COMPLETED (P1 Cycle Closed)
- **P1-007** (GapRule) — ✅ completado y mergeado (PR #106). Formalización exhaustiva de 11 reglas de detección de brechas: 9 evaluables, 2 formalizadas para P2. ComplianceGap FSM (Open → InCorrection → Resolved → Closed/AutomaticReopen) + BlocksApproval simplificado (Critical && Open). 686 tests passing. Deuda técnica residual clara: RETENTION_UNDEFINED (necesita retentionPeriodDays), SYSTEMS_WITHOUT_OWNER (necesita RAT nodo↔propietario mapping).

### ⏳ PENDING (Next Priorities)
- **P1-010** (TimelineEvent) — UI projection derived from AuditEvent. Depends on P1-009 (now complete). Will map AuditEvent catalog to TimelineEvent DTO. Recommended owners: Aragorn + Gandalf (architecture).
- **P1-008** (Exports) — GenerateOfficialExport command + export format. Coordinates with Workflow/Reporting modules. Gate: must not break audit trail (AUD-EXP-001 event type already defined in P1-009 enum).
- **P1-004** (ResourcePermissionsViewModel Producer) — Depends on P1-007 (GapRule, now complete). Legolas (Security Engineer) will implement producer + performance optimization (caching, N+1 avoidance). Gate: ResourcePermissionsViewModel must compose AvailableActions/BlockedActions based on gap catalog + user RBAC role.
- **P1-001** (Control Module Assembly) — Still paused pending P1-004 completion. Gate: ResourcePermissionsViewModel producer (AvailableActions/BlockedActions) must work correctly before `/control` can consume it.
- **P1-DEGRADATION** (State Reconciliation at Runtime) — User requirement: handle schema migrations + stale cache at app start. Deferred; will schedule after P1-004.
- **develop → main reconciliation** — Pending: verify all P1 items stable before merging develop to main branch.

## Previous Priorities (Unchanged)
- **Identity Strategy** — Production: Entra ID (via JwtCurrentUserContext/Entra External ID). Development: LocalDevAuthenticationHandler with real RBAC (Role/Permission model, not simulated).
- **Close RBAC Gap** — Migrate legacy Permission/Role (string resource:action) to RbacRoleCode/PermissionCode per contract 04-rbac-audit-evidence-gaps-contract.md §1. Owner: Legolas.

## Team Capacity Notes
- **Operational Incident Resolved**: Git worktree concurrency issue documented. Future parallel agents require worktree isolation or serial scheduling.
- **Aragorn** (Backend Core) — Available after PR #107 merge. Ready for P1-010 (TimelineEvent) or P1-008 (Exports).
- **Gimli** (Data Architect) — Working solo on P1-007 (GapRule). No parallel agents until P1-007 complete.
- **Gandalf** (Architect/Lead) — Reviewing incoming PRs, owns P1 gate decisions, available for P1-010 architecture.
- **Legolas** (Security Engineer) — Owns P1-004 (producer) + RBAC strategy (Entra + local roles).

