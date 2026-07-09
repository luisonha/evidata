# 01 — Contrato API backend v1

## 1. Regla principal

El contrato público backend Evidata debe exponerse bajo:

```http
/api/v1
```

Las rutas actuales no versionadas bajo `/api/...` se consideran baseline implementado y compatibilidad legacy temporal. No son el contrato público objetivo.

## 2. Rutas legacy y rutas objetivo

| Ruta actual detectada | Ruta objetivo | Clasificación | Acción |
|---|---|---|---|
| `/api/processing-activities` | `/api/v1/processing-activities` | ExistingModify | Agregar ruta versionada oficial. |
| `/api/evidence` | `/api/v1/evidence` o contextual bajo ProcessingActivity | ExistingModify | Versionar y alinear contrato. |
| `/api/gaps` | `/api/v1/gaps` o contextual bajo ProcessingActivity | ExistingModify | Versionar y alinear contrato. |
| `/api/reports` | `/api/v1/exports` | ExistingModify | Mapear Reporting a Exports. |
| `/api/workflows` | `/api/v1/workflows` o endpoints críticos explícitos | ExistingModify | Mantener sólo si aporta al MVP. |
| `/api/audit/tenant/{tenantId}` | `/api/v1/audit-events` | ExistingModify | Exponer auditoría por recurso/tenant según permisos. |
| `/weatherforecast` | Ninguna | Remove | Eliminar del contrato público. |

## 3. Endpoints MVP obligatorios

### Processing Activities

```http
GET    /api/v1/processing-activities
POST   /api/v1/processing-activities
GET    /api/v1/processing-activities/{processingActivityId}
PATCH  /api/v1/processing-activities/{processingActivityId}
GET    /api/v1/processing-activities/{processingActivityId}/control
PATCH  /api/v1/processing-activities/{processingActivityId}/nodes/{nodeCode}
POST   /api/v1/processing-activities/{processingActivityId}/send-to-review
POST   /api/v1/processing-activities/{processingActivityId}/approve
POST   /api/v1/processing-activities/{processingActivityId}/activate
POST   /api/v1/processing-activities/{processingActivityId}/archive
```

### Templates

```http
GET /api/v1/processing-activity-templates
GET /api/v1/processing-activity-templates/{templateId}
```

### Evidence

```http
GET  /api/v1/processing-activities/{processingActivityId}/evidence
POST /api/v1/processing-activities/{processingActivityId}/evidence
POST /api/v1/processing-activities/{processingActivityId}/evidence/{evidenceId}/validate
POST /api/v1/processing-activities/{processingActivityId}/evidence/{evidenceId}/mark-insufficient
POST /api/v1/processing-activities/{processingActivityId}/evidence/{evidenceId}/reject
POST /api/v1/processing-activities/{processingActivityId}/evidence/{evidenceId}/replace
```

### Gaps

```http
GET  /api/v1/processing-activities/{processingActivityId}/gaps
POST /api/v1/processing-activities/{processingActivityId}/gaps
POST /api/v1/processing-activities/{processingActivityId}/gaps/{gapId}/mark-in-correction
POST /api/v1/processing-activities/{processingActivityId}/gaps/{gapId}/resolve
POST /api/v1/processing-activities/{processingActivityId}/gaps/{gapId}/accept-with-risk
POST /api/v1/processing-activities/{processingActivityId}/gaps/{gapId}/dismiss
```

No se expone endpoint `reopen` para gaps en el MVP; `Resolved → Open` ocurre por reevaluación automática de reglas.

### Reviews

```http
GET  /api/v1/processing-activities/{processingActivityId}/reviews
POST /api/v1/processing-activities/{processingActivityId}/reviews
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/take
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/request-changes
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/approve
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/reject
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/cancel
POST /api/v1/processing-activities/{processingActivityId}/reviews/{reviewId}/reopen
```

### Exports

```http
GET  /api/v1/processing-activities/{processingActivityId}/exports
POST /api/v1/processing-activities/{processingActivityId}/exports
GET  /api/v1/exports/{exportId}
GET  /api/v1/exports/{exportId}/download
```

### Timeline / Audit

```http
GET /api/v1/processing-activities/{processingActivityId}/timeline
GET /api/v1/processing-activities/{processingActivityId}/audit-events
```

## 4. Reglas OpenAPI obligatorias

Cada operación debe declarar:

- `operationId` estable;
- `security`;
- `x-change-status`;
- respuestas de error con `ApiErrorResponse`;
- códigos técnicos y `labelKey`, no mensajes finales UI.

Valores permitidos para `x-change-status`:

```text
ExistingKeep
ExistingModify
New
Deprecate
Remove
NotAllowed
DecisionRequired
```

### 4.1 Esquema de seguridad

`securitySchemes` declara un único esquema `http bearer` con `bearerFormat: JWT`. El JWT debe transportar identidad de usuario, `tenantId` y roles de negocio (`TenantOwner`, `ComplianceAdmin`, `ProcessOwner`, `LegalReviewer`, `SecurityReviewer`, `Auditor`, `Viewer`).

Esta iteración declara el esquema en el contrato OpenAPI como contrato objetivo. No implementa un IdP completo ni validación `JwtBearer` real en runtime — eso queda como brecha de implementación (ver `07-closure-scorecard.md`). Mientras no se implemente, el stub `LocalDev` sigue siendo el mecanismo de facto solo en entornos de desarrollo local.

## 5. Errores estándar

```json
{
  "error": {
    "code": "BlockingEvidenceMissing",
    "labelKey": "blockedReason.blockingEvidenceMissing",
    "message": null,
    "correlationId": "corr-001",
    "details": {}
  }
}
```

## 6. Códigos HTTP

| Código | Uso |
|---:|---|
| 200 | Lectura o acción con respuesta. |
| 201 | Creación. |
| 204 | Acción exitosa sin cuerpo. |
| 400 | Request inválido. |
| 401 | No autenticado. |
| 403 | Sin permiso. |
| 404 | No encontrado. |
| 409 | Conflicto de versión/concurrencia. |
| 422 | Regla de negocio bloqueante. |
| 500 | Error inesperado. |

## 7. Prohibiciones

No debe existir en el contrato público:

```http
/api/v1/treatments
/api/treatments
/weatherforecast
```

Si se mantienen rutas `/api/...` por compatibilidad, deben quedar fuera del OpenAPI público o marcadas como legacy en un contrato interno separado.
