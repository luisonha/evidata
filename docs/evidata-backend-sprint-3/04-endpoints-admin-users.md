# Endpoints administración de usuarios

## Regla de origen

La invitación de usuarios inicia desde la aplicación Evidata, en el módulo:

```text
Administración > Usuarios
```

El endpoint de invitación no es público.  
Requiere sesión Evidata, tenant activo, CSRF y permiso administrativo.

## Endpoints obligatorios

| Método | Endpoint | Actor | Permiso | CSRF | AuditEvent | Uso |
|---|---|---|---|---|---|---|
| GET | `/api/v1/admin/users` | Admin tenant | `Admin.ReadUsers` | No | opcional | listar usuarios |
| POST | `/api/v1/admin/users/invitations` | Admin tenant | `Admin.ManageUsers` | Sí | `UserInvited` | invitar usuario desde la app |
| GET | `/api/v1/admin/users/{userId}` | Admin tenant | `Admin.ReadUsers` | No | opcional | detalle usuario |
| PATCH | `/api/v1/admin/users/{userId}` | Admin tenant | `Admin.ManageUsers` | Sí | `UserUpdated` | editar datos administrativos permitidos |
| POST | `/api/v1/admin/users/{userId}/resend-invitation` | Admin tenant | `Admin.ManageUsers` | Sí | `UserInvitationResent` | reenviar invitación |
| POST | `/api/v1/admin/users/{userId}/revoke-invitation` | Admin tenant | `Admin.ManageUsers` | Sí | `UserInvitationRevoked` | revocar invitación |
| POST | `/api/v1/admin/users/{userId}/suspend` | Admin tenant | `Admin.ManageUsers` | Sí | `UserSuspended` | suspender usuario |
| POST | `/api/v1/admin/users/{userId}/reactivate` | Admin tenant | `Admin.ManageUsers` | Sí | `UserReactivated` | reactivar usuario |
| POST | `/api/v1/admin/users/{userId}/disable` | Admin tenant | `Admin.ManageUsers` | Sí | `UserDisabled` | deshabilitar usuario |
| PATCH | `/api/v1/admin/users/{userId}/role` | Admin tenant | `Admin.ChangeUserRole` | Sí | `UserRoleChanged` | cambiar rol principal |
| GET | `/api/v1/admin/roles` | Admin tenant | `Admin.ReadUsers` | No | none | listar roles |
| GET | `/api/v1/admin/permissions` | Admin tenant | `Admin.ReadUsers` | No | none | listar permisos |
| GET | `/api/v1/admin/audit` | Admin tenant | `Admin.ReadAudit` | No | optional | auditoría admin |

## Endpoints no permitidos

| Endpoint | Motivo |
|---|---|
| `POST /api/v1/users/signup` | No hay registro público |
| `POST /api/v1/users/register` | No hay registro autoasistido |
| `POST /api/v1/admin/users/system-invite` | La invitación inicia desde app/admin, no por backend automático |
| `DELETE /api/v1/admin/users/{id}` | No se elimina físicamente usuario |
| `PUT /api/v1/admin/users/{id}/permissions` | Los permisos se derivan de roles en MVP |
| `POST /api/v1/admin/users/{id}/impersonate` | Fuera de alcance por seguridad |

## Regla

Todo cambio administrativo de usuario pasa por `/api/v1/admin/*` y se origina desde una acción de usuario admin en la aplicación.
