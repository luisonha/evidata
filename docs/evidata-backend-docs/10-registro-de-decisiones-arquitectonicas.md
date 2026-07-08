# 10 — Registro de decisiones arquitectónicas

## 1. Propósito

Este documento reemplaza a `10-pendientes-y-decisiones-por-cerrar.md`.

Su función ya no es listar gaps abiertos, sino mantener un registro corto y trazable de decisiones vigentes del backend de **Evidata**. Cada decisión debe indicar estado, criterio, impacto y documentos relacionados.

Las dudas o tareas abiertas deben registrarse en el backlog o en el documento técnico correspondiente, no en este archivo, salvo que sean una decisión arquitectónica real aún no cerrada.

## 2. Convención

Cada decisión usa esta estructura:

- **Estado:** aprobada, propuesta, reemplazada o en revisión.
- **Decisión:** definición concreta.
- **Motivo:** razón principal.
- **Impacto:** consecuencia técnica u operativa.
- **Documentos relacionados:** archivos donde se desarrolla la especificación.

## ADR-001 — Producto, marca y alcance técnico

**Estado:** aprobada.

**Decisión:** el producto se llama **Evidata**. Su descriptor principal es **Digital Trust Operating System**. Los ejes de marca son **Trazabilidad medible · Gobernanza operativa · Automatización con criterio**. **ATLAS** queda reservado como nombre de proyecto técnico, investigación o alcance interno.

**Motivo:** el documento de construcción de marca desarrolla la identidad de Evidata como marca/producto y ATLAS corresponde al informe técnico de factibilidad.

**Impacto:** los documentos técnicos deben referirse a Evidata como producto. ATLAS puede mencionarse sólo como proyecto técnico interno o investigación base.

**Documentos relacionados:** `24-identidad-producto-y-marca.md`, `23-matriz-trazabilidad-fuentes.md`, `01-vision-y-decisiones.md`.

## ADR-002 — Arquitectura backend inicial

**Estado:** aprobada.

**Decisión:** el backend partirá como **core modular .NET 10** con procesos asíncronos desacoplados mediante workers y Azure Functions. No se iniciará con microservicios completos por módulo.

**Motivo:** reduce complejidad operacional inicial, facilita consistencia transaccional y mantiene capacidad de evolución futura.

**Impacto:** los módulos se separan por límites internos, contratos, eventos y dependencias, pero comparten despliegue core al inicio.

**Documentos relacionados:** `02-arquitectura-general-backend.md`, `06-modulos-core-backend.md`, `07-microservicios-y-functions.md`.

## ADR-003 — Base transaccional principal

**Estado:** aprobada para MVP.

**Decisión:** PostgreSQL será la base transaccional principal del backend.

**Motivo:** el producto requiere consultas relacionales, multi-tenant isolation, estados, auditoría, relaciones entre evidencias, flujos, usuarios, permisos y reportes.

**Impacto:** el modelo debe diseñarse con `tenant_id`, migraciones versionadas, índices por tenant y convenciones de auditoría.

**Documentos relacionados:** `11-modelo-datos-backend.md`, `14-especificacion-rat-mvp.md`, `15-especificacion-evidence-mvp.md`.

## ADR-004 — Mensajería inicial y Outbox

**Estado:** aprobada.

**Decisión:** la mensajería inicial usará Azure Storage Queues, con abstracción neutral al broker y patrón Outbox desde el MVP.

**Motivo:** Storage Queues reduce costo y complejidad inicial. El Outbox evita perder eventos cuando hay fallas entre transacción y publicación.

**Impacto:** todos los mensajes relevantes deben persistirse antes de publicarse; los consumidores deben ser idempotentes.

**Documentos relacionados:** `05-mensajeria-asincronia-y-outbox.md`, `16-contratos-eventos-mensajes.md`, `07-microservicios-y-functions.md`.

## ADR-005 — Desarrollo local sin dependencia diaria de Azure

**Estado:** aprobada.

**Decisión:** el desarrollo diario será local-first. Azure no será requerido para programar, depurar ni ejecutar pruebas de integración base.

**Motivo:** evita costos, dependencia de conectividad y fricción para el equipo.

**Impacto:** el entorno local debe levantar API, workers, Functions, PostgreSQL, Azurite y Mailpit. Azure queda para dev-cloud, staging y producción.

**Documentos relacionados:** `25-stack-tecnologico-desarrollo-local.md`, `03-infraestructura-azure.md`, `17-blueprint-infraestructura-azure.md`.

## ADR-006 — .NET Aspire como orquestador local

**Estado:** aprobada.

**Decisión:** .NET Aspire será el orquestador principal del entorno de desarrollo local.

**Motivo:** permite coordinar varios proyectos .NET, dependencias locales, configuración, health checks, logs y trazas desde una topología única.

**Impacto:** el repositorio debe incluir `Evidata.AppHost` y `Evidata.ServiceDefaults`. Docker puede seguir existiendo como soporte de contenedores, pero AppHost es la entrada principal de desarrollo.

**Documentos relacionados:** `25-stack-tecnologico-desarrollo-local.md`.

## ADR-007 — Azure Functions en .NET isolated worker

**Estado:** aprobada.

**Decisión:** las Functions se implementarán con **.NET isolated worker** y se integrarán con Aspire mediante `AddAzureFunctionsProject<TProject>()`.

**Motivo:** isolated worker es el modelo compatible con la estrategia .NET moderna y permite una integración más limpia con Aspire y Service Defaults.

**Impacto:** no se deben crear Functions in-process. Las Functions no deben registrarse en Aspire mediante `AddProject<T>()`.

**Documentos relacionados:** `07-microservicios-y-functions.md`, `25-stack-tecnologico-desarrollo-local.md`.

## ADR-008 — Seguridad local sin Entra

**Estado:** aprobada.

**Decisión:** la seguridad funcional se desarrollará en local sin depender de Entra. Se usarán modos `LocalDev` y `TestAuth`; Entra External ID será obligatorio sólo en ambientes Azure.

**Motivo:** el equipo necesita desarrollar RBAC, tenant isolation, permisos contextuales y auditoría sin depender de login cloud.

**Impacto:** el core debe depender de `CurrentUserContext`, no de claims de Entra. `LocalDev` y `TestAuth` deben fallar en staging y producción.

**Documentos relacionados:** `04-seguridad-transversal.md`, `26-seguridad-desarrollo-local-sin-entra.md`, `13-matriz-rbac-politicas-autorizacion.md`.

## ADR-009 — Observabilidad local con Seq opcional y Azure con Application Insights

**Estado:** aprobada.

**Decisión:** Evidata emitirá logs estructurados y trazas mediante una capa común de observabilidad. En local se usará consola, Aspire Dashboard y opcionalmente Seq. En dev-cloud, staging y producción se usará Application Insights / Azure Monitor.

**Motivo:** Seq mejora la depuración local de flujos distribuidos sin obligar a usar Azure. Application Insights es la herramienta oficial para ambientes Azure.

**Impacto:** la aplicación no debe acoplarse a Seq ni a Application Insights desde dominio o módulos funcionales. El destino de observabilidad se decide por configuración de ambiente. El deploy debe ser transparente: mismo código, distinta configuración.

**Configuración esperada:**

```text
Local:
EVIDATA_OBSERVABILITY_PROVIDER=Local
EVIDATA_LOG_SINKS=Console,AspireDashboard,Seq

DevCloud/Staging/Production:
EVIDATA_OBSERVABILITY_PROVIDER=Azure
EVIDATA_LOG_SINKS=Console,ApplicationInsights
APPLICATIONINSIGHTS_CONNECTION_STRING=<secret>
```

**Regla:** Seq nunca es dependencia productiva. Application Insights no debe ser requisito para depurar localmente.

**Documentos relacionados:** `25-stack-tecnologico-desarrollo-local.md`, `17-blueprint-infraestructura-azure.md`, `18-estrategia-testing-calidad.md`.

## ADR-010 — IA y búsqueda sin dependencia cloud en desarrollo

**Estado:** aprobada.

**Decisión:** en desarrollo local, IA usará proveedor `Mock` o `Recorded`, y búsqueda partirá con PostgreSQL full-text/pgvector opcional. Azure OpenAI y Azure AI Search no son dependencias obligatorias para desarrollo diario.

**Motivo:** reduce costo, evita bloqueo por servicios cloud y permite testear flujos antes de integrar proveedores reales.

**Impacto:** todo uso de IA y búsqueda debe pasar por interfaces intercambiables.

**Documentos relacionados:** `25-stack-tecnologico-desarrollo-local.md`, `37-paquete-search-indexing.md`, `38-paquete-mcp-contextual.md`.

## 3. Decisiones abiertas reales

A la fecha de esta versión, no se mantiene una lista general de pendientes arquitectónicos en este archivo.

Cualquier nueva decisión abierta debe registrarse aquí sólo si cumple al menos una condición:

- cambia el stack tecnológico;
- cambia el modelo de seguridad;
- cambia infraestructura o despliegue;
- cambia límites de módulos;
- cambia persistencia, mensajería, observabilidad o integración cloud;
- afecta costos o riesgos operativos relevantes.

Las tareas de implementación, ajustes menores o preguntas de backlog deben ir al backlog correspondiente, no a este registro.
