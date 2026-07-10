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

## Session 2026-07-10

### Health Endpoints AllowAnonymous Gap Fix (PR #101)

**Gap Discovered & Fixed**:
- FallbackPolicy(RequireAuthenticatedUser) was implemented in PR #98 with explicit documentation: "health endpoints must mark [AllowAnonymous]"
- However, 7 health/version endpoints were never marked:
  - /health, /alive (ServiceDefaults MapDefaultEndpoints)
  - /health/ready, /health/db, /health/storage, /health/queue (HealthCheckExtensions MapEvidataHealthEndpoints)
  - /api/version (Program.cs MapGet)
- PR #99 accidentally removed .AllowAnonymous() from /api/version (unrequired change in commit, not documented)

**Real Impact**:
- Kubernetes/Aspire orchestrator probes hitting /health without credentials now receive 401/403
- Services marked unhealthy and restarted without cause
- Operational/availability risk introduced accidentally by security hardening of PR #98

**Lesson Learned**:
- Always `git pull origin develop` before auditing/branching to ensure merge-base is current
- Previous session (audit for #100, never merged) used stale develop; this session reproduced the real gap correctly
- Documentation + code drift: ADR said [AllowAnonymous] required but wasn't enforced; must audit actual deployed state vs. documented intent

**Changes Made**:
1. Added `.AllowAnonymous()` to all 7 health/version endpoints
2. Created HealthEndpointsAuthorizationTests (5 new tests):
   - ✅ Unauthenticated user fails FallbackPolicy (confirms policy works)
   - ✅ Authenticated TenantOwner can access protected endpoints
   - ✅ Unauthenticated user cannot access role management
   - ✅ Viewer role cannot access role management
   - ✅ Cross-tenant isolation: TenantOwner of tenant A cannot manage roles in tenant B

**Build**: 0 errors, 0 warnings
**Tests**: 595/595 passing (no regression)
**PR**: #101 created against develop

**Security Backlog (Non-Blocking, Not Implemented)**:
- Response headers: HSTS, X-Content-Type-Options, X-Frame-Options, CSP
- Rate limiting on sensitive endpoints: /api/roles/assign, /api/roles/remove, document exports, tenant creation
