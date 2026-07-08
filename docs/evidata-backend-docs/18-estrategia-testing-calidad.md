# 18 — Estrategia de testing y calidad

## 1. Propósito

Este documento define la estrategia de calidad para el backend de ATLAS Opción B. El sistema gestiona datos personales, evidencias, exportaciones y MCP contextual, por lo que las pruebas deben cubrir funcionalidad, seguridad, multi-tenant, asincronía, autorización y trazabilidad.

## 2. Principios

1. La seguridad multi-tenant se prueba, no se asume.
2. Las acciones sensibles deben tener pruebas de autorización y auditoría.
3. Los módulos core deben tener pruebas unitarias de dominio.
4. Los flujos críticos deben tener pruebas de integración.
5. Outbox y colas deben probar idempotencia y fallos.
6. Las Functions deben probar reintentos y poison handling.
7. Los reportes deben probar contenido y permisos.
8. MCP debe probar guardrails, citas, abstención y minimización.
9. Toda regresión de permisos es crítica.
10. No se liberan módulos sin Definition of Done.

## 3. Tipos de pruebas

| Tipo | Objetivo |
|---|---|
| Unitarias | Reglas de dominio, validaciones, políticas. |
| Integración | API + PostgreSQL + Storage/colas simuladas o reales controladas. |
| Contract tests | Contratos API y mensajes. |
| Security tests | Permisos, tenant isolation, acciones sensibles. |
| Async tests | Outbox, queues, Functions, idempotencia. |
| Data tests | Migraciones, constraints, versionado. |
| Report tests | Reportes y evidence packs. |
| MCP tests | Citas, riesgo, abstención, contexto. |
| Smoke tests | Despliegue y health checks. |
| Regression tests | Flujos críticos por release. |

## 4. Pruebas obligatorias por módulo

### 4.1 Tenant Management

- crear tenant;
- suspender tenant;
- bloquear operaciones con tenant suspendido;
- impedir acceso a tenant no asociado;
- auditar cambios de configuración.

### 4.2 Identity y Security

- usuario sin rol no accede;
- rol asignado permite acción esperada;
- cambio de rol genera auditoría;
- acción sensible requiere motivo;
- acceso cross-tenant se deniega;
- consultor externo sólo ve scope permitido.

### 4.3 Legal Knowledge

- crear fuente versionada;
- publicar versión;
- impedir modificación de versión publicada;
- consultar obligaciones por módulo;
- reindexación solicitada tras publicación.

### 4.4 Documents

- subir documento permitido;
- rechazar tipo/tamaño no permitido;
- generar DocumentUploaded;
- impedir descarga sin permiso;
- auditar descarga sensible;
- mantener versiones.

### 4.5 Evidence

- crear evidencia;
- asociar a tratamiento;
- impedir asociación cross-tenant;
- descargar con permiso;
- requerir motivo para Sensitive;
- generar evidence pack;
- preservar historial tras eliminación lógica.

### 4.6 RAT

- crear tratamiento Draft;
- validar completitud para revisión;
- generar flags de riesgo;
- impedir aprobación incompleta;
- aprobar con rol válido;
- crear versión inmutable;
- generar brechas sugeridas;
- exportar RAT;
- impedir edición directa de versión aprobada.

### 4.7 Workflow y Gaps

- asignar tarea;
- completar revisión;
- solicitar cambios;
- crear brecha;
- cerrar brecha con evidencia;
- impedir cierre sin permiso;
- aceptar riesgo sólo con rol elevado.

### 4.8 Outbox y colas

- mensaje se crea en misma transacción que operación;
- publicador marca Published;
- reintenta fallos;
- no duplica efectos;
- poison message se registra;
- destino lógico se resuelve correctamente.

### 4.9 Functions

- consume mensaje válido;
- rechaza mensaje inválido controladamente;
- procesa idempotente;
- actualiza job/estado;
- registra error sin datos sensibles;
- envía a poison tras umbral.

### 4.10 MCP

- no responde consulta normativa sin citas;
- clasifica riesgo alto;
- no usa contexto tenant sin permiso;
- minimiza contexto enviado;
- registra interacción;
- deriva HITL si corresponde;
- no certifica cumplimiento.

## 5. Pruebas cross-tenant

Casos mínimos:

1. Usuario de tenant A no lista tratamientos de tenant B.
2. Usuario de tenant A no descarga evidencia de tenant B.
3. Function no procesa mensaje con tenant inconsistente vs metadata.
4. Search tenant no devuelve resultados de otro tenant.
5. MCP no recupera contexto de otro tenant.
6. ReportJob no genera datos de otro tenant.
7. EvidencePack no mezcla entidades de tenants distintos.

## 6. Pruebas de autorización por estado

Para RAT:

- Draft editable por owner/compliance;
- InReview no editable por ProcessOwner salvo devolución;
- Approved no editable directamente;
- Archived sólo lectura;
- aprobación denegada a ProcessOwner;
- exportación denegada a rol sin permiso.

## 7. Pruebas de datos y migraciones

- migración limpia desde cero;
- rollback o estrategia de recuperación documentada;
- constraints de unicidad por tenant;
- índices creados;
- soft delete respetado;
- row_version o control de concurrencia probado;
- snapshots RAT inmutables.

## 8. Pruebas de reportes

- reporte RAT incluye campos obligatorios;
- reporte respeta filtros;
- reporte clasifica salida;
- reporte expira;
- descarga auditada;
- evidence pack incluye metadata, hashes y alcance.

## 9. Pruebas de observabilidad

La estrategia de pruebas debe validar que la observabilidad sea consistente entre local y Azure, cambiando sólo configuración.

Validaciones obligatorias:

- `correlationId` viaja API → Outbox → Queue → Function;
- `traceId`, `serviceName`, `environment` y `tenantId` se emiten cuando corresponde;
- errores 5xx quedan registrados en el sink configurado;
- en local, los eventos pueden observarse en consola/Aspire Dashboard y opcionalmente Seq;
- en dev-cloud/staging, los eventos llegan a Application Insights;
- poison messages generan alerta o evento verificable;
- acciones sensibles quedan en `AuditLog`;
- métricas de uso MCP se registran;
- intento cross-tenant dispara evento de seguridad;
- no se registran datos personales completos, documentos, tokens ni secretos.

Seq no es requisito de pruebas automatizadas. Puede usarse para depuración manual local, pero los tests no deben depender de que Seq esté levantado.

## 10. Criterios mínimos por release

No se libera a staging si:

- falla cualquier prueba cross-tenant;
- falla autorización de acción sensible;
- falla migración base;
- hay endpoints sin permisos;
- hay mensajes sin versionado;
- hay logs con datos sensibles conocidos;
- no existe auditoría en acciones críticas.

No se libera a producción si:

- no pasan smoke tests;
- no hay health checks;
- no hay alertas críticas;
- no hay backup de base;
- no hay rollback plan;
- no hay revisión de seguridad para cambios críticos.

## 11. Ambientes de prueba

| Ambiente | Uso |
|---|---|
| Local | Unitarias y desarrollo. |
| Integration | PostgreSQL/Storage/Queues controlados. |
| Staging | Validación end-to-end. |
| Production | Sólo smoke y monitoreo. |

## 12. Pendientes

1. Definir tooling exacto de pruebas .NET.
2. Definir cobertura mínima aceptable por módulo.
3. Definir dataset sintético de datos personales.
4. Definir estrategia de pruebas de carga inicial.
5. Definir si se ejecutarán pruebas DAST/SAST desde MVP.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.


## Desarrollo local y Testcontainers

Las pruebas automatizadas de integración no deben depender de recursos Azure reales salvo pruebas explícitas de integración cloud.

Decisión:

- usar Testcontainers para PostgreSQL y dependencias efímeras;
- usar Azurite o emulación equivalente para Blob/Queues;
- validar flujos API → Outbox → Queue → Function en entorno local;
- reservar pruebas contra Azure real para dev-cloud/staging;
- no exigir Application Insights, Entra External ID, Azure OpenAI, Azure AI Search ni Seq para pruebas locales base;
- cuando se pruebe observabilidad local, validar el contrato de logging estructurado, no la UI de Seq.

El stack de desarrollo local está definido en `25-stack-tecnologico-desarrollo-local.md`.


## Pruebas de seguridad local sin Entra

El uso de `LocalDev` y `TestAuth` exige pruebas específicas:

- `LocalDev` permite desarrollar escenarios de usuario/tenant sin Entra.
- `TestAuth` debe permitir construir pruebas reproducibles de autorización.
- Un usuario sin permiso debe recibir 403.
- Un usuario de un tenant no debe acceder a datos de otro tenant.
- Un usuario bloqueado no debe ejecutar acciones funcionales.
- Una acción sensible debe generar auditoría.
- Los headers dev-only deben ser rechazados fuera de ambiente local.
- La aplicación no debe iniciar en staging/producción si `AUTH_MODE` es `LocalDev` o `TestAuth`.
- Las Functions deben validar tenant, correlation id e idempotencia según el tipo de mensaje.
