# Contrato de integración app → backend — invitación de usuarios

## Pantalla origen

```text
Administración > Usuarios
```

## Acción frontend

```text
Invitar usuario
```

## Endpoint backend

```text
POST /api/v1/admin/users/invitations
```

## Requisitos de request frontend

- Enviar cookies con credenciales.
- Enviar `X-CSRF-Token`.
- No enviar tokens Entra.
- No enviar tenantId editable por usuario si tenant se resuelve desde sesión.
- Enviar email, displayName, role y responsibleAreaId si aplica.

## Respuesta esperada para UI

La UI debe poder mostrar:

- usuario invitado,
- estado `invited`,
- rol asignado,
- expiración de invitación,
- mensaje de éxito,
- errores de validación por campo.

## Errores que UI debe manejar

| HTTP | Código | UI |
|---:|---|---|
| 401 | `Unauthenticated` | sesión expirada |
| 403 | `InsufficientPermissions` | acción bloqueada |
| 403 | `CsrfValidationFailed` | recargar/reintentar seguro |
| 409 | `UserAlreadyExists` | email ya existe |
| 422 | `InvalidRole` | error campo rol |
| 422 | `InvalidResponsibleArea` | error campo área |
