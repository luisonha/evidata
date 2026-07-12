# Decisiones sobre Ambigüedades y Contradicciones detectadas en Sprint 3

**Documento**: `13-decisiones-ambiguedades.md`  
**Sprint**: Backend Sprint 3  
**Fecha de aprobación**: 2026-07-12  
**Estado**: Aprobado por Product Owner  

---

## Introducción

Este documento formalizaa las 10 decisiones sobre ambigüedades, contradicciones y vacíos detectados en la documentación de Sprint 3 (autenticación productiva con Entra ID, ciclo de invitación/activación, administración de usuarios, roles/permisos, auditoría).

Cada decisión ha sido revisada y aprobada explícitamente por el stakeholder responsable. Sirven como referencia vinculante durante toda la implementación del sprint, resolviendo dudas sobre interpretación de requisitos y definiendo el comportamiento esperado en casos no contemplados directamente en los documentos 00-12.

---

## Decisión 1: `InvitationRevoked` con dos HTTP status distintos

### Contexto / Ambigüedad detectada
El código de error `InvitationRevoked` aparece en dos contextos diferentes:
- **Doc02 (Login)**: mapeado a HTTP **403** Forbidden
- **Doc07 (Acciones administrativas)**: mapeado a HTTP **409** Conflict

Ambos usan el mismo código de error semántico pero con distinto status HTTP, generando ambigüedad sobre si debería existir un único mapeo o si es intencional que varíe según contexto.

### Decisión tomada
**Se mantiene el mismo código de error `InvitationRevoked` en ambos contextos, mapeado a distinto HTTP status según el contexto operativo:**
- **403** (Forbidden) cuando el usuario intenta accionar sobre su propia invitación revocada (contexto de login/autenticación)
- **409** (Conflict) cuando una acción administrativa detecta que la invitación ya no es válida (contexto de gestión)

### Justificación
El código de error semántico transmite la raíz del problema (invitación revocada) al cliente de forma clara y consistente. El HTTP status varía porque refleja la naturaleza de la operación: acceso denegado a un recurso (403) vs. incompatibilidad de estado de transacción (409). Esto evita crear códigos de error redundantes y permite a clientes diferenciar el contexto por el HTTP status.

### Impacto en implementación
- **Mapeo de errores**: la capa de manejo de errores debe incluir lógica para mapear `InvitationRevoked` a 403 o 409 según la operación que lo generó (login vs. admin action).
- **Documentación de contratos**: cada endpoint debe especificar explícitamente qué status HTTP esperar para `InvitationRevoked` en su contexto.
- **Testing**: pruebas de integración deben validar ambos mappings.

---

## Decisión 2: Roles — Array multi-rol desde el inicio (NO MVP de un solo rol)

### Contexto / Ambigüedad detectada
La documentación mezcla indistintamente lenguaje singular y plural sobre roles:
- **Lenguaje singular**: "rol principal", `PATCH .../role` (singular), comando `Admin.ChangeUserRole` (singular)
- **Lenguaje plural**: campo JSON `"roles": [...]` (array), permitir múltiples roles

Esta ambigüedad sugería un MVP inicial de un solo rol que evolucione a multi-rol, pero esto es rechazado explícitamente por requisito del usuario.

### Decisión tomada
**Se implementa soporte real de múltiples roles simultáneos por usuario desde el arranque del sprint, sin restricción a MVP de un solo rol.**

La arquitectura de datos y contratos deben soportar arrays de roles desde el primer commit, no como refactoring futuro.

### Justificación
Implementar un solo rol en MVP y migrar a multi-rol genera deuda técnica significativa (cambios a modelos de datos, migraciones de BD, refactoring de lógica de negocio). Es más eficiente construir directamente multi-rol desde el inicio.

### Impacto en implementación

**(a) Modelo de datos:**
- La relación no es un único campo `RoleId` en `UserProfile`, sino una **relación many-to-many** `UserProfile ↔ Role` (tabla de join `UserProfileRole` con foreign keys).
- El modelo de identidad de ASP.NET Core debe alimentarse con el conjunto completo de roles del usuario.

**(b) Endpoint `PATCH .../role` (administración):**
- **URL**: `PATCH /admin/tenants/{tenantId}/users/{userId}/roles` (mejor ser explícito: "roles" plural)
- **Body actual (rechazado)**:  
  ```json
  {
    "roleId": "TenantOwner"
  }
  ```
- **Body nuevo (reemplazamiento completo de set de roles)**:  
  ```json
  {
    "roles": ["TenantMember", "InvitationModerator"],
    "reason": "Cambio de responsabilidades en Q3"
  }
  ```
- Semántica: reemplaza completamente el set de roles del usuario, no agrega ni quita incrementalmente.
- Si el body viene vacío (`"roles": []`), el usuario pierde todos los roles (validar si es permitido según restricciones de negocio).

**(c) Evaluación de restricciones de roles:**
Toda regla de negocio que dependa de "qué rol tiene el usuario" debe evaluarse como **pertenencia a un conjunto (array)**, no como igualdad de un único valor.

Ejemplos:
- **Restricción de negocio original**: "No dejar el tenant sin al menos un TenantOwner activo"  
  - **Implementación anterior (incorrecto)**: `if (user.RoleId == "TenantOwner" && user.Status == "Active") ...`
  - **Implementación nueva (correcto)**: `if (user.Roles.Contains("TenantOwner") && user.Status == "Active") ...`

- **Validación de cambio de roles**: antes de confirmar `PATCH .../roles` hacia un set que excluya `TenantOwner` para el último usuario activo en el tenant, debe fallar.

- **Suspend / Disable / Reactivate**: si estas operaciones afectan a un usuario que es `TenantOwner`, aplicar la misma validación de restricción (no permitir suspender/deshabilitar el último `TenantOwner` activo del tenant).

**(d) Modelos de datos C#:**
```csharp
public class UserProfile
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Email { get; set; }
    public UserStatus Status { get; set; }
    // ... otros campos ...
    
    // Relación many-to-many
    public ICollection<UserProfileRole> UserProfileRoles { get; set; }
}

public class UserProfileRole
{
    public Guid UserProfileId { get; set; }
    public string RoleName { get; set; }
    public UserProfile UserProfile { get; set; }
    public Role Role { get; set; }
}

public class Role
{
    public string Name { get; set; }  // pk
    public string Description { get; set; }
    // ...
}
```

---

## Decisión 3: `deleted_soft` fuera de alcance

### Contexto / Ambigüedad detectada
El estado `deleted_soft` aparece mencionado en documentación de UserStatus pero **ningún endpoint obligatorio o permitido lo produce** en las tablas de autorización de doc07 (Admin endpoints). No hay ni endpoint ni flujo de negocio que genere este estado dentro del scope definido para Sprint 3.

### Decisión tomada
**`deleted_soft` queda fuera de alcance de Sprint 3.**

Se reserva como estado para un sprint futuro. **No se implementa ningún endpoint que lo produzca durante este sprint.**

### Justificación
Incluir un estado no alcanzable genera confusión: aparece en enums de código pero nunca se transiciona a él, dejando dudas sobre si falta implementar un endpoint o si fue olvidado. Es más limpio y transparente excluirlo explícitamente del modelo de datos de Sprint 3.

### Impacto en implementación
- **Enum `UserStatus` en C#**: inicialmente contendrá solo: `Active`, `Pending`, `Suspended`, `Disabled`. **No incluir** `deleted_soft`.
- **Migraciones de BD**: no agregar lógica ni columnas relacionadas con soft-delete en Sprint 3.
- **Pruebas**: no escribir casos de test que intenten transicionar a `deleted_soft`.
- **Documentación futura**: cuando se implemente un "Sprint X - Gestión de borrado de datos", incluir `deleted_soft` con sus endpoints y reglas correspondientes.

---

## Decisión 4: Contratos faltantes

### Contexto / Ambigüedad detectada
Existen espacios en la especificación de contratos (request/response) para varios endpoints:

**Sin ejemplo de request/response:**
- `GET .../{userId}` (detalle de usuario individual)
- `PATCH .../{userId}` (actualización de datos de usuario — campos editables no especificados)
- `POST .../resend-invitation`
- `POST .../revoke-invitation`
- `POST .../suspend`, `POST .../reactivate`, `POST .../disable` (todos requieren `reason` según doc07 pero sin ejemplo explícito)
- `GET /roles` (listado de roles disponibles)
- `GET /permissions` (listado de permisos)
- `GET /audit` (sin filtros/paginación documentados)

En contraste, `GET /users` sí tiene filtros de búsqueda y paginación explícitos: `q`, `status`, `role`, `responsibleAreaId`, `page`, `pageSize`, `sort`.

### Decisión tomada
**Los contratos faltantes se definirán y documentarán iterativamente durante la implementación de cada fase correspondiente, siguiendo exactamente el patrón de los contratos ya documentados en docs 00-12.**

No se retardan hasta completar un documento "exhaustivo" antes de codificar. En su lugar:
1. Se crea el contrato durante la implementación de la feature.
2. Se agrega a la documentación de contrato de la fase correspondiente (o a un documento de contratos de fase si es necesario).
3. Se siguen las convenciones ya establecidas: nombres de campos en camelCase, sobres de respuesta (`{ data: ..., meta: ... }`), paginación (`page`, `pageSize`, `sort`, `total`).

### Justificación
Exhaustividad anticipada es enemiga de la agilidad. Los patrones están claros en los documentos existentes; no es necesario bloquear implementación esperando que todos los contratos estén documentados por anticipado. La documentación acompaña a la implementación, no la precede.

### Impacto en implementación
- **Responsabilidad de desarrollador**: al implementar un endpoint, documentar el contrato exacto (request body, response envelope, códigos de error) en el respectivo documento de fase o contrato.
- **Review de PR**: el code review debe incluir validar que el contrato está documentado según patrón.
- **Testing**: incluir test de contrato (body validations, response shape) desde el primer commit.

---

## Decisión 5: Invitación expirada sin código de error

### Contexto / Ambigüedad detectada
El campo `expiresAt` aparece en el ejemplo de respuesta de invitación (doc02), indicando que las invitaciones tienen vencimiento. Sin embargo, **no existe código de error definido para cuando un usuario intenta usar una invitación expirada** (p.ej. intentar activarla pasada la fecha de expiración).

La ausencia de este código de error genera incertidumbre sobre qué responder en tal caso (¿aplicar un `InvitationRevoked` genérico o un código específico?).

### Decisión tomada
**Se agrega nuevo código de error `InvitationExpired` con HTTP 403 Forbidden.**

Este código es específico para el caso donde la invitación existe, pero su `expiresAt` ha sido superado.

### Justificación
Diferencias semánticas en la raíz del error (invitación revocada vs. expirada) son útiles para clientes: permiten feedback específico al usuario ("Tu invitación expiró" vs. "Tu invitación fue cancelada") y facilita logging/auditoría.

### Impacto en implementación
- **Validación de activación de invitación**: en el endpoint de activar invitación (signup), después de recuperar la invitación de BD, verificar `if (invitation.ExpiresAt < DateTime.UtcNow) throw new InvitationExpiredException()`.
- **Mapeo de errores**: `InvitationExpiredException` → HTTP 403 con código `InvitationExpired`.
- **Auditoría**: registrar intentos de uso de invitación expirada.

---

## Decisión 6: Transiciones de estado inválidas sin código catch-all

### Contexto / Ambigüedad detectada
La documentación cubre casos específicos de transición inválida (p.ej. "no se puede reactivar un usuario `Disabled`"), pero **no existe un código de error genérico de respaldo** para transiciones no contempladas explícitamente.

Ejemplo: ¿qué sucede si alguien intenta transicionar un usuario de `Suspended` a `Pending`? ¿Hay endpoint para eso? ¿Debería fallar silenciosamente o con error explícito?

### Decisión tomada
**Se agrega código de error `InvalidStateTransition` con HTTP 409 Conflict como catch-all para cualquier transición de estado no contemplada explícitamente en la especificación de cada endpoint.**

Ejemplos que generarían `InvalidStateTransition`:
- Intentar pasar un usuario de `Suspended` a `Pending`.
- Intentar cambiar roles a un usuario `Disabled`.
- Cualquier otra combinación no explícitamente permitida en doc07.

### Justificación
Permite a la implementación ser defensiva: cualquier transición no prevista falla de forma clara y rastreable, en lugar de ser ignorada silenciosamente. Esto ayuda a detectar bugs en lógica de cliente o autorizaciones mal configuradas.

### Impacto en implementación
- **Validador de máquina de estados**: crear una función centralizada `ValidateStateTransition(UserStatus from, UserStatus to, Operation operation)` que retorne true/false o lance excepción.
- **Endpoints**: cada `POST /users/{id}/suspend`, `.../reactivate`, `.../disable` y `PATCH /users/{id}/roles` debe invocar este validador.
- **Testing**: cases de test para transiciones válidas e inválidas.

---

## Decisión 7: `responsibleAreaId` — referencia a entidad no definida en Sprint 3

### Contexto / Ambigüedad detectada
El campo `responsibleAreaId` aparece en:
- Filtro de búsqueda de `GET /users`: `...?responsibleAreaId=xyz`
- Posible campo de asignación en usuario (aunque no está explícito)

**No existe definición de qué entidad es `responsibleAreaId` ni a qué tabla referencia.** Probablemente corresponde a una entidad ya existente en otro módulo del código (GapManagement o ProcessingInventory), pero esto no está confirmado en la documentación de Sprint 3.

### Decisión tomada
**Se debe confirmar la entidad real en el código existente antes de implementar la validación "pertenece al tenant" en Sprint 3. Este punto queda pendiente de investigación técnica explícita, no se resuelve en el presente documento.**

**Crear tarea separada de Fase 0**: `f0-responsible-area-validation` con descripción "Investigar y confirmar la entidad `ResponsibleArea`, su relación con `UserProfile`, y validar que el `responsibleAreaId` pertenezca al tenant".

### Justificación
Hacer suposiciones sobre una entidad de otro módulo sin confirmación explícita genera riesgo de refactoring. Es mejor investigar primero que pisar implementación posterior.

### Impacto en implementación
- **Bloqueador**: no comenzar implementación del filtro `responsibleAreaId` en `GET /users` hasta confirmar.
- **Tarea de investigación**: debe completarse en Fase 0.
- **Resultado esperado**: documento técnico confirmando la entidad, relación con usuario/tenant, y validaciones aplicables.

---

## Decisión 8: Unicidad de email ambigua entre tenants

### Contexto / Ambigüedad detectada
La documentación menciona que un email "puede existir en otro tenant si la política multi-tenant lo permite", pero **no define explícitamente la política de unicidad de email.**

Ambigüedad: ¿Es email único globalmente? ¿Único por tenant? ¿Permite repeticiónes en ciertos casos?

### Decisión tomada
**La unicidad de email se define de forma explícita como compuesta de `(TenantId, Email)`.**

Semántica: un mismo email puede repetirse en tenants distintos sin restricción adicional. Dentro de un tenant, los emails son únicos.

### Justificación
Es el modelo estándar de SaaS multi-tenant. Permite que múltiples organizaciones inviten al mismo usuario sin conflicto, facilitando casos de uso como "usuarios en múltiples empresas".

### Impacto en implementación
- **Constraint de BD**: `UNIQUE (tenant_id, email)` en tabla `user_profile`, no `UNIQUE (email)`.
- **Validación de invitación**: al crear invitación, verificar `WHERE tenant_id = @tenantId AND email = @email` (no global).
- **Queries**: cualquier búsqueda de usuario por email debe incluir `tenantId` como filtro.

---

## Decisión 9: `/dev/auth/set-scenario` vs `/dev/admin/users/create-scenario`

### Contexto / Ambigüedad detectada
Dos endpoints de desarrollo tienen nombres similares y propósito aparentemente solapado:
- `POST /dev/auth/set-scenario`
- `POST /dev/admin/users/create-scenario`

No está claro si son duplicados, complementarios, o si uno debería eliminar al otro.

### Decisión tomada
**Son complementarios, no duplicados.**

- **`/dev/auth/set-scenario`**: simula **estados de sesión/autenticación** (p.ej. "denied", "no-tenant", "user-in-multiple-tenants-must-choose").
- **`/dev/admin/users/create-scenario`**: crea **datos concretos de usuario** para pruebas (p.ej. usuarios en distintos estados: `Active`, `Suspended`, `Disabled`, con distintos roles).

Ejemplo de uso conjunto en una prueba:
1. `POST /dev/admin/users/create-scenario` → crear usuario `John` con rol `TenantMember`.
2. `POST /dev/auth/set-scenario?scenario=denied` → simular que la autenticación Entra ID falla para la sesión actual.
3. Intentar login → debe fallar con `AuthenticationDenied`.

### Justificación
Separación clara de responsabilidades: uno trata sesión/autenticación, el otro datos. Evita crear god-endpoints que combinen demasiada lógica.

### Impacto en implementación
- **Documentación de endpoints dev**: ser explícito en el propósito de cada uno.
- **Testing**: pruebas de integración deben usar ambos correctamente según contexto.
- **Contrato de `/dev/auth/set-scenario`**: documento en doc de contratos dev con escenarios soportados.

---

## Decisión 10: Re-invitar un email con invitación pending ya existente

### Contexto / Ambigüedad detectada
**No existe regla explícita sobre qué sucede cuando se intenta crear una invitación nueva para un email que ya tiene una invitación `Pending` activa.**

Ambigüedad: ¿Debería permitirse (reemplazar la invitación anterior)? ¿Fallar explícitamente? ¿Retornar la invitación existente?

### Decisión tomada
**Debe fallar explícitamente.**

Regla de negocio: no se puede crear una invitación nueva para un email que ya tiene una invitación `Pending` activa. El cliente debe usar `POST /resend-invitation` para reenviar/actualizar la invitación existente.

**Código de error a definir en Fase 3** durante implementación del endpoint de invitaciones. Por ahora, se documenta el rechazo explícitamente:
- Opción 1: código específico `InvitationAlreadyPending` con HTTP 409.
- Opción 2: código genérico de conflicto existente con HTTP 409.

La decisión específica (qué código exacto) se toma durante implementación de Fase 3.

### Justificación
Evita duplicar invitaciones activas, que complica auditoría y causa confusión al usuario (dos invitaciones simultáneas al mismo email en el mismo tenant). Fuerza al cliente a usar `resend-invitation` para regenerar el link si es necesario.

### Impacto en implementación
- **Fase 2 (Invitaciones)**: al crear invitación, validar:
  ```sql
  SELECT * FROM invitations 
  WHERE tenant_id = @tenantId 
    AND email = @email 
    AND status = 'Pending'
  ```
  Si existe resultado, fallar con el código de error determinado.
- **Documentación de Fase 3**: especificar el código de error exacto en contrato de `POST /invitations`.
- **Testing**: caso de test "intento de crear segunda invitación para email con Pending".

---

## Resumen de Impactos Transversales

| Decisión | Área impactada | Esfuerzo |
|----------|---------------|---------|
| 1: InvitationRevoked (dos status) | Error mapping | Bajo |
| 2: Multi-rol desde inicio | Modelo de datos, lógica de negocio, contratos | **Alto** |
| 3: deleted_soft fuera de alcance | Enums, migraciones | Muy bajo |
| 4: Contratos faltantes | Documentación durante desarrollo | Medio |
| 5: InvitationExpired | Validación de invitación | Bajo |
| 6: InvalidStateTransition | Máquina de estados | Bajo-Medio |
| 7: responsibleAreaId (investigación pendiente) | Tarea de Fase 0 | TBD |
| 8: Unicidad (TenantId, Email) | Constraint de BD, queries | Bajo |
| 9: /dev/auth/set-scenario vs create-scenario | Documentación dev | Bajo |
| 10: Re-invitar email pending | Validación de invitación | Bajo |

**Decisión de mayor impacto**: **#2 (Multi-rol)** — requiere cambios arquitectónicos en modelo de datos, relaciones, lógica de validación y contratos de API.

---

## Referencias

- `docs/evidata-backend-sprint-3/00-*.md` hasta `12-*.md` (documentación existente)
- Fecha de aprobación de decisiones: 2026-07-12
- Aprobador: Product Owner / Stakeholder responsable

---

## Observaciones Finales

Estas 10 decisiones **resuelven ambigüedades** detectadas después de una revisión exhaustiva de la documentación de Sprint 3. Cada una ha sido validada explícitamente con el stakeholder responsable.

Durante la implementación del sprint, si surge una duda que no está cubierta explícitamente por estas decisiones, se debe:
1. Confirmar contra los documentos existentes (00-12).
2. Confirmar contra este documento (13).
3. Si persiste la ambigüedad, elevar al stakeholder responsable para nueva decisión.

**Este documento es vinculante** y debe ser referencia durante todo el sprint.
