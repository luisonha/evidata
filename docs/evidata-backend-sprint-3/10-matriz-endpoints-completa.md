# Matriz completa de endpoints login + usuarios

## Regla de ciclo

La invitación se origina desde la aplicación por un admin del tenant.  
El backend valida y ejecuta la operación.  
El usuario invitado se activa recién en el primer login válido.

| Área | Método | Endpoint | Actor/origen | Existe en rc4.1 | Obligatorio |
|---|---|---|---|---|---|
| Auth | GET | `/auth/login` | usuario invitado/activo | Sí | Sí |
| Auth | GET | `/auth/callback` | Microsoft Entra → BFF | Sí | Sí |
| Auth | POST | `/auth/logout` | usuario autenticado | Sí | Sí |
| Session | GET | `/api/session` | frontend | Sí | Sí |
| Session | GET | `/api/me` | frontend | Sí | Sí |
| Session | GET | `/api/permissions` | frontend | Sí | Sí |
| Session | GET | `/api/csrf` | frontend | Sí | Sí |
| Users | GET | `/api/v1/admin/users` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/invitations` | admin tenant desde app | Sí | Sí |
| Users | GET | `/api/v1/admin/users/{userId}` | admin tenant desde app | Sí | Sí |
| Users | PATCH | `/api/v1/admin/users/{userId}` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/{userId}/resend-invitation` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/{userId}/revoke-invitation` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/{userId}/suspend` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/{userId}/reactivate` | admin tenant desde app | Sí | Sí |
| Users | POST | `/api/v1/admin/users/{userId}/disable` | admin tenant desde app | Sí | Sí |
| Users | PATCH | `/api/v1/admin/users/{userId}/role` | admin tenant desde app | Sí | Sí |
| Roles | GET | `/api/v1/admin/roles` | admin tenant desde app | Sí | Sí |
| Permissions | GET | `/api/v1/admin/permissions` | admin tenant desde app | Sí | Sí |
| Audit | GET | `/api/v1/admin/audit` | admin tenant desde app | Sí | Sí |
| Local | POST | `/dev/auth/login` | local/test | Sí local/test | Sí local/test |
| Local | POST | `/dev/auth/logout` | local/test | Sí local/test | Sí local/test |
| Local | GET | `/dev/auth/users` | local/test | Sí local/test | Sí local/test |
| Local | POST | `/dev/admin/users/reset-seed` | local/test | Sí local/test | Recomendado |
| Local | POST | `/dev/admin/users/create-scenario` | local/test | Sí local/test | Recomendado |

## Resultado

El ciclo app admin → invitación → login Entra → activación → sesión queda cerrado documentalmente.
