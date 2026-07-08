# 25 — Stack tecnológico de desarrollo local

## 1. Objetivo

Definir el stack tecnológico que permitirá desarrollar **Evidata** sin depender de Azure en el trabajo diario, manteniendo compatibilidad con el despliegue posterior en Azure.

La decisión central es adoptar un enfoque **local-first, cloud-compatible**: el desarrollador debe poder levantar la API, los workers, las Azure Functions y las dependencias de infraestructura en su máquina, usando herramientas reproducibles y sin consumir recursos cloud para programar, depurar o ejecutar pruebas de integración básicas.

## 2. Decisión vigente

El stack de desarrollo local queda definido así:

| Capa | Tecnología | Rol |
|---|---|---|
| Orquestación local de servicios | **.NET Aspire** | AppHost para levantar y observar API, workers, Azure Functions y dependencias locales. |
| Runtime backend | **.NET 10** | API, worker de outbox, servicios internos y Azure Functions isolated worker. |
| API principal | **ASP.NET Core .NET 10** | Core modular de Evidata. |
| Procesos asíncronos | **Azure Functions .NET isolated worker** | Procesamiento documental, reportes, notificaciones, indexación, trabajos de seguridad y mantenimiento. |
| Base transaccional local | **PostgreSQL en contenedor** | Persistencia local equivalente a Azure Database for PostgreSQL. |
| Blob y colas locales | **Azurite** | Emulación local de Azure Blob Storage y Azure Storage Queues. |
| Correo local | **Mailpit** | Captura de correos en desarrollo sin envíos reales. |
| Observabilidad local | **Logs estructurados + consola + Aspire Dashboard + Seq opcional** | Diagnóstico local de servicios, trazas y flujos distribuidos sin depender de Azure. |
| Observabilidad cloud | **Application Insights / Azure Monitor** | Telemetría oficial en dev-cloud, staging y producción, activada por configuración. |
| Pruebas de integración | **Testcontainers** | Levantar PostgreSQL/Azurite u otras dependencias para pruebas automatizadas. |
| Proveedor IA desarrollo | **Mock/Recorded Provider** | Evitar dependencia y costo de Azure OpenAI durante desarrollo diario. |
| Búsqueda inicial local | **PostgreSQL full-text / pgvector opcional** | Evitar dependencia de Azure AI Search en desarrollo inicial. |

## 3. Rol de .NET Aspire

.NET Aspire será la capa de **orquestación de desarrollo**, no el runtime productivo obligatorio.

Debe usarse para:

- levantar múltiples servicios con un solo comando;
- modelar dependencias entre API, workers, Functions, PostgreSQL, Azurite y Mailpit;
- centralizar variables de entorno locales;
- observar logs, health checks y trazas desde el Aspire Dashboard;
- evitar que cada desarrollador mantenga scripts distintos;
- mantener una topología local parecida al despliegue esperado.

No debe usarse para:

- acoplar el dominio a Aspire;
- reemplazar IaC productiva;
- definir reglas de negocio;
- asumir que todos los recursos productivos se publicarán automáticamente desde Aspire sin revisión;
- esconder dependencias reales detrás de configuración implícita no documentada.

## 4. Compatibilidad con Azure Functions

La decisión es usar **Azure Functions .NET isolated worker** y registrarlas en el AppHost de Aspire mediante la integración específica para Azure Functions.

Reglas obligatorias:

1. Las Functions deben usar el modelo **isolated worker**, no el modelo in-process.
2. Cada proyecto de Functions debe usar `FunctionsApplication.CreateBuilder(args)`.
3. Cada proyecto de Functions debe llamar `builder.AddServiceDefaults()` antes de construir el host.
4. En Aspire, las Functions deben registrarse con `AddAzureFunctionsProject<TProject>()`, no con `AddProject<TProject>()`.
5. La configuración local de las Functions debe provenir del AppHost siempre que sea posible.
6. `local.settings.json` debe quedar mínimo. Sólo debe contener `FUNCTIONS_WORKER_RUNTIME=dotnet-isolated` cuando el proyecto esté orquestado por Aspire.
7. No se deben configurar conexiones a Azurite manualmente en `local.settings.json` si Aspire las está inyectando.
8. No se debe registrar Application Insights directamente dentro del proyecto de Functions cuando se usa Aspire; la telemetría debe integrarse vía OpenTelemetry y Service Defaults.

## 5. Topología local recomendada

```text
Evidata.AppHost
  ├── Evidata.Api
  ├── Evidata.Worker.Outbox
  ├── Evidata.Functions.DocumentProcessing
  ├── Evidata.Functions.Reporting
  ├── Evidata.Functions.Notifications
  ├── Evidata.Functions.SearchIndexing
  ├── Evidata.Functions.Maintenance
  ├── PostgreSQL
  ├── Azurite Blob
  ├── Azurite Queues
  ├── Mailpit
  └── Seq opcional
```

## 6. Estructura recomendada de solución

```text
evidata-backend/
├── src/
│   ├── Evidata.Api/
│   ├── Evidata.Worker.Outbox/
│   ├── Evidata.ServiceDefaults/
│   ├── Evidata.AppHost/
│   └── Modules/
│       ├── TenantManagement/
│       ├── Identity/
│       ├── Security/
│       ├── Documents/
│       ├── Evidence/
│       ├── ProcessingInventory/
│       ├── Workflow/
│       ├── GapManagement/
│       ├── Reporting/
│       ├── Search/
│       ├── Mcp/
│       └── Audit/
├── functions/
│   ├── Evidata.Functions.DocumentProcessing/
│   ├── Evidata.Functions.Reporting/
│   ├── Evidata.Functions.Notifications/
│   ├── Evidata.Functions.SearchIndexing/
│   └── Evidata.Functions.Maintenance/
├── tests/
├── infra/
│   ├── local/
│   └── azure/
└── docs/
```

## 7. Configuración por ambiente

El mismo código debe correr en todos los ambientes. Lo que cambia es configuración.

| Ambiente | Orquestación | Storage | Queues | Auth | Email | Observabilidad | IA |
|---|---|---|---|---|---|---|---|
| Local | Aspire | Azurite | Azurite | LocalDev | Mailpit | Aspire Dashboard + consola + Seq opcional | Mock/Recorded |
| Dev-cloud | Azure | Azure Blob | Azure Storage Queues | Entra External ID | Proveedor real/sandbox | Application Insights | Azure OpenAI opcional |
| Staging | Azure | Azure Blob | Azure Storage Queues | Entra External ID | Proveedor real | Application Insights | Azure OpenAI |
| Producción | Azure | Azure Blob | Azure Storage Queues | Entra External ID | Proveedor real | Application Insights | Azure OpenAI |

Variables mínimas:

```text
EVIDATA_ENVIRONMENT=Local|DevCloud|Staging|Production
EVIDATA_AUTH_MODE=LocalDev|TestAuth|Entra
EVIDATA_STORAGE_PROVIDER=Azurite|AzureBlob
EVIDATA_QUEUE_PROVIDER=Azurite|AzureStorageQueues
EVIDATA_EMAIL_PROVIDER=Mailpit|Smtp|Provider
EVIDATA_AI_PROVIDER=Mock|Recorded|AzureOpenAI
EVIDATA_SEARCH_PROVIDER=Postgres|AzureAISearch
EVIDATA_OBSERVABILITY_PROVIDER=Local|Azure
EVIDATA_LOG_SINKS=Console,AspireDashboard,Seq|Console,ApplicationInsights
```

## 8. Seguridad local sin Entra

La seguridad funcional debe poder desarrollarse sin Entra External ID en el entorno local.

La decisión es usar un modo `LocalDev` para autenticación simulada y mantener la autorización real dentro del core:

```text
EVIDATA_AUTH_MODE=LocalDev
EVIDATA_ALLOW_DEV_AUTH_HEADERS=true
```

En local, el backend puede resolver el usuario mediante usuarios seed o headers dev-only:

```text
X-Evidata-Dev-User: compliance@evidata.local
X-Evidata-Dev-Tenant: tenant-demo-001
```

Reglas obligatorias:

1. `LocalDev` sólo puede ejecutarse cuando `EVIDATA_ENVIRONMENT=Local`.
2. `TestAuth` sólo puede usarse en pruebas automatizadas.
3. `Staging` y `Production` deben fallar al iniciar si `AUTH_MODE` no es `Entra`.
4. Los headers dev-only deben ser ignorados o rechazados fuera de local.
5. Las reglas reales de RBAC, permisos, tenant isolation y auditoría deben ejecutarse igual en local y en producción.
6. Las capas de aplicación no deben leer claims de Entra directamente; deben usar `CurrentUserContext`.

La especificación completa queda en `26-seguridad-desarrollo-local-sin-entra.md`.

## 9. Observabilidad local y producción

La decisión es usar una **capa común de observabilidad**, seleccionando el destino por configuración de ambiente y no por cambios de código.

| Ambiente | Destino de observabilidad | Regla |
|---|---|---|
| Local | Consola + Aspire Dashboard + Seq opcional | Optimizado para depuración local sin Azure. |
| Dev-cloud | Application Insights + Azure Monitor | Validación cloud con telemetría real. |
| Staging | Application Insights + Azure Monitor | Preproducción con la misma estrategia productiva. |
| Producción | Application Insights + Azure Monitor | Observabilidad oficial, alertas y retención controlada. |

Seq queda como **opcional recomendado**, no obligatorio. Su uso se justifica cuando los flujos distribuidos sean difíciles de seguir sólo con consola y Aspire Dashboard, por ejemplo:

- API → Outbox → Storage Queue → Function;
- procesamiento documental asíncrono;
- generación de reportes;
- reintentos e idempotencia;
- poison messages;
- trazabilidad por `correlationId`;
- diagnóstico cruzado entre API, worker y Functions.

Application Insights queda como herramienta oficial para ambientes Azure. No debe ser requisito para depurar localmente.

### 9.1 Configuración esperada

Local:

```text
EVIDATA_OBSERVABILITY_PROVIDER=Local
EVIDATA_LOG_SINKS=Console,AspireDashboard,Seq
SEQ_SERVER_URL=http://localhost:5341
```

Azure:

```text
EVIDATA_OBSERVABILITY_PROVIDER=Azure
EVIDATA_LOG_SINKS=Console,ApplicationInsights
APPLICATIONINSIGHTS_CONNECTION_STRING=<secret>
```

La variable `SEQ_SERVER_URL` sólo aplica cuando `Seq` esté incluido en `EVIDATA_LOG_SINKS`.

### 9.2 Reglas de implementación

1. El dominio y los módulos funcionales no deben depender de Seq ni de Application Insights.
2. Los logs y trazas deben emitirse mediante abstracciones estándar de .NET, Serilog/OpenTelemetry o la capa técnica aprobada.
3. La selección de sinks debe ser por configuración de ambiente.
4. El mismo binario debe poder desplegarse en local, dev-cloud, staging y producción.
5. El deploy a Azure no debe requerir eliminar código de Seq; simplemente no se configura el sink Seq.
6. Staging y producción deben usar Application Insights/Azure Monitor.
7. Seq nunca es dependencia productiva ni debe aparecer como recurso obligatorio de Azure.

### 9.3 Propiedades mínimas de logs estructurados

La aplicación debe emitir logs estructurados y trazas con propiedades comunes:

- `tenantId`;
- `correlationId`;
- `userId` cuando aplique;
- `module`;
- `operation`;
- `messageType`;
- `jobId`;
- `documentId` cuando aplique;
- `processingActivityId` cuando aplique;
- `environment`;
- `serviceName`;
- `requestId`;
- `traceId`.

No deben registrarse datos personales completos, contenido documental sensible, tokens, claves, secretos, payloads legales completos ni documentos subidos por clientes.

## 10. Uso de Docker Compose

Docker Compose puede mantenerse como soporte para dependencias locales o como alternativa mínima cuando no se quiera ejecutar Aspire.

Sin embargo, la fuente principal de orquestación local será `Evidata.AppHost`.

Regla práctica:

- Aspire coordina proyectos y dependencias.
- Docker provee contenedores.
- Docker Compose puede existir como respaldo operativo, pero no debe competir con AppHost como definición principal del entorno local.

## 11. Uso de Testcontainers

Las pruebas de integración no deben depender de una instancia Azure real.

Se recomienda usar Testcontainers para levantar dependencias efímeras:

- PostgreSQL;
- Azurite o emulador equivalente;
- Mailpit si una prueba necesita validar envío;
- Redis si se incorpora.

Esto permite ejecutar pruebas en CI sin depender de recursos Azure, salvo pruebas específicas de integración cloud.

## 12. Decisiones que quedan fuera del desarrollo local

No se adoptan como requisito del entorno local inicial:

- Kubernetes local;
- AKS;
- Service Bus real;
- Azure AI Search real;
- Azure OpenAI obligatorio;
- Entra External ID obligatorio para todos los desarrolladores;
- Application Insights como dependencia para depurar localmente.

## 13. Criterio de aceptación del entorno local

El entorno de desarrollo se considera correcto cuando un desarrollador nuevo puede:

1. clonar el repositorio;
2. instalar SDK .NET 10, Docker, Aspire y Azure Functions Core Tools;
3. ejecutar el AppHost;
4. levantar API, worker, Functions, PostgreSQL, Azurite y Mailpit;
5. crear un tenant local;
6. subir un documento a Blob local;
7. publicar un mensaje a una cola local;
8. procesarlo con una Function local;
9. observar logs/trazas;
10. ejecutar pruebas de integración sin Azure.

## 14. Decisión final

Evidata usará **.NET Aspire como orquestador principal de desarrollo local** y **Azure Functions .NET isolated worker** para procesos asíncronos. Azure no será requerido para el desarrollo diario. Azure quedará reservado para dev-cloud, staging y producción.
