# 02 — Arquitectura general backend

## 1. Visión general

La arquitectura backend de ATLAS Opción B se define como un **core modular .NET 10** complementado por **Azure Functions** para procesos naturalmente asíncronos. Esta estructura permite avanzar por módulos, reducir costos iniciales, mantener seguridad multi-tenant y preparar una transición controlada hacia microservicios cuando existan señales reales.

## 2. Vista lógica

| Capa | Responsabilidad |
|---|---|
| API Core .NET 10 | Entrada principal al backend, reglas transaccionales, autorización contextual, módulos de negocio. |
| PostgreSQL | Persistencia transaccional inicial. |
| Blob Storage | Almacenamiento de documentos, evidencias y reportes generados. |
| Outbox | Persistencia confiable de mensajes pendientes. |
| Queue Abstraction | Capa neutral para publicar mensajes a Storage Queues hoy y Service Bus mañana. |
| Azure Storage Queues | Broker inicial de bajo costo para jobs asíncronos. |
| Azure Functions | Procesamiento desacoplado: documentos, reportes, indexación, notificaciones, MCP batch, seguridad y mantenimiento. |
| Azure OpenAI / Foundry | Capa IA para MCP y tareas batch, con guardrails. |
| App Insights / Monitor | Observabilidad, logs, trazas y alertas. |
| Key Vault | Secretos, configuración sensible y certificados. |

## 3. Core modular

El core modular agrupa dominios altamente conectados y que requieren consistencia transaccional.

Módulos core:

- Tenant Management.
- Identity Bridge / User Management.
- Security & Authorization.
- Legal Knowledge.
- Document Metadata.
- Evidence.
- Processing Inventory / RAT.
- Workflow.
- Gap Management.
- Reporting Orchestration.
- Search Query.
- MCP Interactive.
- Audit.

Cada módulo debe tener:

- ownership de datos;
- contratos internos;
- servicios de aplicación;
- reglas de autorización;
- eventos de dominio;
- auditoría de acciones relevantes;
- pruebas propias.

## 4. Microservicios y Functions desacopladas

Se implementan fuera del core las capacidades con naturaleza asíncrona o intensiva.

Servicios iniciales:

| Servicio | Tipo | Motivo de separación |
|---|---|---|
| Document Processing | Azure Function | Procesa archivos, extrae texto y puede fallar/reintentarse. |
| Search Indexing | Azure Function | Indexa documentos y fuentes; puede cambiar de motor. |
| Report Generation | Azure Function | Genera Excel, PDF y evidence packs sin bloquear API. |
| Notification Delivery | Azure Function | Entrega correos/avisos de forma desacoplada. |
| MCP Batch Jobs | Azure Function | Ejecuta tareas IA no interactivas. |
| Security Jobs | Azure Function | Ejecuta checks programados y alertas. |
| Maintenance Jobs | Azure Function | Limpieza, expiración, reintentos y tareas programadas. |

## 5. Regla de dependencias

Los módulos core pueden comunicarse mediante servicios internos y eventos de dominio. Las Functions no deben modificar datos críticos sin validaciones de idempotencia y sin respetar ownership del módulo.

Reglas:

- Ningún módulo accede directamente a tablas de otro módulo sin contrato interno.
- Las Functions no toman decisiones de negocio finales.
- Las Functions actualizan estados de jobs o resultados propios.
- Las decisiones legales y aprobaciones humanas permanecen en el core.
- La mensajería se realiza mediante destinos lógicos.

## 6. Flujo de request transaccional

El flujo típico de una operación interactiva es:

1. Frontend invoca API.
2. API valida token.
3. Tenant resolver identifica tenant.
4. Security & Authorization evalúa permisos.
5. Módulo de negocio ejecuta operación.
6. PostgreSQL guarda cambios.
7. Audit registra acción.
8. Outbox registra mensajes derivados si corresponde.
9. API responde.
10. Publicador Outbox envía mensajes a colas.
11. Functions procesan trabajos asíncronos.

## 7. Flujo de documento

1. Usuario sube documento.
2. Core valida permisos y tenant.
3. Document Metadata registra documento y versión.
4. Archivo se almacena en Blob Storage.
5. Outbox registra mensaje a destino `document-processing`.
6. Publicador envía a Azure Storage Queue.
7. Document Processing Function extrae texto y actualiza estado.
8. Se emite mensaje lógico a `search-indexing`.
9. Search Indexing Function indexa contenido.
10. Usuario ve estado procesado/indexado.

## 8. Flujo de reporte

1. Usuario solicita reporte.
2. Core valida permisos.
3. Reporting Orchestration crea job.
4. Outbox registra destino `report-generation`.
5. Report Generation Function genera archivo.
6. Archivo queda en Blob.
7. Job se marca completado.
8. Notification Delivery avisa disponibilidad.
9. Descarga se realiza con control y auditoría.

## 9. Flujo MCP interactivo

1. Usuario realiza consulta.
2. Core valida permiso `mcp.ask` o `mcp.ask_with_tenant_context`.
3. MCP Interactive define tipo de consulta.
4. Search Query recupera fuentes legales y/o contexto tenant autorizado.
5. Risk Classifier clasifica riesgo.
6. MCP invoca modelo con contexto mínimo.
7. Citation Verifier valida referencias.
8. Respuesta se registra.
9. Si hay riesgo alto, se crea tarea de revisión humana.
10. Usuario recibe respuesta con límites explícitos.

## 10. Evolución a microservicios

La evolución no será por reescritura total, sino por extracción gradual.

Orden probable:

1. Document Processing ya separado.
2. Report Generation ya separado.
3. Notification Delivery ya separado.
4. Search Indexing ya separado.
5. MCP completo si el volumen crece.
6. DSAR, Consentimiento e Incidentes cuando entren como nuevos módulos.
7. Evidence sólo si tiene escala o necesidad enterprise.
8. RAT sólo cuando el dominio esté maduro y estable.

## 11. Límites de extracción

Para permitir extracción futura:

- cada módulo debe tener namespace propio;
- cada módulo debe tener ownership de entidades;
- los eventos deben estar versionados;
- los contratos deben ser estables;
- no se deben compartir modelos de persistencia indiscriminadamente;
- las queries cross-domain deben estar controladas mediante vistas/proyecciones;
- el Outbox debe usar destinos lógicos.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
