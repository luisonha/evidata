# Ciclo completo app → invitación → login → usuario activo

## 1. Admin inicia invitación desde la aplicación

Actor:

```text
Admin del tenant
```

Pantalla origen esperada:

```text
Administración > Usuarios
```

Acción:

```text
Invitar usuario
```

Endpoint:

```text
POST /api/v1/admin/users/invitations
```

Validaciones backend:

- sesión Evidata válida,
- cookie de sesión válida,
- CSRF válido,
- tenant activo,
- permiso `Admin.ManageUsers`,
- email válido,
- email no duplicado activo en el mismo tenant,
- rol permitido,
- área responsable válida si aplica,
- no se viola política de tenant.

Resultado:

```text
User.status = invited
Invitation.status = pending
AuditEvent = UserInvited
```

## 2. Usuario invitado inicia login

Endpoint:

```text
GET /auth/login
```

Resultado:

```text
redirect a Microsoft Entra ID
```

## 3. Callback Entra

Endpoint:

```text
GET /auth/callback
```

Backend valida:

- state,
- nonce,
- issuer,
- audience/client,
- tenant,
- usuario Entra,
- email/UPN,
- oid.

## 4. Resolución usuario Evidata

Backend busca en este orden:

```text
tenantId + entraOid
tenantId + normalizedEmail
tenantId + pending invitation
```

## 5. Activación por primer login válido

Si usuario está `invited` y coincide email/tenant:

```text
User.status = active
User.entraOid = oid
User.lastLoginAt = now
Invitation.status = accepted
AuditEvent = UserInvitationAccepted
```

## 6. Sesión Evidata

Backend crea sesión:

```text
Session.userId
Session.tenantId
Session.roles
Session.permissionsVersion
Session.expiresAt
```

Emite:

```text
__Host-evidata.sid
```

## 7. Frontend consulta sesión

```text
GET /api/session
GET /api/me
GET /api/permissions
```

## Casos bloqueados

| Caso | HTTP | Código |
|---|---:|---|
| admin sin sesión invita | 401 | `Unauthenticated` |
| admin sin permiso invita | 403 | `InsufficientPermissions` |
| CSRF inválido | 403 | `CsrfValidationFailed` |
| tenant admin suspended | 403 | `TenantUnavailable` |
| email duplicado | 409 | `UserAlreadyExists` |
| rol no permitido | 422 | `InvalidRole` |
| área responsable inválida | 422 | `InvalidResponsibleArea` |
| usuario no invitado intenta login | 403 | `UserNotProvisioned` |
| usuario suspended | 403 | `UserSuspended` |
| usuario disabled | 403 | `UserDisabled` |
| invitación revocada | 403 | `InvitationRevoked` |
| tenant mismatch | 403 | `TenantMismatch` |
| email mismatch | 403 | `InvitationEmailMismatch` |
