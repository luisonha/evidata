---
updated_at: 2026-07-10T02:15:00Z
focus_area: Evidence & Security formalization + P1 pipeline
active_issues: [P1-DEGRADATION (pending), reconciliation develop→main (pending)]
---

# What We're Focused On

## Current Sprint Status (P1 Pipeline)

### ✅ COMPLETED
- **P1-005/P1-006** (Evidence & Validation) — EvidenceRequirement + EvidenceValidation entities with SEC-EV-001 fail-closed authorization. Domain: ~500 LOC. Tests: 29 state transitions + 8 cross-domain authorization tests (all 643 unit tests passing). **Merged to develop** (PR #105, 2 iterations: reject → fix → approve).

### 🔄 IN PROGRESS
- **P1-007** (GapRule) — Gimli relaunching in solo mode (re-attempt after git concurrency incident during PR #105). Expected: reconcile requirement review domain (Legal|Security) against available gaps, emit GapRule entities.

### ⏳ PENDING
- **P1-001** (Control Module Assembly) — Paused pending Identity & RBAC completion. Gate: ResourcePermissionsViewModel producer (AvailableActions/BlockedActions) must work correctly before `/control` can consume it.
- **P1-004** (ResourcePermissionsViewModel Producer) — Dependent on P1-007 (GapRule). Legolas (Security Engineer) will implement producer + performance optimization (caching, N+1 avoidance).
- **P1-DEGRADATION** (State Reconciliation at Runtime) — User requirement: handle schema migrations + stale cache at app start. Deferred; will schedule after P1-004.
- **develop → main reconciliation** — Pending: verify all P1 items stable before merging develop to main branch.

## Previous Priorities (Unchanged)
- **Identity Strategy** — Production: Entra ID (via JwtCurrentUserContext/Entra External ID). Development: LocalDevAuthenticationHandler with real RBAC (Role/Permission model, not simulated).
- **Close RBAC Gap** — Migrate legacy Permission/Role (string resource:action) to RbacRoleCode/PermissionCode per contract 04-rbac-audit-evidence-gaps-contract.md §1. Owner: Legolas.

## Team Capacity Notes
- **Operational Incident Resolved**: Git worktree concurrency issue documented. Future parallel agents require worktree isolation or serial scheduling.
- **Aragorn** (Backend Core) — Available after PR #105 merge. Will support P1-004 or pick next priority ticket.
- **Gimli** (Data Architect) — Working solo on P1-007 (GapRule). No parallel agents until P1-007 complete.
- **Gandalf** (Architect/Lead) — Reviewing incoming PRs, owning P1 gate decisions.
- **Legolas** (Security Engineer) — Owns P1-004 (producer) + RBAC strategy (Entra + local roles).

