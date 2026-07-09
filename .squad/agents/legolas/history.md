# legolas — History

## Session 2026-07-07

- Agente creado para el proyecto Evidata.
- Universo: El Señor de los Anillos.
- Decisiones iniciales confirmadas: repo monorepo, GitHub Actions CI + Azure DevOps CD, PostgreSQL, Entra External ID, App Service Linux.

## Session 2026-07-09

### RBAC & Identity Security Gap Closure

**Closed**:
- Implemented bridge between legacy Permission/Role schema and contract-defined RbacRoleCode/PermissionCode enums.
- Implemented all 6 critical RBAC rules from contract (SEC-APP-001, SEC-ACT-001, SEC-EV-001, SEC-GAP-001, SEC-EXP-001, SEC-EVDOWN-001).
- Created IResourcePermissionsQueryService to produce real data for ResourcePermissionsViewModel (P1-004).
- Seeded 7 system roles and 6 permissions in SecurityDbContext with predefined mappings.
- Added 12 comprehensive security tests covering all rules; 69 total security tests passing.

**Architecture Decision**:
- Enum→Schema Seed Mapping: Contract enums become the public semantic layer; legacy schema seeded with exact names.
- Rationale: Simplicity, no breaking changes, future-proof for ResourcePermissionsViewModel consumption.

**Blocked**:
- EvidenceRequirement.reviewDomain does not exist yet (Evidence module responsibility).
- SEC-EV-001 will need enhancement once this entity is defined.

**Status**: Build OK (0 errors, 0 warnings). PR #97 created. Architectural decision documented in decisions/inbox/legolas-rbac-model-bridging.md.

**Next**: Identity profile/deactivation/external binding gaps review. Reporting module Export mapping (ADR pending). Reopen /control (P1-001) with real ResourcePermissionsViewModel producer.
