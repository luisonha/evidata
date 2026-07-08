# 13 — Matriz RBAC y políticas de autorización

## 1. Propósito

Este documento define los roles, permisos y políticas contextuales para ATLAS Opción B. Su objetivo es evitar ambigüedad en la implementación backend y garantizar seguridad multi-tenant desde el MVP.

La autorización se implementa en el core modular. No se externaliza como microservicio en el MVP porque depende de contexto de negocio: tenant, rol, estado del tratamiento, owner, sensibilidad, revisión pendiente y tipo de acción.

## 2. Roles funcionales

| Rol | Descripción |
|---|---|
| PlatformAdmin | Administra plataforma completa. No debe operar datos funcionales del tenant salvo soporte autorizado. |
| TenantOwner | Dueño principal del tenant. Puede configurar tenant y aprobar acciones críticas según política. |
| TenantAdmin | Administra usuarios y configuración operativa limitada. |
| ComplianceAdmin | Administra inventario, evidencias, brechas y flujos de cumplimiento. |
| LegalReviewer | Revisa bases legales, tratamientos y observaciones jurídicas. |
| ProcessOwner | Responsable de levantar y mantener tratamientos de su área. |
| SecurityReviewer | Revisa medidas de seguridad, incidentes futuros y riesgos técnicos. |
| ExternalConsultant | Consultor externo con permisos explícitos, acotados y auditados. |
| ReadOnlyAuditor | Consulta información y auditoría permitida, sin modificar. |

## 3. Principios de autorización

1. Deny by default.
2. Todo acceso requiere tenant válido y activo.
3. El usuario debe pertenecer al tenant.
4. Los permisos globales no reemplazan políticas contextuales.
5. Las acciones sensibles requieren motivo y auditoría reforzada.
6. Los documentos/evidencias sensibles requieren permiso y registro de acceso.
7. Los consultores externos deben tener alcance explícito.
8. La aprobación de tratamientos no debe estar disponible para quien no tenga rol de revisión o autorización formal.
9. El acceso MCP con contexto tenant requiere permiso específico.
10. La exportación masiva siempre es sensible.

## 4. Permisos atómicos

| Permiso | Descripción |
|---|---|
| `tenant.manage` | Modificar configuración tenant. |
| `users.view` | Ver usuarios del tenant. |
| `users.invite` | Invitar usuarios. |
| `users.assign_roles` | Asignar roles. |
| `legal_sources.view` | Consultar fuentes legales. |
| `legal_sources.manage` | Administrar fuentes legales. |
| `documents.upload` | Subir documentos. |
| `documents.view` | Ver metadata documental. |
| `documents.download` | Descargar documentos. |
| `documents.delete` | Eliminar lógicamente documentos. |
| `evidence.create` | Crear evidencia. |
| `evidence.view` | Ver evidencia. |
| `evidence.link` | Asociar evidencia. |
| `evidence.export` | Exportar evidence pack. |
| `processing.view` | Ver tratamientos. |
| `processing.create` | Crear tratamiento. |
| `processing.edit` | Editar tratamiento. |
| `processing.submit_review` | Enviar tratamiento a revisión. |
| `processing.review` | Revisar tratamiento. |
| `processing.approve` | Aprobar tratamiento. |
| `processing.archive` | Archivar tratamiento. |
| `processing.export` | Exportar inventario/RAT. |
| `workflow.task_manage` | Gestionar tareas. |
| `gaps.view` | Ver brechas. |
| `gaps.create` | Crear brecha. |
| `gaps.assign` | Asignar brecha. |
| `gaps.close` | Cerrar brecha. |
| `gaps.accept_risk` | Aceptar riesgo. |
| `reports.generate` | Generar reportes. |
| `reports.download` | Descargar reportes. |
| `search.tenant` | Buscar contenido tenant. |
| `mcp.ask` | Preguntar al MCP sin contexto sensible. |
| `mcp.ask_with_tenant_context` | Usar contexto tenant en MCP. |
| `mcp.view_history` | Ver historial MCP. |
| `mcp.request_human_review` | Solicitar revisión humana. |
| `audit.view` | Ver auditoría. |
| `security.manage` | Configurar seguridad. |

## 5. Matriz base por rol

| Acción | PlatformAdmin | TenantOwner | TenantAdmin | ComplianceAdmin | LegalReviewer | ProcessOwner | SecurityReviewer | ExternalConsultant | ReadOnlyAuditor |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Configurar tenant | Soporte autorizado | Sí | Parcial | No | No | No | No | No | No |
| Invitar usuario | No por defecto | Sí | Sí | No | No | No | No | No | No |
| Asignar roles | No por defecto | Sí | Parcial | No | No | No | No | No | No |
| Ver fuentes legales | Sí | Sí | Sí | Sí | Sí | Sí | Sí | Sí | Sí |
| Administrar fuentes globales | Sí | No | No | No | No | No | No | No | No |
| Subir documentos | No | Sí | Sí | Sí | Sí | Sí | Sí | Condicional | No |
| Descargar documentos sensibles | Soporte autorizado | Sí | Condicional | Sí | Sí | Condicional | Sí | Condicional | Condicional |
| Crear evidencia | No | Sí | No | Sí | Sí | Sí | Sí | Condicional | No |
| Asociar evidencia | No | Sí | No | Sí | Sí | Sí | Sí | Condicional | No |
| Exportar evidence pack | No | Sí | No | Sí | Condicional | No | Condicional | No | Condicional |
| Crear tratamiento | No | Sí | No | Sí | Condicional | Sí | No | Condicional | No |
| Editar tratamiento Draft | No | Sí | No | Sí | Condicional | Si es owner | Condicional | Condicional | No |
| Enviar a revisión | No | Sí | No | Sí | Condicional | Si es owner | No | Condicional | No |
| Revisar tratamiento | No | Sí | No | Sí | Sí | No | Sí para seguridad | Condicional | No |
| Aprobar tratamiento | No | Sí | No | Sí | Sí si asignado | No | No salvo seguridad | No | No |
| Archivar tratamiento | No | Sí | No | Sí | Condicional | No | No | No | No |
| Crear brecha | No | Sí | No | Sí | Sí | Condicional | Sí | Condicional | No |
| Cerrar brecha | No | Sí | No | Sí | Sí si legal | Si asignado | Sí si seguridad | Condicional | No |
| Aceptar riesgo | No | Sí | No | Condicional | No | No | No | No | No |
| Generar reporte RAT | No | Sí | No | Sí | Condicional | No | Condicional | No | Condicional |
| Buscar contenido tenant | No | Sí | Condicional | Sí | Sí | Condicional | Sí | Condicional | Condicional |
| Usar MCP con contexto tenant | No | Sí | Condicional | Sí | Sí | Condicional | Sí | Condicional | No |
| Ver auditoría | Soporte autorizado | Sí | Condicional | Condicional | No | No | Condicional | No | Sí |

## 6. Políticas contextuales

### 6.1 Política tenant

Una acción se deniega si:

- el tenant está suspendido;
- el usuario no pertenece al tenant;
- el tenant del recurso no coincide con el tenant del contexto;
- el usuario intenta operar como PlatformAdmin sin modo soporte autorizado.

### 6.2 Política de tratamiento

| Estado | Edición | Revisión | Aprobación |
|---|---|---|---|
| Draft | ProcessOwner, ComplianceAdmin | No aplica | No aplica |
| InReview | Bloqueada salvo devolución | LegalReviewer, ComplianceAdmin, SecurityReviewer según scope | LegalReviewer/ComplianceAdmin/TenantOwner |
| ChangesRequested | ProcessOwner, ComplianceAdmin | No aplica hasta reenvío | No aplica |
| Approved | No se edita directamente | Consulta | No aplica |
| Active | Cambios generan nueva versión | Según flujo | Según flujo |
| Archived | Sólo lectura | No | No |

### 6.3 Política de evidencia

Se deniega acceso si:

- la evidencia pertenece a otro tenant;
- el usuario no puede ver la entidad asociada;
- la evidencia es Sensitive y el usuario no tiene permiso de evidencia sensible;
- el usuario es consultor externo sin scope explícito;
- se solicita descarga sin motivo requerido.

### 6.4 Política MCP

Se deniega uso de contexto tenant si:

- el usuario no tiene `mcp.ask_with_tenant_context`;
- el contexto solicitado contiene tratamiento/evidencia que no puede ver;
- el tratamiento tiene clasificación sensible y el usuario no está autorizado;
- la pregunta intenta extraer información masiva sin justificación;
- la consulta supera límites del tenant.

### 6.5 Política de exportación

Toda exportación masiva requiere:

- permiso específico;
- motivo;
- auditoría reforzada;
- job rastreable;
- clasificación del reporte;
- expiración del artefacto.

## 7. Acciones sensibles y controles

| Acción | Control adicional |
|---|---|
| Asignar rol TenantOwner o ComplianceAdmin | Motivo, auditoría reforzada, notificación al TenantOwner. |
| Aprobar tratamiento | Motivo, snapshot de versión, auditoría reforzada. |
| Descargar evidence pack | Motivo, registro de filtros, expiración de enlace. |
| Exportar RAT completo | Motivo, clasificación del reporte, auditoría. |
| Usar MCP con contexto sensible | Registro de contexto, fuentes, respuesta, riesgo. |
| Eliminar evidencia | Eliminación lógica, motivo, auditoría. |
| Aceptar riesgo de brecha | Rol elevado, motivo, evidencia o justificación. |

## 8. Reglas para implementación

1. Las políticas deben estar centralizadas en el módulo Security & Authorization.
2. Ningún endpoint debe implementar permisos manuales sin usar políticas.
3. Los permisos efectivos deben poder consultarse para que el frontend habilite/deshabilite acciones.
4. La UI no es control de seguridad; el backend debe validar todo.
5. Las denegaciones deben registrarse cuando sean sensibles o repetitivas.
6. Los cambios de roles deben invalidar o afectar sesiones según capacidad del proveedor de identidad.

## 9. Pendientes

1. Confirmar si existirá rol DPO formal dentro del producto o si se mantendrá como ComplianceAdmin/LegalReviewer.
2. Confirmar alcance del ExternalConsultant por área, por tratamiento o por módulo.
3. Confirmar si PlatformAdmin podrá soporte impersonado o sólo diagnóstico sin acceso a datos.
4. Confirmar política de expiración de enlaces de descarga.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
