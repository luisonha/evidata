# 00 — Objetivo y alcance de la iteración backend v1.6.4

## 1. Objetivo

Esta iteración tiene como objetivo convertir la especificación backend Evidata en un contrato implementable contra el backend existente.

La iteración no redefine el producto. Toma como decisiones cerradas:

- Producto comercial: **Evidata**.
- Descriptor: **Digital Trust Operating System**.
- ATLAS queda como nombre interno/proyecto técnico.
- Backend canónico: `ProcessingActivity`.
- Lenguaje UI: Tratamiento / Tratamientos.
- Backend no debe introducir `Treatment` como entidad, DTO canónico, namespace o endpoint.

## 2. Alcance incluido

La iteración incluye:

| Área | Alcance |
|---|---|
| API | Versionar contrato público bajo `/api/v1`. |
| OpenAPI | Generar contrato público oficial con `operationId`, `security`, `x-change-status` y errores estándar. |
| Dominio | Alinear `ProcessingInventory` con `ProcessingActivity`, versiones y nodos. |
| Control | Crear o especificar `GET /api/v1/processing-activities/{id}/control`. |
| RBAC | Backend calcula acciones disponibles y bloqueadas. |
| Auditoría | Acciones críticas generan evento auditable y proyección Timeline. |
| Evidencia | Formalizar requerimientos, validación, acceso y auditoría de evidencia. |
| Brechas | Formalizar reglas, severidad, bloqueo y reapertura. |
| Exportaciones | Mapear Reporting existente hacia contrato de Exports. |
| Calidad | Definir pruebas, gates y DoD backend. |

## 3. Fuera de alcance

Quedan fuera de esta iteración backend:

- Implementación del frontend.
- E2E reales de frontend.
- Portal de titulares.
- DSAR completo.
- Consentimiento multicanal.
- Incidentes como módulo completo.
- Motor legal automático avanzado.
- Certificación comercial.

El criterio “E2E frontend por historias completas” pertenece al paquete frontend y no bloquea el cierre documental backend.

## 4. Decisiones obligatorias

| Decisión | Estado |
|---|---|
| `/api/v1` es contrato público objetivo | Cerrada |
| `/api/...` no versionado es baseline legacy temporal | Cerrada |
| `/api/v1/treatments` está prohibido | Cerrada |
| `/weatherforecast` debe desaparecer del OpenAPI público | Cerrada |
| `ProcessingActivity` es agregado canónico backend | Cerrada |
| `Treatment` sólo puede aparecer como lenguaje UI/frontend | Cerrada |
| Frontend queda separado en documentación propia | Cerrada |

## 5. Criterio de cierre

El cierre documental backend exige:

- puntaje ponderado mínimo **9.50 / 10**;
- evaluación con rúbrica fija;
- brechas restantes justificadas como brechas de implementación, no documentales;
- OpenAPI objetivo consistente;
- backlog P0/P1/P2 ejecutable.
