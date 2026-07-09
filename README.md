# Evidata — Backend

Plataforma SaaS multi-tenant para cumplimiento de la **Ley 21.719** de Chile (datos personales).
Construida sobre .NET 10, Azure Functions, PostgreSQL y Azure.

---

## Estructura del proyecto

```
evidata/
├── src/
│   ├── Evidata.Api/                    # API REST principal (ASP.NET Core)
│   ├── Evidata.AppHost/                # .NET Aspire orchestrator (desarrollo local)
│   ├── Evidata.ServiceDefaults/        # OTel, health checks, métricas compartidas
│   ├── Evidata.Worker.Outbox/          # Worker del patrón Outbox
│   └── Modules/                        # Módulos de dominio
│       ├── TenantManagement/           # Multi-tenancy
│       ├── Identity/                   # Autenticación y usuarios
│       ├── Security/                   # Políticas y autorización
│       ├── Audit/                      # Auditoría transversal
│       ├── LegalKnowledge/             # Base de conocimiento legal (Ley 21.719)
│       ├── Documents/                  # Gestión de documentos y evidencias
│       ├── Evidence/                   # Módulo de evidencias
│       ├── ProcessingInventory/        # RAT (Registro de Actividades de Tratamiento)
│       ├── GapManagement/              # Gestión de brechas de cumplimiento
│       ├── Workflow/                   # Motor de workflows
│       ├── Reporting/                  # Exportación de reportes
│       ├── Search/                     # Búsqueda full-text
│       └── Mcp/                        # Asistente contextual MCP + HITL
├── functions/                          # Azure Functions
│   ├── Evidata.Functions.DocumentProcessing/
│   ├── Evidata.Functions.Notifications/
│   ├── Evidata.Functions.Reporting/
│   ├── Evidata.Functions.SearchIndexing/
│   ├── Evidata.Functions.McpBatch/
│   └── Evidata.Functions.Maintenance/
├── tests/
│   └── Evidata.Tests.Unit/             # Tests unitarios
├── infra/
│   ├── azure/
│   │   ├── alerts/                     # KQL alert rules (Azure Monitor)
│   │   └── dashboards/                 # Azure Monitor Workbooks JSON
│   └── local/                          # docker-compose desarrollo local
└── docs/
    ├── evidata-backend-docs/           # Especificaciones de producto y arquitectura
    ├── runbooks/                       # Runbooks operacionales
    └── architecture/                   # Decisiones de arquitectura (ADRs)
```

---

## Desarrollo local

### Prerrequisitos

- .NET 10 SDK
- Docker Desktop
- Azure Functions Core Tools v4
- `dotnet-aspire` workload: `dotnet workload install aspire`

### Arrancar el stack local

```bash
# Iniciar con .NET Aspire (orquesta todos los servicios)
cd src/Evidata.AppHost
dotnet run

# Acceder al Aspire Dashboard
open http://localhost:15888
```

O con docker-compose (solo infraestructura):

```bash
cd infra/local
docker-compose up -d
```

### Ejecutar tests

```bash
dotnet test tests/Evidata.Tests.Unit/
```

### Migraciones de base de datos

```bash
# Aplicar todas las migraciones (desde la API)
cd src/Evidata.Api
dotnet ef database update --context TenantManagementDbContext
dotnet ef database update --context IdentityDbContext
# ... ver docs/runbooks/runbook-database-migration.md
```

---

## Despliegue

Ver [`docs/runbooks/runbook-deployment.md`](docs/runbooks/runbook-deployment.md).

## Runbooks operacionales

| Runbook | Descripción |
|---|---|
| [Deployment](docs/runbooks/runbook-deployment.md) | Guía de despliegue a Azure |
| [Incident MCP](docs/runbooks/runbook-incident-mcp.md) | Respuesta a incidentes del asistente MCP |
| [HITL Compliance](docs/runbooks/runbook-hitl-compliance.md) | Proceso de revisión humana HITL |
| [Database Migration](docs/runbooks/runbook-database-migration.md) | Aplicación de migraciones EF Core |

## Decisiones de arquitectura

Ver [`docs/architecture/decisions.md`](docs/architecture/decisions.md).

---

## Licencia y contexto legal

Evidata implementa controles requeridos por la Ley 21.719 (Chile). El sistema no constituye asesoría legal.
