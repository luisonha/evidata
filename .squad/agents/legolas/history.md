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

**ADR**: legolas-global-fallback-authorization-policy.md documents FallbackPolicy decision
