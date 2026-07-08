# Workflow y Gap Management — Paquete de implementación backend

## 1. Propósito

Este paquete permite implementar **tareas, revisiones, aprobaciones, comentarios, brechas y remediación** sin leer todo el set documental. Contiene las reglas mínimas de arquitectura, seguridad, datos, APIs, eventos, infraestructura Azure, pruebas y Definition of Done necesarias para el módulo.

## 2. Decisiones globales aplicadas

| Tema | Regla aplicada en este módulo |
|---|---|
| Backend | .NET 10. |
| Estilo | core modular. |
| Multi-tenant | Toda entidad funcional debe incluir `tenant_id` cuando pertenezca a un cliente. |
| Seguridad | Deny by default, RBAC + políticas contextuales, auditoría de acciones sensibles. |
| Mensajería inicial | Azure Storage Queues, no Service Bus por defecto. |
| Confiabilidad | Outbox para publicar mensajes derivados de transacciones. |
| Documentos | Blob Storage para archivos, PostgreSQL para metadata. |
| Secretos | Azure Key Vault y Managed Identity. |
| Observabilidad | App Insights, correlation id, logs estructurados y métricas por operación. |

## 3. Alcance incluido

El alcance incluido es: **orquestación humana y transformación de hallazgos en brechas accionables**.

Incluye además:

- validaciones de entrada;
- autorización por acción;
- auditoría funcional;
- trazabilidad por tenant;
- APIs necesarias para frontend futuro;
- eventos o mensajes cuando existan procesos asíncronos;
- pruebas unitarias, integración y seguridad mínima.

## 4. Alcance excluido

Queda fuera del módulo:

- decisiones comerciales o legales no documentadas;
- cambios globales de arquitectura;
- frontend definitivo;
- automatizaciones no especificadas;
- integración con Service Bus salvo gatillo aprobado;
- funcionalidades de módulos posteriores no declaradas como dependencia.

## 5. Responsabilidades

| Responsabilidad | Descripción |
|---|---|
| Dominio | Mantener consistencia de las entidades bajo su ownership. |
| Seguridad | Aplicar permisos, tenant isolation y auditoría. |
| Datos | Persistir sólo datos del módulo y no modificar ownership ajeno. |
| API | Exponer contratos estables para el frontend y otros módulos. |
| Eventos | Emitir mensajes versionados e idempotentes cuando corresponda. |
| Operación | Registrar métricas, errores y estados procesables. |

## 6. Límites de dominio

El módulo no debe acceder directamente a tablas de otros módulos para modificar estado. Las integraciones deben usar:

- servicios internos del core;
- contratos de lectura autorizados;
- eventos/mensajes;
- vistas o proyecciones explícitamente aprobadas.

Cualquier dependencia nueva debe registrarse en este paquete y en `22-matriz-dependencias-documentales.md`.

## 7. Roles y permisos

Roles relevantes:

- PlatformAdmin cuando aplique operación global;
- TenantOwner para administración del tenant;
- ComplianceAdmin para operación de cumplimiento;
- LegalReviewer para revisión normativa;
- ProcessOwner para datos de proceso;
- SecurityReviewer para controles de seguridad;
- ExternalConsultant con permisos restringidos;
- ReadOnlyAuditor para consulta auditada.

Permisos mínimos a revisar en `13-matriz-rbac-politicas-autorizacion.md`:

- lectura;
- creación;
- edición;
- aprobación o cierre si aplica;
- descarga/exportación si aplica;
- administración;
- auditoría.

Toda acción sensible debe registrar usuario, tenant, fecha, IP, user-agent, entidad afectada, resultado y correlation id.

## 8. Arquitectura del módulo

Tipo arquitectónico: **core modular**.

Reglas:

1. El módulo debe tener namespace propio.
2. Debe separar dominio, aplicación, infraestructura y persistencia.
3. No debe compartir modelos internos como contrato externo.
4. Debe exponer APIs o servicios internos estables.
5. Debe declarar eventos que produce y consume.
6. Debe ser extraíble a futuro si el dominio madura.

Si el módulo usa Azure Functions, la API principal sólo debe orquestar y registrar jobs; la Function ejecuta trabajo pesado.

## 9. Datos y ownership

Entidades principales:

`WorkflowTask, Review, Approval, Comment, ComplianceGap, RemediationTask`

Reglas de datos:

- `tenant_id` obligatorio en datos de cliente;
- `created_at`, `created_by`, `updated_at`, `updated_by` cuando aplique;
- soft delete para entidades funcionales salvo auditoría;
- índices por `tenant_id` y campos de búsqueda/filtro;
- versionado cuando el cambio tenga valor probatorio;
- ningún módulo externo modifica estas entidades directamente.

## 10. APIs

Cada API del módulo debe especificar:

- propósito;
- método y ruta;
- permiso requerido;
- request;
- response;
- errores;
- validaciones;
- auditoría;
- eventos emitidos;
- sensibilidad de datos.

Las APIs deben ser idempotentes cuando creen jobs o disparen procesos asíncronos.

## 11. Eventos, mensajes y asincronía

Si el módulo emite mensajes:

- usar Outbox;
- publicar hacia destino lógico, no hacia nombre físico acoplado;
- incluir `message_id`, `message_type`, `message_version`, `tenant_id`, `entity_id`, `correlation_id`, `created_at`;
- diseñar consumidores idempotentes;
- definir poison behavior;
- no incluir datos sensibles innecesarios en payload.

Colas físicas recomendadas si aplica:

- `document-processing-queue`;
- `search-indexing-queue`;
- `report-generation-queue`;
- `notification-queue`;
- `mcp-batch-queue`;
- `security-jobs-queue`;
- `maintenance-jobs-queue`.

## 12. Seguridad del módulo

Controles obligatorios:

1. Validación de tenant en toda operación.
2. Autorización por permiso y política contextual.
3. Deny by default.
4. Auditoría de acciones sensibles.
5. Logs sin datos personales innecesarios.
6. Cifrado en tránsito y reposo mediante servicios Azure.
7. Acceso a recursos Azure mediante Managed Identity.
8. Secretos en Key Vault.
9. Pruebas cross-tenant.
10. Revisión de exposición de datos en reportes, mensajes y logs.

## 13. Infraestructura Azure

Recursos típicos para este módulo:

- Azure App Service / Container App para el core si aplica;
- Azure Functions si hay trabajo asíncrono;
- PostgreSQL Flexible Server;
- Azure Blob Storage si maneja archivos;
- Azure Storage Queues si maneja jobs;
- Key Vault;
- Application Insights;
- Azure Monitor.

Cada recurso debe tener naming por ambiente y permisos mínimos.

## 14. Observabilidad

Debe registrar:

- inicio y fin de operación crítica;
- errores con correlation id;
- latencia por endpoint o Function;
- fallas de autorización;
- cantidad de jobs pendientes/fallidos si aplica;
- métricas de uso por tenant;
- acciones sensibles.

## 15. Testing mínimo

Pruebas obligatorias:

- unitarias de reglas de dominio;
- integración con PostgreSQL cuando persista datos;
- autorización por rol;
- cross-tenant;
- auditoría de acciones sensibles;
- idempotencia si usa colas;
- poison/retry si usa Functions;
- validaciones de request;
- pruebas de regresión de permisos.

## 16. Definition of Done

El módulo está terminado cuando:

- cumple APIs acordadas;
- datos y migraciones están aplicados;
- permisos están implementados;
- pruebas obligatorias pasan;
- auditoría registra acciones críticas;
- observabilidad mínima está activa;
- documentación del paquete está actualizada;
- no existen accesos cross-tenant conocidos;
- las dependencias están documentadas;
- los eventos/mensajes se procesan de forma idempotente si aplica.

## 17. Dependencias

Dependencias obligatorias:

- Foundation/Security para usuario, tenant y permisos;
- Audit para trazabilidad;
- Outbox/Queue si emite trabajo asíncrono;
- Key Vault y Managed Identity para acceso a recursos Azure.

Dependencias funcionales específicas deben registrarse antes de iniciar desarrollo.

## 18. Riesgos y controles

| Riesgo | Control |
|---|---|
| Implementación fuera de límites del módulo | Revisar límites de dominio antes de codificar. |
| Permisos inconsistentes | Convertir RBAC del paquete en pruebas. |
| Fuga cross-tenant | Pruebas obligatorias con tenants A/B. |
| Mensajes duplicados | Idempotencia por `message_id` y estado de job. |
| Datos sensibles en logs | Revisión de logging y pruebas de seguridad. |
| Dependencia circular | Revisión de dependencias antes de merge. |

## 19. Documentos que sí debe leer el equipo

Obligatorios:

- `21-guia-implementacion-por-modulo.md`
- `22-matriz-dependencias-documentales.md`
- este paquete
- `19-definition-of-done.md`

Consultar según necesidad:

- `11-modelo-datos-backend.md`
- `12-contratos-api-backend.md`
- `13-matriz-rbac-politicas-autorizacion.md`
- `16-contratos-eventos-mensajes.md`
- `17-blueprint-infraestructura-azure.md`
- `18-estrategia-testing-calidad.md`

## 20. Documentos no obligatorios para iniciar este módulo

No es obligatorio leer todos los documentos de visión, mercado, frontend o módulos no relacionados. Si el equipo detecta contradicción entre este paquete y un documento global, debe levantar una decisión de arquitectura antes de implementar.

---

## Trazabilidad de fuentes del paquete

Este paquete es autocontenido para implementación, pero su fundamento proviene de dos documentos base:

- `ley-datos-personales.pdf`: se usa para identificar el problema regulatorio-operativo, los elementos computables, los patrones de evidencia, trazabilidad, derechos, consentimiento, incidentes, EIPD, transferencias y tratamiento de datos que justifican el diseño del módulo.
- `ATLAS-Informe-Factibilidad.pdf`: se usa para justificar la priorización de Opción B, el alcance MVP, el enfoque modular, la decisión de no construir suite completa, el orden de implementación, los riesgos y la arquitectura técnica.

Toda regla del paquete debe clasificarse como una de estas categorías:

1. **Normativo-operativa:** deriva de una necesidad de cumplimiento o trazabilidad asociada a Ley 21.719.
2. **Producto/factibilidad:** deriva de la estrategia ATLAS Opción B.
3. **Arquitectura:** deriva de decisiones técnicas del proyecto.
4. **Futuro/no MVP:** se reconoce como extensión posterior y no debe bloquear la implementación del paquete.
