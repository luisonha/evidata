# 07 — Microservicios y Azure Functions

## 1. Introducción

Este documento define los servicios que sí conviene desacoplar desde el MVP. Se implementan como Azure Functions consumiendo Azure Storage Queues. No reemplazan al core modular; lo complementan.

## 2. Principios comunes

Todos los servicios deben cumplir:

- idempotencia;
- procesamiento por mensajes versionados;
- tenant awareness;
- logging estructurado;
- no registrar datos sensibles completos;
- manejo de poison messages;
- reintentos controlados;
- Managed Identity cuando sea posible;
- acceso mínimo a recursos;
- actualización de estado observable.

---

# 3. Document Processing Function

## 3.1 Definición

Servicio asíncrono encargado de procesar documentos subidos al sistema.

## 3.2 Alcance

Incluye:

- lectura del archivo en Blob Storage;
- validación de tipo/tamaño;
- extracción de texto cuando sea posible;
- cálculo/verificación de hash técnico;
- normalización básica de contenido;
- generación de metadata de procesamiento;
- actualización de estado;
- publicación de mensaje a indexación.

No incluye:

- autorización final de usuario;
- modificación de metadata funcional no propia;
- decisión legal;
- clasificación jurídica definitiva;
- aprobación de evidencia.

## 3.3 Responsabilidades

- Procesar archivo sin bloquear la API.
- Registrar éxito o falla.
- Entregar contenido procesable a Search Indexing.
- Reintentar fallas transitorias.
- Marcar fallas definitivas.

## 3.4 Entrada

Mensaje lógico `DocumentUploaded` con:

- tenant id;
- document id;
- document version id;
- blob reference;
- content type;
- correlation id;
- sensitivity.

## 3.5 Salida

- estado procesado;
- texto extraído o referencia al texto;
- error si falla;
- mensaje `DocumentProcessed` o `DocumentProcessingFailed`.

## 3.6 Arquitectura

- Trigger: Azure Storage Queue.
- Storage: Blob Storage.
- Persistencia de estado: PostgreSQL mediante contrato controlado.
- Siguiente paso: Search Indexing Queue.

## 3.7 Seguridad

- Managed Identity.
- Acceso mínimo a Blob.
- Validación de tenant contra metadata.
- No loguear contenido completo.
- Control de tamaño.
- Manejo de archivos no soportados.
- Poison queue.

## 3.8 Infraestructura Azure

- Azure Function App.
- Storage Queue `document-processing-queue`.
- Blob Storage.
- PostgreSQL.
- App Insights.
- Key Vault.

---

# 4. Search Indexing Function

## 4.1 Definición

Servicio asíncrono que mantiene índices de búsqueda legal y tenant.

## 4.2 Alcance

Incluye:

- indexar fuentes legales;
- indexar documentos procesados;
- indexar metadata relevante;
- generar embeddings si se habilita;
- eliminar documentos del índice;
- reindexar por versión;
- registrar errores de indexación.

No incluye:

- responder búsquedas interactivas;
- aplicar permisos de usuario final;
- razonar con IA;
- aprobar contenido.

## 4.3 Responsabilidades

- Mantener índices actualizados.
- Separar contenido global y tenant.
- Permitir cambio futuro de motor.
- Emitir estado observable.

## 4.4 Entrada

- `DocumentProcessed`.
- `LegalSourcePublished`.
- `DocumentDeleted`.
- `TenantReindexRequested`.

## 4.5 Salida

- `SearchIndexUpdated`.
- `SearchIndexingFailed`.

## 4.6 Arquitectura

Inicialmente puede indexar en PostgreSQL full-text/pgvector. Azure AI Search queda como opción futura.

## 4.7 Seguridad

- Índices segregados por tenant o filtros estrictos.
- No loguear contenido sensible.
- Managed Identity.
- Reintentos y poison queue.
- Respeto a eliminación lógica.

## 4.8 Infraestructura Azure

- Azure Function App.
- Storage Queue `search-indexing-queue`.
- PostgreSQL o Azure AI Search futuro.
- Blob Storage.
- App Insights.

---

# 5. Report Generation Function

## 5.1 Definición

Servicio asíncrono encargado de generar reportes, exportaciones y evidence packs.

## 5.2 Alcance

Incluye:

- RAT Excel;
- RAT PDF si se habilita;
- brechas Excel;
- evidence packs;
- reportes ejecutivos;
- almacenamiento del resultado;
- actualización de job;
- publicación de notificación.

No incluye:

- autorización inicial;
- definición de permisos;
- modificación de datos fuente;
- decisión jurídica.

## 5.3 Responsabilidades

- Generar archivos sin bloquear API.
- Mantener estado del job.
- Controlar errores.
- Guardar resultados en Blob.
- Emitir evento de completitud.

## 5.4 Entrada

`ReportRequested` con:

- tenant id;
- report job id;
- report type;
- parameters reference;
- requested by;
- sensitivity.

## 5.5 Salida

- archivo en Blob;
- job Completed o Failed;
- `ReportCompleted` o `ReportFailed`.

## 5.6 Arquitectura

- Trigger: Storage Queue.
- Lectura: PostgreSQL mediante vistas/proyecciones.
- Escritura: Blob Storage y estado de job.

## 5.7 Seguridad

- Acceso mínimo.
- No loguear contenido.
- Archivos privados.
- Expiración si aplica.
- Tenant isolation.
- Poison queue.

## 5.8 Infraestructura Azure

- Azure Function App.
- Storage Queue `report-generation-queue`.
- Blob Storage.
- PostgreSQL.
- App Insights.

---

# 6. Notification Delivery Function

## 6.1 Definición

Servicio asíncrono para envío de notificaciones.

## 6.2 Alcance

Incluye:

- notificaciones de tarea;
- revisión solicitada;
- reporte listo;
- brecha vencida;
- alerta de seguridad;
- fallas operativas;
- recordatorios.

No incluye:

- reglas de negocio que originan la notificación;
- preferencias multicanal avanzadas;
- autorización funcional.

## 6.3 Responsabilidades

- Entregar mensajes.
- Aplicar plantillas.
- Registrar resultado.
- Reintentar fallas.
- Evitar incluir datos sensibles innecesarios.

## 6.4 Entrada

`NotificationRequested` con:

- tenant id;
- recipient id;
- template;
- parameters minimal;
- priority;
- correlation id.

## 6.5 Salida

- `NotificationSent`.
- `NotificationFailed`.

## 6.6 Seguridad

- No enviar datos sensibles en claro salvo necesidad justificada.
- Plantillas controladas.
- Auditoría de envíos críticos.
- Control anti-spam.
- Configuración por tenant.

## 6.7 Infraestructura Azure

- Azure Function App.
- Storage Queue `notification-queue`.
- Proveedor de correo pendiente de definición.
- App Insights.
- Key Vault.

---

# 7. MCP Batch Jobs Function

## 7.1 Definición

Servicio para procesos IA no interactivos.

## 7.2 Alcance

Incluye:

- verificación batch de citas;
- sugerencia de brechas;
- clasificación documental asistida;
- evaluación de calidad de respuestas;
- refresco de embeddings;
- revisión de interacciones MCP.

No incluye:

- chat interactivo;
- aprobación automática;
- modificación automática de tratamientos;
- certificación de cumplimiento.

## 7.3 Responsabilidades

- Ejecutar tareas IA controladas.
- Registrar resultados como sugerencias.
- Requerir revisión humana para cambios.
- Mantener trazabilidad.

## 7.4 Entrada

- `McpBatchJobRequested`.
- `McpCitationVerificationRequested`.
- `DocumentClassificationRequested`.

## 7.5 Salida

- sugerencia;
- resultado de verificación;
- error;
- tarea de revisión humana si aplica.

## 7.6 Seguridad

- Minimización de contexto.
- No loguear prompts completos sensibles.
- No aplicar cambios automáticamente.
- Tenant isolation.
- Control de costo por tenant.
- Alertas por uso anómalo.

## 7.7 Infraestructura Azure

- Azure Function App.
- Storage Queue `mcp-batch-queue`.
- Azure OpenAI / Foundry.
- PostgreSQL.
- App Insights.
- Key Vault.

---

# 8. Security Jobs Function

## 8.1 Definición

Servicio para tareas de seguridad asíncronas y programadas.

## 8.2 Alcance

Incluye:

- usuarios inactivos;
- permisos inconsistentes;
- descargas masivas;
- consultas MCP de alto riesgo;
- intentos cross-tenant;
- exportación de auditoría;
- alertas operativas.

No incluye:

- autorización en línea;
- autenticación;
- SIEM completo.

## 8.3 Responsabilidades

- Consumir eventos de seguridad.
- Ejecutar checks programados.
- Crear alertas.
- Registrar hallazgos.

## 8.4 Entrada

- `SensitiveActionPerformed`.
- `AccessDenied`.
- `CrossTenantAccessAttempted`.
- timer schedule.

## 8.5 Salida

- `SecurityAlertCreated`.
- `SecurityCheckCompleted`.
- notificaciones.

## 8.6 Seguridad

- Acceso mínimo.
- No exponer datos sensibles.
- Logs controlados.
- Managed Identity.
- Alertas ante fallas del job.

## 8.7 Infraestructura Azure

- Azure Function App.
- Storage Queue `security-jobs-queue`.
- Timer triggers.
- PostgreSQL.
- App Insights.

---

# 9. Maintenance Jobs Function

## 9.1 Definición

Servicio programado para mantenimiento operacional.

## 9.2 Alcance

Incluye:

- limpieza de jobs antiguos;
- expiración de enlaces temporales;
- revisión de reportes vencidos;
- reintentos Outbox;
- revisión de documentos pendientes;
- control de brechas vencidas;
- tareas de retención si se definen.

No incluye:

- borrado definitivo sin política aprobada;
- cambios regulatorios;
- decisiones de negocio.

## 9.3 Responsabilidades

- Mantener salud operativa.
- Detectar estados atascados.
- Emitir alertas.
- Ejecutar tareas programadas seguras.

## 9.4 Seguridad

- No borrar datos sin política.
- Auditar tareas destructivas.
- Managed Identity.
- Acceso mínimo.

## 9.5 Infraestructura Azure

- Azure Function App.
- Timer triggers.
- Storage Queue `maintenance-jobs-queue`.
- PostgreSQL.
- Blob Storage.
- App Insights.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.


---

# 10. Desarrollo local con .NET Aspire

Las Azure Functions de Evidata deben poder ejecutarse localmente como parte del AppHost de .NET Aspire.

## 10.1 Decisión técnica

- Modelo obligatorio: Azure Functions **.NET isolated worker**.
- Registro en AppHost: `AddAzureFunctionsProject<TProject>()`.
- No usar `AddProject<TProject>()` para Functions.
- Configuración local inyectada desde Aspire.
- `local.settings.json` mínimo, con `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` cuando corresponda.
- Host storage local provisto por Azurite a través de Aspire.

## 10.2 Implicancias

Esta decisión permite:

- levantar API, worker, Functions, PostgreSQL, Azurite y Mailpit con una sola orquestación;
- depurar flujos de cola sin desplegar a Azure;
- mantener Application Insights fuera del desarrollo diario;
- mantener la telemetría cloud mediante OpenTelemetry y Service Defaults;
- evitar configuraciones locales divergentes por desarrollador.

## 10.3 Límite

Aspire se usa para desarrollo y modelado de la topología. No reemplaza la revisión de infraestructura productiva ni las decisiones de hosting Azure para producción.
