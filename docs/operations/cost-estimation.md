# Estimación de costos Azure — Evidata

**Versión:** 1.0  
**Fecha:** 2026-07-08  
**Referencia de precios:** Azure Calculator (región East US 2 / Brazil South según disponibilidad). Todos los valores en USD/mes aproximados. Los precios reales dependen de la región elegida, descuentos por compromiso y consumo real.

---

## 1. Resumen ejecutivo por ambiente

| Ambiente | Estimado mensual | Notas |
|---|---|---|
| **dev-cloud** | ~$85–$140 | Mínimos para validación, puede apagarse en horas |
| **staging** | ~$140–$220 | Configuración similar a prod, SKUs reducidos |
| **prod** | ~$280–$480 | Estimado para 1–5 tenants activos, crecimiento lineal |

> La variación principal es el número de tenants activos, volumen de documentos en Blob y uso de Azure OpenAI.

---

## 2. Estimación detallada por servicio

### 2.1 Azure App Service (API principal)

| Ambiente | Plan | vCPU | RAM | Costo estimado |
|---|---|---|---|---|
| dev-cloud | B1 | 1 | 1.75 GB | ~$13/mes |
| staging | B2 | 2 | 3.5 GB | ~$26/mes |
| prod | P1v3 | 2 | 8 GB | ~$90/mes |

Notas:
- dev-cloud puede pausarse cuando no se usa para reducir costos.
- prod debe evaluar autoescalado horizontal a partir de 10 tenants o tráfico sostenido >100 req/s.
- Alternativa Container Apps en prod tiene pricing por vCPU-segundo — conveniente con cargas intermitentes.

### 2.2 Azure Database for PostgreSQL Flexible Server

| Ambiente | SKU | Storage | Costo estimado |
|---|---|---|---|
| dev-cloud | Burstable B1ms | 32 GB | ~$14/mes |
| staging | Burstable B2s | 64 GB | ~$28/mes |
| prod | General Purpose D2ds_v4 | 128 GB | ~$115/mes |

Notas:
- Backups automáticos 7 días incluidos en prod.
- 13 schemas (uno por módulo) en la misma instancia — sin costo adicional.
- Extensión `pgvector` disponible sin costo extra en Flexible Server.
- Calcular ~$0.115/GB-mes de storage adicional cuando supere el incluido.

### 2.3 Azure Blob Storage

| Ambiente | Tier | Uso estimado | Costo estimado |
|---|---|---|---|
| dev-cloud | LRS Hot | < 5 GB | ~$0.50/mes |
| staging | LRS Hot | < 20 GB | ~$2/mes |
| prod | ZRS Hot | 50–200 GB | ~$5–$20/mes |

Desglose producción (primeros 6 meses):
- Storage: 100 GB × $0.018/GB = $1.80/mes
- Operaciones PUT/LIST: ~500K × $0.005/10K = $0.25/mes
- Operaciones GET: ~2M × $0.0004/10K = $0.08/mes
- **Total Blob prod:** ~$3–$5/mes inicial, escala con volumen de documentos

Containers:
- `evidata-documents` — documentos y evidencias por tenant
- `evidata-reports` — Excel/PDF generados por funciones (lifecycle policy: purgar >90 días)
- `evidata-exports` — exports temporales (lifecycle policy: purgar >7 días)

### 2.4 Azure Storage Queues

| Ambiente | Uso estimado | Costo estimado |
|---|---|---|
| dev-cloud | < 10K operaciones/mes | ~$0.01/mes (incluido en cuenta de storage) |
| staging | < 100K operaciones/mes | ~$0.01/mes |
| prod | ~1M–5M operaciones/mes | ~$0.04–$0.20/mes |

Notas:
- Costo negligible: $0.004 por 10K operaciones.
- Colas activas: `report-generation`, `gap-notifications`, `rat-notifications`, `document-processing`, `search-indexing`, `mcp-batch`, `*-poison` por cada cola.
- El costo real se mueve con el volumen de eventos del Outbox Worker.

### 2.5 Azure Functions

| Ambiente | Plan | Invocaciones estimadas | Costo estimado |
|---|---|---|---|
| dev-cloud | Consumption | < 100K/mes | Gratis (incluido en primer millón) |
| staging | Consumption | < 500K/mes | < $0.10/mes |
| prod | Consumption | 1M–10M/mes | ~$0.20–$2/mes |

Funciones activas (6 apps):
- `Evidata.Functions.Notifications`
- `Evidata.Functions.Reporting`
- `Evidata.Functions.DocumentProcessing`
- `Evidata.Functions.SearchIndexing`
- `Evidata.Functions.McpBatch`
- `Evidata.Functions.Maintenance`

Pricing Consumption:
- Primer 1M invocaciones/mes: **gratis**
- Excedente: $0.20 por millón de invocaciones
- GB-segundo: $0.000016 (primer 400,000 GB-s gratis)

> Para prod con carga real se puede evaluar Premium EP1 (~$180/mes) si la latencia de cold start en Consumption afecta SLA.

### 2.6 Application Insights + Log Analytics

| Ambiente | Ingesta estimada | Retención | Costo estimado |
|---|---|---|---|
| dev-cloud | < 1 GB/mes | 30 días | ~$0 (5 GB gratis) |
| staging | 2–5 GB/mes | 30 días | ~$0–$1/mes |
| prod | 5–20 GB/mes | 90 días | ~$5–$20/mes |

Pricing:
- Primeros 5 GB/mes por workspace: **gratis**
- Excedente: $2.30/GB ingestado
- Retención extendida: $0.10/GB/mes más allá de 30 días básicos

### 2.7 Azure Key Vault

| Ambiente | Operaciones estimadas | Costo estimado |
|---|---|---|
| dev-cloud | < 10K/mes | ~$0.03/mes |
| staging | < 50K/mes | ~$0.15/mes |
| prod | < 200K/mes | ~$0.60/mes |

Pricing: $0.03 por 10K operaciones sobre el primer millón gratuito.

### 2.8 Azure OpenAI / Foundry (módulo MCP)

Modelo de referencia: **GPT-4o** (estimado para consultas MCP de cumplimiento).

| Modelo | Input | Output |
|---|---|---|
| GPT-4o | $2.50 / 1M tokens | $10.00 / 1M tokens |
| GPT-4o-mini | $0.15 / 1M tokens | $0.60 / 1M tokens |

Estimado prod con 100 consultas/día (~3,000/mes):
- Promedio 1,000 tokens input + 500 tokens output por consulta
- GPT-4o: 3M tokens input × $2.50 + 1.5M output × $10 = ~$22.50/mes
- GPT-4o-mini: ~$0.90/mes

**Recomendación:** usar GPT-4o-mini para consultas de bajo riesgo, GPT-4o para revisiones HITL y análisis complejos. Implementar caché de respuestas para preguntas frecuentes.

---

## 3. Tags obligatorios para cost tracking

Todos los recursos Azure de Evidata deben incluir estos tags. Sin ellos los reportes de cost management no permiten desglosar correctamente.

```json
{
  "project":     "evidata",
  "environment": "dev | staging | prod",
  "module":      "api | db | storage | functions | observability | ai | security",
  "cost-center": "engineering | ops | tenant-{id}",
  "managed-by":  "terraform | bicep | manual"
}
```

### Convención por recurso

| Recurso | `module` | Notas |
|---|---|---|
| App Service | `api` | |
| PostgreSQL | `db` | |
| Storage Account (blobs + queues) | `storage` | |
| Function Apps | `functions` | Agregar tag `function-app: {nombre}` |
| App Insights / Log Analytics | `observability` | |
| Key Vault | `security` | |
| Azure OpenAI | `ai` | Agregar tag `model: gpt-4o` |

### Política de tagging en CI/CD

Agregar validación en pipeline Azure DevOps que rechace deployments sin los tags `project`, `environment` y `module`:

```yaml
# azure-pipelines.yml — gate de tags
- task: AzureCLI@2
  displayName: Validar tags obligatorios
  inputs:
    scriptType: bash
    scriptLocation: inlineScript
    inlineScript: |
      RESOURCE_ID=$(az resource show --name $RESOURCE_NAME --resource-type $RESOURCE_TYPE -g $RG --query id -o tsv)
      TAGS=$(az resource show --ids $RESOURCE_ID --query tags -o json)
      for TAG in project environment module; do
        if ! echo "$TAGS" | jq -e ".[\"$TAG\"]" > /dev/null; then
          echo "ERROR: tag obligatorio '$TAG' ausente en $RESOURCE_NAME"
          exit 1
        fi
      done
```

---

## 4. Alertas de budget recomendadas

### 4.1 Configuración por ambiente

Crear una alerta por ambiente en **Azure Cost Management → Budgets**:

| Ambiente | Budget mensual | Alertas |
|---|---|---|
| dev-cloud | $200 | 50% ($100) → aviso email · 80% ($160) → aviso + Slack · 100% ($200) → bloqueo opcional |
| staging | $300 | 70% ($210) → aviso email · 90% ($270) → aviso + Slack |
| prod | $600 | 60% ($360) → aviso · 80% ($480) → aviso urgente · 95% ($570) → escalada a CTO |

### 4.2 Alertas específicas por servicio

Además del budget global, configurar en **Azure Monitor → Metric Alerts**:

```
# PostgreSQL — conexiones activas
Métrica: active_connections
Umbral: > 80 conexiones
Acción: email + crear issue GitHub

# Blob Storage — egress anómalo
Métrica: Egress (bytes)
Umbral: > 10 GB en 1 hora
Acción: email urgente (posible exfiltración o bug)

# Functions — tasa de errores
Métrica: FunctionExecutionCount con status=Failed
Umbral: > 5% en ventana 5 min
Acción: email + PagerDuty (prod)

# App Insights — tiempo de respuesta
Métrica: requests/duration p95
Umbral: > 2 segundos sostenido 5 min
Acción: email

# OpenAI — tokens por hora
Métrica: TokenTransaction (via Log Analytics query)
Umbral: > 100K tokens/hora
Acción: email + revisar caché
```

### 4.3 Query de Log Analytics para monitoreo de tokens OpenAI

```kusto
AzureDiagnostics
| where ResourceType == "ACCOUNTS" and ResourceProvider == "MICROSOFT.COGNITIVESERVICES"
| where OperationName == "ChatCompletions_Create"
| summarize 
    TotalInputTokens  = sum(todouble(prompt_tokens_s)),
    TotalOutputTokens = sum(todouble(completion_tokens_s)),
    Requests = count()
    by bin(TimeGenerated, 1h), CallerIPAddress
| extend EstimatedCostUSD = (TotalInputTokens / 1000000 * 2.50) + (TotalOutputTokens / 1000000 * 10.0)
| order by TimeGenerated desc
```

---

## 5. Lifecycle policies para Blob Storage

Configurar en el Storage Account de prod para reducir costos de reportes temporales:

```json
{
  "rules": [
    {
      "name": "purge-old-reports",
      "type": "Lifecycle",
      "definition": {
        "filters": { "blobTypes": ["blockBlob"], "prefixMatch": ["*/reports/"] },
        "actions": {
          "baseBlob": {
            "tierToCool": { "daysAfterModificationGreaterThan": 30 },
            "delete":     { "daysAfterModificationGreaterThan": 90 }
          }
        }
      }
    },
    {
      "name": "purge-exports",
      "type": "Lifecycle",
      "definition": {
        "filters": { "blobTypes": ["blockBlob"], "prefixMatch": ["*/exports/"] },
        "actions": {
          "baseBlob": {
            "delete": { "daysAfterModificationGreaterThan": 7 }
          }
        }
      }
    }
  ]
}
```

---

## 6. Checklist antes de ir a producción

- [ ] Tags obligatorios configurados en todos los recursos (IaC)
- [ ] Budget alerts configuradas en Azure Cost Management para los 3 ambientes
- [ ] Lifecycle policy aplicada al Storage Account de prod
- [ ] Log Analytics workspace creado y conectado a App Insights y Functions
- [ ] Query de tokens OpenAI guardada como saved query en Log Analytics
- [ ] Revisión mensual de costos agendada como tarea recurrente
- [ ] Dashboard de costos en Azure Portal compartido con el equipo
- [ ] Alert actions apuntan a emails válidos + canal de Slack/Teams
