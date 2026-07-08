# 12 — Contratos API backend

## 1. Propósito

Este documento define los contratos API funcionales para el backend de ATLAS Opción B. No contiene implementación ni código. Cada endpoint debe indicar propósito, permisos, entrada, salida, errores, eventos y auditoría esperada.

Los contratos son preliminares pero implementables. Deben ser refinados antes de iniciar cada módulo.

## 2. Convenciones API

| Regla | Definición |
|---|---|
| Base path | `/api/v1` |
| Autenticación | Token emitido por Entra External ID o proveedor confirmado. |
| Tenant | Se resuelve desde contexto de usuario y/o encabezado controlado. No se acepta tenant arbitrario sin autorización. |
| Fechas | UTC en formato ISO 8601. |
| Errores | Estructura estándar con código funcional, mensaje, correlation ID y detalles controlados. |
| Paginación | Obligatoria en listados. |
| Auditoría | Acciones sensibles registran AuditLog. |
| Idempotencia | Operaciones asíncronas y creación crítica deben aceptar clave idempotente si aplica. |

## 3. Errores estándar

| Código | Uso |
|---|---|
| `validation_error` | Datos incompletos o inválidos. |
| `unauthorized` | No autenticado. |
| `forbidden` | Autenticado sin permiso. |
| `tenant_not_found` | Tenant inexistente o inaccesible. |
| `tenant_suspended` | Tenant suspendido. |
| `not_found` | Recurso inexistente o no visible por permiso. |
| `conflict` | Conflicto de estado o concurrencia. |
| `invalid_state_transition` | Transición no permitida. |
| `sensitive_action_reason_required` | Acción sensible sin motivo. |
| `async_job_failed` | Job asíncrono fallido. |
| `rate_limited` | Límite de uso excedido. |

## 4. Tenant Management API

### 4.1 Crear tenant

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/platform/tenants` |
| Permiso | `platform.tenants.create` |
| Propósito | Crear tenant desde administración de plataforma. |
| Entrada | Nombre, país, zona horaria, plan, estado inicial, industria opcional. |
| Salida | Tenant creado, estado, configuración base. |
| Eventos | `TenantCreated` |
| Auditoría | Sí, acción crítica de plataforma. |
| Errores | validation_error, forbidden, conflict. |

### 4.2 Obtener tenant actual

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/tenant` |
| Permiso | usuario autenticado del tenant |
| Propósito | Obtener configuración visible del tenant actual. |
| Salida | Identidad tenant, estado, módulos habilitados, límites funcionales. |
| Auditoría | No obligatoria, salvo diagnóstico. |

### 4.3 Actualizar configuración tenant

| Ítem | Definición |
|---|---|
| Método | PATCH |
| Ruta | `/api/v1/tenant/settings` |
| Permiso | `tenant.manage` |
| Entrada | Configuraciones permitidas por tenant. |
| Salida | Configuración actualizada. |
| Eventos | `TenantConfigurationChanged` |
| Auditoría | Sí. |

## 5. Identity Bridge y usuarios

### 5.1 Invitar usuario

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/users/invitations` |
| Permiso | `users.invite` |
| Entrada | Email, nombre, roles propuestos, mensaje opcional. |
| Salida | Invitación creada. |
| Eventos | `UserInvited`, `NotificationRequested` |
| Auditoría | Sí. |
| Reglas | No puede asignar roles superiores al propio nivel administrativo. |

### 5.2 Listar usuarios del tenant

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/users` |
| Permiso | `users.view` |
| Parámetros | estado, rol, búsqueda, paginación. |
| Salida | Lista paginada. |

### 5.3 Asignar rol

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/users/{userId}/roles` |
| Permiso | `users.assign_roles` |
| Entrada | Rol, motivo si rol sensible. |
| Salida | Asignación vigente. |
| Eventos | `UserRoleAssigned`, `SensitiveActionPerformed` si aplica. |
| Auditoría | Sí, reforzada. |

## 6. Legal Knowledge API

### 6.1 Listar fuentes legales

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/legal/sources` |
| Permiso | `legal_sources.view` |
| Parámetros | tipo, jurisdicción, estado, global/tenant. |
| Salida | Fuentes y versiones vigentes. |

### 6.2 Publicar versión de fuente legal

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/legal/sources/{sourceId}/versions` |
| Permiso | `legal_sources.manage` |
| Entrada | Versión, vigencia, documento asociado, notas. |
| Salida | Versión creada. |
| Eventos | `LegalSourceVersioned`, `LegalSourcePublished`, `SearchIndexingRequested` |
| Auditoría | Sí. |

### 6.3 Listar obligaciones

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/legal/obligations` |
| Permiso | `legal_sources.view` |
| Parámetros | módulo, riesgo, artículo, fuente, búsqueda. |
| Salida | Obligaciones versionadas. |

## 7. Document Metadata API

### 7.1 Crear documento y subir versión inicial

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/documents` |
| Permiso | `documents.upload` |
| Entrada | Archivo, tipo documental, clasificación, título, metadata. |
| Salida | Documento, versión y estado de procesamiento. |
| Eventos | `DocumentUploaded`, `DocumentProcessingRequested` |
| Auditoría | Sí si clasificación Confidential o Sensitive. |
| Reglas | Tamaño máximo y tipos permitidos según tenant. |

### 7.2 Obtener metadata de documento

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/documents/{documentId}` |
| Permiso | `documents.view` más política de entidad. |
| Salida | Metadata, versiones, estado de procesamiento. |

### 7.3 Solicitar descarga

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/documents/{documentId}/download-link` |
| Permiso | `documents.download` |
| Entrada | Motivo si documento sensible. |
| Salida | Enlace temporal o mecanismo de descarga controlada. |
| Eventos | `DocumentDownloadRequested`, `SensitiveActionPerformed` si aplica. |
| Auditoría | Sí. |

## 8. Evidence API

### 8.1 Crear evidencia

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/evidence` |
| Permiso | `evidence.create` |
| Entrada | Tipo, título, descripción, clasificación, documento opcional, fecha de captura, origen. |
| Salida | Evidencia creada. |
| Eventos | `EvidenceCreated` |
| Auditoría | Sí si clasificación sensible. |

### 8.2 Asociar evidencia a entidad

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/evidence/{evidenceId}/links` |
| Permiso | `evidence.link` y permiso sobre entidad destino. |
| Entrada | Módulo destino, tipo entidad, id entidad, motivo. |
| Salida | Link creado. |
| Eventos | `EvidenceLinked` |
| Auditoría | Sí. |

### 8.3 Solicitar evidence pack

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/evidence-packs` |
| Permiso | `evidence.export` |
| Entrada | Alcance, entidades, filtros, formato, motivo. |
| Salida | ReportJob/EvidencePackJob. |
| Eventos | `EvidencePackRequested`, `ReportRequested` |
| Auditoría | Sí reforzada. |

## 9. Processing Inventory / RAT API

### 9.1 Crear tratamiento

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/processing-activities` |
| Permiso | `processing.create` |
| Entrada | Nombre, descripción, área, owner funcional, finalidad inicial opcional. |
| Salida | Tratamiento en Draft. |
| Eventos | `ProcessingActivityCreated` |
| Auditoría | Sí. |

### 9.2 Actualizar tratamiento

| Ítem | Definición |
|---|---|
| Método | PATCH |
| Ruta | `/api/v1/processing-activities/{activityId}` |
| Permiso | `processing.edit` más política por estado. |
| Entrada | Campos editables según estado. |
| Salida | Tratamiento actualizado, flags recalculados. |
| Eventos | `ProcessingActivityUpdated`, posibles `ProcessingActivityRiskFlagged` |
| Auditoría | Sí, con before/after de campos críticos. |
| Reglas | No editar directamente snapshot aprobado. |

### 9.3 Obtener tratamiento

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/processing-activities/{activityId}` |
| Permiso | `processing.view` más política de entidad. |
| Salida | Tratamiento, relaciones, evidencias, brechas, estado y versión. |

### 9.4 Enviar a revisión

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/processing-activities/{activityId}/submit-review` |
| Permiso | `processing.submit_review` |
| Entrada | Comentario opcional, revisores sugeridos. |
| Salida | Estado InReview, tareas creadas. |
| Eventos | `ProcessingActivitySubmitted`, `ReviewRequested`, `NotificationRequested` |
| Auditoría | Sí. |
| Reglas | Debe cumplir completitud mínima de revisión. |

### 9.5 Aprobar tratamiento

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/processing-activities/{activityId}/approve` |
| Permiso | `processing.approve` |
| Entrada | Comentario y motivo obligatorio. |
| Salida | Estado Approved/Active, versión aprobada. |
| Eventos | `ProcessingActivityApproved`, `ProcessingActivityVersionCreated`, `SensitiveActionPerformed` |
| Auditoría | Sí reforzada. |
| Reglas | Debe cumplir reglas de aprobación definidas en especificación RAT. |

### 9.6 Solicitar cambios

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/processing-activities/{activityId}/request-changes` |
| Permiso | `processing.review` |
| Entrada | Observaciones obligatorias. |
| Salida | Estado ChangesRequested. |
| Eventos | `ProcessingActivityChangesRequested`, `NotificationRequested` |
| Auditoría | Sí. |

## 10. Gap Management API

### 10.1 Crear brecha

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/gaps` |
| Permiso | `gaps.create` |
| Entrada | Origen, obligación opcional, título, descripción, severidad, owner, fecha. |
| Salida | Brecha creada. |
| Eventos | `GapCreated` |
| Auditoría | Sí. |

### 10.2 Actualizar brecha

| Ítem | Definición |
|---|---|
| Método | PATCH |
| Ruta | `/api/v1/gaps/{gapId}` |
| Permiso | según campo: assign, edit, close, accept risk. |
| Entrada | Estado, owner, fecha, severidad, resolución. |
| Salida | Brecha actualizada. |
| Eventos | `GapAssigned`, `GapSeverityChanged`, `GapResolved` según cambio. |
| Auditoría | Sí. |

### 10.3 Cerrar brecha

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/gaps/{gapId}/close` |
| Permiso | `gaps.close` |
| Entrada | Resolución, evidencias asociadas, motivo. |
| Salida | Estado Closed. |
| Eventos | `GapClosed` |
| Auditoría | Sí, requiere evidencia salvo excepción autorizada. |

## 11. Reporting API

### 11.1 Solicitar reporte RAT

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/reports/rat` |
| Permiso | `processing.export` o `reports.generate` |
| Entrada | Formato, filtros, inclusión de evidencias, motivo. |
| Salida | ReportJob. |
| Eventos | `ReportRequested`, `ReportGenerationRequested` |
| Auditoría | Sí reforzada si exporta datos sensibles. |

### 11.2 Consultar job de reporte

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/reports/jobs/{jobId}` |
| Permiso | `reports.download` o dueño del job. |
| Salida | Estado, error controlado, artifact si completado. |

## 12. Search API

### 12.1 Buscar fuentes legales

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/search/legal` |
| Permiso | `legal_sources.view` |
| Parámetros | query, fuente, versión, artículo, top. |
| Salida | Resultados con fuente, versión y referencia. |
| Auditoría | Sólo si se marca búsqueda sensible o MCP-related. |

### 12.2 Buscar dentro del tenant

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/search/tenant` |
| Permiso | según tipo de entidad consultada. |
| Parámetros | query, entity types, clasificación, top. |
| Salida | Resultados filtrados por tenant y permisos. |
| Auditoría | Sí para evidencia o documentos sensibles. |

## 13. MCP API

### 13.1 Realizar consulta MCP

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/mcp/questions` |
| Permiso | `mcp.ask` o `mcp.ask_with_tenant_context` si usa contexto. |
| Entrada | Pregunta, contexto opcional, ids de tratamiento/evidencia opcionales, consentimiento de uso de contexto tenant. |
| Salida | Respuesta, citas, nivel de riesgo, supuestos, flags de revisión humana. |
| Eventos | `McpInteractionCreated`, posible `McpHighRiskQueryCreated` |
| Auditoría | Sí. |
| Reglas | No responde sin citas en consultas normativas. No certifica cumplimiento. |

### 13.2 Solicitar revisión humana MCP

| Ítem | Definición |
|---|---|
| Método | POST |
| Ruta | `/api/v1/mcp/interactions/{interactionId}/human-review` |
| Permiso | `mcp.request_human_review` |
| Entrada | Motivo, revisor sugerido. |
| Salida | Tarea de revisión. |
| Eventos | `McpHumanReviewRequested`, `TaskAssigned` |
| Auditoría | Sí. |

## 14. Audit API

### 14.1 Consultar auditoría

| Ítem | Definición |
|---|---|
| Método | GET |
| Ruta | `/api/v1/audit` |
| Permiso | `audit.view` |
| Parámetros | rango fecha, actor, módulo, entidad, sensibilidad, acción. |
| Salida | Eventos paginados. |
| Auditoría | Sí para consultas amplias o críticas. |

## 15. Reglas para desarrollo frontend posterior

1. Todo endpoint de lista debe soportar paginación.
2. Toda acción asincrónica debe devolver job id o estado rastreable.
3. Toda acción sensible debe permitir enviar motivo desde UI.
4. Toda respuesta de error debe permitir mostrar mensaje funcional sin filtrar información sensible.
5. Toda entidad principal debe exponer estado, versión y permisos efectivos del usuario para facilitar UI.

## 16. Pendientes para cierre API definitivo

1. Confirmar estilo REST puro vs endpoints command-oriented para acciones de workflow.
2. Confirmar política de idempotency key para creación y jobs.
3. Confirmar formato estándar de error.
4. Confirmar si se expondrá OpenAPI como contrato formal por ambiente.
5. Confirmar límites de payload y carga documental.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
