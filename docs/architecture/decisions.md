# Decisiones de Arquitectura — Evidata Backend

**Registro de decisiones técnicas relevantes para el mantenimiento y evolución del sistema.**

---

## ADR-001: Monolito modular con bounded contexts

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

Evidata backend se implementa como un **monolito modular** donde cada módulo de dominio es
un proyecto de clase independiente (`.csproj`) con su propio `DbContext`, schema PostgreSQL,
y sin referencias cruzadas directas entre módulos de aplicación.

### Fundamento

- El equipo es pequeño — microservicios agregarían overhead operacional sin beneficio real en esta etapa
- La separación en proyectos mantiene el aislamiento sin la complejidad de deployment distribuido
- PostgreSQL con múltiples schemas ofrece aislamiento de datos suficiente para MVP
- Permite migrar a microservicios por módulo en el futuro si el volumen lo justifica

### Consecuencias

- Las referencias cruzadas se realizan solo a nivel de dominio (IDs), nunca FK entre schemas
- Cada módulo es responsable de sus migraciones
- Tests unitarios usan InMemory provider por módulo

---

## ADR-002: Azure Functions para procesamiento asíncrono

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

Todo procesamiento asíncrono (notificaciones, reportes, indexación, jobs MCP) se implementa
como **Azure Functions** con triggers de cola (`QueueTrigger`).

### Fundamento

- Desacoplamiento: la API no espera operaciones costosas (Excel, OCR, HITL)
- Escalado independiente: las Functions escalan por cola sin afectar la API
- Cost-efficient: serverless — pago por ejecución, no por uptime
- Patrón Outbox garantiza entrega at-least-once

### Consecuencias

- Todas las Functions usan el mismo envelope `QueueMessageEnvelope`
- El esquema de mensajes es versionado (campo `SchemaVersion`)
- Mensajes desconocidos se loguean como warning y se descartan (no se rompe el pipeline)
- Las colas son: `notification-delivery`, `report-generation`, `search-indexing`, `mcp-batch`

---

## ADR-003: Asistente MCP con clasificación de riesgo y HITL

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

El asistente MCP clasifica cada respuesta en `Low/Medium/High` risk. Las respuestas `High`
generan automáticamente una `McpReviewTask` para revisión humana.

Las interacciones `Failed` (error del modelo) no pueden cambiar de estado — se crean
`McpReviewTask` para que el equipo de cumplimiento atienda el caso.

### Fundamento

- La Ley 21.719 impone responsabilidad sobre el responsable del tratamiento
- Las respuestas de alto riesgo sobre tratamiento de datos sensibles requieren validación humana
- El sistema NUNCA debe bloquear al usuario — el HITL es asíncrono

### Consecuencias

- `McpInteractionStatus.Failed` es terminal — no permite `RequestHumanReview()`
- `McpRetryHandler` crea `McpReviewTask` para interacciones failed sin tarea (idempotente)
- `HitlEscalationHandler` detecta tareas estancadas >2h y emite alertas
- El dashboard de compliance muestra tareas HITL pendientes

---

## ADR-004: Multi-tenancy por TenantId en todas las entidades

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

El aislamiento multi-tenant se implementa filtrando por `TenantId` en **todas** las queries.
No se usa Row-Level Security de PostgreSQL en MVP.

### Fundamento

- RLS agrega complejidad de configuración y debugging difícil en desarrollo local
- El filtrado explícito en EF es más auditable y testeable
- Los intentos cross-tenant son detectados por `EvidataMetrics.CrossTenantAttempt()` y alertados

### Consecuencias

- **TODA** consulta a la base de datos debe incluir filtro por `TenantId`
- El middleware de autorización inyecta `TenantId` del JWT en todas las requests
- Los tests unitarios deben verificar aislamiento de tenant como caso de prueba estándar
- La alerta `cross-tenant-attempt.kql` es de severidad **Critical** (zero tolerance)

---

## ADR-005: OpenTelemetry como estándar de observabilidad

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

Toda observabilidad (trazas, métricas, logs) usa **OpenTelemetry** con exportadores configurables.
En local se usa Aspire Dashboard. En producción, Azure Monitor via OTLP exporter.

### Fundamento

- Vendor-neutral: permite cambiar de Azure Monitor a Datadog/Grafana sin cambiar el código
- `EvidataMetrics` centraliza todos los contadores de negocio
- Las métricas custom son requeridas para los SLOs de cumplimiento

### Consecuencias

- `EvidataMetrics` se registra como Singleton en `AddServiceDefaults()`
- Los instrumentos siguen la convención `evidata_{dominio}_{accion}_{unidad}`
- Los dashboards de Azure Monitor usan las queries KQL en `infra/azure/alerts/` y `infra/azure/dashboards/`

---

## ADR-006: Branching y merge strategy

**Fecha:** 2026  
**Estado:** Vigente

### Decisión

- Ramas de trabajo: `dev/YYYY/MM/DD/nombre-funcionalidad`
- Cada rama apunta a la anterior (cadena, no a main directamente)
- `main` está protegida: requiere PR aprobado por otro usuario + CI verde
- Merge squash para mantener historial limpio

### Consecuencias

- El orden de merge debe respetar la cadena de dependencias entre PRs
- CI (dotnet build + dotnet test) debe pasar antes de cualquier merge
- No se hace push directo a `main` bajo ninguna circunstancia
