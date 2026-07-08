# 09 — Plan de implementación backend

## 1. Objetivo

Definir el plan de construcción backend por fases, permitiendo avanzar por módulos, validar el core, desacoplar procesos asíncronos y cerrar Opción B antes de pasar al diseño detallado de frontend.

## 2. Enfoque

La implementación debe avanzar en capas:

1. Base técnica.
2. Seguridad y tenant.
3. Legal/documental.
4. Evidencias.
5. RAT.
6. Workflow y brechas.
7. Reportes y búsqueda.
8. MCP contextual.

Cada fase debe cerrar API, persistencia, seguridad, auditoría, eventos y pruebas mínimas.

## 3. Fase 0 — Preparación técnica

## 3.1 Objetivo

Crear la base de ingeniería del backend.

## 3.2 Alcance

- Solución .NET 10.
- Estructura modular.
- PostgreSQL inicial, si se confirma.
- Blob Storage.
- Storage Queues.
- Key Vault.
- App Insights.
- CI/CD.
- Ambientes.
- Outbox base.
- Queue abstraction.
- Convenciones de eventos.

## 3.3 Entregables

- Repositorio backend.
- Pipeline inicial.
- Infraestructura base.
- API health check.
- Conexión a base.
- Logging estructurado.
- Outbox técnico.
- Publicador a Storage Queues.

## 3.4 Criterio de aceptación

El backend despliega en ambiente no productivo, registra logs, conecta con base, publica un mensaje de prueba a una cola mediante abstracción y ejecuta una Function de prueba.

---

# 4. Fase 1 — Core SaaS y seguridad base

## 4.1 Objetivo

Habilitar multi-tenancy, usuarios, roles, permisos, tenant guard y auditoría.

## 4.2 Módulos

- Tenant Management.
- Identity Bridge.
- Security & Authorization.
- Audit.
- Security Jobs inicial.

## 4.3 Entregables

- Crear tenant.
- Configurar tenant.
- Invitar usuario.
- Asignar rol.
- Validar token externo.
- Resolver tenant.
- Evaluar permisos.
- Auditar acciones.
- Registrar accesos denegados.

## 4.4 Criterio de aceptación

Ninguna API funcional puede ejecutarse sin tenant, usuario y permiso válido. Los cambios de roles y accesos denegados quedan auditados.

---

# 5. Fase 2 — Legal Knowledge y Document Metadata

## 5.1 Objetivo

Crear la base legal/documental que soportará RAT, evidencias, búsqueda y MCP.

## 5.2 Módulos

- Legal Knowledge.
- Document Metadata.
- Document Processing Function.
- Search Indexing base para fuentes legales, si se decide.

## 5.3 Entregables

- Fuentes legales.
- Versionado de fuente.
- Obligaciones.
- Taxonomías.
- Carga documental.
- Versionado documental.
- Blob privado.
- Estado de procesamiento.
- Function de procesamiento.

## 5.4 Criterio de aceptación

Se puede cargar una fuente/documento, versionarlo, procesarlo asíncronamente y dejarlo disponible para consulta o indexación, con auditoría.

---

# 6. Fase 3 — Evidence

## 6.1 Objetivo

Implementar la capa probatoria antes de construir RAT.

## 6.2 Módulos

- Evidence.
- integración con Document Metadata.
- solicitud de evidence pack.

## 6.3 Entregables

- Crear evidencia.
- Asociar documento.
- Asociar evidencia a entidad genérica.
- Clasificar evidencia.
- Consultar evidencias.
- Descargar con auditoría.
- Solicitar evidence pack.

## 6.4 Criterio de aceptación

Cualquier entidad soportada puede tener evidencia asociada con control de permisos y descarga auditada.

---

# 7. Fase 4 — Processing Inventory / RAT

## 7.1 Objetivo

Construir el núcleo funcional de Opción B.

## 7.2 Módulos

- Processing Inventory / RAT.
- integración con Legal Knowledge.
- integración con Evidence.
- integración con Security.
- integración con Audit.

## 7.3 Entregables

- CRUD de tratamientos.
- Finalidad.
- Base de licitud.
- Categorías de datos.
- Titulares.
- Datos sensibles/NNA/biometría.
- Sistemas.
- Proveedores básicos.
- Retención.
- Medidas de seguridad.
- Flags regulatorios.
- Estados.
- Versionado.
- Aprobación.

## 7.4 Criterio de aceptación

Se puede construir, revisar, aprobar y versionar un tratamiento, con evidencias y auditoría. No se aprueba un tratamiento incompleto.

---

# 8. Fase 5 — Workflow y Gap Management

## 8.1 Objetivo

Convertir el RAT en operación: revisión, tareas, brechas y remediación.

## 8.2 Módulos

- Workflow.
- Gap Management.
- Notification Delivery.

## 8.3 Entregables

- Solicitar revisión.
- Asignar tarea.
- Comentar.
- Aprobar.
- Devolver cambios.
- Crear brecha.
- Asignar brecha.
- Cerrar con evidencia.
- Aceptar riesgo.
- Notificar eventos.

## 8.4 Criterio de aceptación

Un tratamiento puede recorrer flujo de revisión y generar brechas con responsables, fechas, estados y evidencias de cierre.

---

# 9. Fase 6 — Reportes y búsqueda

## 9.1 Objetivo

Permitir exportación, navegación y búsqueda operativa.

## 9.2 Módulos

- Reporting Orchestration.
- Report Generation Function.
- Search Query.
- Search Indexing Function.

## 9.3 Entregables

- Solicitar reporte RAT.
- Solicitar reporte de brechas.
- Solicitar evidence pack.
- Generar archivos asíncronos.
- Descargar con auditoría.
- Buscar fuentes legales.
- Buscar tratamientos/documentos autorizados.

## 9.4 Criterio de aceptación

El usuario puede generar y descargar reportes sin bloquear la API. Las búsquedas respetan tenant y permisos.

---

# 10. Fase 7 — MCP contextual

## 10.1 Objetivo

Implementar MCP como capa asistiva sobre el core determinístico.

## 10.2 Módulos

- MCP Interactive.
- MCP Batch Jobs.
- Search Query.
- Legal Knowledge.
- RAT.
- Evidence.
- Workflow para revisión humana.

## 10.3 Entregables

- Consulta legal general.
- Consulta sobre tratamiento.
- Consulta sobre brechas/evidencias.
- Respuesta con citas.
- Abstención.
- Clasificación de riesgo.
- Historial.
- Feedback.
- Revisión humana.

## 10.4 Criterio de aceptación

El MCP no responde consultas normativas sin fuente, no certifica cumplimiento, registra interacción completa y deriva alto riesgo a revisión humana.

---

# 11. Orden sugerido de sprints backend

| Sprint | Contenido |
|---|---|
| Sprint 0 | Repo, solución .NET 10, infra base, health, logging. |
| Sprint 1 | Tenant, usuarios, roles, permisos, audit base. |
| Sprint 2 | Security policies, tenant guard, Outbox, Storage Queues. |
| Sprint 3 | Legal Knowledge base y taxonomías. |
| Sprint 4 | Document Metadata y Blob Storage. |
| Sprint 5 | Document Processing Function. |
| Sprint 6 | Evidence base y vínculos. |
| Sprint 7 | Processing Activity base. |
| Sprint 8 | RAT avanzado, flags y versionado. |
| Sprint 9 | Workflow revisión/aprobación. |
| Sprint 10 | Gap Management. |
| Sprint 11 | Report Orchestration y Report Function. |
| Sprint 12 | Search Query e Indexing. |
| Sprint 13 | MCP Interactive base. |
| Sprint 14 | MCP guardrails, HITL, feedback, hardening. |

## 12. Criterios de calidad backend

- Pruebas unitarias por módulo.
- Pruebas de integración de API.
- Pruebas de tenant isolation.
- Pruebas de autorización.
- Pruebas de idempotencia Functions.
- Pruebas de Outbox.
- Pruebas de auditoría.
- Pruebas de exportación.
- Pruebas de MCP guardrails cuando aplique.

## 13. Riesgos técnicos

| Riesgo | Mitigación |
|---|---|
| Monolito acoplado | Límites de módulos y ownership. |
| Fuga cross-tenant | Tenant guard, pruebas y auditoría. |
| Colas duplican mensajes | Idempotencia. |
| Poison messages | Colas/error tables y alertas. |
| MCP sobreconcluye | Citas, abstención, HITL. |
| Reportes pesados | Function asíncrona. |
| Documentos sensibles en logs | Política de logging y seguridad. |
| Cambio futuro a Service Bus | Abstracción de mensajería y destinos lógicos. |

## Nota v0.3 — Implementación por paquetes de módulo

A partir de la versión 0.3, cada fase debe ejecutarse usando el paquete de implementación correspondiente (`30` a `39`). El equipo no debe asumir que todos los documentos globales son lectura obligatoria para cada módulo. La matriz `22-matriz-dependencias-documentales.md` define la lectura mínima por fase y por módulo.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
