---
updated_at: 2026-07-10T00:30:38Z
focus_area: Identity & RBAC Critical Work — Complete
status: closed-merged
active_issues: []
---

# What We're Focused On

**Status**: ✅ Identity & RBAC critical work from PR #102 **CLOSED & MERGED to develop**.

## Completed Cycle (PRs #97–#102)

All critical Identity/Security/RBAC tasks are **complete and merged to develop**:
- **PR #97**: Bridged legacy Permission/Role schema ↔ contract RbacRoleCode/PermissionCode; seeded 7 system roles + 6 permissions; IResourcePermissionsQueryService ready for P1-004
- **PR #98**: Closed critical authorization gap (added [Authorize] to 6 unprotected controllers; implemented global FallbackPolicy; marked 7 public endpoints with [AllowAnonymous])
- **PR #99**: Closed privilege escalation (TenantOwnerOrComplianceAdminRequirement policy for role assignment)
- **PR #102**: Reconciled main/develop divergence (cherry-picked PR #98 work to develop; resolved Program.cs conflict)

**Test Results**: 597/597 passing. No security regressions.

## Architecture Decisions Locked
1. Enum→Schema Seed Mapping (contract enums = semantic layer; schema seeded with exact names)
2. Global FallbackPolicy (require authenticated user by default)
3. TenantOwnerOrComplianceAdmin policy (fine-grained RBAC for sensitive operations)
4. Branching rule: always use `gh pr create --base develop --head <branch>` explicitly

## Pending (User Decision, Not Urgent)
1. **Reconcile develop→main**: main still lacks RBAC work from PR #97/#99. Not blocking; candidate for future sprint when develop stabilizes.
2. **Backlog (Ready to Start)**:
   - Resume `/control` endpoint (P1-001)—now has RBAC foundation from PR #97
   - Complete ResourcePermissionsViewModel producer (P1-004)—IResourcePermissionsQueryService ready to consume

## Cross-Tenant Security Verified
- Handler filters by (UserId, TenantId)
- TenantOwner of tenant A cannot assign roles in tenant B
- Attack scenario blocked: different TenantId → 403 Forbidden
