# 20 — Backlog backend por fases

## 1. Propósito

Este documento traduce la arquitectura backend de ATLAS Opción B en un backlog ejecutable por fases. No reemplaza una herramienta de gestión como Azure DevOps o GitHub Issues, pero define épicas, features, historias técnicas, dependencias y entregables.

## 2. Fase 0 — Cierre técnico y base de repositorio

### Objetivo

Preparar el entorno de desarrollo, estructura modular, decisiones pendientes mínimas y pipelines base.

### Épicas

| Épica | Resultado |
|---|---|
| E0.1 Decisiones bloqueantes | Hosting, DB, identidad, IaC, región, search inicial definidos. |
| E0.2 Repositorio backend | Solución .NET 10, estructura modular y convenciones. |
| E0.3 CI base | Build, tests, análisis básico y empaquetado. |
| E0.4 Infra dev | PostgreSQL, Storage, Key Vault, App Insights mínimos. |

### Historias técnicas

- Definir estructura de solución backend.
- Crear módulos base vacíos.
- Definir convenciones de naming.
- Configurar pipeline de build.
- Configurar pipeline de test.
- Configurar ambientes iniciales.
- Crear documentación de setup local.

### Criterio de salida

El equipo puede compilar, testear y desplegar skeleton backend.

## 3. Fase 1 — Core SaaS, identidad y seguridad base

### Objetivo

Construir base multi-tenant segura.

### Épicas

| Épica | Resultado |
|---|---|
| E1.1 Tenant Management | Tenants activos/suspendidos y settings base. |
| E1.2 Identity Bridge | Usuarios funcionales vinculados a identidad externa. |
| E1.3 RBAC | Roles, permisos y asignaciones. |
| E1.4 Tenant Guard | Protección cross-tenant. |
| E1.5 Audit base | Auditoría funcional mínima. |

### Historias funcionales

- Crear tenant desde plataforma.
- Consultar tenant actual.
- Invitar usuario.
- Listar usuarios.
- Asignar rol.
- Deshabilitar usuario.
- Auditar cambios de rol.
- Denegar acceso cross-tenant.

### Dependencias

- Proveedor de identidad confirmado.
- Modelo multi-tenant confirmado.

### Criterio de salida

Un usuario autenticado opera sólo dentro de su tenant y con permisos efectivos.

## 4. Fase 2 — Mensajería, Outbox y Functions base

### Objetivo

Habilitar asincronía de bajo costo con Storage Queues.

### Épicas

| Épica | Resultado |
|---|---|
| E2.1 Outbox | Mensajes transaccionales persistidos. |
| E2.2 Queue Publisher | Publicación neutral a Storage Queues. |
| E2.3 Function base | Primera Function consume mensaje real. |
| E2.4 Poison handling | Manejo de fallos y colas poison. |
| E2.5 Observabilidad | Correlación API-Outbox-Queue-Function. |

### Historias técnicas

- Crear OutboxMessage.
- Crear publicador background.
- Crear abstracción de destino lógico.
- Crear colas MVP.
- Crear DocumentProcessingFunction skeleton.
- Registrar ProcessedMessage para idempotencia.
- Crear alertas de poison.

### Criterio de salida

Un evento generado por API se publica a Storage Queue y es consumido por Function con trazabilidad.

## 5. Fase 3 — Legal Knowledge y documentos

### Objetivo

Crear base legal y documental del sistema.

### Épicas

| Épica | Resultado |
|---|---|
| E3.1 Legal Sources | Fuentes legales versionadas. |
| E3.2 Legal Obligations | Obligaciones consultables. |
| E3.3 Taxonomías | Bases, datos, titulares y medidas. |
| E3.4 Document Metadata | Documentos y versiones. |
| E3.5 Document Processing | Extracción/control de procesamiento. |

### Historias

- Crear fuente legal.
- Publicar versión.
- Crear obligación.
- Consultar taxonomías.
- Subir documento.
- Crear versión documental.
- Procesar documento asíncrono.
- Actualizar estado de procesamiento.

### Criterio de salida

El sistema puede administrar fuentes/taxonomías y subir documentos procesables.

## 6. Fase 4 — Evidence MVP

### Objetivo

Construir capa probatoria reutilizable.

### Épicas

| Épica | Resultado |
|---|---|
| E4.1 Evidence CRUD | Evidencias creadas y clasificadas. |
| E4.2 Evidence Links | Asociación a entidades. |
| E4.3 Seguridad Evidence | Descarga y clasificación. |
| E4.4 Evidence Pack | Exportación asíncrona. |

### Historias

- Crear evidencia.
- Asociar documento a evidencia.
- Asociar evidencia a entidad.
- Descargar evidencia con auditoría.
- Eliminar lógicamente.
- Solicitar evidence pack.
- Generar evidence pack.

### Criterio de salida

RAT puede usar evidencia con seguridad, auditoría y exportación.

## 7. Fase 5 — RAT MVP

### Objetivo

Construir el núcleo funcional de Opción B.

### Épicas

| Épica | Resultado |
|---|---|
| E5.1 Tratamientos Draft | Crear y editar tratamientos. |
| E5.2 Secciones RAT | Finalidad, base, datos, titulares, sistemas, proveedores, retención, medidas. |
| E5.3 Flags de riesgo | Sensibles, NNA, biometría, transferencias, automatización. |
| E5.4 Evidencias RAT | Asociar evidencias a tratamiento. |
| E5.5 Validaciones | Completitud revisión/aprobación. |
| E5.6 Versionado | Snapshot aprobado. |

### Historias

- Crear tratamiento.
- Editar identificación.
- Editar finalidad/base.
- Asociar categorías de datos.
- Asociar titulares.
- Asociar sistemas/proveedores.
- Registrar retención.
- Registrar medidas.
- Calcular flags.
- Asociar evidencia.
- Validar completitud.
- Crear versión aprobada.

### Criterio de salida

Un tratamiento puede levantarse, validarse, evidenciarse y versionarse.

## 8. Fase 6 — Workflow y brechas

### Objetivo

Convertir RAT en proceso revisable y accionable.

### Épicas

| Épica | Resultado |
|---|---|
| E6.1 Workflow revisión | Enviar, revisar, devolver, aprobar. |
| E6.2 Tareas | Asignaciones y vencimientos. |
| E6.3 Gap Management | Brechas, severidad, owner, cierre. |
| E6.4 Brechas automáticas | Sugerencias desde reglas RAT. |
| E6.5 Notificaciones | Avisos de tareas y cambios. |

### Historias

- Enviar tratamiento a revisión.
- Crear tarea de revisión.
- Solicitar cambios.
- Aprobar tratamiento.
- Crear brecha manual.
- Crear brechas automáticas.
- Asignar brecha.
- Cerrar brecha con evidencia.
- Aceptar riesgo.

### Criterio de salida

Un tratamiento puede pasar por revisión formal y generar backlog de brechas.

## 9. Fase 7 — Reportes y búsqueda

### Objetivo

Permitir exportación, consulta y evidencia portable.

### Épicas

| Épica | Resultado |
|---|---|
| E7.1 Report Orchestration | Jobs y estados. |
| E7.2 RAT Export | Excel/PDF RAT. |
| E7.3 Gap Export | Reporte de brechas. |
| E7.4 Evidence Pack | Paquetes por alcance. |
| E7.5 Search Query | Búsqueda legal y tenant. |
| E7.6 Search Indexing | Indexación asíncrona. |

### Historias

- Solicitar reporte RAT.
- Generar reporte RAT.
- Descargar reporte.
- Solicitar reporte brechas.
- Buscar fuente legal.
- Buscar documento tenant.
- Indexar fuente legal.
- Indexar documento procesado.

### Criterio de salida

El cliente puede exportar su inventario y consultar fuentes/datos internos con permisos.

## 10. Fase 8 — MCP contextual

### Objetivo

Agregar asistencia IA controlada sobre corpus legal y contexto RAT.

### Épicas

| Épica | Resultado |
|---|---|
| E8.1 MCP base | Consulta con citas. |
| E8.2 Contexto RAT | Uso minimizado de tratamientos. |
| E8.3 Risk router | Clasificación riesgo. |
| E8.4 Citation verifier | Verificación de citas. |
| E8.5 HITL | Revisión humana. |
| E8.6 Auditoría MCP | Historial, feedback y métricas. |

### Historias

- Preguntar sobre fuente legal.
- Preguntar sobre tratamiento.
- Responder con citas.
- Abstenerse sin fuente.
- Clasificar riesgo.
- Crear tarea de revisión humana.
- Registrar interacción.
- Enviar feedback.

### Criterio de salida

El MCP aporta valor contextual sin certificar cumplimiento ni romper seguridad.

## 11. Fase 9 — Hardening piloto

### Objetivo

Preparar piloto pagado.

### Épicas

| Épica | Resultado |
|---|---|
| E9.1 Seguridad | Threat review y pruebas cross-tenant. |
| E9.2 Observabilidad | Dashboards y alertas. |
| E9.3 Costos | Métricas de consumo. |
| E9.4 Documentación | Handoff técnico. |
| E9.5 Operación | Runbooks de soporte. |

### Criterio de salida

La plataforma puede operar un piloto controlado con trazabilidad y soporte.

## 12. Dependencias críticas

| Dependencia | Bloquea |
|---|---|
| Proveedor identidad | Fase 1 |
| PostgreSQL confirmado | Fase 0–1 |
| Hosting core | Fase 0 |
| Search inicial | Fase 7 |
| Proveedor IA | Fase 8 |
| Proveedor correo | Fase 6–7 |
| Política tenant | Todo |
| Tipos evidencia | Fase 4–5 |
| Campos RAT definitivos | Fase 5 |

## 13. Priorización MVP estricta

Si hay que recortar alcance, mantener:

1. Tenant/Identity/Security.
2. Documents metadata.
3. Evidence.
4. RAT.
5. Workflow aprobación.
6. Export RAT.
7. Outbox/Queues.

Recortar o postergar:

- MCP;
- búsqueda tenant avanzada;
- reportes sofisticados;
- evidence pack complejo;
- notificaciones avanzadas;
- dashboards ejecutivos.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
