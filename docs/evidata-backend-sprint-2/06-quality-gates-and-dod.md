# 06 — Quality gates y Definition of Done backend

## 1. Principios

No se puede cerrar funcionalidad crítica sólo con mocks unitarios.

Reglas:

- No mockear persistencia principal en pruebas de integración.
- No mockear autorización real en pruebas de autorización/integración.
- No mockear reglas de brecha bajo prueba.
- No aceptar sólo `200 OK` como validación.
- No publicar OpenAPI con rutas prohibidas.

## 2. Gates PR

| Gate | Bloquea PR | Evidencia esperada |
|---|---:|---|
| Build backend | Sí | build log |
| Unit tests | Sí | test report |
| Domain tests | Sí | test report |
| OpenAPI parse/lint | Sí | openapi-validation.json |
| No `/api/v1/treatments` | Sí | forbidden-routes.log |
| No `/weatherforecast` | Sí | forbidden-routes.log |
| `operationId` completo | Sí | openapi-lint report |
| `security` completo | Sí | openapi-lint report |
| `x-change-status` completo | Sí | openapi-lint report |
| i18n `labelKey` en contratos visibles | Sí | contract report |

## 3. Gates main

| Gate | Bloquea main | Evidencia esperada |
|---|---:|---|
| Integration tests API | Sí | test-results |
| Contract tests OpenAPI vs handlers | Sí | contract-report |
| Security/RBAC tests | Sí | security-report |
| Audit tests críticos | Sí | audit-report |
| Evidence lifecycle tests | Sí | evidence-report |
| Gap rule tests | Sí | gap-rule-report |
| Export tests | Sí | export-report |

## 4. Gates release

| Gate | Bloquea release | Evidencia esperada |
|---|---:|---|
| Full regression backend | Sí | regression report |
| Performance `/control` | Sí | perf report |
| Resilience tests | Sí | resilience report |
| Tenant isolation tests | Sí | security report |
| Official export auditability | Sí | audit/export report |
| Documentation score >= 9.5 | Sí | scorecard |

## 5. Definition of Done backend

Una historia backend está cerrada sólo si cumple:

- endpoint documentado en OpenAPI;
- permisos definidos;
- errores definidos;
- eventos/auditoría definidos si aplica;
- pruebas unitarias/domain;
- pruebas integración/API si toca endpoint;
- pruebas seguridad si toca permisos;
- pruebas auditoría si toca acción crítica;
- fixture determinístico;
- gate asignado;
- no introduce naming prohibido;
- no introduce ruta prohibida;
- actualiza trazabilidad historia → endpoint → handler → fixture → test → gate.

## 6. DoD de iteración

La iteración backend v1.6.4 puede cerrarse como implementación cuando:

- OpenAPI público expone sólo `/api/v1`;
- no existe `/weatherforecast`;
- no existe `/api/v1/treatments`;
- `/control` existe;
- `ProcessingActivityVersion` y `ProcessingActivityNode` están implementados o mapeados formalmente;
- `availableActions` y `blockedActions` son calculados por backend;
- acciones críticas generan auditoría;
- Timeline está disponible como proyección;
- evidencia tiene requerimiento, asociación, validación y auditoría;
- brechas automáticas están trazadas a RuleCode, fixture y test;
- gates PR/main/release se ejecutan.
