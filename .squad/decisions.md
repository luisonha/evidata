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

### 2026-07-10T00-30-38Z: Regla de branching para PRs en evidata
**By:** Legolas (Security & Authorization Engineer)
**What:** SIEMPRE usar `gh pr create --base develop --head <rama-de-trabajo>` explícitamente para todas las PRs. Nunca omitir `--base develop` ni asumir que el default del repo es `develop`. Esto previene merges accidentales fuera del flujo dev-first (PR #98 fue mergeada accidentalmente en `main` en lugar de `develop`).
**Why:** El default del repositorio está configurado como `main` históricamente. Sin `--base develop` explícito, `gh pr create` usa `main` como destino, causando divergencia real entre ramas. Explícito es mejor que implícito. Previene merges accidentales en ramas de producción y reduce fricción en reconciliación posterior.
**Action:** Documentado en charter del repositorio o guía de contrib. PR #102 reconciliaba esta divergencia cherry-pickando cambios desde `main` a `develop`.

## Governance

- All meaningful changes require team consensus
- Document architectural decisions here
- Keep history focused on work, decisions focused on direction
