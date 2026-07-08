# 03 — Infraestructura Azure

## 1. Objetivo

Definir una infraestructura Azure suficiente para implementar Evidata con bajo costo inicial, seguridad razonable, escalabilidad gradual y capacidad de evolución a servicios más avanzados, dejando explícito que el desarrollo diario se ejecuta en un stack local-first documentado en `25-stack-tecnologico-desarrollo-local.md`.

## 2. Principio de infraestructura

La infraestructura inicial debe priorizar:

- costo bajo;
- simplicidad operativa;
- seguridad multi-tenant;
- observabilidad suficiente;
- separación de ambientes;
- posibilidad de migrar componentes sin rediseño.

No se debe desplegar infraestructura enterprise innecesaria antes de validar pilotos pagados.

## 3. Recursos Azure iniciales

| Recurso | Uso | Requerido MVP |
|---|---|---|
| Azure App Service Linux o Container Apps | Hospedar Atlas API .NET 10 | Sí |
| Azure Database for PostgreSQL Flexible Server | Base transaccional | Sí, pendiente confirmación final |
| Azure Blob Storage | Documentos, evidencias, reportes | Sí |
| Azure Storage Queues | Mensajería asíncrona inicial | Sí |
| Azure Functions | Procesamiento asíncrono | Sí |
| Azure Key Vault | Secretos y configuración sensible | Sí |
| Application Insights | Telemetría de API y Functions en ambientes cloud | Sí |
| Azure Monitor | Alertas operativas | Sí |
| Entra External ID | Autenticación | Pendiente confirmación final |
| Azure OpenAI / Foundry | MCP y tareas IA | Sí, cuando entre MCP |
| Azure AI Search | Búsqueda avanzada | No inicial, opcional futuro |
| Azure Service Bus | Pub/sub avanzado | No inicial, opcional futuro |


## 3.1 Relación con desarrollo local

Azure no debe ser dependencia obligatoria para desarrollar diariamente.

El stack local oficial queda definido en `25-stack-tecnologico-desarrollo-local.md`:

- .NET Aspire como orquestador de desarrollo;
- PostgreSQL en contenedor;
- Azurite para Blob Storage y Storage Queues;
- Mailpit para correo;
- Azure Functions .NET isolated worker ejecutadas desde Aspire;
- Aspire Dashboard y consola para observabilidad local;
- Seq opcional sólo para depuración local de flujos distribuidos;
- Testcontainers para pruebas de integración.

En Azure, la observabilidad oficial será Application Insights/Azure Monitor. Seq no debe provisionarse como recurso Azure ni ser dependencia productiva.

Los ambientes Azure quedan reservados para `dev-cloud`, `staging` y `production`.

## 4. Hosting del core

La decisión final entre App Service Linux y Container Apps queda pendiente. Recomendación por defecto para MVP:

| Opción | Ventaja | Cuándo elegirla |
|---|---|---|
| App Service Linux | Simple, conocido, fácil de operar | MVP con una API principal |
| Container Apps | Mejor si se containeriza todo desde el inicio | Si se busca estandarizar despliegue con contenedores |
| Azure Functions para API principal | No recomendado como primera opción | Sólo si se decide serverless completo y se acepta complejidad de API distribuida |

La recomendación actual es **App Service Linux o Container Apps**, no Azure Functions para el API principal, porque el core requiere autorización contextual, workflows y consistencia transaccional.

## 5. Base de datos

Recurso recomendado: Azure Database for PostgreSQL Flexible Server.

Configuración inicial sugerida:

- instancia pequeña/mediana según carga esperada;
- backups automáticos;
- cifrado por defecto;
- firewall restringido;
- acceso desde API y Functions mediante red/control de identidad según presupuesto;
- extensiones full-text y eventualmente pgvector si se confirma.

Pendiente por cerrar: si PostgreSQL queda confirmado o se evalúa Cosmos DB por experiencia previa.

## 6. Blob Storage

Usos:

- documentos cargados;
- versiones documentales;
- evidencias adjuntas;
- reportes generados;
- evidence packs;
- textos procesados si se decide persistirlos fuera de PostgreSQL.

Reglas:

- contenedores privados;
- acceso mediante backend;
- URLs temporales controladas sólo cuando corresponda;
- metadata de tenant obligatoria;
- path lógico por tenant;
- auditoría de descargas;
- no exponer blobs directamente sin autorización.

## 7. Azure Storage Queues

Colas iniciales:

| Cola | Consumidor |
|---|---|
| `document-processing-queue` | Document Processing Function |
| `search-indexing-queue` | Search Indexing Function |
| `report-generation-queue` | Report Generation Function |
| `notification-queue` | Notification Delivery Function |
| `mcp-batch-queue` | MCP Batch Jobs Function |
| `security-jobs-queue` | Security Jobs Function |
| `maintenance-jobs-queue` | Maintenance Jobs Function |

Además deben existir colas poison por responsabilidad o un mecanismo equivalente de errores operativos.

## 8. Azure Functions

Functions iniciales:

- Document Processing.
- Search Indexing.
- Report Generation.
- Notification Delivery.
- MCP Batch Jobs.
- Security Jobs.
- Maintenance Jobs.

Reglas:

- Managed Identity siempre que sea posible;
- acceso mínimo a recursos;
- logging estructurado;
- no registrar datos personales en logs;
- idempotencia obligatoria;
- reintentos controlados;
- manejo de poison messages.

## 9. Key Vault

Debe contener:

- cadenas de conexión si no se usan identidades administradas;
- claves de servicios externos;
- configuración sensible de OpenAI/Foundry;
- secretos de integración futura;
- certificados si aplica.

Reglas:

- acceso por Managed Identity;
- separación por ambiente;
- rotación planificada;
- prohibido guardar secretos en repositorio;
- prohibido guardar secretos en configuración plana no protegida.

## 10. Observabilidad

En ambientes Azure, Application Insights y Azure Monitor deben capturar:

- latencia API;
- errores por módulo;
- fallas de autorización;
- intentos cross-tenant;
- errores de Functions;
- mensajes fallidos;
- jobs atascados;
- consumo MCP;
- generación de reportes;
- indexación fallida;
- descargas de evidencia.

Métricas mínimas:

- tasa de errores API;
- duración de requests;
- número de mensajes pendientes por cola;
- número de poison messages;
- duración de jobs;
- fallas MCP;
- consultas MCP de alto riesgo;
- reportes generados;
- documentos procesados;
- tratamientos aprobados.

## 11. Red y acceso

Para MVP de bajo costo:

- restringir acceso por configuración de App Service/Container Apps;
- storage privado mediante backend;
- PostgreSQL con firewall controlado;
- Key Vault con acceso restringido;
- separar ambientes.

Para etapa enterprise futura:

- private endpoints;
- VNet integration;
- WAF/Front Door;
- API Management;
- Service Bus;
- políticas avanzadas de red.

## 12. Ambientes

Mínimo recomendado:

| Ambiente | Uso |
|---|---|
| local | Desarrollo diario sin Azure, orquestado por .NET Aspire |
| dev-cloud | Validación técnica compartida en Azure |
| staging | Validación funcional y demos controladas |
| prod | Clientes reales |

Ideal:

| Ambiente | Uso |
|---|---|
| dev | Desarrollo |
| test | QA automatizado/integración |
| staging | Preproducción |
| prod | Producción |

Decisión pendiente: cantidad definitiva de ambientes.

## 13. Infraestructura futura

Se incorporará Service Bus sólo cuando existan gatillos reales. Se incorporará Azure AI Search sólo si PostgreSQL full-text/pgvector deja de ser suficiente o si MCP/búsqueda necesita capacidades avanzadas.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
