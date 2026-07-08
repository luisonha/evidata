# 17 — Blueprint de infraestructura Azure

## 1. Propósito

Este documento define el blueprint de infraestructura Azure para implementar el backend de ATLAS Opción B con bajo costo inicial y capacidad de evolución.

No reemplaza Bicep, Terraform ni scripts IaC. Define los recursos, responsabilidades, configuración y criterios que IaC debe implementar.

## 2. Decisiones de infraestructura vigentes

| Área | Decisión MVP |
|---|---|
| API principal | Pendiente: App Service Linux o Container Apps. Recomendación inicial: App Service Linux por simplicidad. |
| Backend | .NET 10 |
| Base de datos | PostgreSQL Flexible Server, pendiente confirmación final. |
| Mensajería | Azure Storage Queues. |
| Archivos | Azure Blob Storage privado. |
| Identidad usuarios | Entra External ID, pendiente confirmación final. |
| Identidad recursos | Managed Identity. |
| Secretos | Azure Key Vault. |
| Observabilidad | Application Insights + Log Analytics + Azure Monitor. Seq no forma parte de Azure ni de producción. |
| IA | Azure OpenAI / Foundry, proveedor intercambiable. |
| Search | PostgreSQL full-text/pgvector inicial; Azure AI Search opcional futuro. |

## 3. Ambientes

Mínimo recomendado:

| Ambiente | Propósito | Recursos dedicados |
|---|---|---|
| dev | Desarrollo compartido | Sí, con SKUs mínimos. |
| staging | Validación preproductiva | Sí, similar a prod en configuración. |
| prod | Producción | Sí, aislado. |

Opcional:

- test automatizado efímero;
- sandbox para demos;
- pilot por cliente enterprise si se requiere aislamiento.

## 4. Naming convention

Formato sugerido:

| Recurso | Patrón |
|---|---|
| Resource Group | `rg-atlas-{env}-{region}` |
| App Service | `app-atlas-api-{env}` |
| Function App | `func-atlas-{capability}-{env}` |
| Storage Account | `statlas{env}{suffix}` |
| PostgreSQL | `psql-atlas-{env}` |
| Key Vault | `kv-atlas-{env}` |
| App Insights | `appi-atlas-{env}` |
| Log Analytics | `log-atlas-{env}` |

El naming definitivo debe cumplir restricciones de Azure por recurso.

## 5. Resource groups

MVP puede usar un resource group por ambiente. En producción madura puede separarse:

- core compute;
- data;
- observability;
- security;
- networking.

Para MVP de bajo costo, mantener un resource group por ambiente reduce complejidad.

## 6. Recursos core

### 6.1 Atlas API

| Aspecto | Definición |
|---|---|
| Recurso recomendado | Azure App Service Linux o Container Apps. |
| Runtime | .NET 10. |
| Escalamiento inicial | 1 instancia staging/prod. |
| Escalamiento futuro | Horizontal según CPU, memoria, latencia. |
| Identidad | System-assigned Managed Identity. |
| Acceso secretos | Key Vault. |

Recomendación MVP: App Service Linux por menor complejidad operativa.

### 6.2 PostgreSQL Flexible Server

| Aspecto | Definición |
|---|---|
| Uso | Base transaccional. |
| Aislamiento | Base compartida con `tenant_id`, salvo decisión contraria. |
| Backups | Retención configurada por ambiente. |
| Acceso | Restringido a API/Functions. |
| Extensiones | Evaluar pgvector si search inicial lo requiere. |

### 6.3 Storage Account

Usos:

- Blob Storage para documentos;
- Storage Queues;
- artefactos de reportes;
- payloads grandes si no caben en cola;
- texto extraído si se decide almacenar fuera de DB.

Contenedores sugeridos:

| Contenedor | Uso |
|---|---|
| `documents` | Documentos cargados. |
| `document-text` | Texto extraído si aplica. |
| `reports` | Reportes generados. |
| `evidence-packs` | Paquetes de evidencia. |
| `legal-sources` | Fuentes legales administradas. |

Colas sugeridas:

- document-processing-queue;
- document-processing-poison;
- search-indexing-queue;
- search-indexing-poison;
- report-generation-queue;
- report-generation-poison;
- notification-queue;
- notification-poison;
- mcp-batch-queue;
- mcp-batch-poison;
- security-jobs-queue;
- security-jobs-poison;
- maintenance-jobs-queue;
- maintenance-jobs-poison.

## 7. Azure Functions

Function Apps sugeridas para MVP:

| Function App | Funciones | Motivo separación |
|---|---|---|
| `func-atlas-documents-{env}` | Document Processing | Procesa archivos y puede fallar/reintentar. |
| `func-atlas-indexing-{env}` | Search Indexing | Cambia de proveedor y escala distinto. |
| `func-atlas-reports-{env}` | Report Generation | Genera archivos pesados. |
| `func-atlas-notifications-{env}` | Notification Delivery | Entrega asíncrona. |
| `func-atlas-jobs-{env}` | MCP Batch, Security, Maintenance | Jobs no interactivos. |

Para reducir costo, varias funciones pueden convivir en menos Function Apps al inicio, siempre que mantengan separación lógica.

## 8. Key Vault

Debe almacenar:

- connection strings si no se resuelven con identidad;
- secretos de proveedores externos;
- claves de IA;
- configuración de correo;
- certificados;
- claves de cifrado si se define CMK.

Reglas:

1. Ningún secreto en código.
2. Ningún secreto en App Settings sin Key Vault reference si aplica.
3. Acceso mediante Managed Identity.
4. Separación por ambiente.
5. Auditoría de acceso habilitada si el presupuesto lo permite.

## 9. Identidades administradas y RBAC Azure

| Identidad | Permisos mínimos |
|---|---|
| Atlas API | Leer Key Vault, acceder PostgreSQL, Blob necesario, colas para publicar. |
| Document Functions | Leer Blob documentos, escribir texto/estado, leer cola documentos. |
| Report Functions | Leer PostgreSQL controlado, escribir Blob reportes, leer cola reportes. |
| Indexing Functions | Leer texto/fuentes, escribir índice. |
| Notification Functions | Leer cola notificaciones, acceder proveedor correo. |

## 10. Red

MVP de bajo costo:

- recursos públicos restringidos por configuración y credenciales;
- Storage privado a nivel de acceso lógico;
- PostgreSQL con firewall limitado;
- HTTPS obligatorio.

Evolución recomendada:

- Private endpoints;
- VNet integration para API/Functions;
- restricciones de red PostgreSQL/Storage;
- WAF/Front Door si hay exposición pública relevante;
- segmentación por ambientes.

## 11. Observabilidad

La observabilidad cloud se implementa con **Application Insights, Log Analytics y Azure Monitor**.

Seq queda explícitamente fuera del blueprint Azure: es una herramienta opcional de desarrollo local y no debe provisionarse en dev-cloud, staging ni producción.

Recursos Azure:

- Application Insights;
- Log Analytics;
- Azure Monitor alerts;
- dashboards por ambiente.

Reglas:

1. Application Insights es obligatorio en dev-cloud, staging y producción.
2. La conexión a Application Insights debe resolverse por configuración/secretos de ambiente.
3. El código no debe depender directamente de Application Insights desde dominio o módulos funcionales.
4. Los mismos logs estructurados emitidos en local deben poder enviarse a Application Insights en Azure.
5. Seq no debe ser recurso IaC de Azure ni requisito operativo de producción.

Métricas mínimas:

- latencia API;
- tasa de errores API;
- errores autorización;
- intentos cross-tenant;
- mensajes outbox pendientes;
- mensajes poison;
- jobs report failed;
- document processing failed;
- costo IA estimado;
- consultas MCP;
- exportaciones masivas;
- uso por tenant.

## 12. Alertas mínimas

| Alerta | Severidad |
|---|---|
| API 5xx sostenido | Alta |
| PostgreSQL no disponible | Crítica |
| Outbox acumulado | Alta |
| Poison messages > umbral | Alta |
| Cross-tenant access attempt | Crítica |
| Descarga masiva de evidencia | Alta |
| Costo IA supera umbral | Media/Alta |
| Report generation failures | Media |
| Document processing failures | Media |

## 13. Costos esperados MVP

Rango mensual objetivo para arquitectura con Storage Queues:

| Escenario | Rango |
|---|---:|
| Dev/Staging liviano | USD 100–250 por ambiente si se controla uso. |
| Producción MVP | USD 350–700. |
| Producción con Azure AI Search S1 | Sumar costo fijo relevante. |
| Producción con mayor observabilidad/logs | Puede subir materialmente. |

## 14. Criterios de aceptación infraestructura

1. Ambientes separados.
2. API desplegable con CI/CD.
3. PostgreSQL accesible sólo desde componentes autorizados.
4. Storage con contenedores y colas definidas.
5. Key Vault integrado.
6. Managed Identity configurada.
7. App Insights recibiendo telemetría.
8. Alertas mínimas activas.
9. Outbox publisher operativo.
10. Una Function consumiendo una cola en prueba end-to-end.

## 15. Pendientes

1. Confirmar región Azure.
2. Confirmar App Service vs Container Apps.
3. Confirmar IaC: Bicep, Terraform o Azure Developer CLI.
4. Confirmar PostgreSQL SKU inicial.
5. Confirmar si se habilita pgvector desde MVP.
6. Confirmar proveedor de correo.
7. Confirmar política Private Endpoint para MVP.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.


## Relación con stack local

Este blueprint describe recursos Azure para ambientes cloud. No debe interpretarse como requisito para que un desarrollador pueda trabajar diariamente.

La topología local se define en `25-stack-tecnologico-desarrollo-local.md` y usa:

- .NET Aspire;
- PostgreSQL local;
- Azurite;
- Mailpit;
- Azure Functions isolated worker;
- Aspire Dashboard y logs estructurados.

La equivalencia local/cloud debe mantenerse por abstracciones de configuración y no por cambios de código.


## Anexo — Identidad local vs Azure

La infraestructura Azure no debe incluir componentes para `LocalDev` ni `TestAuth`. Esos modos existen sólo para desarrollo y pruebas automatizadas.

En Azure, la identidad productiva será Entra External ID. Las validaciones mínimas de despliegue deben impedir que una configuración cloud quede con:

```text
EVIDATA_AUTH_MODE=LocalDev
EVIDATA_AUTH_MODE=TestAuth
EVIDATA_ALLOW_DEV_AUTH_HEADERS=true
```

La configuración esperada en ambientes Azure es:

```text
EVIDATA_AUTH_MODE=Entra
EVIDATA_ALLOW_DEV_AUTH_HEADERS=false
```
