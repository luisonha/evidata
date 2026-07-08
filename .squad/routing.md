# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Arquitectura, decisiones técnicas, diseño de módulos, ADRs | Gandalf | Qué se construye, cómo se estructura, límites entre módulos |
| Backend core, módulos dominio, APIs, migraciones | Aragorn | Endpoints, servicios de aplicación, lógica de negocio |
| Seguridad, RBAC, tenant guard, auditoría, auth | Legolas | Permisos, middleware, políticas, LocalDev auth |
| Base de datos, modelos de datos, migraciones EF, queries | Gimli | Entidades, índices, relaciones, PostgreSQL, Outbox |
| Azure Functions, mensajería, Outbox, colas, jobs asíncronos | Frodo | Document processing, reportes, notificaciones, indexación |
| DevOps, CI/CD, Azure infra, pipelines, IaC, branch setup | Sam | GitHub Actions, Azure DevOps, App Service, Key Vault |
| Code review | Gandalf | Review PRs, calidad, sugerir mejoras |
| Testing | Aragorn + Gimli | Pruebas unitarias, integración, cross-tenant |
| Scope & priorities | Gandalf | Qué construir, trade-offs, decisiones de alcance |
| Session logging | Scribe | Automático — nunca necesita routing |
| RAI review | Rai | Content safety, bias, credential detection |

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analiza issue, asigna label `squad:{member}` | Gandalf |
| `squad:gandalf` | Arquitectura, decisiones, revisiones | Gandalf |
| `squad:aragorn` | Backend core, módulos, APIs | Aragorn |
| `squad:legolas` | Seguridad, auth, tenant isolation | Legolas |
| `squad:gimli` | DB, modelos, migraciones | Gimli |
| `squad:frodo` | Functions, async, colas | Frodo |
| `squad:sam` | DevOps, infra, pipelines | Sam |

## Rules

1. **Eager by default** — spawn todos los agentes que puedan empezar en paralelo.
2. **Scribe siempre** corre después de trabajo sustancial, siempre `mode: "background"`.
3. **Hechos rápidos → coordinator responde directo.** No spawnar por "qué puerto usa el server".
4. **Cuando dos agentes sirven**, elegir el de dominio primario.
5. **"Team, ..." → fan-out.** Spawnar todos los relevantes en paralelo como `mode: "background"`.
6. **Anticipar trabajo downstream.** Si se construye un feature, el tester escribe casos en paralelo.
