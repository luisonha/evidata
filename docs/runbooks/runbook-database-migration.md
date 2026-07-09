# Runbook: Migraciones de Base de Datos

**Versión:** 1.0 | **Actualizado:** 2026-07-08

---

## Contexto

Evidata usa EF Core Code-First. Cada módulo tiene su propio `DbContext` y sus propias migraciones.
Las migraciones se aplican **antes** del despliegue de la nueva versión de la API.

---

## Módulos y sus DbContexts

| Módulo | DbContext | Schema PostgreSQL |
|---|---|---|
| TenantManagement | `TenantManagementDbContext` | `tenant` |
| Identity | `IdentityDbContext` | `identity` |
| Security | `SecurityDbContext` | `security` |
| Audit | `AuditDbContext` | `audit` |
| LegalKnowledge | `LegalKnowledgeDbContext` | `legal` |
| Documents | `DocumentDbContext` | `documents` |
| Evidence | `EvidenceDbContext` | `evidence` |
| ProcessingInventory | `ProcessingInventoryDbContext` | `processing` |
| GapManagement | `GapManagementDbContext` | `gap` |
| Workflow | `WorkflowDbContext` | `workflow` |
| Reporting | `ReportingDbContext` | `reporting` |
| Search | `SearchDbContext` | `search` |
| Mcp | `McpDbContext` | `mcp` |

---

## Aplicar todas las migraciones

```bash
export PATH="$HOME/.dotnet:$PATH"
export DB_CONN="Host=...;Database=evidata;Username=...;Password=..."

cd src/Evidata.Api

contexts=(
  "TenantManagementDbContext"
  "IdentityDbContext"
  "SecurityDbContext"
  "AuditDbContext"
  "LegalKnowledgeDbContext"
  "DocumentDbContext"
  "EvidenceDbContext"
  "ProcessingInventoryDbContext"
  "GapManagementDbContext"
  "WorkflowDbContext"
  "ReportingDbContext"
  "SearchDbContext"
  "McpDbContext"
)

for ctx in "${contexts[@]}"; do
  echo "Migrando $ctx..."
  dotnet ef database update --context "$ctx" --connection "$DB_CONN"
  if [ $? -ne 0 ]; then
    echo "❌ FALLÓ migración de $ctx — DETENIENDO"
    exit 1
  fi
  echo "✅ $ctx OK"
done
echo "✅ Todas las migraciones aplicadas"
```

---

## Agregar una nueva migración

```bash
export PATH="$HOME/.dotnet:$PATH"

# Ejemplo para el módulo Mcp
cd src/Modules/Mcp
dotnet ef migrations add NombreDeLaMigracion \
  --context McpDbContext \
  --project Evidata.Modules.Mcp.csproj \
  --startup-project ../../Evidata.Api/Evidata.Api.csproj

# Verificar el script SQL antes de aplicar
dotnet ef migrations script --context McpDbContext \
  --idempotent \
  --startup-project ../../Evidata.Api/Evidata.Api.csproj \
  > migration-preview.sql
```

---

## Rollback de migración

EF Core no soporta rollback automático. Proceso manual:

```bash
# 1. Identificar la migración anterior
dotnet ef migrations list --context McpDbContext

# 2. Revertir a la migración anterior
dotnet ef database update NombreMigracionAnterior --context McpDbContext

# 3. Eliminar la migración del código
dotnet ef migrations remove --context McpDbContext
```

⚠️ El rollback de datos es responsabilidad del equipo — EF Core solo revierte el schema.

---

## Verificación post-migración

```sql
-- Ver migraciones aplicadas por schema
SELECT schema_name, table_name
FROM information_schema.tables
WHERE table_schema IN ('tenant','identity','security','audit','legal',
                        'documents','evidence','processing','gap',
                        'workflow','reporting','search','mcp')
ORDER BY schema_name, table_name;

-- Ver estado de migraciones EF Core (tabla __EFMigrationsHistory por schema)
SELECT * FROM mcp."__EFMigrationsHistory" ORDER BY migration_id;
```

---

## Troubleshooting

| Error | Causa probable | Solución |
|---|---|---|
| `relation does not exist` | Migración pendiente | Aplicar migraciones |
| `duplicate column` | Migración aplicada dos veces | Verificar `__EFMigrationsHistory` |
| `permission denied` | Usuario DB sin permisos DDL | Usar usuario con rol `DDL` para migraciones |
| `FATAL: remaining connection slots` | Pool agotado | Aumentar `max_connections` o reducir pool size |
