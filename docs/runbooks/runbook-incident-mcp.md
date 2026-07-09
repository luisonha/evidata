# Runbook: Incidente MCP

**Versión:** 1.0 | **Actualizado:** 2026-07-08

---

## Clasificación de incidentes

| Tipo | Síntoma | Acción |
|---|---|---|
| Alta tasa de errores | Alert `mcp-high-error-rate` disparada (>10%) | Ver [Sección 1](#1-alta-tasa-de-errores-mcp) |
| Intento cross-tenant | Alert `cross-tenant-attempt` disparada | Ver [Sección 2](#2-intento-cross-tenant) |
| HITL estancado | Alert `hitl-stale-tasks` disparada | Ver [Sección 3](#3-hitl-estancado) |
| Queue `mcp-batch` acumulada | >100 mensajes pendientes | Ver [Sección 4](#4-queue-mcp-batch-acumulada) |

---

## 1. Alta tasa de errores MCP

**Alert:** `mcp-high-error-rate.kql` — tasa de errores >10% en 15 min

### Diagnóstico

```kql
// Errores recientes con detalle
exceptions
| where timestamp > ago(30m)
| where cloud_RoleName contains "Evidata"
| where type !contains "TaskCanceledException"
| project timestamp, type, outerMessage, cloud_RoleName, operation_Id
| order by timestamp desc
| take 50
```

### Causas comunes

| Causa | Indicador | Solución |
|---|---|---|
| DB connection pool agotado | `Npgsql.NpgsqlException` en logs | Reiniciar App Service, revisar pool size |
| Timeout de respuesta | `OperationCanceledException` | Verificar latencia PostgreSQL, escalar tier |
| Migración pendiente | `relation does not exist` en logs | Aplicar migraciones pendientes |

### Acción inmediata

```bash
# 1. Verificar estado de la API
curl https://api.evidata.cl/health

# 2. Si la API está caída — reiniciar slot
az webapp restart --resource-group evidata-rg --name evidata-api

# 3. Ver logs en tiempo real
az webapp log tail --resource-group evidata-rg --name evidata-api
```

---

## 2. Intento cross-tenant

**Alert:** `cross-tenant-attempt.kql` — **CRÍTICO — zero tolerance**

Este evento indica que un usuario intentó acceder a datos de otro tenant.
Puede ser un bug o un ataque activo.

### Acción inmediata (primeros 5 minutos)

1. **NO descartar** — tratar como incidente de seguridad activo
2. Identificar la operación con el `operation_Id` del log:

```kql
traces
| where timestamp > ago(1h)
| where message contains "cross_tenant"
| project timestamp, message, customDimensions, operation_Id
```

3. **Si el usuario es identificable** — suspender la sesión:

```bash
# Revocar tokens del usuario via API de identidad
curl -X POST https://api.evidata.cl/admin/identity/revoke-user \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -d '{"userId": "<user-id>"}'
```

4. Escalar a **Compliance Officer** inmediatamente
5. Documentar en el registro de incidentes de seguridad

### Análisis forense

```kql
// Operaciones del usuario en las últimas 24h
requests
| where timestamp > ago(24h)
| where customDimensions["userId"] == "<user-id>"
| project timestamp, url, resultCode, customDimensions["tenantId"]
| order by timestamp asc
```

---

## 3. HITL estancado

**Alert:** `hitl-stale-tasks.kql` — tareas sin asignar >2 horas

### Diagnóstico

```sql
-- Tareas abiertas sin asignar más de 2 horas (ejecutar en DB de prod)
SELECT id, interaction_id, tenant_id, created_at,
       EXTRACT(EPOCH FROM (NOW() - created_at))/60 AS minutes_open
FROM mcp.mcp_review_tasks
WHERE status = 'Open'
  AND assigned_to IS NULL
  AND created_at < NOW() - INTERVAL '2 hours'
ORDER BY created_at ASC;
```

### Acción

El procesador automático (`HitlEscalationTimerFunction`) corre cada hora y emite los logs.
Si hay acumulación persistente:

1. Notificar al equipo de cumplimiento para asignar revisores
2. Si el backlog es >50 tareas: disparar job manual via cola `mcp-batch`:

```json
{
  "messageType": "mcp.hitl.escalation.v1",
  "payload": "{\"tenantId\": null, \"staleThresholdMinutes\": 60, \"batchSize\": 200}",
  "correlationId": "manual-escalation",
  "sentAt": "2026-07-08T00:00:00Z",
  "schemaVersion": 1
}
```

---

## 4. Queue `mcp-batch` acumulada

### Diagnóstico

```bash
az storage queue show \
  --account-name evidatastg \
  --name mcp-batch \
  --query approximateMessageCount
```

### Causas comunes

| Causa | Solución |
|---|---|
| `McpBatchFunction` detenida | Reiniciar Function App |
| DB saturada | Verificar conexiones activas, escalar DTUs |
| Poison messages | Ver `mcp-batch-poison` queue, descartar o reencolar |

```bash
# Reiniciar McpBatch function
az functionapp restart \
  --resource-group evidata-rg \
  --name evidata-fn-mcp-batch
```

---

## Escalación

| Nivel | Condición | Tiempo máximo |
|---|---|---|
| L1 — DevOps | Error rate <30%, sin cross-tenant | 30 min |
| L2 — Backend Lead | Error rate >30% o DB issue | 15 min |
| L3 — Compliance + CTO | Cross-tenant detectado | **Inmediato** |
