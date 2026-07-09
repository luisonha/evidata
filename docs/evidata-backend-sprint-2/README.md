# Evidata Backend — Iteración v1.6.4

Este paquete contiene el **set limpio de documentos de la nueva iteración backend**. No es una historia de cambios ni un patch explicativo; es el material de trabajo para implementar la siguiente iteración sobre el backend existente.

## Objetivo de la iteración

Alinear el backend actual de Evidata al contrato objetivo backend, usando como base el repositorio existente y no una implementación greenfield.

La iteración se concentra en:

1. Contrato público versionado `/api/v1`.
2. OpenAPI objetivo con `operationId`, `security`, `x-change-status` y `ApiErrorResponse`.
3. Mapeo del dominio real hacia `ProcessingActivity`, versionado y nodos.
4. `/control` como read model principal del frontend futuro.
5. RBAC, `availableActions` y `blockedActions`.
6. Auditoría, Timeline, evidencia, brechas y exportaciones.
7. Backlog ejecutable P0/P1/P2.
8. Quality gates y Definition of Done backend.
9. Evaluación de cierre documental backend con umbral 9.5.

## Documentos incluidos

| Documento | Uso |
|---|---|
| `00-iteration-objective-and-scope.md` | Define alcance, decisiones y exclusiones de esta iteración. |
| `01-api-contract-v1.md` | Define el contrato API versionado y las reglas OpenAPI. |
| `02-domain-implementation-contract.md` | Define cómo debe quedar el dominio backend y cómo mapear lo existente. |
| `03-control-viewmodel-and-ui-contract.md` | Define `/control` y el contrato mínimo que usará el frontend. |
| `04-rbac-audit-evidence-gaps-contract.md` | Define autorización, auditoría, evidencias y brechas. |
| `05-implementation-backlog.md` | Define backlog P0/P1/P2 de implementación. |
| `06-quality-gates-and-dod.md` | Define gates, pruebas obligatorias y DoD backend. |
| `07-closure-scorecard.md` | Evalúa la iteración con la rúbrica fija y umbral 9.5. |
| OpenAPI objetivo (generado desde código) | Se generará directamente desde el código siguiendo las reglas de `01-api-contract-v1.md`, en vez de mantenerse como archivo YAML estático en este paquete. |

## Resultado esperado

Al terminar esta iteración, el backend debe tener al menos:

- rutas oficiales `/api/v1/...`;
- OpenAPI público sin rutas legacy ni `/weatherforecast`;
- `ProcessingActivity` como agregado canónico;
- contrato `/control` implementado;
- permisos y bloqueos calculados por backend;
- auditoría de acciones críticas;
- reglas de brecha trazables;
- evidencia con ciclo completo;
- pruebas y gates mínimos ejecutables.

## Importante

El PASS de esta iteración es **documental backend**. No afirma que el código ya esté implementado. El cierre de implementación requiere ejecutar los quality gates definidos en `06-quality-gates-and-dod.md`.
