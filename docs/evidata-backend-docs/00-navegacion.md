# Evidata Backend — Documento de navegación

## Estado del paquete

Versión documental: **0.9 — seguridad local sin Entra y stack de desarrollo local**  
Foco: **backend primero**  
Formato: Markdown  
Alcance: Evidata, con ATLAS como proyecto técnico interno y arquitectura modular evolutiva hacia servicios desacoplados.

La versión 0.3 corrige el principal gap detectado en la revisión de v0.2: **para implementar un módulo no debe ser necesario leer todo el paquete completo**.  
Para eso se agregan **paquetes de implementación por módulo**, cada uno con arquitectura, alcance, límites, seguridad, infraestructura Azure, datos, APIs, eventos, pruebas, DoD y dependencias documentales mínimas.

## Objetivo del paquete

El objetivo no es sólo describir arquitectura. El objetivo es dejar una **especificación técnica ejecutable** que permita a un equipo backend desarrollar Evidata por módulos, con seguridad multi-tenant, bajo control de costos y con posibilidad real de evolucionar hacia microservicios sin reescritura traumática.

El documento debe servir para:

1. Decidir qué se construye en el core .NET 10.
2. Decidir qué se desacopla desde el MVP como Azure Functions.
3. Definir cómo se comunican los módulos usando Outbox y Azure Storage Queues.
4. Establecer seguridad, permisos, auditoría y aislamiento tenant.
5. Especificar datos, APIs, eventos, reportes, testing y criterios de aceptación.
6. Permitir que cada equipo implemente un módulo leyendo un **paquete acotado**, sin sacrificar calidad.
7. Guiar la implementación backend antes de cerrar el frontend.

## Cómo usar esta documentación

### Lectura mínima por perfil

| Perfil | Lectura obligatoria inicial |
|---|---|
| Arquitecto backend | 00, 01, 02, 03, 04, 05, 21, 22, 25 |
| Desarrollador de un módulo | 21, 22, paquete 30-39 correspondiente, 19 |
| DevOps / Cloud | 03, 05, 17, 25, paquete 30-39 correspondiente |
| Seguridad / Compliance técnico | 04, 13, 25, 26, 21, 22, paquete 30-39 correspondiente |
| QA | 18, 19, paquete 30-39 correspondiente |
| Product Owner técnico | 00, 01, 09, 20, paquete del módulo correspondiente |

### Regla de implementación por módulo

Para implementar un módulo concreto se debe leer:

1. `21-guia-implementacion-por-modulo.md`
2. `22-matriz-dependencias-documentales.md`
3. El paquete del módulo correspondiente (`30` a `39`)
4. `19-definition-of-done.md`

Sólo se deben leer documentos globales adicionales si la matriz del módulo lo exige explícitamente.

## Bloque A — Dirección y arquitectura

1. **01-vision-y-decisiones.md**  
   Define visión, alcance, decisiones vigentes y reglas de evolución.

2. **02-arquitectura-general-backend.md**  
   Describe el core modular .NET 10, Azure Functions, colas, límites de módulos y ruta a microservicios.

3. **03-infraestructura-azure.md**  
   Resume la infraestructura Azure para MVP y remite al blueprint detallado.

4. **04-seguridad-transversal.md**  
   Define identidad, tenant isolation, autorización, auditoría, acciones sensibles y seguridad MCP.

5. **05-mensajeria-asincronia-y-outbox.md**  
   Define Storage Queues, Outbox, idempotencia, poison messages y gatillos para Service Bus.

## Bloque B — Dominios y servicios

6. **06-modulos-core-backend.md**  
   Define módulos del core: Tenant, Identity Bridge, Security, Legal Knowledge, Documents, Evidence, RAT, Workflow, Gaps, Reporting Orchestration, Search, MCP Interactive y Audit.

7. **07-microservicios-y-functions.md**  
   Define Azure Functions desacopladas: Document Processing, Search Indexing, Report Generation, Notification Delivery, MCP Batch, Security Jobs y Maintenance Jobs.

8. **08-modelo-operativo-datos-eventos.md**  
   Define ownership de datos, eventos, integración y reglas operativas.

## Bloque C — Especificación implementable global

9. **11-modelo-datos-backend.md**  
   Define entidades, campos, ownership, relaciones, índices, versionado, soft delete y tenant isolation.

10. **12-contratos-api-backend.md**  
    Define contratos API por módulo: propósito, permisos, request, response, errores, eventos y auditoría.

11. **13-matriz-rbac-politicas-autorizacion.md**  
    Define roles, permisos, matriz de acciones y políticas contextuales.

12. **14-especificacion-rat-mvp.md**  
    Define en profundidad el módulo RAT: campos, reglas, estados, validaciones, versionado, brechas y exportaciones.

13. **15-especificacion-evidence-mvp.md**  
    Define evidencia: tipos, asociaciones, estados, seguridad, descargas, evidence packs y retención.

14. **16-contratos-eventos-mensajes.md**  
    Define contratos de eventos y mensajes para Storage Queues, Outbox, idempotencia y reprocesamiento.

15. **17-blueprint-infraestructura-azure.md**  
    Define recursos Azure, ambientes, naming, RBAC, identidades, secretos, redes, costos y configuración.

16. **18-estrategia-testing-calidad.md**  
    Define pruebas unitarias, integración, seguridad, cross-tenant, colas, Functions, reportes, MCP y regresión.

17. **19-definition-of-done.md**  
    Define criterios de aceptación por fase, módulo y entregable técnico.

18. **20-backlog-backend-fases.md**  
    Define épicas, features, historias técnicas y dependencias backend.

19. **25-stack-tecnologico-desarrollo-local.md**  
    Define stack local-first para desarrollo: .NET Aspire, Azure Functions isolated worker, PostgreSQL, Azurite, Mailpit, observabilidad local, Testcontainers y seguridad local sin Entra.

20. **26-seguridad-desarrollo-local-sin-entra.md**  
    Define cómo desarrollar autenticación simulada, autorización, RBAC, tenant isolation y auditoría sin depender de Entra en local, dejando Entra obligatorio para ambientes Azure.

## Bloque D — Implementación modular autocontenida

20. **21-guia-implementacion-por-modulo.md**  
    Explica cómo implementar módulos sin leer todo el paquete completo, qué debe contener cada paquete y cómo mantener consistencia global.

21. **22-matriz-dependencias-documentales.md**  
    Indica qué documentos debe leer cada equipo según el módulo que implementará.

22. **30-paquete-foundation-seguridad-auditoria.md**  
    Paquete autocontenido para Tenant Management, Identity Bridge, Security & Authorization y Audit.

23. **31-paquete-legal-knowledge.md**  
    Paquete autocontenido para fuentes legales, obligaciones y taxonomías.

24. **32-paquete-documentos-procesamiento.md**  
    Paquete autocontenido para Document Metadata y Document Processing Function.

25. **33-paquete-evidence.md**  
    Paquete autocontenido para Evidence, Evidence Links, descargas y Evidence Pack orchestration.

26. **34-paquete-rat-processing-inventory.md**  
    Paquete autocontenido para RAT / Processing Inventory, versionado, aprobación y flags.

27. **35-paquete-workflow-gap-management.md**  
    Paquete autocontenido para Workflow y Gap Management.

28. **36-paquete-reporting.md**  
    Paquete autocontenido para Reporting Orchestration y Report Generation Function.

29. **37-paquete-search-indexing.md**  
    Paquete autocontenido para Search Query y Search Indexing Function.

30. **38-paquete-mcp-contextual.md**  
    Paquete autocontenido para MCP Interactive y MCP Batch Jobs.

31. **39-paquete-notificaciones-maintenance-jobs.md**  
    Paquete autocontenido para Notification Delivery, Security Jobs y Maintenance Jobs.

## Bloque E — Control de avance

32. **09-plan-implementacion-backend.md**  
    Ordena la implementación por fases y sprints.

33. **10-registro-de-decisiones-arquitectonicas.md**  
    Registro de decisiones vigentes (ADR liviano): qué se decidió, por qué y qué documentos desarrollan cada definición.

34. **99-evaluacion-autocritica.md**  
    Evalúa fortalezas, debilidades, suficiencia y riesgos de la versión vigente del paquete.

## Decisiones vigentes consolidadas

| Tema | Decisión vigente |
|---|---|
| Backend | .NET 10 |
| Estilo | Core modular + servicios asíncronos desacoplados |
| Microservicios completos | No desde el día 1, salvo capacidades naturalmente asíncronas |
| Mensajería inicial | Azure Storage Queues |
| Mensajería futura | Azure Service Bus sólo con gatillos concretos |
| Patrón confiabilidad | Outbox desde el MVP |
| Base transaccional | PostgreSQL, pendiente confirmación final |
| Documentos | Azure Blob Storage |
| Identidad productiva | Entra External ID en Azure; LocalDev/TestAuth sólo para desarrollo y pruebas |
| Secretos | Azure Key Vault |
| MCP comercial | Al final de Opción B, conectado al RAT y evidencias |
| Seguridad | Identidad/secretos separados; autorización contextual en core |
| Frontend | Se define después; backend primero |
| Desarrollo local | .NET Aspire como orquestador principal |
| Seguridad local | LocalDev/TestAuth sin Entra; autorización real en core |
| Azure Functions local | .NET isolated worker registradas con AddAzureFunctionsProject |
| Infra local | PostgreSQL + Azurite + Mailpit; Seq opcional |
| Observabilidad local/cloud | Logs estructurados comunes; consola + Aspire Dashboard + Seq opcional en local; Application Insights/Azure Monitor en Azure |
| Dependencia Azure en desarrollo diario | No requerida |

## Regla de mantenimiento documental

Toda modificación de arquitectura, seguridad, datos, APIs, eventos, infraestructura o alcance debe actualizar el archivo correspondiente. Las decisiones no deben quedar sólo en conversaciones sueltas; deben registrarse en `10-registro-de-decisiones-arquitectonicas.md` cuando cambien arquitectura, seguridad, infraestructura, persistencia, mensajería u observabilidad. El documento de navegación debe reflejar siempre el estado vigente del paquete.

## Regla nueva de v0.3

Cada paquete de módulo debe ser suficientemente autocontenido para permitir implementación responsable sin obligar a leer todos los documentos globales. Cuando un paquete dependa de una decisión global, debe copiar la regla operativa mínima y referenciar el documento fuente para profundización.

## Trazabilidad de fuentes

- `23-matriz-trazabilidad-fuentes.md` — matriz que explica cómo se usan los documentos fuente entregados: `ley-datos-personales.pdf`, `ATLAS-Informe-Factibilidad.pdf` y `construccion-de-marca.pdf`.
- `24-identidad-producto-y-marca.md` — definición oficial de nombre de producto, descriptor de marca, uso de ATLAS y límites de naming.
