# Validación de brechas del backend rc3 anterior

## Resultado

El paquete `evidata-backend-bff-auth-adjustments-v2.0-rc3` no era suficiente para cubrir el ciclo completo de registro y administración de usuarios.

## Qué sí cubría

| Área | Estado |
|---|---|
| Login productivo Entra | Parcialmente cubierto |
| Callback Entra | Parcialmente cubierto |
| Sesión backend/BFF | Cubierto base |
| Cookie segura | Cubierto |
| CSRF | Cubierto |
| Auth local sin Entra | Cubierto base |
| `/api/session` | Cubierto |
| `/api/me` | Cubierto |
| `/api/permissions` | Cubierto |

## Qué faltaba

| Área | Estado rc3 | Decisión rc4 |
|---|---|---|
| Invitación de usuario | Faltante | Agregar endpoints de invitación |
| Registro interno de usuario | Faltante | Modelo de usuario Evidata |
| Activación por primer login | Faltante | Activación controlada desde invitación |
| Listado usuarios | Faltante | `GET /api/v1/admin/users` |
| Detalle usuario | Faltante | `GET /api/v1/admin/users/{userId}` |
| Cambio de rol | Nombrado pero sin contrato | Agregar endpoint y auditoría |
| Suspender usuario | Nombrado pero sin contrato | Agregar endpoint y motivo |
| Reactivar usuario | Faltante | Agregar endpoint |
| Revocar invitación | Faltante | Agregar endpoint |
| Reenviar invitación | Faltante | Agregar endpoint |
| Roles disponibles | Faltante | `GET /api/v1/admin/roles` |
| Permisos disponibles | Faltante | `GET /api/v1/admin/permissions` |
| Auditoría admin | Faltante | eventos administrativos obligatorios |

## Decisión

No se considera cerrado el backend login/admin hasta cubrir el ciclo completo descrito en este paquete.
