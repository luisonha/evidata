# Criterios de aceptación backend login/admin

## Invitación desde la aplicación

- Un admin del tenant puede invitar desde Administración > Usuarios.
- La invitación usa `POST /api/v1/admin/users/invitations`.
- El endpoint requiere sesión válida.
- El endpoint requiere CSRF.
- El endpoint requiere `Admin.ManageUsers`.
- El tenant debe estar activo.
- El email debe ser válido y no duplicado activo en el tenant.
- El rol debe ser asignable por el actor.
- La invitación crea usuario `invited`.
- La acción audita `UserInvited`.

## Login productivo

- `/auth/login` inicia flujo Entra.
- `/auth/callback` valida identidad y tenant.
- Usuario no invitado no obtiene sesión.
- Usuario invited se activa en primer login válido.
- Usuario suspended/disabled no obtiene sesión.
- Tenant unavailable bloquea sesión.
- Sesión emite cookie `__Host-evidata.sid`.
- Frontend no recibe access token ni refresh token productivo.

## Administración usuarios

- Admin puede invitar usuario desde la app.
- Admin puede listar usuarios.
- Admin puede ver detalle.
- Admin puede reenviar invitación.
- Admin puede revocar invitación.
- Admin puede suspender usuario con motivo.
- Admin puede reactivar usuario con motivo.
- Admin puede deshabilitar usuario con motivo.
- Admin puede cambiar rol con motivo.
- No se puede eliminar físicamente usuario.
- No se puede dejar tenant sin `TenantOwner`.

## Seguridad

- Mutaciones admin requieren CSRF.
- Todo endpoint admin valida tenant.
- Todo endpoint admin valida permisos.
- Acciones críticas se auditan.
- No se loguean tokens, cookies ni secrets.
- `/dev/*` no existe en producción.

## Local/test

- Ciclo completo puede probarse sin Entra.
- Perfiles locales cubren active, invited, suspended, disabled, denied y tenant unavailable.
