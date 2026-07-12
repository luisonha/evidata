# Auditoría admin users

## Eventos obligatorios

| Evento | Cuándo |
|---|---|
| `UserInvited` | usuario invitado |
| `UserInvitationResent` | invitación reenviada |
| `UserInvitationRevoked` | invitación revocada |
| `UserInvitationAccepted` | usuario activado por primer login |
| `UserUpdated` | datos administrativos actualizados |
| `UserRoleChanged` | rol cambiado |
| `UserSuspended` | usuario suspendido |
| `UserReactivated` | usuario reactivado |
| `UserDisabled` | usuario deshabilitado |
| `UserLoginSucceeded` | login exitoso |
| `UserLoginDenied` | login denegado |
| `UserLogout` | logout |

## Campos mínimos audit

```json
{
  "eventId": "evt_123",
  "eventType": "UserRoleChanged",
  "tenantId": "ten_123",
  "actorUserId": "usr_admin",
  "targetUserId": "usr_123",
  "occurredAt": "2026-07-12T12:00:00Z",
  "reason": "Cambio aprobado",
  "correlationId": "corr_123"
}
```

## Prohibido en auditoría

- access tokens,
- refresh tokens,
- cookie values,
- secrets,
- claims completos sin necesidad,
- payload sensible no requerido.
