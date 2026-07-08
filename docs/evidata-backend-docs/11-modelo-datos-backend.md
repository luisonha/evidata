# 11 — Modelo de datos backend

## 1. Propósito

Este documento define el modelo de datos lógico para el backend de ATLAS Opción B. Su objetivo es permitir implementación consistente en PostgreSQL, con aislamiento multi-tenant, ownership claro por módulo, versionado, auditoría y posibilidad de extracción futura a microservicios.

Este documento no reemplaza migraciones físicas ni scripts SQL. Define el contrato funcional que esas migraciones deben respetar.

## 2. Principios de diseño de datos

1. Toda entidad funcional debe incluir `tenant_id`, salvo catálogos globales estrictamente administrados por plataforma.
2. Ningún módulo debe modificar directamente entidades owned por otro módulo.
3. Las relaciones entre módulos deben preferir referencias por identificador y contratos internos.
4. Las entidades aprobadas o auditables no deben sobrescribirse sin versionado.
5. Toda eliminación de negocio debe ser lógica, salvo datos técnicos temporales definidos por política.
6. Toda entidad sensible debe tener trazabilidad de creación, modificación y actor.
7. Toda tabla crítica debe tener índices por `tenant_id` y claves de búsqueda habituales.
8. Las entidades usadas por MCP deben permitir minimización de contexto.
9. Los reportes deben poder generarse desde vistas/proyecciones sin romper ownership.
10. Las tablas de outbox y auditoría son transversales y no pertenecen a un dominio de negocio específico.

## 3. Convenciones globales

| Campo | Uso |
|---|---|
| `id` | Identificador primario. Preferir UUID. |
| `tenant_id` | Identificador obligatorio de tenant para entidades funcionales. |
| `created_at` | Fecha de creación en UTC. |
| `created_by` | Usuario funcional que creó el registro, cuando aplique. |
| `updated_at` | Fecha de última modificación en UTC. |
| `updated_by` | Usuario funcional de última modificación. |
| `deleted_at` | Fecha de eliminación lógica, cuando aplique. |
| `deleted_by` | Usuario que eliminó lógicamente. |
| `row_version` | Control de concurrencia optimista. |
| `status` | Estado funcional controlado por catálogo o enum de dominio. |

## 4. Ownership por módulo

| Módulo | Entidades owned |
|---|---|
| Tenant Management | Tenant, TenantSettings, TenantQuota, TenantModuleFlag |
| Identity Bridge | UserProfile, TenantUser, Role, Permission, UserRoleAssignment |
| Security | AccessDecisionLog, SensitiveActionLog, SecurityEvent |
| Legal Knowledge | LegalSource, LegalSourceVersion, LegalArticle, LegalObligation, LegalTaxonomyItem |
| Document Metadata | Document, DocumentVersion, DocumentClassification |
| Evidence | Evidence, EvidenceLink, EvidenceType, EvidencePackJob |
| Processing Inventory / RAT | ProcessingActivity, ProcessingActivityVersion, ProcessingActivityDataCategory, ProcessingActivitySystem, ProcessingActivityVendor, ProcessingActivityTransfer, ProcessingActivitySecurityMeasure |
| Workflow | WorkflowTask, Review, Approval, Comment, StateTransitionLog |
| Gap Management | ComplianceGap, GapAssignment, GapResolution, GapRiskAcceptance |
| Reporting Orchestration | ReportJob, ReportArtifact |
| Search Query | SearchQueryLog, SearchResultSelection |
| MCP Interactive | McpInteraction, McpCitation, McpReviewTask, McpFeedback |
| Audit | AuditLog |
| Messaging | OutboxMessage, ProcessedMessage |

## 5. Tenant Management

### 5.1 Tenant

| Campo | Requerido | Descripción |
|---|---:|---|
| `id` | Sí | Identificador del tenant. |
| `name` | Sí | Nombre visible de la organización. |
| `legal_name` | No | Razón social. |
| `tax_id` | No | Identificador tributario. |
| `country` | Sí | País principal. Para MVP, Chile. |
| `industry` | No | Industria declarada. |
| `plan_code` | Sí | Plan comercial o funcional. |
| `status` | Sí | Active, Suspended, Provisioning, Closed. |
| `default_locale` | Sí | Idioma/región por defecto. |
| `timezone` | Sí | Zona horaria operativa. |
| `data_region` | No | Región lógica de residencia si se habilita. |

### 5.2 TenantSettings

Define configuración funcional por tenant: módulos habilitados, límites, seguridad, retención, exportaciones, MCP y notificaciones.

### 5.3 Índices mínimos

| Entidad | Índices |
|---|---|
| Tenant | `status`, `tax_id`, `name` |
| TenantSettings | `tenant_id` único |

## 6. Identity Bridge y autorización funcional

### 6.1 UserProfile

Representa al usuario funcional en ATLAS. No almacena contraseña.

| Campo | Requerido | Descripción |
|---|---:|---|
| `id` | Sí | Usuario ATLAS. |
| `external_identity_id` | Sí | Identificador de Entra External ID. |
| `email` | Sí | Correo normalizado. |
| `display_name` | Sí | Nombre visible. |
| `status` | Sí | Invited, Active, Disabled. |
| `last_login_at` | No | Último acceso conocido. |

### 6.2 TenantUser

Relación usuario-tenant.

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `user_id` | Sí | Usuario. |
| `status` | Sí | Active, Disabled, Pending. |
| `joined_at` | No | Fecha de activación. |

### 6.3 Role, Permission, UserRoleAssignment

Los roles son asignables por tenant. Los permisos son capacidades atómicas. La asignación debe guardar quién asignó, cuándo y por qué si el rol es sensible.

## 7. Legal Knowledge

### 7.1 LegalSource

Fuente legal o guía oficial versionable.

| Campo | Requerido | Descripción |
|---|---:|---|
| `id` | Sí | Fuente. |
| `name` | Sí | Nombre. |
| `source_type` | Sí | Law, Regulation, Guide, InternalPolicy. |
| `jurisdiction` | Sí | Jurisdicción. |
| `is_global` | Sí | Global o tenant-specific. |
| `owner_tenant_id` | No | Sólo si es tenant-specific. |
| `status` | Sí | Draft, Published, Deprecated. |

### 7.2 LegalSourceVersion

| Campo | Requerido | Descripción |
|---|---:|---|
| `legal_source_id` | Sí | Fuente padre. |
| `version_label` | Sí | Versión legible. |
| `effective_from` | No | Vigencia. |
| `effective_to` | No | Fin de vigencia. |
| `document_id` | No | Documento asociado. |
| `published_at` | No | Fecha publicación interna. |

### 7.3 LegalObligation

Obligación computable o referencia operativa.

| Campo | Requerido | Descripción |
|---|---:|---|
| `source_version_id` | Sí | Versión fuente. |
| `article_reference` | Sí | Artículo/sección. |
| `title` | Sí | Título operativo. |
| `description` | Sí | Descripción. |
| `module_hint` | No | Módulo relacionado. |
| `risk_level` | Sí | Low, Medium, High, Critical. |
| `evidence_expected` | No | Evidencia sugerida. |

## 8. Document Metadata

### 8.1 Document

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant owner, salvo fuente global administrada por plataforma. |
| `title` | Sí | Nombre del documento. |
| `document_type` | Sí | Contract, Policy, LegalSource, EvidenceAttachment, ReportArtifact, Other. |
| `classification` | Sí | Public, Internal, Confidential, Sensitive. |
| `status` | Sí | Active, Archived, Deleted. |
| `current_version_id` | No | Versión vigente. |

### 8.2 DocumentVersion

| Campo | Requerido | Descripción |
|---|---:|---|
| `document_id` | Sí | Documento padre. |
| `version_number` | Sí | Número secuencial. |
| `blob_uri` | Sí | Ruta privada en Blob. |
| `content_type` | Sí | MIME type. |
| `size_bytes` | Sí | Tamaño. |
| `sha256_hash` | Sí | Hash. |
| `processing_status` | Sí | Pending, Processed, Failed, Skipped. |
| `extracted_text_location` | No | Ruta si el texto extraído se almacena fuera de DB. |

## 9. Evidence

### 9.1 Evidence

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `evidence_type_id` | Sí | Tipo de evidencia. |
| `title` | Sí | Título. |
| `description` | No | Descripción. |
| `document_id` | No | Documento asociado. |
| `source_type` | Sí | Manual, SystemGenerated, Imported, External. |
| `captured_at` | Sí | Momento de captura. |
| `captured_by` | No | Actor. |
| `classification` | Sí | Internal, Confidential, Sensitive. |
| `status` | Sí | Draft, Active, Superseded, Deleted. |

### 9.2 EvidenceLink

Asocia evidencia con entidades de negocio sin que Evidence conozca su modelo interno.

| Campo | Requerido | Descripción |
|---|---:|---|
| `evidence_id` | Sí | Evidencia. |
| `target_module` | Sí | Módulo destino. |
| `target_entity_type` | Sí | Tipo de entidad. |
| `target_entity_id` | Sí | Identificador. |
| `link_reason` | No | Motivo. |
| `linked_by` | Sí | Usuario. |
| `linked_at` | Sí | Fecha. |

## 10. Processing Inventory / RAT

### 10.1 ProcessingActivity

Representa el tratamiento activo editable en estado de borrador o trabajo. Las versiones aprobadas se congelan en ProcessingActivityVersion.

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `code` | Sí | Código único por tenant. |
| `name` | Sí | Nombre del tratamiento. |
| `description` | No | Descripción operacional. |
| `organization_unit_id` | No | Área responsable. |
| `business_owner_id` | Sí | Dueño del proceso. |
| `legal_owner_id` | No | Revisor legal principal. |
| `purpose_text` | Sí para aprobación | Finalidad. |
| `legal_basis_id` | Sí para aprobación | Base de licitud. |
| `legal_basis_justification` | Sí para aprobación | Justificación. |
| `retention_period` | No | Período de conservación. |
| `retention_justification` | No | Justificación de conservación. |
| `has_sensitive_data` | Sí | Flag calculado/declarado. |
| `has_children_data` | Sí | Flag calculado/declarado. |
| `has_biometric_data` | Sí | Flag calculado/declarado. |
| `has_international_transfer` | Sí | Flag calculado/declarado. |
| `has_automated_decision` | Sí | Flag calculado/declarado. |
| `status` | Sí | Draft, InReview, ChangesRequested, Approved, Active, Deprecated, Archived. |
| `current_version_number` | Sí | Versión vigente. |

### 10.2 Entidades relacionadas RAT

| Entidad | Propósito |
|---|---|
| ProcessingActivityDataCategory | Categorías de datos asociadas. |
| ProcessingActivityDataSubject | Categorías de titulares. |
| ProcessingActivitySystem | Sistemas que tratan datos. |
| ProcessingActivityVendor | Proveedores/encargados básicos asociados. |
| ProcessingActivityTransfer | Transferencias internacionales declaradas. |
| ProcessingActivitySecurityMeasure | Medidas técnicas/organizativas declaradas. |
| ProcessingActivityRiskFlag | Flags de riesgo generados por reglas. |
| ProcessingActivityVersion | Snapshot aprobado o versionado. |

## 11. Workflow

### 11.1 WorkflowTask

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `target_module` | Sí | Módulo objetivo. |
| `target_entity_type` | Sí | Entidad objetivo. |
| `target_entity_id` | Sí | Identificador. |
| `task_type` | Sí | Review, Approval, Remediation, HumanReview, Other. |
| `assigned_to` | Sí | Usuario responsable. |
| `due_at` | No | Fecha de vencimiento. |
| `status` | Sí | Open, InProgress, Completed, Cancelled, Overdue. |

### 11.2 Review, Approval, Comment

Deben mantener historial inmutable de decisiones humanas relevantes, comentarios y cambios de estado.

## 12. Gap Management

### 12.1 ComplianceGap

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `source_module` | Sí | Módulo origen. |
| `source_entity_id` | Sí | Entidad origen. |
| `legal_obligation_id` | No | Obligación relacionada. |
| `title` | Sí | Título. |
| `description` | Sí | Descripción. |
| `severity` | Sí | Low, Medium, High, Critical. |
| `status` | Sí | Open, Assigned, InProgress, Blocked, Resolved, AcceptedRisk, Closed. |
| `owner_id` | No | Responsable. |
| `due_at` | No | Fecha objetivo. |
| `closed_at` | No | Cierre. |

## 13. Reporting

### 13.1 ReportJob

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `report_type` | Sí | RAT, Gaps, EvidencePack, ExecutiveSummary. |
| `parameters` | Sí | Parámetros serializados. |
| `status` | Sí | Requested, Running, Completed, Failed, Expired. |
| `requested_by` | Sí | Usuario. |
| `artifact_document_id` | No | Documento generado. |
| `error_message` | No | Error funcional. |

## 14. Search y MCP

### 14.1 SearchQueryLog

Debe registrar búsquedas sensibles, usuario, tenant, filtros, fuente consultada y selección de resultados cuando sea relevante.

### 14.2 McpInteraction

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Sí | Tenant. |
| `user_id` | Sí | Usuario. |
| `question` | Sí | Pregunta original. |
| `answer` | Sí | Respuesta generada. |
| `risk_level` | Sí | Low, Medium, High. |
| `used_tenant_context` | Sí | Indica si usó contexto tenant. |
| `requires_human_review` | Sí | Flag HITL. |
| `status` | Sí | Completed, Failed, ReviewRequested. |

### 14.3 McpCitation

Debe registrar fuente, versión, fragmento o referencia, tipo de fuente y relación con la respuesta.

## 15. Audit y mensajería

### 15.1 AuditLog

Append-only lógico. No debe editarse desde módulos funcionales.

| Campo | Requerido | Descripción |
|---|---:|---|
| `tenant_id` | Cuando aplique | Tenant. |
| `actor_user_id` | Cuando aplique | Usuario. |
| `action` | Sí | Acción. |
| `target_module` | Sí | Módulo. |
| `target_entity_id` | No | Entidad. |
| `sensitivity` | Sí | Normal, Sensitive, Critical. |
| `metadata` | No | Datos mínimos. |
| `occurred_at` | Sí | Fecha. |

### 15.2 OutboxMessage

Neutral al broker. No debe mencionar Service Bus ni Storage Queues en el dominio.

| Campo | Requerido | Descripción |
|---|---:|---|
| `id` | Sí | Identificador. |
| `tenant_id` | Cuando aplique | Tenant. |
| `message_type` | Sí | Tipo lógico. |
| `message_version` | Sí | Versión. |
| `destination` | Sí | Destino lógico. |
| `payload` | Sí | Mensaje serializado. |
| `status` | Sí | Pending, Published, Failed, Dead. |
| `attempt_count` | Sí | Intentos. |
| `next_attempt_at` | No | Próximo intento. |
| `correlation_id` | Sí | Correlación. |
| `causation_id` | No | Causa. |
| `sensitivity` | Sí | Sensibilidad. |

### 15.3 ProcessedMessage

Registra mensajes consumidos para idempotencia.

## 16. Índices mínimos por seguridad y rendimiento

| Tabla | Índices mínimos |
|---|---|
| UserProfile | email único, external_identity_id único |
| TenantUser | tenant_id + user_id único |
| Document | tenant_id + status + classification |
| DocumentVersion | document_id + version_number único |
| Evidence | tenant_id + evidence_type + status |
| EvidenceLink | tenant_id derivado, target_module + target_entity_id |
| ProcessingActivity | tenant_id + code único, tenant_id + status, tenant_id + business_owner_id |
| ComplianceGap | tenant_id + status, tenant_id + severity, owner_id |
| ReportJob | tenant_id + status + requested_at |
| McpInteraction | tenant_id + user_id + created_at |
| AuditLog | tenant_id + occurred_at, actor_user_id + occurred_at |
| OutboxMessage | status + next_attempt_at, destination + status |
| ProcessedMessage | message_id único |

## 17. Decisiones pendientes del modelo

1. Confirmar PostgreSQL como base final.
2. Confirmar si el MVP usa base compartida con `tenant_id` o schema por tenant.
3. Definir si se aplicará Row Level Security en PostgreSQL desde MVP.
4. Definir almacenamiento de texto extraído: tabla, Blob separado o motor de búsqueda.
5. Definir política de retención para documentos generados y audit logs.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
