# Evidata — Seed de Datos de Prueba

Este directorio contiene scripts y documentación para poblar el ambiente local con datos realistas de prueba para Ley 21.719.

## Uso rápido

```bash
# Desde la raíz del repo:
scripts/local/seed.sh
```

## Dataset creado

### Tenant

| Campo | Valor |
|---|---|
| ID | `00000000-0000-0000-0000-000000000001` |
| Slug | `empresa-demo` |
| Nombre | `Empresa Demo S.A.` |

### Usuarios

| Usuario | ID | Rol |
|---|---|---|
| `admin@localdev.evidata` | `00000000-0000-0000-0000-000000000010` | DPO |
| `user@localdev.evidata` | `00000000-0000-0000-0000-000000000011` | PrivacyAnalyst |

### Roles del Sistema

| Rol | ID | Permisos |
|---|---|---|
| DPO | `b0000001-0000-0000-0000-000000000001` | Todos (10 permisos) |
| PrivacyAnalyst | `b0000001-0000-0000-0000-000000000002` | Lectura + escritura (sin approve, sin admin) |
| Auditor | `b0000001-0000-0000-0000-000000000003` | Solo lectura |

### Permisos (Ley 21.719)

| ID | Nombre | Descripción |
|---|---|---|
| `a0000001-...01` | `documents:read` | Leer documentos del tenant |
| `a0000001-...02` | `documents:write` | Crear y editar documentos |
| `a0000001-...03` | `documents:delete` | Eliminar documentos |
| `a0000001-...04` | `rat:read` | Leer RATs |
| `a0000001-...05` | `rat:write` | Crear y editar RATs |
| `a0000001-...06` | `rat:approve` | Aprobar RATs (solo DPO) |
| `a0000001-...07` | `evidence:read` | Ver evidencias |
| `a0000001-...08` | `evidence:write` | Registrar evidencias |
| `a0000001-...09` | `reports:generate` | Generar reportes |
| `a0000001-...10` | `admin:tenant` | Administrar configuración |

### Actividades de Tratamiento (RAT)

| Nombre | Estado | Responsable |
|---|---|---|
| Gestión de Nómina | `Approved` | RRHH |
| Atención al Cliente | `Draft` | Comercial |
| Marketing Directo | `Draft` | Marketing |
| Videovigilancia | `UnderReview` | Seguridad |
| Portal de Proveedores | `Draft` | Adquisiciones |

## Seed incremental (idempotente)

El seed es idempotente: ejecutarlo múltiples veces no crea duplicados. Usa `ON CONFLICT DO NOTHING` para inserciones SQL y verifica existencia antes de crear via API.

## Resetear datos

```bash
scripts/local/reset.sh
```

⚠️ Esto es destructivo: elimina toda la base de datos y vuelve a aplicar migraciones + seed.
