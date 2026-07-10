# Squad Decisions

## Active Decisions

### 2026-07-09T23:09:55Z: Prioridad — cerrar Identity y Security/RBAC
**By:** luisonha (via Squad Coordinator)
**What:** Se pausa el ensamblado de `/control` (P1-001). Prioridad inmediata: terminar Identity y todo lo referente a seguridad, incluyendo cerrar la brecha entre el esquema RBAC legado (`Permission`/`Role` string `resource:action`) y el contrato objetivo (`RbacRoleCode`/`PermissionCode`, doc `04-rbac-audit-evidence-gaps-contract.md` §1), e implementar el productor real de `ResourcePermissionsViewModel` (`AvailableActions`/`BlockedActions`, backlog `P1-004`).
**Why:** Petición explícita del usuario. El campo `Permissions` de `ProcessingActivityControlViewModel` no tiene fuente de datos real hoy; cerrar RBAC es bloqueante para completar `/control` correctamente.
**Owner:** Legolas (Security & Authorization Engineer).

### 2026-07-09T23:13:55Z: Estrategia de identidad — Entra en producción, modelo simple + roles locales en desarrollo
**By:** luisonha (via Squad Coordinator)
**What:** En producción se debe usar Microsoft Entra ID como gestor de identidades (ya cubierto parcialmente por `JwtCurrentUserContext`/Entra External ID). En desarrollo/local se debe usar un mecanismo más simple (ya existe `LocalDevAuthenticationHandler`/`LocalDevCurrentUserContext`), pero los roles deben resolverse contra el modelo RBAC local (tablas `Role`/`Permission`/`UserRoleAssignment` o su evolución hacia `RbacRoleCode`/`PermissionCode`), no hardcodeados ni simulados fuera de ese modelo. La implementación debe seguir mejores prácticas de seguridad y ser performante (evitar N+1, cachear evaluación de permisos donde sea seguro, no exponer claims sin validar).
**Why:** Petición explícita del usuario — evitar mezclar simulación de auth con simulación de roles; los roles deben ser reales y consistentes entre entornos, sólo el proveedor de identidad cambia.
**Owner:** Legolas (Security & Authorization Engineer).

### 2026-07-10T02:15:00Z: PR #105 - Formalización de EvidenceRequirement + EvidenceValidation con SEC-EV-001 Fail-Closed ✅ APROBADO
**By:** Aragorn (Backend Core Engineer) → Gandalf (Architect/Lead, Code Review Approved)
**What:** PR #105 (P1-005/P1-006) implementa formalización completa de `EvidenceRequirement` y `EvidenceValidation`:
- **P1-005**: `EvidenceRequirement` entity con campo `reviewDomain` (Legal|Security) que determina qué rol (LegalReviewer|SecurityReviewer) puede validar.
- **P1-006**: `EvidenceValidation` state machine (Pending → Attached → Validated|Insufficient|Rejected) con auditoría preparada para handlers.
- **SEC-EV-001 Authorization**: Extensión de `ResourceContextData` + nuevo método `IsBlocked_ValidateEvidenceWrongDomain()` que valida domain-specific role authorization (LegalReviewer solo puede validar Legal, SecurityReviewer solo Security). Implementación **fail-closed**: si `ReviewDomain` es null/unknown, bloquea por defecto.
- Domain: ~500 LOC (entities, value objects, factory methods). DbContext config + indexes: ~200 LOC. Tests: 29 domain state transitions + 8 SEC-EV-001 cross-domain authorization tests (LegalReviewer ≠ Security, SecurityReviewer ≠ Legal).
- **Result**: All 643 unit tests passing, CI GREEN, merged to develop.
**Why:** Cierra contrato SEC-EV-001 (domain-specific RBAC para evidence validation). Permite que handlers posteriores (P1 Iteration 2) construyan commands sin tocar autorización. Responde a petición explícita del usuario sobre seguridad fail-closed.
**Status:** ✅ APROBADO + MERGED
**Process:** 2 iteraciones — 1ª rechazada (CS7036 compilation error + SEC-EV-001 fail-open + test gaps); 2ª aprobada (todos los issues corregidos, fail-closed, cross-domain tests).
**Owner:** Aragorn (implementation) + Gandalf (review/approval).

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
