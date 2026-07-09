# 10 — Diseño de pipeline CI para quality gates backend

## 1. Estado actual

Hoy `.github/workflows/ci.yml` define un único job `build-and-test` que corre en `push` y `pull_request` para `develop`, `main` y `dev/**`; restaura, compila y ejecuta `dotnet test` sobre `Evidata.sln`. No separa gates PR/main/release ni publica artefactos de evidencia específicos por regla.

## 2. Diseño de jobs por gate

| Job | Trigger (PR/push main/tag release) | Qué ejecuta | Artefacto de evidencia | Bloquea merge/deploy |
|---|---|---|---|---|
| `pr-build-backend` | PR | Restore + build backend en configuración Release con versionado reproducible. | `build.log` | Merge PR |
| `pr-unit-tests` | PR | Suite de unit tests con reporte estructurado. | `unit-test-report.trx` | Merge PR |
| `pr-domain-tests` | PR | Suite de domain tests con fixtures determinísticos y reporte. | `domain-test-report.trx` | Merge PR |
| `pr-openapi-parse-lint` | PR | Generación/parseo de OpenAPI y lint sintáctico/estructural. | `openapi-validation.json` | Merge PR |
| `pr-forbidden-routes-treatments` | PR | Regla que falla si el contrato expone `/api/v1/treatments`. | `forbidden-routes-treatments.log` | Merge PR |
| `pr-forbidden-routes-weatherforecast` | PR | Regla que falla si el contrato o handlers exponen `/weatherforecast`. | `forbidden-routes-weatherforecast.log` | Merge PR |
| `pr-openapi-operationid-complete` | PR | Lint que valida `operationId` en todas las operaciones públicas. | `openapi-operationid-report.json` | Merge PR |
| `pr-openapi-security-complete` | PR | Lint que valida `security` completo en todas las operaciones que lo requieren. | `openapi-security-report.json` | Merge PR |
| `pr-openapi-x-change-status-complete` | PR | Lint que valida `x-change-status` completo en el contrato público. | `openapi-x-change-status-report.json` | Merge PR |
| `pr-i18n-labelkey-contracts` | PR | Verificación de `labelKey` en contratos visibles/UI-facing. | `contract-labelkey-report.json` | Merge PR |
| `main-integration-api` | Push `main` | Integration tests API contra persistencia/autorización reales según DoD. | `integration-api-results.trx` | Promote en `main` |
| `main-contract-openapi-vs-handlers` | Push `main` | Contract tests que comparan OpenAPI generado vs handlers/rutas reales. | `contract-openapi-vs-handlers.json` | Promote en `main` |
| `main-security-rbac` | Push `main` | Tests de autorización, permisos, bloqueos y RBAC real. | `security-rbac-report.json` | Promote en `main` |
| `main-audit-critical` | Push `main` | Tests de auditoría para acciones críticas y eventos esperados. | `audit-critical-report.json` | Promote en `main` |
| `main-evidence-lifecycle` | Push `main` | Tests de ciclo de vida de evidencia: requerimiento, asociación, validación y auditoría. | `evidence-lifecycle-report.json` | Promote en `main` |
| `main-gap-rule` | Push `main` | Tests de reglas de brecha trazadas a `RuleCode`, fixture y test. | `gap-rule-report.json` | Promote en `main` |
| `main-export` | Push `main` | Tests de exportaciones funcionales y consistencia del resultado. | `export-report.json` | Promote en `main` |
| `release-full-regression-backend` | Tag `v*` / release | Regresión completa backend (build + suites unit/domain/integration/contract/security/audit). | `full-regression-report.json` | Deploy release |
| `release-performance-control` | Tag `v*` / release | Pruebas de performance sobre `/control` con presupuesto de latencia/throughput. | `performance-control-report.json` | Deploy release |
| `release-resilience` | Tag `v*` / release | Resilience tests: fallos transitorios, timeouts, reintentos y degradación controlada. | `resilience-report.json` | Deploy release |
| `release-tenant-isolation` | Tag `v*` / release | Tests de aislamiento entre tenants y no filtración de datos/permisos. | `tenant-isolation-security-report.json` | Deploy release |
| `release-official-export-auditability` | Tag `v*` / release | Validación de auditabilidad de export oficial: quién, cuándo, qué filtros, qué archivo. | `official-export-auditability-report.json` | Deploy release |
| `release-documentation-score` | Tag `v*` / release | Scorecard documental automatizado con umbral `>= 9.5` para release. | `documentation-scorecard.json` | Deploy release |

## 3. Estructura de archivos sugerida

- `.github/workflows/pr-gates.yml`
  - `pr-build-backend`
  - `pr-unit-tests`
  - `pr-domain-tests`
  - `pr-openapi-parse-lint`
  - `pr-forbidden-routes-treatments`
  - `pr-forbidden-routes-weatherforecast`
  - `pr-openapi-operationid-complete`
  - `pr-openapi-security-complete`
  - `pr-openapi-x-change-status-complete`
  - `pr-i18n-labelkey-contracts`

- `.github/workflows/main-gates.yml`
  - `main-integration-api`
  - `main-contract-openapi-vs-handlers`
  - `main-security-rbac`
  - `main-audit-critical`
  - `main-evidence-lifecycle`
  - `main-gap-rule`
  - `main-export`

- `.github/workflows/release-gates.yml`
  - `release-full-regression-backend`
  - `release-performance-control`
  - `release-resilience`
  - `release-tenant-isolation`
  - `release-official-export-auditability`
  - `release-documentation-score`

## 4. Nota sobre publicación de artefactos

La evidencia debe publicarse como **GitHub Actions artifacts** en cada workflow, con nombres estables por job para que branch protection, auditoría y troubleshooting puedan referenciar archivos concretos. Retención mínima sugerida: **30 días para PR**, **60-90 días para `main`** y **180 días para release**, conservando además los artefactos de release asociados a tags/versiones oficiales.
