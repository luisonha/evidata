# legolas — History

## Session 2026-07-07

- Agente creado para el proyecto Evidata.
- Universo: El Señor de los Anillos.
- Decisiones iniciales confirmadas: repo monorepo, GitHub Actions CI + Azure DevOps CD, PostgreSQL, Entra External ID, App Service Linux.

## Session 2026-07-09

### RBAC & Identity Security Gap Closure (PR #97)

**Closed**:
- Implemented bridge between legacy Permission/Role schema and contract-defined RbacRoleCode/PermissionCode enums.
- Implemented all 6 critical RBAC rules from contract (SEC-APP-001, SEC-ACT-001, SEC-EV-001, SEC-GAP-001, SEC-EXP-001, SEC-EVDOWN-001).
- Created IResourcePermissionsQueryService to produce real data for ResourcePermissionsViewModel (P1-004).
- Seeded 7 system roles and 6 permissions in SecurityDbContext with predefined mappings.
- Added 12 comprehensive security tests covering all rules; 69 total security tests passing.

**Architecture Decision**:
- Enum→Schema Seed Mapping: Contract enums become the public semantic layer; legacy schema seeded with exact names.
- Rationale: Simplicity, no breaking changes, future-proof for ResourcePermissionsViewModel consumption.

**Status**: Build OK (0 errors, 0 warnings). PR #97 created.

### Critical Authorization Gap Closure (PR #98)

**CRITICAL GAP DISCOVERED & FIXED**:
- UserProfileController, RolesController, TenantsController, DocumentsController, SearchController, McpController had NO [Authorize]
- All endpoints were completely unprotected (any unauthenticated request could execute)
- Specific vulnerabilities: GET/POST/DELETE profile, GET/POST/DELETE role assignments, create tenants, upload documents

**Changes Made**:
1. Added [Authorize] to all 6 vulnerable controllers (class-level)
2. Implemented global FallbackPolicy in Program.cs (RequireAuthenticatedUser by default)
3. Marked legitimate public endpoints with [AllowAnonymous] (health checks, /api/version)
4. Refactored McpController to use class-level [Authorize] instead of manual IsAuthenticated checks
5. Added AuthorizationAttributeTests with 7 tests validating [Authorize] presence

**Validation of Decision #2 (Entra/LocalDev/RBAC)**:
- Production: JwtCurrentUserContext validates Entra tokens, no bypass possible
- Development: LocalDevAuthenticationHandler + LocalDevEnvironmentGuard (403 outside Dev)
- Both: AuthorizationEvaluator always queries real RBAC tables (never hardcoded)

**Build**: 0 errors, 0 warnings  
**Tests**: AuthorizationAttributeTests 7/7 passing  
**PR**: #98 created against develop  

### Fine-Grained Role Management Authorization (PR #99)

**Vulnerability Closed**: Privilege escalation via unrestricted role assignment/removal

**Root Cause**:
- RolesController.AssignRole and RemoveRole protected only by [Authorize] (authentication only)
- Any authenticated user could assign/remove roles, including escalating to TenantOwner/ComplianceAdmin
- Violated SEC-ACT-001/SEC-GAP-001 principle: "only TenantOwner/ComplianceAdmin"

**Solution**:
1. Created TenantOwnerOrComplianceAdminRequirement + AuthorizationHandler
   - Queries SecurityDbContext.UserRoleAssignments filtered by (UserId, TenantId)
   - Validates user has "TenantOwner" OR "ComplianceAdmin" role in current tenant
   - Returns 403 Forbidden if check fails

2. Registered policy in Program.cs:
   ```csharp
   options.AddPolicy("TenantOwnerOrComplianceAdmin", policy =>
       policy.AddRequirements(new TenantOwnerOrComplianceAdminRequirement()));
   ```

3. Applied [Authorize(Policy = "TenantOwnerOrComplianceAdmin")] to:
   - AssignRole (POST /api/roles/assign)
   - RemoveRole (DELETE /api/roles/remove)
   - GetUserRoles remains read-only, protected by class-level [Authorize]

**Cross-Tenant Isolation Verified**:
- Handler filters by ICurrentUserContext.TenantId (from authenticated user context)
- TenantOwner of tenant A cannot assign roles in tenant B
- Attack scenario blocked: different TenantId → no roles found in that tenant → 403

**Tests (7/7 passing)**:
- ✅ TenantOwner can assign/remove roles in their tenant
- ✅ ComplianceAdmin can assign/remove roles in their tenant
- ✅ Viewer CANNOT assign/remove roles (403)
- ✅ ProcessOwner CANNOT assign/remove roles (403)
- ✅ TenantOwner of tenant A CANNOT assign roles in tenant B (cross-tenant isolation)
- ✅ Unauthenticated user CANNOT assign/remove roles (403)
- ✅ User with multiple roles including TenantOwner CAN perform operations

**Build**: 0 errors, 0 warnings
**Security Tests**: 76/76 passing (no regression)
**PR**: #99 created against develop
**ADR**: legolas-tenant-owner-compliance-admin-policy.md (documents architectural decision)

**ADR**: legolas-global-fallback-authorization-policy.md documents FallbackPolicy decision

## Session 2026-07-10

### Main→Develop Reconciliation (PR #102)

**Problem Discovered**:
- PR #98 was accidentally merged into `main` (merge commit 50ee9a5) instead of `develop`
- Root cause: `gh pr create` without `--base develop` flag used repo's default (`main`)
- Divergence: main has [Authorize] + AuthorizationAttributeTests; develop has FallbackPolicy + TenantOwnerOrComplianceAdminRequirement

**Security Status**: develop NOT vulnerable today (fail-closed by FallbackPolicy), but lacks defense-in-depth and regression tests.

**Reconciliation Actions**:
1. Cherry-picked ce2b1b8 + dcb5225 from main into develop branch
2. Resolved conflict in Program.cs: kept FallbackPolicy (identical) + added TenantOwnerOrComplianceAdminRequirement policy
3. Verified all 7 public endpoints have .AllowAnonymous(): /health, /alive, /health/ready, /health/db, /health/storage, /health/queue, /api/version
4. Confirmed RolesController maintains [Authorize] + policy enforcement on AssignRole/RemoveRole
5. All 5 protected controllers now have explicit [Authorize]: UserProfileController, DocumentsController, SearchController, TenantsController, McpController

**Test Results**:
- Build: 0 errors (19 pre-existing warnings in AuthorizationAttributeTests related to null safety)
- Tests: 597/597 passing (includes 7 AuthorizationAttributeTests from cherry-pick)

**Process Correction**:
- Established rule: always use `gh pr create --base develop --head <branch>` explicitly
- Never rely on repo's default base branch; always specify `--base develop` for evidata
- Documented in .squad/decisions/inbox/legolas-branching-process-fix.md

**PR**: #102 created against develop
**Status**: Ready for merge after review
