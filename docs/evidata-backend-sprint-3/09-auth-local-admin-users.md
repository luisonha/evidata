# Auth local y usuarios admin en local/test

## Decisión

Local/test debe permitir validar el ciclo completo sin Entra.

## Perfiles locales mínimos

| Perfil | Estado | Roles | Caso |
|---|---|---|---|
| `admin.local` | active | TenantOwner | admin total |
| `compliance.local` | active | ComplianceAdmin | operación compliance |
| `owner.local` | active | ProcessOwner | responsable |
| `reviewer.local` | active | LegalReviewer | revisión |
| `viewer.local` | active | Viewer | lectura |
| `invited.local` | invited | ProcessOwner | invitación pendiente |
| `suspended.local` | suspended | Viewer | usuario suspendido |
| `disabled.local` | disabled | Viewer | usuario deshabilitado |
| `denied.local` | active | none | sin permisos |
| `no-tenant.local` | active | TenantOwner | tenant unavailable |

## Endpoints local/test adicionales

| Método | Endpoint | Uso |
|---|---|---|
| POST | `/dev/admin/users/reset-seed` | reset usuarios locales |
| POST | `/dev/admin/users/create-scenario` | crear escenario admin |
| POST | `/dev/auth/login` | login local |
| POST | `/dev/auth/expire-session` | expirar sesión |

## Prohibición

Estos endpoints no existen en producción.
