# Estados y transiciones de usuario

## Estados

| Estado | Login permitido | Visible admin | Descripción |
|---|---|---|---|
| `invited` | No hasta aceptar/iniciar SSO válido | Sí | usuario invitado |
| `active` | Sí | Sí | usuario activo |
| `suspended` | No | Sí | suspensión temporal |
| `disabled` | No | Sí | deshabilitado administrativo |
| `invitation_revoked` | No | Sí | invitación revocada |
| `deleted_soft` | No | Sólo auditoría/admin avanzado | baja lógica |

## Transiciones permitidas

| Desde | Hacia | Acción | Endpoint |
|---|---|---|---|
| none | invited | invitar | `POST /admin/users/invitations` |
| invited | active | primer login Entra válido | `/auth/callback` |
| invited | invitation_revoked | revocar invitación | `POST /admin/users/{id}/revoke-invitation` |
| invited | invited | reenviar invitación | `POST /admin/users/{id}/resend-invitation` |
| active | suspended | suspender | `POST /admin/users/{id}/suspend` |
| suspended | active | reactivar | `POST /admin/users/{id}/reactivate` |
| active | disabled | deshabilitar | `POST /admin/users/{id}/disable` |
| suspended | disabled | deshabilitar | `POST /admin/users/{id}/disable` |

## Transiciones prohibidas

| Desde | Hacia | Motivo |
|---|---|---|
| disabled | active | usar endpoint explícito futuro de restauración si se define |
| invitation_revoked | active | debe crear nueva invitación |
| deleted_soft | active | fuera de alcance |
| active | invited | no se revierte a invitado |

## Regla

Toda transición administrativa requiere auditoría.
