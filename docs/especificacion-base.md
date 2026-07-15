# Especificación Base del Sistema — Evidata Backend

> Documento vivo del **estado actual** del backend Evidata (ATLAS Opción B) al 2026-07-14, más las **brechas** explícitas pendientes de implementar.
> Complementa `docs/constitution.md` (principios) con el mapa real del sistema (qué existe, qué es parcial, qué falta).
> No es una spec de una sola feature nueva: es el baseline sobre el que el equipo (y spec-kit) construye especificaciones incrementales.

**Producto:** Evidata — Digital Trust Operating System (nombre técnico interno: ATLAS Opción B)
**Dominio regulatorio:** Ley 21.719 (Chile)
**Modelo:** SaaS multi-tenant, monolito modular .NET 10 + Azure Functions asíncronas

---

## 1. Panorama actual (as-is)

Evidata es un backend en construcción activa que ya tiene:

- Solución `.NET 10` compilando y ejecutable localmente vía `.NET Aspire` (`src/Evidata.AppHost`) o vía `docker-compose` (`infra/local/`).
- 14 módulos de dominio scaffoldados en `src/Modules/`: TenantManagement, Identity, Security, Audit, LegalKnowledge, Documents, Evidence, ProcessingInventory, GapManagement, Workflow, Reporting, Search, Mcp, Contracts.
- 6 Azure Functions (isolated worker, .NET 10): DocumentProcessing, SearchIndexing, Reporting, Notifications, McpBatch, Maintenance.
- Un worker Outbox (`src/Evidata.Worker.Outbox`) publicando a Azure Storage Queues por destino lógico.
- Contratos OpenAPI base publicados en `docs/openapi/openapi.json`.
- CI en GitHub Actions (`.github/workflows/ci.yml`) con guardas de naming/rutas prohibidas y ejecución de tests.
- Documentación arquitectónica completa en `docs/evidata-backend-docs/` (00–39, 99).
- Alertas KQL y workbook operacional en `infra/azure/`.
- 4 runbooks operacionales (deployment, migración DB, HITL, incidente MCP).

El sistema está en la mitad del **Sprint 3** (login productivo con Entra + administración de usuarios). El Sprint 2 (contrato de RAT/Evidence/Gaps/Exports) cerró como paquete documental y con controladores implementados. El MCP tiene modelo de datos e infraestructura de HITL, pero la integración productiva con Azure OpenAI y el pipeline de guardrails están en implementación parcial.

---

## 2. Estado real por módulo

Leyenda de estado:
- **✅ Implementado**: código presente y funcionalmente ejercitable.
- **🟡 Parcial**: infraestructura o parte del contrato existen; falta cerrar endpoints/reglas específicas.
- **📄 Documentado / Planeado**: sólo docs; sin código todavía o con scaffold vacío.

| Módulo | Estado | Notas |
|---|---|---|
| TenantManagement | 🟡 Parcial | `TenantsController` presente; alcance MVP de configuración/suspensión de tenant no confirmado en revisión. |
| Identity | 🟡 Parcial | Ver §2.1 — modelo de datos + sesión server-side ✅; endpoints de auth/session/admin ❌. |
| Security | 🟡 Parcial | RBAC seed data en migración `20260711192535_SeedRbacData`; `RolesController` + `AuthorizationEvaluator` presentes; endpoints admin de listado de roles/permisos ❌. |
| Audit | 🟡 Parcial | `AuditController` y `IAuditService` presentes; adopción por otros módulos parcial (comentarios `P2 con RBAC service centralizado` sugieren refactor pendiente). |
| LegalKnowledge | ✅ Implementado | Migrations InitialLegalObligations, AddLegalSources, AddTaxonomies aplicadas; controladores y query handlers presentes. |
| Documents | ✅ Implementado | `DocumentsController`, entidad `Document`, integración con Blob esperada vía Function. |
| Evidence | ✅ Implementado | `EvidenceController`, `ValidateEvidenceCommandHandler`, `EvidenceSummaryQueryHandler`. Sprint 2 lo cerró contractualmente. |
| ProcessingInventory (RAT) | ✅ Implementado | Entidad `ProcessingActivity` como canónica (no `Treatment`); controller y handlers presentes. Sprint 2 cerró contrato. |
| GapManagement | ✅ Implementado | `GapsController`, `AcceptGapWithRiskCommandHandler`, `IRatFlagsProvider`. |
| Workflow | ✅ Implementado | `WorkflowController`, `ReviewRequirementsController`, entidad `ReviewRequirement`. |
| Reporting | 🟡 Parcial | `ReportsController` + `ExportsController` + `ProcessingActivityReadOnlyQueryAdapter` + entidad `Export`; generación real corre en Function. |
| Search | 🟡 Parcial | Sólo `SearchQueryLogService` + `SearchQueryLog`. Indexación real y consulta contra fuentes/documentos pendiente — la Function `SearchIndexing` existe pero no cerrada. |
| Mcp | 🟡 Parcial | Modelo de dominio + HITL (`McpReviewTask`, feedback) + abstracciones (`IMcpInteractionService`, `IMcpRiskRouter`, `IMcpCitationVerifier`, `IMcpQueryService`, `IRatContextProvider`) presentes. Ver §2.2. |
| Contracts | ✅ Implementado | SDK compartido de tipos públicos entre módulos. |

### 2.1 Módulo Identity — detalle (foco del Sprint 3)

**Lo que YA está implementado en código:**

- Entidad `UserProfile` con relación many-to-many a `Role` vía `UserProfileRole` (Decisión 2 del Sprint 3 aplicada desde el modelo de datos).
- Enum `UserStatus`: `Active`, `Pending`, `Suspended`, `Disabled` (sin `deleted_soft`, conforme Decisión 3).
- Entidad `Invitation` con `InvitationStatus`, `ExpiresAt`, configuración EF (`InvitationConfiguration`).
- Entidad `Session` persistida (`SessionConfiguration`, tabla `sessions` vía migración `20260712103829_AddSessionsTable`).
- `SessionResolutionMiddleware` — lee cookie opaca `__Host-evidata.sid` (HttpOnly + Secure + SameSite=Strict), resuelve `SessionCurrentUserContext`, aplica sliding expiration y limpia la cookie si la sesión está expirada/revocada.
- `SessionCurrentUserContext` — implementación productiva de `ICurrentUserContext` para sesiones server-side.
- `ISessionService` con `CreateSessionAsync`, `GetActiveSessionAsync`, `RevokeSessionAsync`, `RevokeAllSessionsForUserAsync`.
- Modo local: `LocalDevAuthenticationHandler`, `LocalDevCurrentUserContext`, `LocalDevSeedUsers`, `LocalDevEnvironmentGuard`, migración `20260712103640_SeedLocalDevUsers`.
- `JwtCurrentUserContext` como alternativa histórica/de compatibilidad.
- Middlewares transversales: `TenantIsolationMiddleware`, `CorrelationIdMiddleware`.
- Catálogo `IdentityErrorCodes` con: `InvitationRevoked`, `InvitationExpired`, `InvalidStateTransition`, códigos de callback de login, códigos de cross-tenant.
- Endpoints existentes en `UserProfileController`: `GET /api/users/{userId}`, `POST /api/users/link` (`LinkExternalIdentityCommand`), y `DeactivateUserCommand` disponible como handler (endpoint por confirmar).

**Lo que NO está en código todavía (ver §5 Brechas para detalle):**

- `POST /api/v1/auth/login` (arranque de flujo con Entra External ID).
- `GET|POST /api/v1/auth/callback` (procesamiento del callback de Entra).
- `POST /api/v1/auth/logout` (revocación de sesión).
- `GET /api/session`, `GET /api/me`, `GET /api/permissions` (endpoints de introspección de sesión).
- Endpoints de invitación (`POST/GET/DELETE /api/v1/admin/tenants/{tenantId}/invitations*`, activación por primer login).
- Endpoints de administración de usuarios (`GET /api/v1/admin/tenants/{tenantId}/users`, `GET .../users/{userId}`, `PATCH .../users/{userId}`, `PATCH .../users/{userId}/roles`, `POST .../users/{userId}/suspend|reactivate|disable`).
- Endpoints de metadatos admin (`GET /api/v1/admin/roles`, `GET /api/v1/admin/permissions`, `GET /api/v1/admin/audit`).
- Endpoints dev-only (`POST /dev/auth/set-scenario`, `POST /dev/admin/users/create-scenario`).
- Validador centralizado de transiciones de estado (`ValidateStateTransition(from, to, operation)`).

### 2.2 Módulo Mcp — detalle

- Modelo de dominio y persistencia (`InitialMcp`, `AddMcpReviewAndFeedback`) presentes.
- Abstracciones `IMcpInteractionService`, `IMcpQueryService`, `IMcpRiskRouter`, `IMcpCitationVerifier`, `IRatContextProvider`, `IMcpAuditService` definidas — sujetas a implementación real en runtime cloud.
- `McpController` presente.
- Estados `McpInteractionStatus.Failed` documentados como terminales (ADR-003).
- Integración productiva con Azure OpenAI/Foundry, evaluación efectiva del `CitationVerifier` y `RiskClassifier` contra un modelo real, y cierre del pipeline HITL end-to-end permanecen como brechas (§5).

---

## 3. Requerimientos Funcionales del sistema (baseline)

*Los `FR-XXX` son requerimientos que ya son "verdad" en el sistema o que deben serlo para considerar el MVP cerrado. Los marcados con `[NEEDS CLARIFICATION: ...]` corresponden a ambigüedades reales aún no resueltas.*

### 3.1 Multi-tenancy y Seguridad

- **FR-001**: El sistema MUST rechazar cualquier request funcional sin `tenant_id` resuelto y sin usuario resuelto.
- **FR-002**: El sistema MUST rechazar cualquier acceso cross-tenant y MUST emitir evento de seguridad auditable en ese caso.
- **FR-003**: El sistema MUST evaluar autorización contra `ICurrentUserContext` internamente, independiente del proveedor de auth.
- **FR-004**: El sistema MUST soportar múltiples roles simultáneos por usuario y evaluar toda regla de negocio como pertenencia a conjunto.
- **FR-005**: El sistema MUST fallar al iniciar en `Staging`/`Production` si `EVIDATA_AUTH_MODE != Entra`.
- **FR-006**: El sistema MUST invalidar la sesión y revocarla al detectar `UserStatus ∈ {Suspended, Disabled}` en resolución de sesión.
- **FR-007**: El sistema MUST impedir la operación que dejaría a un tenant sin al menos un `TenantOwner` activo (aplica a cambio de roles, suspender, deshabilitar).
- **FR-008**: El sistema MUST garantizar unicidad de email por `(tenant_id, email)` — no globalmente (Decisión 8).
- **FR-009**: [NEEDS CLARIFICATION: ¿MFA obligatorio desde MVP o configurable por tenant?] — decisión pendiente en `04 §15`.
- **FR-010**: [NEEDS CLARIFICATION: retención formal de logs de auditoría — número de años, particionamiento y política de purga.]

### 3.2 Autenticación y sesión

- **FR-020**: El sistema MUST emitir sesión server-side opaca vía cookie `__Host-evidata.sid` (HttpOnly, Secure, SameSite=Strict). No debe emitir JWT al cliente en producción.
- **FR-021**: El sistema MUST validar CSRF en endpoints de mutación autenticados por cookie.
- **FR-022**: El sistema MUST soportar dos modos de autenticación: `Entra` (productivo) y `LocalDev`/`TestAuth` (no productivo, gateado por `LocalDevEnvironmentGuard`).
- **FR-023**: El sistema MUST permitir logout que revoque la sesión activa y limpie la cookie.
- **FR-024**: El sistema MUST exponer `GET /api/session`, `GET /api/me` y `GET /api/permissions` para introspección del contexto autenticado por parte del BFF/frontend.

### 3.3 Invitación y ciclo de usuario

- **FR-030**: Un admin del tenant con permiso `users.invite` MUST poder crear una invitación por email dentro de su tenant.
- **FR-031**: El sistema MUST rechazar crear una nueva invitación para un email que ya tiene invitación `Pending` en el tenant (Decisión 10 — usar reenvío).
- **FR-032**: Toda invitación MUST expirar tras un TTL configurable [NEEDS CLARIFICATION: ¿valor por defecto del TTL? — no explícito en Sprint 3].
- **FR-033**: El sistema MUST responder `InvitationExpired` (403) al intentar activar una invitación con `ExpiresAt < now` (Decisión 5).
- **FR-034**: El sistema MUST responder `InvitationRevoked` con status según contexto: 403 en flujos de login, 409 en acciones administrativas (Decisión 1).
- **FR-035**: El sistema MUST auditar toda invitación creada, reenviada, revocada, y toda activación de invitación.
- **FR-036**: El sistema MUST activar el usuario en su primer login válido contra la invitación (transición `Pending → Active`).

### 3.4 Administración de usuarios

- **FR-040**: El sistema MUST exponer listado paginado y filtrado de usuarios por tenant (`q`, `status`, `role`, `responsibleAreaId`, `page`, `pageSize`, `sort`).
- **FR-041**: El sistema MUST permitir cambio completo de set de roles vía `PATCH .../users/{userId}/roles` con body `{ roles: [...], reason: "..." }`. Semántica: reemplazo total del set, no incremental.
- **FR-042**: El sistema MUST validar transiciones de estado vía función centralizada `ValidateStateTransition(from, to, operation)` y responder `InvalidStateTransition` (409) como catch-all (Decisión 6).
- **FR-043**: Los estados válidos en Sprint 3 son `Active`, `Pending`, `Suspended`, `Disabled`. `deleted_soft` está explícitamente fuera de alcance (Decisión 3).
- **FR-044**: Toda acción admin (invitar, revocar, reenviar, cambio roles, suspend, reactivate, disable) MUST requerir `reason` cuando esté documentado y MUST auditarse.
- **FR-045**: [NEEDS CLARIFICATION: `responsibleAreaId` — Decisión 7 declara pendiente confirmar la entidad real (probablemente en GapManagement o ProcessingInventory) y las validaciones de pertenencia al tenant.]

### 3.5 RAT (ProcessingInventory)

- **FR-050**: El sistema MUST permitir crear, editar, versionar y aprobar `ProcessingActivity` con snapshot inmutable al aprobar.
- **FR-051**: El sistema MUST impedir edición directa de versión aprobada; toda modificación crea nueva versión.
- **FR-052**: El sistema MUST validar completitud antes de habilitar `submit_review`.
- **FR-053**: El sistema MUST generar flags regulatorios básicos (datos sensibles, NNA, biometría, transferencias, decisiones automatizadas).
- **FR-054**: El sistema MUST auditar aprobación, edición de aprobado, cambio de owner, y exportación completa.
- **FR-055**: El sistema MUST NOT introducir `Treatment` como entidad, DTO, namespace o endpoint (canónico es `ProcessingActivity`).

### 3.6 Evidence

- **FR-060**: El sistema MUST permitir crear evidencia, clasificarla por sensibilidad, y asociarla a entidades soportadas (`ProcessingActivity`, `Gap`, etc.) dentro del mismo tenant.
- **FR-061**: El sistema MUST impedir asociación cross-tenant y auditar el intento.
- **FR-062**: Descargas de evidencia sensible MUST requerir motivo y ser auditadas.
- **FR-063**: El sistema MUST soportar eliminación lógica y preservar historial de evidencias en versiones aprobadas.
- **FR-064**: El sistema MUST poder generar `evidence pack` de forma asíncrona (Function Reporting).

### 3.7 Gaps

- **FR-070**: El sistema MUST convertir hallazgos del RAT en `Gap` con severidad, responsable, fecha objetivo y estado.
- **FR-071**: El sistema MUST permitir cerrar `Gap` con evidencia asociada o aceptar riesgo con justificación auditada.
- **FR-072**: El sistema MUST impedir cierre de brechas críticas sin permiso `gaps.close` y sin evidencia mínima.

### 3.8 Workflow

- **FR-080**: El sistema MUST soportar flujo de revisión de `ProcessingActivity` (solicitar revisión, asignar, comentar, aprobar, devolver).
- **FR-081**: El sistema MUST notificar via Function `Notifications` los eventos de flujo relevantes.

### 3.9 Documents

- **FR-090**: El sistema MUST recibir carga documental, persistir en Blob privado, calcular hash, versionar, y disparar procesamiento asíncrono vía Outbox → destino `document-processing`.
- **FR-091**: Toda descarga MUST ser mediada por backend con URL de expiración corta y auditada.

### 3.10 Reporting

- **FR-100**: El sistema MUST generar reportes RAT completos, reportes de brechas, y evidence packs de forma asíncrona.
- **FR-101**: El sistema MUST auditar generación y descarga.

### 3.11 Search

- **FR-110**: El sistema MUST permitir búsqueda sobre fuentes legales y sobre entidades tenant autorizadas, respetando permisos.
- **FR-111**: [NEEDS CLARIFICATION: motor de indexación productivo — Sprint actual usa Postgres FTS/pgvector como asunción; Azure AI Search sigue como opción futura sin decisión formal.]

### 3.12 MCP

- **FR-120**: El sistema MUST clasificar riesgo (`Low`/`Medium`/`High`) de cada respuesta MCP.
- **FR-121**: El sistema MUST citar fuente o abstenerse; nunca certificar cumplimiento.
- **FR-122**: El sistema MUST crear `McpReviewTask` automáticamente para respuestas `High`.
- **FR-123**: El sistema MUST persistir la interacción completa (query, fuentes, respuesta, clasificación, feedback) para auditoría.
- **FR-124**: El sistema MUST NOT permitir `RequestHumanReview()` sobre interacción `Failed` (ADR-003).
- **FR-125**: [NEEDS CLARIFICATION: proveedor de modelo productivo — Azure OpenAI vs Foundry — sin decisión final documentada.]

### 3.13 Mensajería (Outbox + Functions)

- **FR-130**: El sistema MUST persistir mensaje en `OutboxMessages` **dentro de la misma transacción** que el cambio de estado que lo origina.
- **FR-131**: El sistema MUST publicar a **destino lógico** y no a cola concreta. La resolución destino→cola es infraestructural (`DefaultDestinationResolver` es el default actual).
- **FR-132**: Toda Function MUST ser idempotente y manejar poison messages.
- **FR-133**: Todo mensaje MUST portar `MessageVersion`/`SchemaVersion` y `CorrelationId`.

### 3.14 Observabilidad y operación

- **FR-140**: Toda respuesta y todo mensaje asíncrono MUST portar `correlation-id`.
- **FR-141**: Las 5 alertas KQL versionadas en `infra/azure/alerts/` MUST estar activas en `staging` y `prod` antes de release.
- **FR-142**: El endpoint `/api/version` MUST retornar la versión SemVer del deploy activo (ya implementado en `HostBuilderFactory.cs:166`).

---

## 4. Entidades clave del dominio

| Entidad | Módulo | Descripción |
|---|---|---|
| `Tenant` | TenantManagement | Organización cliente; unidad de aislamiento absoluta. |
| `UserProfile` | Identity | Usuario interno del sistema. FK a Tenant. Estado ∈ `Active|Pending|Suspended|Disabled`. Muchos roles. |
| `Role` | Security | Rol de RBAC (`PlatformAdmin`, `TenantOwner`, `TenantAdmin`, `ComplianceAdmin`, `LegalReviewer`, `ProcessOwner`, `SecurityReviewer`, `ExternalConsultant`, `ReadOnlyAuditor`). |
| `UserProfileRole` | Identity | Tabla de join many-to-many `UserProfile ↔ Role`. |
| `Invitation` | Identity | Invitación a email. Estado + `ExpiresAt`. Unicidad por `(TenantId, Email)`. |
| `Session` | Identity | Sesión server-side opaca. Snapshot de roles y `permissionsVersion`. Sliding expiration. |
| `AuditEvent` | Audit | Registro append-only de acción sensible. |
| `LegalSource` / `LegalObligation` / `DataCategory` / `DataSubjectCategory` / `SecurityMeasure` | LegalKnowledge | Catálogo legal versionado. Puede ser global o tenant-específico. |
| `Document` | Documents | Documento con versiones, hash, blob privado, estado de procesamiento. |
| `Evidence` | Evidence | Evidencia clasificada por sensibilidad, asociada a entidad polimórfica dentro del tenant. |
| `ProcessingActivity` | ProcessingInventory | RAT canónico. Versionado, con estados y flags regulatorios. |
| `Gap` | GapManagement | Brecha derivada de RAT. Severidad, responsable, estado, evidencia de cierre. |
| `ReviewRequirement` | Workflow | Requisito de revisión (Legal, Security) sobre `ProcessingActivity`. |
| `Export` / `Report` | Reporting | Job de exportación asíncrono con estado `Generating|Completed|Failed`. |
| `SearchQueryLog` | Search | Log de consultas para auditoría y analítica. |
| `McpInteraction` / `McpReviewTask` / `McpFeedback` | Mcp | Interacción MCP con clasificación de riesgo y HITL. |
| `OutboxMessage` | (transversal) | Mensaje asíncrono con destino lógico, versionado, con `Status`, `AttemptCount`, `CorrelationId`. |

---

## 5. Brechas Pendientes de Implementar (autoritativo)

Este es el corazón operativo de este documento. Cada brecha declara: **qué falta**, **por qué importa**, y **si hay plan** (con referencia). La lista se mantiene viva — cerrar una brecha requiere PR + actualización de este documento.

### 5.1 Identity — Sprint 3 en curso (mayor densidad de brechas)

#### G-ID-01. Endpoints de autenticación productiva con Entra
- **Qué falta:** `POST /api/v1/auth/login`, `GET /api/v1/auth/callback` (o `POST` según flujo elegido), `POST /api/v1/auth/logout`.
- **Por qué importa:** Sin ellos, la única auth funcional real es `LocalDev`. El sistema no puede desplegarse en `staging` o `prod` conforme al Principio III.
- **Plan:** Sprint 3, Fases 1-2. Contratos ya definidos en `docs/evidata-backend-sprint-3/03-endpoints-auth-session.md` y `02-ciclo-completo-login-usuario.md`. La infraestructura de sesión ya existe (falta el pipeline OpenIdConnect + callback + creación de sesión + linkeo con `UserProfile`).

#### G-ID-02. Endpoints de introspección de sesión
- **Qué falta:** `GET /api/session`, `GET /api/me`, `GET /api/permissions`.
- **Por qué importa:** Requeridos por el BFF/frontend para hidratar contexto y evaluar visibilidad de acciones. Sin ellos, el frontend no puede consumir la sesión existente.
- **Plan:** Sprint 3, Fase 2. Contrato en `03-endpoints-auth-session.md`.

#### G-ID-03. Endpoints de invitación
- **Qué falta:** crear invitación, reenviar, revocar, listar, activar por primer login. Rutas en `docs/evidata-backend-sprint-3/04-endpoints-admin-users.md` y `05-contratos-request-response-admin-users.md`.
- **Por qué importa:** Es el único mecanismo previsto de alta de usuarios en un tenant. Sin invitación, no hay onboarding productivo.
- **Plan:** Sprint 3, Fase 3.

#### G-ID-04. Endpoints de administración de usuarios
- **Qué falta:** `GET /api/v1/admin/tenants/{tenantId}/users` (listado paginado con filtros), `GET .../users/{userId}` (detalle), `PATCH .../users/{userId}` (campos editables), `PATCH .../users/{userId}/roles` (reemplazo total del set con `reason`), `POST .../users/{userId}/suspend|reactivate|disable`.
- **Por qué importa:** Es el flujo operativo diario del TenantAdmin. Sin ellos, los tenants no pueden gestionar su membresía sin acceso directo a DB.
- **Plan:** Sprint 3, Fase 4. Contratos en `04`, `05`, `06`, `07`. Estados válidos y transiciones en `06-estados-transiciones-usuario.md`.

#### G-ID-05. Validador centralizado de transiciones de estado
- **Qué falta:** función `ValidateStateTransition(UserStatus from, UserStatus to, Operation operation)` invocada por todos los endpoints admin y por `PATCH .../roles`.
- **Por qué importa:** Sin él, cada endpoint duplica lógica y las transiciones no explícitas fallan de forma inconsistente. Decisión 6 lo definió como obligatorio con `InvalidStateTransition` (409) como catch-all.
- **Plan:** Sprint 3, Fase 4, transversal a los endpoints admin.

#### G-ID-06. Metadatos admin: roles, permisos, audit
- **Qué falta:** `GET /api/v1/admin/roles`, `GET /api/v1/admin/permissions`, `GET /api/v1/admin/audit` (con filtros y paginación aún por documentar — Decisión 4 permite documentar contrato durante implementación).
- **Por qué importa:** Requeridos por la UI de administración para poblar selects y para revisar auditoría.
- **Plan:** Sprint 3, Fase 5.

#### G-ID-07. Endpoints dev-only y escenarios de prueba
- **Qué falta:** `POST /dev/auth/set-scenario` (simulación de estados de auth: denied, no-tenant, must-choose-tenant) y `POST /dev/admin/users/create-scenario` (creación de datos concretos de usuario). Complementarios (Decisión 9).
- **Por qué importa:** Habilitan pruebas de integración deterministas del ciclo de login sin depender de Entra real.
- **Plan:** Sprint 3, Fase 6. Contratos en `09-auth-local-admin-users.md`.

#### G-ID-08. Endurecimiento y auditoría admin
- **Qué falta:** cobertura completa de auditoría para acciones admin (`08-auditoria-admin-users.md`), pruebas de autorización de todos los endpoints admin (`07-autorizacion-admin-users.md`), pruebas cross-tenant sobre listados y detalle.
- **Por qué importa:** DoD del módulo Identity requiere estas pruebas para poder cerrar Sprint 3.
- **Plan:** Sprint 3, Fase 7 (endurecimiento). Criterios en `11-criterios-aceptacion-backend-login-admin.md`.

#### G-ID-09. Confirmación de `responsibleAreaId`
- **Qué falta:** identificar la entidad real (probablemente en GapManagement o ProcessingInventory), definir FK y validaciones de pertenencia al tenant.
- **Por qué importa:** Filtro `responsibleAreaId` en `GET /users` está documentado pero sin backend real detrás. Decisión 7 declaró bloqueador de Fase 0 con tarea `f0-responsible-area-validation`. Investigación registrada en `14-investigacion-responsible-area.md`.
- **Plan:** cerrar antes de implementar el filtro. Investigación en curso.

### 5.2 Infraestructura Azure

#### G-INF-01. IaC (Bicep o Terraform) inexistente
- **Qué falta:** Bicep/Terraform reproducible para dev-cloud, staging y prod. Sólo hay `.gitkeep` en `infra/azure/` (excepto alertas y workbook).
- **Por qué importa:** El DoD de infraestructura (`19 §10`) exige recursos creados por IaC o proceso reproducible antes del release. Sin IaC no hay `prod` conforme.
- **Plan:** blueprint definido en `17-blueprint-infraestructura-azure.md`; falta la implementación real. Owner: Sam (DevOps).

#### G-INF-02. Deploy pipeline a Azure
- **Qué falta:** workflow de deploy diferencial por ambiente, con approval gates, health checks, y rollback.
- **Por qué importa:** El runbook `runbook-deployment.md` documenta el proceso, pero no existe pipeline automatizado alineado.
- **Plan:** Sprint post-3, alineado con IaC.

#### G-INF-03. Gates CI del Sprint 2 no completamente activos
- **Qué falta:** el pipeline real (`ci.yml`) tiene guardas de naming/rutas prohibidas y ejecuta tests, pero los gates específicos de OpenAPI (parse/lint, `operationId` completo, `security` completo, `x-change-status` completo, `labelKey`) diseñados en `docs/evidata-backend-sprint-2/10-ci-pipeline-design.md` **no** están todos activos.
- **Por qué importa:** El contrato público puede degradarse silenciosamente entre PRs.
- **Plan:** Sprint 2 dejó el diseño; activación de cada gate es tarea explícita pendiente.

### 5.3 Módulos con implementación parcial

#### G-MOD-01. Search — indexación real y consulta autorizada
- **Qué falta:** integración real de indexación (Postgres FTS/pgvector) desde la Function `SearchIndexing`, endpoint de consulta que respete tenant y permisos, contrato de resultados unificados fuentes-legales + entidades-tenant.
- **Por qué importa:** Bloqueador para MCP contextual (que consume Search) y para experiencia de usuario en el frontend.
- **Plan:** Fase 6 del plan de implementación (`09 §9`).

#### G-MOD-02. MCP — pipeline productivo end-to-end
- **Qué falta:** integración real con Azure OpenAI/Foundry, implementación efectiva de `CitationVerifier` y `RiskClassifier` contra el modelo, cierre del flujo HITL (creación automática de `McpReviewTask` en `High`, notificaciones, resolución), pruebas de guardrails (citas obligatorias, abstención, minimización).
- **Por qué importa:** Es el diferencial comercial declarado; ADR-003 fija los invariantes.
- **Plan:** Fase 7 (`09 §10`). Runbook operativo ya existe (`runbook-incident-mcp.md`, `runbook-hitl-compliance.md`).

#### G-MOD-03. TenantManagement — administración operativa del tenant
- **Qué falta:** verificar cobertura de configuración de tenant, suspender/reactivar tenant, cambios de configuración auditados.
- **Por qué importa:** Sin ello, el ciclo comercial B2B (onboarding, off-boarding, suspend por impago) no puede ejecutarse.
- **Plan:** Fase 1 (`09 §4`) revisada para cierre.

#### G-MOD-04. Audit — adopción transversal completa
- **Qué falta:** comentarios `P2 con RBAC service centralizado` en varios command handlers (`ValidateEvidenceCommandHandler`, `AcceptGapWithRiskCommandHandler`) sugieren que la auditoría no está uniformemente aplicada. Falta refactor para inyectar `IAuditService` de forma consistente en todas las acciones sensibles del catálogo.
- **Por qué importa:** Principio VII (auditoría) exige cobertura uniforme.

### 5.4 Decisiones de negocio abiertas

#### G-DEC-01. Proveedor de identidad definitivo
- **Estado:** Entra External ID es la asunción declarada; confirmación formal aún pendiente (`04 §15`, `01 §2`).

#### G-DEC-02. Base de datos definitiva
- **Estado:** PostgreSQL Flexible Server es la asunción declarada; confirmación formal aún pendiente (`01 §2`, `17 §2`).

#### G-DEC-03. MFA obligatorio en MVP
- **Estado:** sin decisión (`04 §15`).

#### G-DEC-04. Política formal de retención de auditoría
- **Estado:** sin decisión (`04 §15`).

#### G-DEC-05. Clasificación formal de documentos en MVP
- **Estado:** sin decisión (`04 §15`).

#### G-DEC-06. IP allowlist enterprise
- **Estado:** sin decisión (`04 §15`).

#### G-DEC-07. TTL de invitación
- **Estado:** referencia a `expiresAt` en documentación de contrato sin valor por defecto declarado (Sprint 3).

### 5.5 Testing y calidad

#### G-QA-01. Proyecto de tests unitarios
- **Qué falta:** existe `tests/Evidata.Tests.Integration/` pero no un proyecto explícito de tests unitarios por módulo. `19 §3` exige unitarias.
- **Por qué importa:** DoD lo requiere; regresiones de dominio no cubiertas.

#### G-QA-02. Suite cross-tenant
- **Qué falta:** batería explícita de pruebas de intento cross-tenant en todos los módulos con controllers activos.
- **Por qué importa:** Principio I. `18 §4` lo lista como obligatorio por módulo.

#### G-QA-03. Suite de idempotencia por Function
- **Qué falta:** pruebas explícitas de consumo doble y poison en cada Function.
- **Por qué importa:** Principio V + DoD (`19 §4`).

---

## 6. Success Criteria (medibles)

Criterios agnósticos de tecnología que declaran "cuándo consideramos que el sistema entrega su promesa". Los tomamos y consolidamos desde `18-estrategia-testing-calidad.md` y `19-definition-of-done.md`.

- **SC-001**: 100% de las acciones del catálogo de acciones sensibles (`04 §8`) están cubiertas por auditoría automatizada con `before/after` y motivo cuando aplique. Verificado por batería de tests de auditoría.
- **SC-002**: 0 accesos cross-tenant exitosos en test suite. Toda pieza de código con FK a Tenant tiene al menos un test que intenta cross-tenant y verifica denegación + evento de seguridad.
- **SC-003**: 100% de mensajes procesados por Functions son idempotentes bajo prueba de doble consumo. Cero side-effects duplicados en un run de la suite async.
- **SC-004**: Tiempo de respuesta p95 del API en operaciones interactivas < 500ms para endpoints CRUD principales bajo carga nominal. [NEEDS CLARIFICATION: "carga nominal" — número concreto de RPS y tenants activos aún sin declarar.]
- **SC-005**: 100% de las 5 alertas KQL versionadas activas en `staging` y `prod` durante toda la ventana de release.
- **SC-006**: 100% de módulos core pasan su DoD antes de release (checklist de `19 §3`).
- **SC-007**: 100% de PRs merged pasan los gates de CI aplicables. Cero merges con gates skipped.
- **SC-008**: Contrato OpenAPI público (`/api/v1/*`) mantiene 100% de operaciones con `operationId`, `security` y `x-change-status` definidos.
- **SC-009**: Costo mensual de `prod` con 1–5 tenants activos ≤ $480 USD (banda superior de `docs/operations/cost-estimation.md`).
- **SC-010**: 100% de respuestas MCP `Low`/`Medium`/`High` correctamente clasificadas y con cita o abstención — verificado por batería de MCP guardrails (`18 §4.13`).
- **SC-011**: MTTR sobre incidentes MCP (según `runbook-incident-mcp.md`) < 4h para severidad alta. [NEEDS CLARIFICATION: SLO formal.]
- **SC-012**: Ninguna liberación introduce regresión de permisos ni de tenant isolation (gate absoluto).

---

## 7. Supuestos (Assumptions)

- Este documento asume que el equipo (Squad) y la constitución están en vigor (`docs/constitution.md`, `.squad/team.md`).
- Se asume que los ADRs vigentes en `docs/architecture/decisions.md` no serán rescritos sin proceso formal.
- Se asume que los paquetes documentales de sprint (`evidata-backend-sprint-2`, `evidata-backend-sprint-3`) son las fuentes autoritativas del contrato de esas iteraciones.
- Se asume que Entra External ID será el proveedor definitivo hasta que un ADR declare lo contrario (Brecha G-DEC-01).
- Se asume que PostgreSQL Flexible Server será el motor definitivo (Brecha G-DEC-02).
- Se asume que las 5 alertas KQL versionadas son el mínimo de observabilidad para release cloud.
- Se asume que el frontend consumirá el backend vía BFF con cookies opacas (no JWT en cliente).
- Se asume que ninguna característica de Sprint 3 se considera cerrada mientras las brechas G-ID-01..G-ID-08 permanezcan abiertas.
- Se asume que la ausencia de IaC (G-INF-01) es tolerable en dev/dev-cloud pero **no** en un release comercial a `prod`.

---

## 8. Cómo evoluciona este documento

- Cada brecha cerrada se marca como cerrada con referencia al PR/commit que la cerró y se mueve a un anexo histórico (`docs/especificacion-base-cerrada.md`) o se elimina si el trazado ya está en Git.
- Cada brecha nueva detectada durante un sprint se agrega en la sección §5 con el mismo formato.
- Toda decisión que cierre un `[NEEDS CLARIFICATION: ...]` requiere: (a) actualizar aquí, (b) ADR si aplica, (c) referencia cruzada al doc de sprint correspondiente.
- Este documento no es una spec de feature en el sentido estricto de spec-kit; es el **baseline vivo** contra el que las specs de feature deben leerse. Cuando se cree una spec formal de una nueva feature usando spec-kit, esa spec debe referenciar este baseline y declarar cómo interactúa con las brechas listadas.

---

**Versión del documento:** 1.0.0 | **Fecha:** 2026-07-14 | **Autoría:** Gandalf (Tech Architect) | **Basado en:** `docs/evidata-backend-docs/` (00–39, 99), `docs/evidata-backend-sprint-2/`, `docs/evidata-backend-sprint-3/`, `docs/architecture/decisions.md`, `docs/operations/*`, `docs/local-dev/SETUP.md`, `docs/runbooks/*`, e inspección directa de `src/` y `functions/` al 2026-07-14.
