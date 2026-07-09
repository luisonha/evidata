# Guía de Ambiente Local — Evidata Backend

> **Versión:** 1.0  
> **Última actualización:** 2026-07-08  
> **Stack:** .NET 10 · PostgreSQL 16 · Azurite · Mailpit · .NET Aspire

---

## Índice

1. [Pre-requisitos](#1-pre-requisitos)
2. [Primer Setup](#2-primer-setup)
3. [Levantar el Stack](#3-levantar-el-stack)
4. [Ejecutar Migraciones](#4-ejecutar-migraciones)
5. [Poblar Datos de Prueba (Seed)](#5-poblar-datos-de-prueba-seed)
6. [Verificar con Smoke Tests](#6-verificar-con-smoke-tests)
7. [Endpoints disponibles](#7-endpoints-disponibles)
8. [Autenticación en modo local](#8-autenticación-en-modo-local)
9. [Usuarios y datos de prueba](#9-usuarios-y-datos-de-prueba)
10. [Solución de problemas](#10-solución-de-problemas)

---

## 1. Pre-requisitos

| Herramienta | Versión mínima | Verificar |
|---|---|---|
| .NET SDK | 10.0 | `dotnet --version` |
| Docker Desktop | 4.x | `docker --version` |
| Git | 2.x | `git --version` |
| `curl` | cualquiera | `curl --version` |
| `jq` (opcional) | cualquiera | `jq --version` |

### Instalación rápida (macOS)

```bash
# .NET 10
brew install --cask dotnet

# Docker Desktop
brew install --cask docker

# jq (para parsear respuestas JSON en scripts)
brew install jq
```

> **Nota:** Docker Desktop debe estar corriendo antes de iniciar el stack. Los contenedores de PostgreSQL, Azurite y Mailpit se inician automáticamente via .NET Aspire.

---

## 2. Primer Setup

Ejecutar una sola vez al clonar el repositorio:

```bash
cd /path/to/evidata
scripts/local/setup.sh
```

Este script:
- Verifica los pre-requisitos
- Instala el workload de Aspire (`dotnet workload install aspire`)
- Configura los User Secrets necesarios para desarrollo local
- Restaura los paquetes NuGet

### Configuración manual de User Secrets (alternativa)

Si prefieres configurar manualmente:

```bash
export PATH="$HOME/.dotnet:$PATH"

# API
cd src/Evidata.Api
dotnet user-secrets set "ConnectionStrings:evidata-db" "Host=localhost;Port=5432;Database=evidata_dev;Username=evidata;Password=evidata_local_pw"
dotnet user-secrets set "ConnectionStrings:evidata-storage" "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OGLjX+N6+6j4f3DjFj3fj3f3Dj3f3Dj3==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1"
dotnet user-secrets set "Email:Host" "localhost"
dotnet user-secrets set "Email:Port" "1025"
```

---

## 3. Levantar el Stack

```bash
scripts/local/start.sh
```

O manualmente:

```bash
export PATH="$HOME/.dotnet:$PATH"
cd src/Evidata.AppHost
dotnet run
```

### URLs una vez levantado

| Servicio | URL | Descripción |
|---|---|---|
| **Aspire Dashboard** | http://localhost:18888 | Observabilidad: logs, trazas, métricas |
| **API REST** | http://localhost:5000 | Backend principal |
| **API HTTPS** | https://localhost:5001 | Backend principal (TLS) |
| **Swagger/OpenAPI** | http://localhost:5000/openapi/v1.json | Schema JSON |
| **Health Check** | http://localhost:5000/health | Estado de salud |
| **Mailpit** | http://localhost:8025 | Bandeja de correos de prueba |
| **PostgreSQL** | localhost:5432 | DB: `evidata_dev`, User: `evidata` |
| **Azurite Blobs** | http://localhost:10000 | Azure Blob Storage emulado |
| **Azurite Queues** | http://localhost:10001 | Azure Queue Storage emulado |

> El Aspire Dashboard muestra en tiempo real los logs estructurados, trazas distribuidas y métricas de cada servicio. Es la primera herramienta para diagnosticar problemas.

---

## 4. Ejecutar Migraciones

**Con el stack levantado** (PostgreSQL debe estar corriendo):

```bash
scripts/local/migrate.sh
```

Esto aplica las migraciones de todos los DbContext en el orden correcto:

1. `TenantManagement` → base multi-tenant
2. `Identity` → perfiles de usuario
3. `Security` → RBAC / roles / permisos
4. `Audit` → trazabilidad
5. `LegalKnowledge` → catálogo Ley 21.719
6. `ProcessingInventory` → RAT (Registros de Actividad de Tratamiento)
7. `Evidence` → evidencias de cumplimiento
8. `GapManagement` → brechas de cumplimiento
9. `Workflow` → flujos de aprobación
10. `Documents` → gestión documental
11. `Reporting` → reportes
12. `Mcp` → revisión asistida por IA (HITL)
13. `Search` → índice de búsqueda

---

## 5. Poblar Datos de Prueba (Seed)

```bash
scripts/local/seed.sh
```

### Qué se crea

El seed crea un dataset completo para probar todos los flujos de la plataforma:

**Organización demo:**
- **Tenant:** `empresa-demo` — "Empresa Demo S.A."
- **Admin:** `admin@localdev.evidata` (rol: DPO, RBAC: todos los permisos)
- **Usuario regular:** `user@localdev.evidata` (rol: Privacy Analyst)

**Datos legales (Ley 21.719):**
- 10 artículos de la ley con requisitos asociados
- 5 bases de licitud (consentimiento, contrato, interés legítimo, etc.)

**Actividades de Tratamiento (RAT):**
- 3 RATs en estado `Draft`
- 1 RAT en estado `Approved`
- 1 RAT en estado `UnderReview`

**Evidencias y cumplimiento:**
- 4 evidencias de cumplimiento
- 3 gaps identificados (1 crítico, 1 alto, 1 medio)

**Documentos:**
- 3 documentos de muestra (política de privacidad, aviso, etc.)

**Consultas MCP:**
- 2 interacciones MCP (1 resuelta, 1 pendiente HITL)

Ver [`scripts/local/seed.sh`](../../scripts/local/seed.sh) para más detalles.

---

## 6. Verificar con Smoke Tests

```bash
scripts/local/smoke-test.sh
```

Ejecuta una batería de verificaciones contra la API local para confirmar que todos los módulos responden correctamente. Al finalizar muestra un reporte de `✅ PASS` / `❌ FAIL` por endpoint.

---

## 7. Endpoints disponibles

### Tenant Management
```
GET    /api/tenants/{id}
POST   /api/tenants
PATCH  /api/tenants/{id}/settings
PATCH  /api/tenants/{id}/status
```

### Identity / Perfiles
```
GET    /api/users/profile
POST   /api/users/link
```

### Security / RBAC
```
GET    /api/roles/users/{userId}?tenantId={tenantId}
POST   /api/roles/assign
DELETE /api/roles/remove
```

### Audit
```
GET    /api/audit?tenantId={tenantId}
```

### Health
```
GET    /health
GET    /health/detail
```

---

## 8. Autenticación en modo local

En desarrollo local **no hay JWT**. La autenticación se hace via headers HTTP especiales:

| Header | Valor | Descripción |
|---|---|---|
| `X-Evidata-Dev-User` | GUID del usuario | Identidad del usuario |
| `X-Evidata-Dev-Tenant` | GUID del tenant | Contexto del tenant |
| `X-Evidata-Dev-Email` | email | Email del usuario (opcional) |

> ⚠️ **Seguridad:** Estos headers solo funcionan en `Development`. En `Staging` y `Production` el `LocalDevEnvironmentGuard` rechaza cualquier request que los incluya con `403 Forbidden`.

### Ejemplo con curl

```bash
curl http://localhost:5000/health \
  -H "X-Evidata-Dev-User: 00000000-0000-0000-0000-000000000010" \
  -H "X-Evidata-Dev-Tenant: 00000000-0000-0000-0000-000000000001" \
  -H "X-Evidata-Dev-Email: admin@localdev.evidata"
```

---

## 9. Usuarios y datos de prueba

### Usuarios seed

| Usuario | GUID | Email | Rol |
|---|---|---|---|
| Admin | `00000000-0000-0000-0000-000000000010` | `admin@localdev.evidata` | DPO |
| Regular | `00000000-0000-0000-0000-000000000011` | `user@localdev.evidata` | Privacy Analyst |

### Tenant seed

| Campo | Valor |
|---|---|
| ID | `00000000-0000-0000-0000-000000000001` |
| Slug | `empresa-demo` |
| Nombre | `Empresa Demo S.A.` |

### Variables de entorno para scripts

```bash
export EVIDATA_API=http://localhost:5000
export EVIDATA_TENANT=00000000-0000-0000-0000-000000000001
export EVIDATA_ADMIN=00000000-0000-0000-0000-000000000010
export EVIDATA_USER=00000000-0000-0000-0000-000000000011
```

---

## 10. Solución de problemas

### Error de certificado HTTPS en el browser (NET::ERR_CERT_INVALID)

El certificado de desarrollo de .NET no está en el keychain del sistema. Ejecutar:

```bash
dotnet dev-certs https --trust
```

Pedirá tu contraseña para instalarlo en el keychain. Después de eso, recargar la página. Este paso también lo hace automáticamente `setup.sh`.

### Error: "Connection refused" al levantar la API

El PostgreSQL todavía no terminó de iniciar. Esperar 10-15 segundos y reintentar.

```bash
# Verificar que los contenedores estén corriendo
docker ps
```

### Error: "Migration already applied"

Las migraciones son idempotentes. Si ves este error, ya fue aplicada. No es un problema.

### Error: "evidata-db connection string not found"

Los User Secrets no están configurados. Ejecutar:
```bash
scripts/local/setup.sh
```

### El health check responde `Degraded`

Alguna dependencia (DB, Storage) no está disponible. Revisar el Aspire Dashboard en http://localhost:18888 para ver qué servicio falló.

### Los correos no llegan

Mailpit captura todos los emails enviados en desarrollo. Revisar la bandeja en http://localhost:8025.

### Resetear todo y empezar desde cero

```bash
scripts/local/reset.sh
```

> ⚠️ Esto elimina todos los datos de la base de datos local y vuelve a hacer seed.
