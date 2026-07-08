# 05 — Mensajería, asincronía y Outbox

## 1. Decisión vigente

La mensajería inicial de ATLAS Opción B usará:

- Outbox Pattern;
- abstracción interna de publicación;
- Azure Storage Queues;
- Azure Functions consumidoras.

No se usará Azure Service Bus como dependencia inicial obligatoria.

## 2. Motivo de la decisión

El MVP no tiene evidencia suficiente de requerir:

- topics/subscriptions;
- múltiples consumidores por evento;
- filtros por suscripción;
- sesiones;
- orden por grupo;
- duplicate detection administrada;
- integración enterprise;
- pub/sub avanzado.

Para bajar costo y complejidad, Storage Queues es suficiente para los jobs iniciales.

## 3. Principio de diseño

Los módulos de negocio no deben depender del proveedor de mensajería. Deben publicar mensajes a destinos lógicos.

Ejemplos de destinos lógicos:

- `document-processing`;
- `search-indexing`;
- `report-generation`;
- `notification`;
- `mcp-batch`;
- `security-jobs`;
- `maintenance-jobs`.

La infraestructura decide si ese destino se implementa como Storage Queue, Service Bus Queue o Service Bus Topic.

## 4. Arquitectura inicial

Flujo:

1. Operación de negocio guarda datos en PostgreSQL.
2. En la misma transacción guarda mensaje en Outbox.
3. Publicador Outbox toma mensajes pendientes.
4. Publicador envía mensaje a Azure Storage Queue correspondiente.
5. Function consume la cola.
6. Function procesa de forma idempotente.
7. Function actualiza estado o emite otro mensaje.
8. Si falla, se reintenta.
9. Si excede límite, se mueve a manejo poison/error.

## 5. OutboxMessages

La tabla Outbox debe ser neutral al broker.

Campos mínimos:

| Campo | Propósito |
|---|---|
| Id | Identificador único del mensaje. |
| TenantId | Tenant asociado, cuando aplique. |
| MessageType | Tipo lógico del mensaje. |
| MessageVersion | Versión del contrato. |
| Destination | Destino lógico. |
| Payload | Contenido serializado. |
| Status | Pending, Published, Failed, Cancelled. |
| AttemptCount | Número de intentos. |
| LastAttemptAt | Último intento. |
| NextAttemptAt | Próximo intento. |
| CreatedAt | Fecha creación. |
| PublishedAt | Fecha publicación. |
| Error | Último error. |
| CorrelationId | Trazabilidad del flujo. |
| CausationId | Evento/comando origen. |
| Sensitivity | Nivel de sensibilidad. |

## 6. Colas iniciales

| Cola física | Destino lógico | Consumidor |
|---|---|---|
| `document-processing-queue` | `document-processing` | Document Processing Function |
| `search-indexing-queue` | `search-indexing` | Search Indexing Function |
| `report-generation-queue` | `report-generation` | Report Generation Function |
| `notification-queue` | `notification` | Notification Delivery Function |
| `mcp-batch-queue` | `mcp-batch` | MCP Batch Jobs Function |
| `security-jobs-queue` | `security-jobs` | Security Jobs Function |
| `maintenance-jobs-queue` | `maintenance-jobs` | Maintenance Jobs Function |

## 7. Contrato base de mensaje

Todo mensaje debe incluir:

| Campo | Obligatorio |
|---|---|
| message_id | Sí |
| message_type | Sí |
| message_version | Sí |
| destination | Sí |
| tenant_id | Cuando aplique |
| entity_id | Cuando aplique |
| correlation_id | Sí |
| causation_id | Sí |
| occurred_at | Sí |
| created_by | Cuando aplique |
| sensitivity | Sí |
| payload | Sí |

## 8. Idempotencia

Todo consumidor debe soportar duplicados.

Reglas:

- verificar si el job ya fue procesado;
- no crear salidas duplicadas;
- registrar message id procesado cuando corresponda;
- operar por estado, no por suposición;
- aceptar reintentos seguros;
- manejar fallas parciales.

## 9. Poison messages

Storage Queues permite detectar múltiples dequeues. ATLAS debe tener estrategia propia para poison messages.

Opciones aceptadas:

- cola poison por responsabilidad;
- tabla de errores operativos;
- estado Failed en job;
- alerta Azure Monitor;
- reintento manual controlado.

Colas poison sugeridas:

- `document-processing-poison`;
- `search-indexing-poison`;
- `report-generation-poison`;
- `notification-poison`;
- `mcp-batch-poison`;
- `security-jobs-poison`.

## 10. Reintentos

Debe existir una política por tipo de trabajo.

| Trabajo | Reintento recomendado |
|---|---|
| Document processing | Reintento limitado, luego revisión manual. |
| Search indexing | Reintento y reindexación manual posible. |
| Report generation | Reintento limitado; usuario ve error de job. |
| Notification | Reintento con backoff; luego failed. |
| MCP batch | Reintento limitado; nunca aplicar cambios automáticos. |
| Security jobs | Reintento y alerta si falla. |

## 11. Seguridad de mensajes

Reglas:

- no incluir documentos completos en mensajes;
- no incluir datos personales innecesarios;
- usar referencias a entidades y blobs;
- incluir tenant id;
- incluir sensitivity;
- cifrado en tránsito y reposo según Azure;
- acceso a colas mediante Managed Identity o credenciales seguras;
- no loguear payload completo si es sensible.

## 12. Cuándo migrar a Service Bus

Migrar a Service Bus sólo si se cumple al menos un gatillo:

| Gatillo | Motivo |
|---|---|
| Un evento requiere múltiples consumidores | Storage Queues obliga duplicación manual. |
| Se requiere pub/sub real | Service Bus Topics es más adecuado. |
| Se requieren filtros por suscripción | Storage Queues no los ofrece. |
| Se requiere orden por entidad | Service Bus Sessions puede resolverlo. |
| Se integran sistemas enterprise | Service Bus es más robusto. |
| Operación de colas se vuelve crítica | Service Bus ofrece operación avanzada. |
| Hay módulos DSAR/Consentimiento/Incidentes con conectores externos | Mayor necesidad de integración formal. |

## 13. Arquitectura futura mixta

La arquitectura futura puede usar ambos:

| Uso | Tecnología |
|---|---|
| Jobs simples | Storage Queues |
| Procesamiento documental | Storage Queues |
| Reportes | Storage Queues |
| Notificaciones simples | Storage Queues |
| Eventos de dominio multi-consumidor | Service Bus Topics |
| Integración enterprise | Service Bus |
| Flujos con orden/sesiones | Service Bus |

## 14. Impacto en presupuesto

La decisión reduce costo mensual y complejidad inicial. No elimina la necesidad de Outbox, idempotencia, logging, reintentos y monitoreo.

La estimación de infraestructura inicial queda en un rango más bajo que la variante con Service Bus, manteniendo una vía de migración controlada.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
