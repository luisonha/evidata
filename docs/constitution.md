# Evidata Constitution

> Producto: **Evidata — Digital Trust Operating System**
> Alcance técnico interno: proyecto **ATLAS Opción B**
> Dominio regulatorio: Ley 21.719 (Chile) — protección de datos personales

Esta constitución consolida los principios no-negociables, arquitectura y flujo de trabajo del backend Evidata. Es la referencia vinculante para toda decisión técnica y funcional. Está basada en la documentación autoritativa en `docs/evidata-backend-docs/` (especialmente 01, 02, 04, 05, 09, 18, 19, 26), en los ADRs vigentes de `docs/architecture/decisions.md` y en las decisiones aprobadas del Sprint 3 (`docs/evidata-backend-sprint-3/13-decisiones-ambiguedades.md`).

---

## Core Principles

### I. Multi-Tenant Isolation por Diseño (Non-Negotiable)

Toda entidad funcional del sistema debe portar `tenant_id`. Toda query funcional debe filtrar por `tenant_id`, todo comando debe validarlo, todo evento del Outbox debe incluirlo cuando aplique, todo blob debe llevarlo en metadata y/o ruta lógica, y todo contexto MCP debe limitarse por él. Un módulo **nunca** puede confiar en filtros del frontend; el filtro es siempre servidor. Todo intento cross-tenant debe generar evento de seguridad auditable y disparar la alerta KQL `cross-tenant-attempt`. La unicidad de datos sensibles (p.ej. email de usuario) se define de forma compuesta con `tenant_id`, no global (Decisión 8 del Sprint 3: `UNIQUE(tenant_id, email)`).

*Rationale:* Evidata es un SaaS de compliance donde una fuga cross-tenant es riesgo legal directo bajo Ley 21.719. No hay excepciones a este principio, ni siquiera para endpoints de administración.

### II. Autorización Contextual en el Core (Non-Negotiable)

La autorización vive dentro del core, nunca externalizada a un servicio de auth genérico. Cada acción debe evaluarse en función de: usuario, tenant, rol(es), permiso, entidad, estado de la entidad, asignación del usuario, sensibilidad del recurso y acción solicitada. Un usuario puede portar **múltiples roles simultáneos** desde el día uno (Decisión 2 del Sprint 3: relación many-to-many `UserProfile ↔ Role` vía `UserProfileRole`). Toda regla de negocio que dependa de "qué rol tiene el usuario" debe evaluarse como pertenencia a conjunto, nunca como igualdad de un único valor. Los permisos base son los definidos en `docs/evidata-backend-docs/04-seguridad-transversal.md §6` y los roles base los de `§5`. Ningún endpoint funcional puede ejecutarse sin tenant resuelto, usuario resuelto y permiso validado — este es un criterio de aceptación absoluto de la Fase 1.

### III. Autenticación Externa, Sin Auth Productiva Propia (Non-Negotiable)

Evidata **no** implementa autenticación propia productiva. En ambientes cloud (dev-cloud, staging, prod) la autenticación es delegada a **Microsoft Entra External ID**. En desarrollo local y pruebas automatizadas existen modos controlados: `LocalDev` (headers dev-only + usuarios seed) y `TestAuth` (identidades sintéticas para test suites). Reglas obligatorias:

1. `LocalDev` y `TestAuth` **nunca** pueden estar habilitados en `Staging` ni en `Production`. La API debe fallar al iniciar si `EVIDATA_AUTH_MODE != Entra` en esos ambientes (verificado por `LocalDevEnvironmentGuard`).
2. Los headers dev-only sólo se aceptan cuando el guard confirma ambiente local.
3. El dominio y los módulos **no** dependen directamente de claims de Entra. Toda autorización se evalúa contra `ICurrentUserContext` (abstracción interna), independiente del proveedor.
4. La sesión productiva es **server-side opaca**: cookie `__Host-evidata.sid` HttpOnly + Secure + SameSite=Strict, referenciando fila persistida en tabla `sessions` con sliding expiration. Nunca se emiten tokens JWT al cliente.

### IV. Consistencia Eventual vía Outbox Neutral al Broker (Non-Negotiable)

Toda operación de negocio que dispara efectos asíncronos debe persistir el mensaje en la tabla `OutboxMessages` **dentro de la misma transacción de PostgreSQL** que modifica el estado. Un worker (Evidata.Worker.Outbox) publica los mensajes a Azure Storage Queues. Los módulos publican a **destinos lógicos** (`document-processing`, `search-indexing`, `report-generation`, `notification`, `mcp-batch`, `security-jobs`, `maintenance-jobs`), no a colas concretas — la resolución destino→cola es infraestructural. Los mensajes son versionados (`MessageVersion`, `SchemaVersion` en el envelope). Storage Queues es el broker inicial; Service Bus se incorpora **por destino** sólo cuando existan gatillos (pub/sub, sesiones, filtros) — la abstracción de mensajería debe permitirlo sin tocar código de negocio (ADR-002).

### V. Idempotencia Obligatoria en Todo Consumo Asíncrono (Non-Negotiable)

Toda Azure Function y todo handler de mensajería debe ser idempotente. Consumo dos veces del mismo mensaje debe producir el mismo resultado final. Cada Function debe: validar mensaje y tenant, tener manejo explícito de poison messages (dead-letter después de N reintentos), no registrar payloads sensibles completos en logs, usar Managed Identity donde aplique, y actualizar el estado del job/entidad correspondiente. Las Functions **no toman decisiones de negocio finales** — sólo actualizan estados de jobs o resultados propios; las aprobaciones y decisiones legales permanecen en el core.

### VI. Arquitectura Modular con Ownership Estricto de Datos (Non-Negotiable)

Evidata se implementa como **monolito modular** en .NET 10: un `.csproj` por módulo (`src/Modules/{Nombre}/`), cada uno con su propio `DbContext`, su propio schema PostgreSQL, y sin FK cruzadas entre schemas (ADR-001). Reglas de dependencia:

1. Ningún módulo accede directamente a tablas de otro módulo. Toda referencia cruzada es por ID a nivel de dominio.
2. Cada módulo expone un contrato interno (`Application/Abstractions` + `Contracts/`) y publica eventos de dominio versionados.
3. Los módulos no comparten modelos de persistencia. Queries cross-domain van por vistas/proyecciones controladas o por eventos.
4. Cada módulo tiene ownership completo: entidades, migraciones, políticas de autorización propias, auditoría de acciones relevantes, y sus pruebas.
5. La extracción futura a microservicios se hará por módulo, gradualmente, sólo cuando existan gatillos reales de volumen o SLA. Los candidatos naturales son las capacidades ya asíncronas (Document Processing, Report Generation, Notification, Search Indexing) y eventualmente MCP.

### VII. Auditoría Append-Only de Acciones Sensibles (Non-Negotiable)

Toda acción sensible debe auditarse de forma funcionalmente inmutable (append-only). Las acciones sensibles están enumeradas en `04-seguridad-transversal.md §8` e incluyen — sin ser exhaustivo — asignación de rol administrador, invitación de usuario, cambio de owner de tratamiento, aprobación/edición de tratamiento aprobado, eliminación/reemplazo de evidencia, descarga de evidence pack, exportación RAT completa, consulta MCP con contexto sensible, cierre de brecha crítica, aceptación de riesgo, y cambios de configuración de tenant. Cada registro debe incluir: usuario, tenant, rol, permiso, acción, entidad, timestamp UTC, IP, user-agent, correlation-id, resultado, motivo cuando corresponda, y `before/after` cuando aplique. Los logs técnicos **no** deben contener datos personales completos salvo estricta necesidad — se preferirán identificadores, referencias y metadata.

### VIII. Testing Multi-Capa como Definition of Done (Non-Negotiable)

Ningún módulo se considera terminado sin pasar su DoD (`docs/evidata-backend-docs/19-definition-of-done.md`). Como mínimo cada módulo core debe tener: pruebas unitarias de dominio, pruebas de integración de API + PostgreSQL, **pruebas de tenant isolation cross-tenant** (regresión crítica), pruebas de autorización por rol/permiso, pruebas de idempotencia para consumo asíncrono, pruebas de Outbox (persistencia + publicación), y pruebas de auditoría de acciones sensibles. Las Functions añaden: prueba de consumo correcto, prueba de fallo controlado, y prueba de poison handling. Toda regresión de permisos o de tenant isolation es un bloqueador de release, no un bug menor.

### IX. MCP Determinístico Primero, Asistivo Después (Non-Negotiable)

El core determinístico (RAT, Evidence, Gaps, Workflow, Legal Knowledge, Search, Reporting) debe funcionar y ser útil **sin** el MCP. El MCP se construye al final de Opción B, sobre datos ya confiables y auditables. Reglas duras para el MCP (ADR-003 + `04 §11`):

1. No usar contexto tenant sin permiso `mcp.ask_with_tenant_context`.
2. Aplicar minimización de contexto — nunca enviar documentos completos al modelo si no es necesario.
3. Toda respuesta legal debe **citar fuente o abstenerse**. El sistema no certifica cumplimiento.
4. Clasificar riesgo de cada respuesta (`Low`/`Medium`/`High`). Las respuestas `High` generan automáticamente `McpReviewTask` (HITL) y no bloquean al usuario.
5. Registrar interacción completa (query, fuentes utilizadas, respuesta, clasificación, feedback) en auditoría.
6. Nunca entrenar modelos con datos tenant sin decisión explícita documentada.
7. Los estados `Failed` (error del modelo) son terminales — no permiten `RequestHumanReview()` sobre la interacción fallida; se crea `McpReviewTask` independiente.

### X. Versionado Deliberado y Contratos Estables

Toda API pública vive bajo `/api/v1/*`. Los contratos OpenAPI del backend son la fuente de verdad para clientes (`docs/openapi/openapi.json`). Todo endpoint debe declarar: método, ruta, permiso requerido, request, response, códigos de error, validaciones, auditoría emitida, y eventos Outbox emitidos. Todo evento y mensaje asíncrono está versionado. El versionado del producto sigue SemVer 2.0 manual/deliberado (`docs/operations/versioning.md`) — no hay incremento automático. Breaking changes en API pública requieren nueva versión mayor y ventana de deprecación.

### XI. Registro de Decisiones y Trazabilidad Documental

Toda decisión arquitectónica relevante se registra como ADR en `docs/architecture/decisions.md`. Toda ambigüedad detectada durante un sprint se resuelve con documento de decisión explícito antes de codificar (patrón del Sprint 3 `13-decisiones-ambiguedades.md`). Toda decisión pendiente crítica debe declararse en el documento correspondiente (`§15` en el doc de seguridad transversal, sección "Decisiones pendientes de seguridad"), no dejarse implícita.

---

## Arquitectura y Requerimientos Técnicos

### Stack canónico

- **Runtime:** .NET 10, C# nullable habilitado, `Directory.Build.props` centralizado.
- **API principal:** ASP.NET Core (`src/Evidata.Api`).
- **Orquestación local:** .NET Aspire (`src/Evidata.AppHost`) para levantar todo el stack.
- **Compartido:** `Evidata.ServiceDefaults` para OpenTelemetry, health checks y métricas comunes.
- **Worker Outbox:** `Evidata.Worker.Outbox` — publisher neutral al broker.
- **Módulos de dominio (14):** TenantManagement, Identity, Security, Audit, LegalKnowledge, Documents, Evidence, ProcessingInventory (RAT), GapManagement, Workflow, Reporting, Search, Mcp, Contracts (SDK compartido de tipos públicos).
- **Azure Functions (6):** `Evidata.Functions.DocumentProcessing`, `SearchIndexing`, `Reporting`, `Notifications`, `McpBatch`, `Maintenance` — isolated worker model, .NET 10.
- **Persistencia:** PostgreSQL con un schema dedicado por módulo. Migraciones EF Core por módulo, aplicadas de forma independiente.
- **Storage:** Azure Blob (documentos, evidencias, reportes generados). Blobs privados, descarga mediada por backend con URLs de expiración corta y auditoría.
- **Mensajería:** Azure Storage Queues como broker inicial; abstracción `IQueuePublisher` + destinos lógicos permite migrar a Service Bus por destino sin tocar módulos.
- **Secretos:** variables locales en dev; **Azure Key Vault** en cloud, accedido por Managed Identity.
- **IA/MCP:** Azure OpenAI / Foundry, proveedor intercambiable a nivel de configuración.
- **Search inicial:** PostgreSQL full-text + `pgvector` cuando aplique; Azure AI Search queda como opción futura no bloqueante.
- **Observabilidad:** logging estructurado (OTel) + Application Insights + Log Analytics + Azure Monitor Workbooks en cloud. En local: consola estructurada + Aspire Dashboard.

### Variante Local Dev (obligatoria y reproducible)

- Stack orquestado por `.NET Aspire` (`cd src/Evidata.AppHost && dotnet run`) o `docker-compose` en `infra/local/` como alternativa sólo-infra.
- **PostgreSQL 16** en contenedor; **Azurite** para Blob y Queues; **Mailpit** para captura de correos salientes.
- Aspire Dashboard en `http://localhost:15888`.
- Auth = `LocalDev`. Los usuarios seed provienen de `LocalDevSeedUsers` y de la migración `20260712103640_SeedLocalDevUsers`. Los roles se inyectan al identity context por headers dev-only o por sesión seed.
- El guard `LocalDevEnvironmentGuard` **debe** validar que `EVIDATA_AUTH_MODE=LocalDev` sólo se resuelva cuando `ASPNETCORE_ENVIRONMENT ∈ {Development, Test}` — la API falla al iniciar en caso contrario.
- `TestAuth` está reservado exclusivamente para test suites automatizados (`tests/Evidata.Tests.Integration`).
- No requiere acceso a red externa ni credenciales de Azure/Entra para trabajo diario. La guía autoritativa es `docs/local-dev/SETUP.md`.

### Variante Azure Producción

- **Autenticación:** Microsoft Entra External ID obligatorio. `EVIDATA_AUTH_MODE=Entra`. Cualquier otro valor debe abortar el bootstrap.
- **Hosting API:** Azure App Service Linux (P1v3 en prod; B-tier en dev-cloud/staging). Managed Identity habilitado.
- **Hosting Functions:** Function Apps individuales por capability (`func-atlas-{capability}-{env}`), isolated worker model, Managed Identity.
- **PostgreSQL Flexible Server** con backup automatizado y ventana de mantenimiento definida por ambiente. Confirmación definitiva del proveedor de DB queda como decisión declarada en `01-vision-y-decisiones.md` — hasta cierre formal, Postgres es la asunción de trabajo.
- **Azure Storage Account** para Blob (containers privados por tenant o por tipo de contenido) y Queues (una por destino lógico).
- **Azure Key Vault** por ambiente, separación estricta prod/staging/dev-cloud. Acceso por Managed Identity, rotación planificada, mínimo privilegio, auditoría de acceso a secretos habilitada.
- **Application Insights + Log Analytics** por ambiente. Alertas KQL ya versionadas en `infra/azure/alerts/`: `cross-tenant-attempt`, `function-failure-spike`, `hitl-stale-tasks`, `mcp-high-error-rate`, `api-high-latency`. Workbook operacional en `infra/azure/dashboards/`.
- **Azure OpenAI** (o Foundry) para MCP, con guardrails aplicados en el core (citation verifier, risk classifier, HITL router).
- **Naming convention** (`17-blueprint-infraestructura-azure.md §4`): `rg-atlas-{env}-{region}`, `app-atlas-api-{env}`, `func-atlas-{capability}-{env}`, `statlas{env}{suffix}`, etc.
- **Ambientes:** `dev-cloud`, `staging`, `prod`. Ninguno de ellos puede tener `LocalDev` o `TestAuth` habilitados.
- **IaC:** debe existir Bicep o Terraform reproducible por ambiente antes del primer deploy formal a `prod`. Los recursos manualmente creados no son aceptables como estado de producción.

### Datos personales y protección documental

- Blobs privados sin acceso anónimo, URLs temporales con expiración corta, descarga mediada y auditada, hash de archivo persistido, versionado documental, y eliminación lógica preferente sobre borrado físico (excepto en flujos de derecho al olvido regulados que se implementen a futuro).
- Toda evidencia clasificada `Sensitive` requiere motivo obligatorio para descarga/reemplazo/eliminación.
- Ningún log técnico debe imprimir datos personales completos: se prefieren identificadores y referencias.

---

## Flujo de Desarrollo y Calidad

### Ramas y PR

- Rama base de trabajo: `develop`. Rama de release: `main`. Ramas de feature: `dev/**`.
- CI (`.github/workflows/ci.yml`) corre en `push` y `pull_request` contra `develop`, `main` y `dev/**`, incluyendo guardas de naming/rutas prohibidas (`/api/v1/treatments`, `/weatherforecast`) y ejecución de la solución completa (`Evidata.sln`).
- El diseño completo de gates PR/main/release está en `docs/evidata-backend-sprint-2/10-ci-pipeline-design.md` (gates de build, unit tests, domain tests, OpenAPI parse/lint, forbidden routes, `operationId` completo, `security` completo, `x-change-status` completo, `labelKey` en contratos UI-facing). Esos gates son la meta operativa del pipeline; los que aún no estén en CI son deuda explícita.
- Todo PR debe: compilar en Release, pasar los gates de CI aplicables, tener tests que ejerzan la ruta modificada, actualizar OpenAPI si el contrato cambió, y actualizar la documentación de fase/módulo cuando corresponda.
- Las branches de agentes automáticos (Squad) siguen la convención documentada en `.squad/routing.md`.

### Definition of Done (resumen operativo)

Un componente cerrado cumple con `docs/evidata-backend-docs/19-definition-of-done.md`. En resumen:

1. Cumple la especificación funcional del módulo/fase.
2. Contratos API o mensaje documentados (OpenAPI actualizado si aplica).
3. Validación de entrada + autorización backend + tenant isolation efectivos.
4. Auditoría emitida cuando la acción es sensible.
5. Eventos Outbox emitidos cuando hay efectos asíncronos.
6. Pruebas unitarias, de integración, cross-tenant y de autorización pasando.
7. Observabilidad mínima: logs correlacionados, métricas y trazas.
8. Manejo de errores funcionales estándar (envelope de error consistente).
9. Documentación actualizada en el Markdown correspondiente.
10. Desplegado y validado en ambiente de validación.
11. No deja decisiones críticas implícitas.

### Comandos de referencia

- **Build:** `dotnet build Evidata.sln -c Release`
- **Test:** `dotnet test tests/Evidata.Tests.Integration/` (y demás proyectos de test cuando se sumen)
- **Migraciones:** `dotnet ef database update --context {ModuleDbContext}` desde `src/Evidata.Api/`. Guía completa en `docs/runbooks/runbook-database-migration.md`.
- **Stack local:** `cd src/Evidata.AppHost && dotnet run`
- **Deploy:** ver `docs/runbooks/runbook-deployment.md`
- **Incidentes MCP:** ver `docs/runbooks/runbook-incident-mcp.md`
- **HITL:** ver `docs/runbooks/runbook-hitl-compliance.md`

### Squad de trabajo

El equipo humano/agente está descrito en `.squad/team.md`. Roles vinculantes para la arquitectura: **Gandalf** (Lead / Tech Architect) es owner de esta constitución y de los ADRs; **Aragorn** de módulos core; **Legolas** de seguridad/autorización; **Gimli** de datos/migraciones; **Frodo** de Functions/async; **Sam** de DevOps/Infra Azure. **Rai** ejecuta el gate RAI antes de release. **Fact Checker** valida claims documentales. **Scribe** mantiene la bitácora de sesión. Toda decisión que afecte un principio de esta constitución debe pasar por Gandalf.

---

## Seguridad Transversal (Sección específica por criticidad del dominio)

Esta sección se declara como capítulo propio, no como subsección de arquitectura, porque Evidata es un producto de compliance legal donde la seguridad es requisito de negocio, no característica.

### Superficie de identidad

`ICurrentUserContext`, `IIdentityResolver`, `ITenantResolver`, `IPermissionEvaluator`, `IAuthorizationPolicyEvaluator`, `ISecurityAuditWriter` son las abstracciones canónicas. El dominio se programa contra ellas; Entra y LocalDev son implementaciones intercambiables. Los middlewares `SessionResolutionMiddleware`, `TenantIsolationMiddleware` y `CorrelationIdMiddleware` son parte del pipeline obligatorio de la API.

### Sesiones

Sesiones server-side opacas, cookie `__Host-evidata.sid` (HttpOnly, Secure, SameSite=Strict). Sliding expiration por defecto 24h. Revocación explícita al logout, al suspender/deshabilitar usuario, o al cambio de rol crítico. La tabla `sessions` es la fuente de verdad; nunca se emite JWT al cliente en producción. Toda sesión persistida guarda snapshot de roles y `permissionsVersion`.

### RBAC y estados de usuario

Estados válidos de `UserProfile` en la fase actual: `Active`, `Pending`, `Suspended`, `Disabled`. `deleted_soft` está fuera de alcance de Sprint 3 (Decisión 3). Roles múltiples por usuario vía `UserProfileRole`. Restricciones invariantes (Decisión 2): nunca dejar un tenant sin al menos un `TenantOwner` activo — se aplica también en suspender/deshabilitar/cambio de roles.

### Códigos de error de identidad

Definidos en `Evidata.Modules.Identity.Contracts.IdentityErrorCodes`. Casos con mapping HTTP dependiente de contexto: `InvitationRevoked` → 403 en flujos de login y 409 en acciones administrativas (Decisión 1); `InvitationExpired` → 403 (Decisión 5); `InvalidStateTransition` → 409 catch-all para transiciones no contempladas (Decisión 6); `InvitationAlreadyPending` → 409 (Decisión 10, pendiente de código exacto durante implementación).

### Acciones sensibles

Toda acción del catálogo `04 §8` genera auditoría reforzada con `before/after` y motivo cuando corresponda. Descarga de evidence pack, exportación completa de RAT, aprobación/edición de tratamiento aprobado, cambio de configuración de retención, y consulta MCP con contexto sensible siempre disparan auditoría y — cuando aplique — evento hacia `security-jobs`.

### Alertas mínimas obligatorias

Las 5 alertas KQL versionadas en `infra/azure/alerts/` son parte del contrato de release a `staging` y `prod`. Cualquier ambiente cloud sin ellas cargadas se considera fuera de conformidad. Nuevas alertas se agregan por ADR.

### Decisiones de seguridad aún abiertas

Enumeradas por transparencia — no son excusa para no arrancar, pero deben cerrarse antes del release comercial: proveedor de identidad definitivo (Entra External ID es la asunción), MFA obligatorio desde MVP (sí/no), política formal de retención de auditoría, clasificación formal de documentos desde MVP, y IP allowlist para tenants enterprise.

---

## Governance

1. Esta constitución **supera** cualquier práctica ad-hoc, comentario en código, o instrucción de agente que la contradiga. En caso de conflicto entre esta constitución y otro documento del repo, prevalece esta constitución hasta que un ADR aprobado la modifique.
2. Toda enmienda a esta constitución requiere: (a) redacción explícita del cambio y de su rationale, (b) ADR de soporte en `docs/architecture/decisions.md`, (c) revisión y aprobación de Gandalf (Tech Architect) y del Product Owner, (d) incremento de versión de la constitución según impacto (MAJOR = cambio de principio o eliminación, MINOR = principio nuevo o sección nueva, PATCH = clarificaciones y correcciones), (e) actualización de `Last Amended` y bump del changelog interno del documento.
3. Todo PR que introduzca desviación de un principio debe: (a) declararla explícitamente en la descripción, (b) referenciar el principio afectado, (c) proponer plan de retorno a conformidad o ADR que enmiende el principio. Un PR que viole silenciosamente esta constitución se rechaza en revisión sin discusión.
4. Los principios marcados **Non-Negotiable** no admiten "excepciones para este sprint". Cualquier propuesta de excepción exige ADR previo.
5. La verificación de conformidad es parte del gate RAI (Rai) previo a release. Fact Checker puede levantar objeciones documentales si detecta contradicciones entre docs y esta constitución.
6. La documentación de arquitectura en `docs/evidata-backend-docs/`, los ADRs, los paquetes de sprint (`docs/evidata-backend-sprint-2/`, `docs/evidata-backend-sprint-3/`), los runbooks y la especificación base del sistema (`docs/especificacion-base.md`) son las fuentes autoritativas complementarias. Esta constitución las referencia y no las reescribe.

**Version**: 1.0.0 | **Ratified**: 2026-07-14 | **Last Amended**: 2026-07-14
