# Contratos request/response — admin users

## `POST /api/v1/admin/users/invitations`

### Actor

Admin del tenant desde la aplicación Evidata.

### Requiere

- sesión Evidata válida,
- cookie de sesión,
- CSRF,
- permiso `Admin.ManageUsers`,
- tenant activo.

### Request

```json
{
  "email": "usuario@empresa.com",
  "displayName": "Nombre Usuario",
  "role": "ProcessOwner",
  "responsibleAreaId": "area_123",
  "message": "Texto opcional de invitación"
}
```

### Validaciones

| Campo | Regla |
|---|---|
| `email` | requerido, email válido, normalizado |
| `displayName` | requerido |
| `role` | requerido, debe existir y ser asignable por el actor |
| `responsibleAreaId` | opcional según rol, debe pertenecer al tenant |
| `message` | opcional, sin HTML activo |

### Response 201

```json
{
  "user": {
    "id": "usr_123",
    "email": "usuario@empresa.com",
    "displayName": "Nombre Usuario",
    "status": "invited",
    "roles": ["ProcessOwner"],
    "responsibleAreaId": "area_123",
    "createdAt": "2026-07-12T12:00:00Z"
  },
  "invitation": {
    "id": "inv_123",
    "status": "pending",
    "expiresAt": "2026-07-19T12:00:00Z"
  }
}
```

### Errores

| Caso | HTTP | Código |
|---|---:|---|
| sin sesión | 401 | `Unauthenticated` |
| sin permiso | 403 | `InsufficientPermissions` |
| CSRF inválido | 403 | `CsrfValidationFailed` |
| email duplicado | 409 | `UserAlreadyExists` |
| rol inválido | 422 | `InvalidRole` |
| área inválida | 422 | `InvalidResponsibleArea` |

## `GET /api/v1/admin/users`

### Query

```text
q
status
role
responsibleAreaId
page
pageSize
sort
```

### Response 200

```json
{
  "items": [
    {
      "id": "usr_123",
      "email": "usuario@empresa.com",
      "displayName": "Nombre Usuario",
      "status": "active",
      "roles": ["ProcessOwner"],
      "responsibleArea": {
        "id": "area_123",
        "name": "Operaciones"
      },
      "lastLoginAt": "2026-07-12T12:00:00Z"
    }
  ],
  "page": 1,
  "pageSize": 25,
  "total": 1
}
```

## `PATCH /api/v1/admin/users/{userId}/role`

### Request

```json
{
  "role": "ComplianceAdmin",
  "reason": "Cambio aprobado por administración del tenant"
}
```

### Response 200

```json
{
  "id": "usr_123",
  "roles": ["ComplianceAdmin"],
  "updatedAt": "2026-07-12T12:00:00Z"
}
```
