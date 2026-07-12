# Decisión — modelo de registro de usuarios

## Decisión principal

Evidata no usa registro público abierto.

El alta de usuarios inicia **desde la aplicación Evidata**, ejecutada por un **admin autorizado del tenant**.

Backend no inicia invitaciones por sí mismo. Backend expone el endpoint seguro y valida que la acción venga de una sesión administrativa válida.

## Flujo correcto

```text
Admin del tenant abre Administración > Usuarios
  → completa formulario de invitación
  → frontend envía POST /api/v1/admin/users/invitations
  → backend valida sesión Evidata
  → backend valida tenant activo
  → backend valida permiso Admin.ManageUsers
  → backend valida CSRF
  → backend valida email/rol/área
  → backend crea usuario invited
  → backend crea invitación pending
  → backend registra auditoría UserInvited
  → sistema envía invitación o deja invitación pendiente según canal configurado
```

## Flujo de activación

```text
Usuario invitado recibe invitación
  → usuario inicia login con Microsoft Entra ID
  → BFF valida identidad Entra
  → BFF resuelve tenant + email + invitación
  → si coincide invitación, activa usuario
  → asocia entraOid
  → crea sesión Evidata
  → emite cookie segura
```

## Reglas

- No existe endpoint público de signup abierto.
- No existe auto-registro por dominio en MVP.
- Sólo un usuario autenticado con permiso `Admin.ManageUsers` puede invitar.
- El tenant debe estar `active`.
- El usuario invitado queda en estado `invited`.
- El usuario invitado no obtiene sesión hasta iniciar login válido con Entra o perfil local en test.
- El email del login Entra debe coincidir con la invitación.
- El `oid` de Entra se almacena al primer login exitoso.
- Un usuario suspendido o disabled no puede iniciar sesión.
- Un tenant suspended/unavailable bloquea sesión aunque el usuario sea válido.

## Estados de usuario

| Estado | Significado |
|---|---|
| `invited` | usuario invitado desde la aplicación, aún no activado |
| `active` | usuario activo |
| `suspended` | usuario suspendido temporalmente |
| `disabled` | usuario deshabilitado administrativamente |
| `invitation_revoked` | invitación revocada |
| `deleted_soft` | baja lógica, no login |

## Regla de identidad

Microsoft Entra autentica.  
Evidata autoriza.  
La aplicación inicia la invitación.  
Backend valida y registra la acción.
