# 08 — Modelo operativo, datos y eventos

## 1. Objetivo

Definir convenciones transversales para datos, eventos, ownership, auditoría, contratos y operación multi-tenant.

## 2. Ownership de datos

Cada entidad tiene un módulo dueño. Otros módulos no deben modificarla directamente.

| Entidad | Módulo dueño |
|---|---|
| Tenant | Tenant Management |
| User | Identity Bridge |
| Role / Permission | Security & Authorization |
| LegalSource | Legal Knowledge |
| LegalObligation | Legal Knowledge |
| Document | Document Metadata |
| DocumentVersion | Document Metadata |
| Evidence | Evidence |
| ProcessingActivity | Processing Inventory / RAT |
| ProcessingActivityVersion | Processing Inventory / RAT |
| WorkflowTask | Workflow |
| Review | Workflow |
| ComplianceGap | Gap Management |
| ReportJob | Reporting Orchestration |
| SearchIndexStatus | Search Query / Search Indexing |
| McpInteraction | MCP Interactive |
| AuditLog | Audit |
| OutboxMessage | Platform Messaging |

## 3. Multi-tenant

Entidades tenant-scoped deben incluir `tenant_id`.

Entidades globales:

- fuentes legales globales;
- taxonomías base;
- obligaciones globales;
- configuración de plataforma.

Entidades tenant-scoped:

- usuarios funcionales por tenant;
- tratamientos;
- evidencias;
- documentos;
- brechas;
- reportes;
- auditoría tenant;
- búsquedas internas;
- interacciones MCP con contexto tenant.

## 4. Convenciones de estado

Cada entidad con ciclo de vida debe tener estado explícito y auditable.

Ejemplos:

| Entidad | Estados |
|---|---|
| ProcessingActivity | Draft, InReview, ChangesRequested, Approved, Active, Deprecated, Archived |
| ComplianceGap | Open, Assigned, InProgress, Blocked, Resolved, AcceptedRisk, Closed |
| DocumentVersion | Uploaded, Processing, Processed, Indexing, Indexed, Failed |
| ReportJob | Requested, Processing, Completed, Failed, Expired |
| OutboxMessage | Pending, Published, Failed, Cancelled |
| McpInteraction | Created, Answered, RequiresReview, Reviewed, Failed |

## 5. Versionado

Deben versionarse:

- fuentes legales;
- documentos;
- tratamientos aprobados;
- plantillas de notificación;
- contratos de eventos;
- respuestas MCP almacenadas con fuentes usadas;
- reportes generados.

Reglas:

- no sobrescribir versiones aprobadas;
- cambios relevantes generan nueva versión;
- evidencias deben apuntar a versiones específicas cuando aplique;
- MCP debe registrar versión de fuentes usadas.

## 6. Eventos

Todo evento debe tener contrato versionado.

Campos base:

| Campo | Descripción |
|---|---|
| event_id | Identificador único. |
| event_type | Tipo lógico. |
| event_version | Versión del contrato. |
| occurred_at | Fecha real de ocurrencia. |
| tenant_id | Tenant asociado cuando aplique. |
| correlation_id | Correlación de request/flujo. |
| causation_id | Evento o comando que originó. |
| actor_user_id | Usuario origen cuando aplique. |
| source_module | Módulo emisor. |
| sensitivity | Sensibilidad del mensaje. |
| payload | Datos mínimos necesarios. |

## 7. Eventos core mínimos

| Evento | Emisor | Destino típico |
|---|---|---|
| TenantCreated | Tenant Management | Audit, Notifications |
| UserInvited | Identity Bridge | Notifications, Audit |
| UserRoleAssigned | Identity Bridge | Audit, Security Jobs |
| DocumentUploaded | Document Metadata | Document Processing |
| DocumentProcessed | Document Processing | Search Indexing |
| EvidenceCreated | Evidence | Audit, Search Indexing |
| EvidenceLinked | Evidence | Audit |
| ProcessingActivityCreated | RAT | Audit |
| ProcessingActivitySubmitted | RAT | Workflow, Notifications |
| ProcessingActivityApproved | RAT | Audit, Reporting, Search Indexing |
| GapCreated | Gap Management | Workflow, Notifications |
| ReportRequested | Reporting Orchestration | Report Generation |
| ReportCompleted | Report Generation | Notifications |
| LegalSourcePublished | Legal Knowledge | Search Indexing |
| McpInteractionCreated | MCP Interactive | Audit |
| McpHighRiskQueryCreated | MCP Interactive | Security Jobs, Workflow |
| AccessDenied | Security | Audit, Security Jobs |
| SensitiveActionPerformed | Security | Audit, Security Jobs |

## 8. Destinos lógicos

No acoplar eventos a tecnología.

Destinos iniciales:

- document-processing;
- search-indexing;
- report-generation;
- notification;
- mcp-batch;
- security-jobs;
- maintenance-jobs.

## 9. Outbox

Outbox es obligatorio para eventos derivados de operaciones transaccionales.

Responsabilidades:

- registrar mensaje en la misma transacción de negocio;
- publicar posteriormente a la cola;
- reintentar;
- registrar error;
- evitar pérdida de mensajes.

## 10. Auditoría

AuditLog debe registrar:

- acción;
- usuario;
- tenant;
- entidad;
- resultado;
- before/after cuando aplique;
- IP y user-agent cuando estén disponibles;
- correlation id;
- sensibilidad;
- timestamp.

No debe registrar:

- payloads completos sensibles sin justificación;
- secretos;
- tokens;
- claves;
- documentos completos.

## 11. Proyecciones de lectura

Para evitar joins cruzados excesivos, se pueden crear vistas/proyecciones:

- ProcessingActivityListView.
- TreatmentEvidenceSummary.
- GapDashboardView.
- ReportableRatView.
- McpTreatmentContextView.
- AuditSearchView.

Estas proyecciones facilitan frontend y futura extracción.

## 12. API boundaries

Cada módulo debe exponer operaciones de aplicación, no acceso libre a tablas.

Reglas:

- no compartir modelos de persistencia entre módulos;
- usar DTOs/contratos internos;
- mantener validaciones en el módulo dueño;
- mantener transacciones dentro del módulo cuando sea posible;
- si una operación cruza módulos, orquestar explícitamente.

## 13. Datos sensibles

Toda entidad que pueda contener datos sensibles debe tener clasificación.

Niveles sugeridos:

- Public.
- Internal.
- Confidential.
- Sensitive.
- Restricted.

La clasificación impacta:

- permisos;
- auditoría;
- exportación;
- logging;
- contexto MCP;
- notificaciones;
- retención.

## 14. Retención

La política definitiva de retención queda pendiente. Desde el MVP se debe preparar:

- fecha de creación;
- fecha de última actualización;
- estado;
- marca de eliminación lógica;
- clasificación;
- tenant config de retención;
- eventos de lifecycle.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
