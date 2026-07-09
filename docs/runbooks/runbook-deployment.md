# Runbook: Despliegue a Azure

**Versión:** 1.0 | **Actualizado:** 2026-07-08

---

## Prerrequisitos

- Azure CLI: `az login`
- GitHub Actions con secretos configurados: `AZURE_CREDENTIALS`, `AZURE_SUBSCRIPTION_ID`
- Connection string PostgreSQL en Azure Key Vault: `evidata-db`
- Storage Account con colas: `notification-delivery`, `report-generation`, `search-indexing`, `mcp-batch`

---

## Pipeline CI/CD (Azure DevOps)

El despliegue se ejecuta automáticamente cuando un PR es **aprobado y mergeado a `main`**.

```
PR aprobado por revisor
  → CI: dotnet build + dotnet test
  → Si verde: merge a main
  → CD: deploy a staging (automático)
  → Aprobación manual para prod
  → CD: deploy a prod
```

**IMPORTANTE:** Nunca hacer push directo a `main`. Todo cambio requiere PR con aprobación.

---

## Variables de entorno requeridas

### API (`Evidata.Api`)

| Variable | Descripción |
|---|---|
| `ConnectionStrings__evidata-db` | Connection string PostgreSQL |
| `ConnectionStrings__blobs` | Azure Blob Storage connection |
| `ConnectionStrings__queues` | Azure Queue Storage connection |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | OTLP endpoint (si se usa Aspire en cloud) |

### Azure Functions (todas)

| Variable | Descripción |
|---|---|
| `AzureWebJobsStorage` | Azure Storage connection |
| `ConnectionStrings__evidata-db` | PostgreSQL |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Application Insights |

---

## Pasos manuales de despliegue (emergencia)

Solo si el pipeline CI/CD falla. Requiere aprobación de dos personas.

### 1. Build

```bash
export PATH="$HOME/.dotnet:$PATH"
dotnet publish src/Evidata.Api/ -c Release -o ./publish/api
dotnet publish functions/Evidata.Functions.Notifications/ -c Release -o ./publish/fn-notifications
dotnet publish functions/Evidata.Functions.Reporting/ -c Release -o ./publish/fn-reporting
dotnet publish functions/Evidata.Functions.SearchIndexing/ -c Release -o ./publish/fn-search
dotnet publish functions/Evidata.Functions.McpBatch/ -c Release -o ./publish/fn-mcp-batch
dotnet publish functions/Evidata.Functions.DocumentProcessing/ -c Release -o ./publish/fn-docproc
```

### 2. Migraciones

```bash
# Siempre aplicar migraciones ANTES de desplegar la nueva versión de la API
cd src/Evidata.Api
dotnet ef database update --context TenantManagementDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context IdentityDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context AuditDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context DocumentDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context ProcessingInventoryDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context GapManagementDbContext --connection "$DB_CONNECTION"
dotnet ef database update --context McpDbContext --connection "$DB_CONNECTION"
# ... continuar con todos los módulos
```

### 3. Deploy API

```bash
az webapp deploy --resource-group evidata-rg \
  --name evidata-api \
  --src-path ./publish/api \
  --type zip
```

### 4. Deploy Functions

```bash
for fn in notifications reporting search mcp-batch docproc; do
  az functionapp deployment source config-zip \
    --resource-group evidata-rg \
    --name "evidata-fn-${fn}" \
    --src "./publish/fn-${fn}.zip"
done
```

---

## Verificación post-despliegue

```bash
# Health check API
curl -s https://api.evidata.cl/health | jq .

# Health check alive
curl -s https://api.evidata.cl/alive

# Verificar en Application Insights que no hay spike de errores (primeros 5 min)
```

### Señales de despliegue exitoso

- `/health` responde `200 OK` con todos los checks `Healthy`
- Métricas `evidata_mcp_queries_total` aparecen en App Insights dentro de 10 min
- Colas de Azure Storage vacías (no hay backlog inesperado)

### Rollback

```bash
# Restaurar slot anterior en App Service
az webapp deployment slot swap \
  --resource-group evidata-rg \
  --name evidata-api \
  --slot staging \
  --target-slot production
```

---

## Contactos de escalación

| Rol | Responsabilidad |
|---|---|
| DevOps Lead | Pipeline CI/CD, infraestructura Azure |
| Backend Lead | Migraciones, módulos de dominio |
| Compliance Officer | Incidentes con datos personales |
