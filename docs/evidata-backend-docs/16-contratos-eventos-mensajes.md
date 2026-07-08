# 16 — Contratos de eventos y mensajes

## 1. Propósito

Este documento define los contratos de eventos y mensajes para ATLAS Opción B usando Outbox + Azure Storage Queues como broker inicial. Los contratos deben ser neutrales al broker para permitir migración futura a Azure Service Bus sin reescribir dominios.

## 2. Decisión vigente

| Tema | Decisión |
|---|---|
| Broker inicial | Azure Storage Queues |
| Patrón confiabilidad | Outbox en PostgreSQL |
| Broker futuro | Azure Service Bus sólo con gatillos concretos |
| Tipo de mensajes iniciales | Jobs y eventos simples de un consumidor principal |
| Pub/sub avanzado | No en MVP salvo necesidad demostrada |

## 3. Estructura común de mensaje

Todo mensaje debe incluir:

| Campo | Requerido | Descripción |
|---|---:|---|
| `message_id` | Sí | UUID único del mensaje. |
| `message_type` | Sí | Nombre lógico del mensaje. |
| `message_version` | Sí | Versión del contrato. |
| `tenant_id` | Cuando aplique | Tenant relacionado. |
| `correlation_id` | Sí | Correlación de flujo. |
| `causation_id` | No | Mensaje o acción que causó este mensaje. |
| `occurred_at` | Sí | Fecha UTC. |
| `actor_user_id` | Cuando aplique | Usuario que originó acción. |
| `source_module` | Sí | Módulo productor. |
| `destination` | Sí | Destino lógico. |
| `sensitivity` | Sí | Normal, Confidential, Sensitive, Critical. |
| `payload` | Sí | Contenido versionado. |

## 4. Destinos lógicos iniciales

| Destino lógico | Cola inicial | Consumidor |
|---|---|---|
| document-processing | `document-processing-queue` | Document Processing Function |
| search-indexing | `search-indexing-queue` | Search Indexing Function |
| report-generation | `report-generation-queue` | Report Generation Function |
| notification | `notification-queue` | Notification Delivery Function |
| mcp-batch | `mcp-batch-queue` | MCP Batch Jobs Function |
| security-jobs | `security-jobs-queue` | Security Jobs Function |
| maintenance-jobs | `maintenance-jobs-queue` | Maintenance Jobs Function |

## 5. Outbox

### 5.1 Estados

| Estado | Uso |
|---|---|
| Pending | Mensaje registrado, no publicado. |
| Publishing | En intento de publicación. |
| Published | Publicado a cola. |
| Failed | Falló pero puede reintentarse. |
| Dead | Falló definitivamente y requiere intervención. |

### 5.2 Reglas

1. El mensaje se crea en la misma transacción que la operación de negocio.
2. El publicador de Outbox no interpreta reglas de negocio.
3. El publicador sólo resuelve destino lógico a cola física.
4. La publicación debe ser idempotente.
5. Los mensajes Dead deben ser visibles en operación.
6. Todo error debe conservar mensaje, destino, intento y causa.

## 6. Idempotencia

Cada consumidor debe registrar `message_id` en ProcessedMessage antes o después del procesamiento según estrategia transaccional. Si recibe el mismo mensaje, debe responder sin duplicar efectos.

Reglas:

- Report Generation no debe generar múltiples artefactos activos para el mismo job.
- Document Processing no debe reprocesar si la versión ya está Processed, salvo reproceso solicitado.
- Notification puede evitar duplicados por message_id y recipient.
- Search Indexing puede ser idempotente por document_version_id + index_version.
- MCP Batch nunca debe aplicar cambios automáticos sin aprobación.

## 7. Poison messages

Para Storage Queues se utilizará manejo de poison con colas separadas o mecanismo equivalente.

| Cola principal | Cola poison |
|---|---|
| document-processing-queue | document-processing-poison |
| search-indexing-queue | search-indexing-poison |
| report-generation-queue | report-generation-poison |
| notification-queue | notification-poison |
| mcp-batch-queue | mcp-batch-poison |
| security-jobs-queue | security-jobs-poison |

Al enviar a poison:

- actualizar job o entidad relacionada como Failed;
- registrar error controlado;
- emitir alerta operativa;
- conservar correlation_id;
- evitar datos sensibles en logs.

## 8. Contratos iniciales

### 8.1 DocumentProcessingRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `document_id` | Sí | Documento. |
| `document_version_id` | Sí | Versión. |
| `blob_uri` | Sí | Ruta privada. |
| `content_type` | Sí | Tipo. |
| `classification` | Sí | Sensibilidad. |
| `processing_options` | No | Extracción texto, OCR futuro, indexación. |

Productor: Document Metadata.  
Consumidor: Document Processing Function.  
Resultado esperado: `DocumentProcessed` o `DocumentProcessingFailed`.

### 8.2 DocumentProcessed

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `document_id` | Sí | Documento. |
| `document_version_id` | Sí | Versión. |
| `text_extracted` | Sí | Indica si se extrajo texto. |
| `extracted_text_location` | No | Ubicación texto. |
| `hash` | Sí | Hash confirmado. |
| `warnings` | No | Advertencias no fatales. |

Destino lógico posterior: search-indexing si corresponde.

### 8.3 SearchIndexingRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `source_type` | Sí | LegalSource, Document, ProcessingActivity, Evidence. |
| `source_id` | Sí | Entidad. |
| `source_version_id` | No | Versión. |
| `index_scope` | Sí | Global o Tenant. |
| `operation` | Sí | Upsert, Delete, Rebuild. |

### 8.4 ReportGenerationRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `report_job_id` | Sí | Job. |
| `report_type` | Sí | RAT, Gaps, EvidencePack, ExecutiveSummary. |
| `requested_by` | Sí | Usuario. |
| `parameters` | Sí | Filtros controlados. |
| `classification` | Sí | Clasificación de salida. |

Resultado esperado: `ReportCompleted` o `ReportFailed`.

### 8.5 NotificationRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `notification_type` | Sí | Tipo de notificación. |
| `recipient_user_ids` | Sí | Destinatarios internos. |
| `template_key` | Sí | Plantilla. |
| `template_data` | Sí | Datos mínimos. |
| `priority` | Sí | Normal, High, Critical. |

### 8.6 McpBatchJobRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `job_type` | Sí | CitationVerification, GapSuggestion, EmbeddingRefresh. |
| `target_module` | Sí | Módulo. |
| `target_entity_id` | Sí | Entidad. |
| `requires_human_acceptance` | Sí | Debe ser true para cambios sugeridos. |

### 8.7 SecurityCheckRequested

| Campo payload | Requerido | Descripción |
|---|---:|---|
| `check_type` | Sí | InactiveUsers, PermissionDrift, SuspiciousDownloads, CrossTenantAttempt. |
| `scope` | Sí | Tenant o plataforma. |
| `requested_by` | No | Usuario o sistema. |

## 9. Eventos de dominio registrados en Outbox

No todos los eventos deben publicarse a cola en MVP. Algunos sólo se registran para auditoría o evolución futura.

| Evento | Publicar en MVP | Observación |
|---|---:|---|
| TenantCreated | No obligatorio | Puede quedar auditado. |
| UserInvited | Sí | Notificación. |
| UserRoleAssigned | Sí si rol sensible | Seguridad/notificación. |
| DocumentUploaded | Sí | Document processing. |
| DocumentProcessed | Sí | Search indexing. |
| EvidenceCreated | No inicial | Puede activar indexación futura. |
| EvidencePackRequested | Sí | Report generation. |
| ProcessingActivityCreated | No inicial | Audit. |
| ProcessingActivitySubmitted | Sí | Notificación. |
| ProcessingActivityApproved | Sí | Report/search/notificación según configuración. |
| GapCreated | Sí | Notificación. |
| ReportRequested | Sí | Report generation. |
| LegalSourcePublished | Sí | Search indexing. |
| McpInteractionCreated | No inicial | Audit/metrics. |
| McpHighRiskQueryCreated | Sí | Security/human review. |
| SensitiveActionPerformed | Sí | Security jobs. |

## 10. Gatillos para migrar a Service Bus

Migrar destino lógico a Service Bus cuando:

1. un evento requiere múltiples consumidores independientes;
2. se necesitan topics/subscriptions;
3. se necesitan filtros por suscripción;
4. se requiere orden por entidad;
5. se integran partners externos;
6. la operación de poison/reintentos supera capacidad simple;
7. existe SLA enterprise;
8. se requieren sesiones o duplicate detection administrada.

## 11. Criterios de aceptación

1. Outbox persiste mensajes con la operación de negocio.
2. Publicador procesa Pending y marca Published.
3. Functions consumen Storage Queues.
4. Consumidores son idempotentes.
5. Poison messages quedan visibles.
6. Mensajes tienen versión.
7. No hay dependencia directa de módulos de negocio al SDK de colas.
8. Se puede cambiar destino lógico sin tocar dominio.

## 12. Pendientes

1. Definir número máximo de reintentos por cola.
2. Definir política de visibilidad/timeouts por tipo de job.
3. Definir tamaño máximo de mensaje y uso de Blob para payloads grandes.
4. Definir tablero operativo de outbox y poison queues.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
