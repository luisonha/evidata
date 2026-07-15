# Autorización admin users

## Permisos

| Permiso | Uso |
|---|---|
| `Admin.ReadUsers` | listar y leer usuarios |
| `Admin.ManageUsers` | invitar, editar, suspender, reactivar, revocar |
| `Admin.ChangeUserRole` | cambiar roles |
| `Admin.ReadAudit` | ver auditoría |
| `Admin.ManageTenantSettings` | configuración tenant |

## Reglas

- Un usuario no puede cambiar su propio rol a uno inferior si eso deja al tenant sin `TenantOwner`.
- Debe existir al menos un `TenantOwner` activo.
- Cambiar rol requiere motivo.
- Suspender/reactivar/deshabilitar requiere motivo.
- No se puede suspender al último `TenantOwner` activo.
- No se puede invitar un email duplicado activo en el mismo tenant.
- Un email puede existir en otro tenant si la política multi-tenant lo permite.
- Todo endpoint admin valida tenant isolation.

## Respuestas esperadas

| Caso | HTTP | Código |
|---|---:|---|
| sin sesión | 401 | `Unauthenticated` |
| sin permiso | 403 | `InsufficientPermissions` |
| último owner | 409 | `LastTenantOwnerBlocked` |
| usuario no encontrado | 404 | `UserNotFound` |
| email duplicado | 409 | `UserAlreadyExists` |
| invitación revocada | 409 | `InvitationRevoked` |
| motivo faltante | 422 | `ReasonRequired` |
