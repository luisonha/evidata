# Evidata Backend Login + User Administration v2.0-rc4.1

## Estado

Paquete backend/BFF específico para:

- login productivo con Microsoft Entra ID,
- auth local sin Entra,
- sesión Evidata,
- ciclo completo de registro/invitación/activación de usuarios,
- administración de usuarios,
- roles y permisos,
- auditoría de acciones administrativas.

Fecha: `2026-07-12`

## Dictamen inicial

El paquete backend/BFF auth rc3 anterior cubría auth/session base, pero **no cerraba el ciclo completo de usuarios**.

Faltaban endpoints explícitos para:

- invitar usuario,
- reenviar invitación,
- revocar invitación,
- activar usuario en primer login,
- listar usuarios,
- leer detalle,
- cambiar rol,
- suspender,
- reactivar,
- deshabilitar,
- listar roles,
- listar permisos,
- auditar cambios de usuario.

Esta versión corrige eso sin incluir frontend, MUI ni pantallas.


## Ajuste rc4.1

Se deja explícito que la invitación de usuarios **inicia desde la aplicación**, por un admin del tenant, en Administración > Usuarios.

Backend sólo expone el endpoint seguro, valida sesión/tenant/permiso/CSRF y registra la acción.
