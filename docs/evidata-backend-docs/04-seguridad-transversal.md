# 04 — Seguridad transversal

## 1. Objetivo

Definir los controles de seguridad comunes para todo el backend de Evidata, incluyendo identidad, autorización, tenant isolation, auditoría, protección de documentos, acciones sensibles, controles Azure y seguridad para MCP.

## 2. Modelo de seguridad

Evidata usa una combinación de:

| Capa | Decisión |
|---|---|
| Autenticación | `LocalDev`/`TestAuth` para desarrollo y pruebas; Entra External ID obligatorio en producción. |
| Autorización | Dentro del core, con RBAC y políticas contextuales. |
| Tenant isolation | Obligatorio en todas las entidades, queries, eventos, blobs y contexto MCP. |
| Secretos | Variables/secretos locales en desarrollo; Azure Key Vault en ambientes Azure. |
| Auditoría | Core Audit + logs estructurados locales; Application Insights en Azure. |
| Jobs de seguridad | Azure Functions para checks y alertas. |

## 3. Identidad

El sistema no debe implementar autenticación propia productiva en el MVP. La autenticación real de usuarios finales en ambientes Azure será gestionada por Entra External ID.

Para desarrollo local y pruebas automatizadas, Evidata sí tendrá modos controlados de identidad simulada:

| Modo | Uso permitido | Descripción |
|---|---|---|
| `LocalDev` | Sólo entorno local | Permite desarrollar sin Entra usando usuarios seed o headers dev-only. |
| `TestAuth` | Sólo pruebas automatizadas | Permite construir escenarios reproducibles de autorización. |
| `Entra` | Dev-cloud, staging y producción | Valida tokens reales emitidos por Entra External ID. |

Responsabilidades del proveedor de identidad en producción:

- login;
- emisión de token;
- recuperación de contraseña;
- MFA si se habilita;
- federación futura;
- gestión primaria de credenciales.

Responsabilidades de Evidata en todos los ambientes:

- transformar la identidad externa o local a `CurrentUserContext`;
- asociar identidad con usuario interno;
- validar tenant;
- aplicar roles y permisos;
- evaluar políticas contextuales;
- bloquear usuario funcionalmente si corresponde;
- auditar accesos y cambios.

Reglas obligatorias:

1. `LocalDev` y `TestAuth` nunca pueden ejecutarse en staging ni producción.
2. Los headers dev-only sólo se aceptan en ambiente local.
3. Producción debe fallar al iniciar si `EVIDATA_AUTH_MODE` no es `Entra`.
4. El dominio y los módulos no deben depender directamente de claims de Entra.
5. Toda autorización debe evaluarse contra el modelo interno `CurrentUserContext`.

La especificación completa de este modo de trabajo está en `26-seguridad-desarrollo-local-sin-entra.md`.

## 4. Autorización

La autorización vive en el core porque depende del contexto funcional.

Debe evaluar:

- usuario;
- tenant;
- rol;
- permiso;
- entidad;
- estado de la entidad;
- asignación del usuario;
- sensibilidad del recurso;
- acción solicitada.

Ejemplo de acciones que requieren autorización contextual:

- aprobar tratamiento;
- editar tratamiento aprobado;
- descargar evidence pack;
- cerrar brecha crítica;
- consultar MCP con contexto tenant;
- acceder a evidencia sensible.

## 5. Roles base

| Rol | Propósito |
|---|---|
| PlatformAdmin | Administración global de plataforma. |
| TenantOwner | Dueño funcional del tenant. |
| TenantAdmin | Administración operativa del tenant. |
| ComplianceAdmin | Gestión de cumplimiento. |
| LegalReviewer | Revisión legal de tratamientos y brechas. |
| ProcessOwner | Levantamiento y mantenimiento de tratamientos. |
| SecurityReviewer | Revisión de medidas de seguridad. |
| ExternalConsultant | Apoyo externo controlado. |
| ReadOnlyAuditor | Consulta y exportación según permisos. |

## 6. Permisos base

Permisos mínimos:

- `tenant.manage`
- `users.invite`
- `users.assign_roles`
- `legal_sources.view`
- `legal_sources.manage`
- `documents.upload`
- `documents.view`
- `documents.download`
- `documents.delete`
- `evidence.create`
- `evidence.view`
- `evidence.link`
- `evidence.export`
- `processing.create`
- `processing.edit`
- `processing.submit_review`
- `processing.review`
- `processing.approve`
- `processing.archive`
- `processing.export`
- `gaps.create`
- `gaps.assign`
- `gaps.close`
- `reports.generate`
- `reports.download`
- `mcp.ask`
- `mcp.ask_with_tenant_context`
- `mcp.view_history`
- `mcp.request_human_review`
- `audit.view`
- `security.manage`

## 7. Tenant isolation

Reglas obligatorias:

1. Toda entidad funcional tiene `tenant_id`.
2. Toda query funcional filtra por `tenant_id`.
3. Todo comando valida tenant.
4. Todo evento incluye tenant cuando aplica.
5. Todo blob tiene tenant en metadata y/o ruta lógica.
6. Toda interacción MCP limita contexto por tenant.
7. Todo intento cross-tenant genera evento de seguridad.
8. Nunca se debe confiar sólo en filtros frontend.

## 8. Acciones sensibles

Acciones que requieren auditoría reforzada:

- asignar rol administrador;
- invitar usuario externo;
- cambiar owner de tratamiento;
- aprobar tratamiento;
- editar tratamiento aprobado;
- eliminar o reemplazar evidencia;
- descargar evidence pack;
- exportar RAT completo;
- consultar MCP con contexto sensible;
- acceder a documentos clasificados sensibles;
- cerrar brecha crítica;
- aceptar riesgo;
- modificar configuración de retención;
- cambiar configuración de tenant.

Para estas acciones se debe registrar:

- usuario;
- tenant;
- rol;
- permiso;
- acción;
- entidad;
- fecha;
- IP;
- user-agent;
- correlation id;
- resultado;
- motivo cuando corresponda;
- before/after cuando aplique.

## 9. Auditoría

La auditoría debe ser append-only o funcionalmente inmutable.

Debe capturar:

- cambios de roles;
- accesos denegados;
- descargas;
- exportaciones;
- cambios de estado;
- aprobaciones;
- devoluciones;
- cierres de brecha;
- consultas MCP;
- eventos de seguridad;
- cambios de configuración.

No debe registrar datos personales completos en logs técnicos salvo que sea estrictamente necesario. Debe preferir identificadores, referencias y metadata.

## 10. Seguridad documental

Reglas:

- blobs privados;
- descarga mediada por backend;
- URLs temporales con expiración;
- auditoría de descarga;
- classification label por documento/evidencia;
- bloqueo cross-tenant;
- eliminación lógica preferente;
- versionado;
- hash de archivo;
- razón obligatoria para eliminar/reemplazar evidencia.

## 11. Seguridad MCP

El MCP debe tener controles específicos:

- no usar contexto tenant sin permiso;
- aplicar minimización de contexto;
- no enviar documentos completos al modelo si no es necesario;
- registrar fuentes utilizadas;
- registrar respuesta generada;
- clasificar riesgo de la consulta;
- abstenerse cuando no haya fuente suficiente;
- no certificar cumplimiento;
- derivar a revisión humana en alto riesgo;
- no entrenar modelos con datos tenant sin decisión explícita;
- proteger consultas con datos sensibles;
- alertar consultas de alto riesgo.

## 12. Seguridad de Azure Functions

Cada Function debe:

- usar Managed Identity cuando sea posible;
- tener permisos mínimos;
- leer sólo las colas necesarias;
- acceder sólo al storage necesario;
- no registrar payloads sensibles completos;
- ser idempotente;
- manejar poison messages;
- registrar correlation id;
- publicar estado de job;
- fallar de manera controlada.

## 13. Key Vault

Debe usarse para:

- secretos;
- claves API;
- cadenas de conexión cuando no se use identidad;
- certificados;
- credenciales de proveedores externos.

Reglas:

- acceso por Managed Identity;
- separación por ambiente;
- rotación planificada;
- mínimo privilegio;
- auditoría de acceso a secretos.

## 14. Alertas mínimas

Configurar alertas para:

- errores 5xx elevados;
- intentos cross-tenant;
- fallas repetidas de autorización;
- descarga masiva de evidencia;
- poison messages;
- jobs atascados;
- fallas MCP;
- consultas MCP de alto riesgo;
- errores de publicación Outbox;
- fallas de procesamiento documental;
- reportes fallidos.

## 15. Decisiones pendientes de seguridad

- Confirmar proveedor de identidad final.
- Definir si se requerirá MFA obligatorio desde MVP.
- Definir política de retención de auditoría.
- Definir si habrá clasificación formal de documentos desde MVP.
- Definir si se requiere IP allowlist para clientes enterprise.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
