# 26 — Seguridad en desarrollo local sin Entra

## 1. Objetivo

Definir cómo desarrollar, probar y depurar la capa de seguridad de **Evidata** sin depender de Entra External ID en el trabajo diario, manteniendo compatibilidad directa con Entra para ambientes cloud y producción.

La decisión es separar claramente dos responsabilidades:

1. **Autenticación técnica**: quién emite y valida la identidad inicial del usuario.
2. **Autorización funcional**: qué puede hacer ese usuario dentro de Evidata según tenant, roles, permisos, políticas contextuales y estado del recurso.

En desarrollo local se simula la autenticación. La autorización funcional real se desarrolla desde el inicio dentro del core.

## 2. Decisión vigente

Evidata usará dos modos de identidad:

| Ambiente | Modo | Propósito |
|---|---|---|
| Local | `LocalDev` | Permitir desarrollo sin Entra, sin Azure y sin credenciales externas. |
| Test automatizado | `TestAuth` | Generar identidades controladas para pruebas unitarias/integración. |
| Dev-cloud | `Entra` | Validar integración real con Entra External ID. |
| Staging | `Entra` | Ensayar seguridad real previa a producción. |
| Producción | `Entra` | Identidad real obligatoria. |

Regla obligatoria:

> `LocalDev` y `TestAuth` nunca pueden estar habilitados en `Staging` ni en `Production`.

## 3. Principio arquitectónico

El dominio y los módulos de seguridad no deben depender directamente de Entra.

El core trabaja contra abstracciones internas:

```text
ICurrentUserContext
IIdentityResolver
ITenantResolver
IPermissionEvaluator
IAuthorizationPolicyEvaluator
ISecurityAuditWriter
```

Entra es sólo una implementación de autenticación externa para ambientes cloud. LocalDev es otra implementación para desarrollo. Las reglas reales de permisos, tenant isolation, auditoría y políticas contextuales son compartidas.

## 4. Modo LocalDev

`LocalDev` permite iniciar sesión localmente sin red externa.

Debe entregar una identidad simulada con claims equivalentes a los que la aplicación necesita:

```text
sub=user-local-compliance-001
email=compliance@evidata.local
name=Compliance Local
provider=LocalDev
tenant_id=tenant-demo-001
roles=ComplianceAdmin,ProcessOwner
```

El usuario puede seleccionarse mediante:

1. header HTTP sólo disponible en desarrollo;
2. endpoint local de selección de usuario;
3. archivo seed/configuración local;
4. UI dev-only en frontend cuando exista.

La opción recomendada para backend inicial es header controlado:

```text
X-Evidata-Dev-User: compliance@evidata.local
X-Evidata-Dev-Tenant: tenant-demo-001
```

Estos headers deben ser rechazados automáticamente fuera del ambiente `Local`.

## 5. Usuarios seed recomendados

El entorno local debe crear usuarios y tenants de prueba al ejecutar migraciones/seed:

| Usuario | Rol principal | Uso |
|---|---|---|
| `owner@evidata.local` | TenantOwner | Configuración de tenant y usuarios. |
| `admin@evidata.local` | TenantAdmin | Administración operativa. |
| `compliance@evidata.local` | ComplianceAdmin | Gestión de cumplimiento. |
| `legal@evidata.local` | LegalReviewer | Revisión legal. |
| `process@evidata.local` | ProcessOwner | Mantención de tratamientos/procesos. |
| `security@evidata.local` | SecurityReviewer | Revisión de medidas de seguridad. |
| `auditor@evidata.local` | ReadOnlyAuditor | Lectura y exportación controlada. |
| `consultant@evidata.local` | ExternalConsultant | Acceso limitado externo. |

También debe existir un segundo tenant local para pruebas cross-tenant:

```text
tenant-demo-001
tenant-demo-002
```

## 6. Qué se puede desarrollar sin Entra

Con `LocalDev` se puede desarrollar y probar:

- modelo de usuarios internos;
- roles;
- permisos;
- políticas contextuales;
- tenant isolation;
- bloqueo funcional de usuario;
- auditoría de accesos;
- auditoría de acciones sensibles;
- pruebas cross-tenant;
- autorización por estado de entidad;
- descarga controlada de evidencias;
- exportaciones;
- seguridad de Functions usando contexto de mensaje;
- pruebas de denegación y escalamiento de privilegios.

Esto cubre la parte más importante del desarrollo de seguridad funcional.

## 7. Qué no se valida con LocalDev

`LocalDev` no reemplaza las pruebas reales con Entra.

No valida:

- configuración de tenant Entra;
- flujos reales de login;
- MFA;
- recuperación de contraseña;
- federación;
- expiración y renovación real de tokens;
- configuración de redirect URIs;
- consentimientos de aplicación;
- políticas reales de Conditional Access;
- errores de configuración cloud.

Por eso debe existir un ambiente `dev-cloud` con Entra real antes de staging.

## 8. Contrato interno de usuario

El backend debe transformar cualquier identidad externa o local a un modelo interno único:

```text
CurrentUserContext
  userId
  externalSubject
  email
  displayName
  provider
  tenantId
  roles[]
  permissions[]
  isPlatformAdmin
  isTenantActive
  correlationId
```

Las capas de aplicación nunca deben leer directamente claims de Entra. Sólo deben usar `CurrentUserContext`.

## 9. Seguridad de configuración

Variables mínimas:

```text
EVIDATA_AUTH_MODE=LocalDev|TestAuth|Entra
EVIDATA_ENVIRONMENT=Local|DevCloud|Staging|Production
EVIDATA_ALLOW_DEV_AUTH_HEADERS=true|false
```

Validación de arranque obligatoria:

```text
Si EVIDATA_ENVIRONMENT es Staging o Production:
  EVIDATA_AUTH_MODE debe ser Entra
  EVIDATA_ALLOW_DEV_AUTH_HEADERS debe ser false
```

Si esta regla falla, la aplicación no debe iniciar.

## 10. Testing de seguridad

Las pruebas automatizadas deben cubrir como mínimo:

1. un usuario sin permiso recibe 403;
2. usuario de un tenant no accede a datos de otro tenant;
3. rol lector no modifica entidades;
4. acción sensible genera auditoría;
5. usuario bloqueado no ejecuta acciones;
6. headers `LocalDev` son ignorados fuera de ambiente local;
7. `Production` no inicia si `AUTH_MODE=LocalDev`;
8. Functions rechazan mensajes sin tenant o correlation id cuando corresponda;
9. exportaciones requieren permiso elevado;
10. evidencia sensible exige permiso explícito.

## 11. Relación con Entra

Entra External ID queda reservado para ambientes cloud:

- `dev-cloud` para validar integración temprana;
- `staging` para validación preproductiva;
- `production` como identidad real.

La integración con Entra debe implementarse como adapter, no como dependencia del dominio.

## 12. Decisión final

Evidata desarrollará la seguridad funcional sin depender de Entra en local. Para ello usará `LocalDev` y `TestAuth` como modos controlados de autenticación simulada. Entra External ID se usará sólo en ambientes Azure, con validación temprana en `dev-cloud` y obligatoriedad en producción.
