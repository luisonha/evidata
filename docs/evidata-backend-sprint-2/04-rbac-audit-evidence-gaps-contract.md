# 04 — Contrato RBAC, auditoría, evidencias y brechas

## 1. RBAC

El backend es la fuente de verdad de autorización. Debe calcular permisos por recurso, rol, tenant, propietario, área, estado, sensibilidad y tipo de revisión.

### Roles mínimos

```text
TenantOwner
ComplianceAdmin
ProcessOwner
LegalReviewer
SecurityReviewer
Auditor
Viewer
```

### Permisos críticos

| PermissionCode | Regla mínima | Test obligatorio |
|---|---|---|
| `ApproveProcessingActivity` | ProcessOwner no puede aprobar su propio tratamiento. | SEC-APP-001 |
| `ActivateProcessingActivity` | Sólo TenantOwner/ComplianceAdmin según política. | SEC-ACT-001 |
| `ValidateEvidence` | Reviewer correcto según tipo de evidencia. | SEC-EV-001 |
| `AcceptGapWithRisk` | Sólo TenantOwner/ComplianceAdmin, con justificación. | SEC-GAP-001 |
| `GenerateOfficialExport` | Sólo roles autorizados y actividad aprobada/activa. | SEC-EXP-001 |
| `DownloadEvidence` | Viewer no descarga evidencia sensible. | SEC-EVDOWN-001 |

### Mapeo obligatorio para `ValidateEvidence`

`EvidenceRequirement.reviewDomain` define qué rol puede validar.

| `reviewDomain` | Rol autorizado |
|---|---|
| `Legal` | `LegalReviewer` |
| `Security` | `SecurityReviewer` |

`ValidateEvidence` debe autorizar contra `reviewDomain`; no contra nombre de archivo, MIME type ni texto UI.

### Contrato de permisos

`ResourcePermissionsViewModel` debe incluir:

```text
roleCodes
availableActions
readOnly
blockedActions
```

## 2. Auditoría

Toda acción crítica debe generar evento auditable.

### Decisión de implementación

Si el repo ya tiene `AuditLog`, puede ser la implementación concreta de `AuditEvent`, siempre que cumpla campos mínimos:

```text
id
tenantId
eventType
resourceType
resourceId
actorUserId
occurredAt
result
correlationId
metadata
```

Si no cumple, debe extenderse o crearse `AuditEvent`.

### 2.1 Política de correlationId y shape mínimo AuditEvent

Se adopta una política única de trazabilidad por request: el backend debe aceptar `X-Correlation-Id` cuando el cliente lo envía y, si no viene informado, debe generar uno al inicio de la request. Ese `correlationId` debe propagarse durante toda la cadena de logging y auditoría asociada a esa request y debe incluirse también en toda respuesta de error `ApiErrorResponse.correlationId`, en línea con `01-api-contract-v1.md` sección 5.

El shape mínimo final de `AuditEvent` (o de `AuditLog` si se extiende para cumplirlo) queda definido así:

| Campo | Tipo | Regla |
|---|---|---|
| `id` | `uuid` | Identificador único del evento. |
| `tenantId` | `uuid` | Tenant propietario del evento. |
| `eventType` | `string` | Catálogo cerrado alineado con la tabla **Acciones críticas auditables** de este documento. |
| `resourceType` | `string` | Tipo de recurso auditado. |
| `resourceId` | `uuid` | Identificador del recurso auditado. |
| `actorUserId` | `uuid` | Usuario actor responsable de la acción. |
| `occurredAt` | `datetime utc` | Timestamp UTC de ocurrencia. |
| `result` | `string enum` | Sólo `Success`, `Failure` o `Blocked`. |
| `correlationId` | `string` | Correlation ID propagado desde `X-Correlation-Id` o generado por el backend. |
| `metadata` | `json/dictionary` | Payload estructurado con detalles adicionales auditables. |

Contra el código actual, `src/Modules/Audit/Domain/AuditLog.cs` ya cubre `Id`, `TenantId`, `OccurredAt` y parcialmente el mapeo funcional `Action → eventType`, `Resource → resourceType`, `ResourceId → resourceId`, `UserId → actorUserId`, pero no cumple todavía el shape final tal como está definido aquí: `UserId` y `ResourceId` hoy son nullable, `Details` es `string?` y no un `json/dictionary` tipado, y faltan campos explícitos para `result` y `correlationId` (`AuditLog.cs`). Además, `src/Modules/Audit/Infrastructure/AuditService.cs` sólo recibe y persiste `action`, `resource`, `resourceId`, `details`, `ipAddress` y `severity`, por lo que también requiere extensión para transportar `result` y `correlationId` de forma nativa. Como evidencia complementaria, `src/Modules/Evidence/Domain/EvidenceAccessLog.cs` ya demuestra que el dominio sí maneja `CorrelationId` explícito en auditoría de descargas, por lo que la política debe unificarse a nivel transversal.

### Timeline

`TimelineEvent` es una proyección UI/read model derivada de auditoría. Si no existe, debe crearse.

### Acciones críticas auditables

| Acción | AuditEvent/AuditLog | TimelineEvent | Test |
|---|---|---|---|
| Crear ProcessingActivity | Sí | Sí | AUD-PA-001 |
| Actualizar nodo | Sí | Sí | AUD-NODE-001 |
| Enviar a revisión | Sí | Sí | AUD-REV-001 |
| Aprobar | Sí | Sí | AUD-APP-001 |
| Activar | Sí | Sí | AUD-ACT-001 |
| Archivar | Sí | Sí | AUD-ARC-001 |
| Validar evidencia | Sí | Sí | AUD-EV-001 |
| Rechazar evidencia | Sí | Sí | AUD-EV-002 |
| Aceptar brecha con riesgo | Sí | Sí | AUD-GAP-001 |
| Generar export oficial | Sí | Sí | AUD-EXP-001 |

## 3. Evidencias

El ciclo mínimo es:

```text
EvidenceRequirement → Evidence attached → EvidenceValidation → Validated/Insufficient/Rejected → Audit → Download control
```

### Conceptos

| Concepto | Decisión |
|---|---|
| `Evidence` | Reutilizar módulo existente. |
| `EvidenceRequirement` | Crear si no existe. |
| `EvidenceValidation` | Crear o formalizar si existe parcialmente. |
| `EvidenceAccessLog` | Mantener si existe. |
| `EvidenceVersion` | P2/recomendado si el storage lo permite. |

### Estados

```text
Pending
Attached
Validated
Insufficient
Rejected
```

`Waived` queda fuera del MVP de esta iteración. Si se incorpora en P2, deberá definirse con endpoint explícito, permiso admin específico y auditoría propia.

## 4. Brechas / Gaps

Las brechas son hallazgos operativos, no declaraciones automáticas de incumplimiento legal.

### Ciclo mínimo

```text
Open → InCorrection → Resolved
Open → AcceptedWithRisk
Open → Dismissed
Resolved → Open, de forma automática durante la reevaluación síncrona de reglas cuando un cambio en nodos, evidencias o reviews vuelve a detectar la condición
```

No existe endpoint público `POST /api/v1/processing-activities/{processingActivityId}/gaps/{gapId}/reopen` en el MVP. La reapertura es una transición interna del motor de reglas y debe quedar auditada como reapertura automática.

### Reglas mínimas

| RuleCode | Severity | BlocksApproval | Fixture | Test |
|---|---|---:|---|---|
| `TRANSFER_WITHOUT_DESTINATION_COUNTRY` | Critical | Sí | `pa-transfer-no-country` | GAP-RULE-001 |
| `TRANSFER_WITHOUT_RECEIVER` | High | Sí | `pa-transfer-no-receiver` | GAP-RULE-002 |
| `TRANSFER_WITHOUT_SAFEGUARD` | Critical | Sí | `pa-transfer-no-safeguard` | GAP-RULE-003 |
| `TRANSFER_WITHOUT_BLOCKING_EVIDENCE` | Critical | Sí | `pa-transfer-no-evidence` | GAP-RULE-004 |
| `SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW` | Critical | Sí | `pa-sensitive-no-security-review` | GAP-RULE-005 |
| `LEGAL_BASIS_MISSING` | Critical | Sí | `pa-no-legal-basis` | GAP-RULE-006 |
| `RETENTION_UNDEFINED` | High | Sí | `pa-retention-undefined` | GAP-RULE-007 |
| `DATA_CATEGORIES_EMPTY` | High | Sí | `pa-no-data-categories` | GAP-RULE-008 |
| `PURPOSE_UNDEFINED` | Critical | Sí | `pa-purpose-undefined` | GAP-RULE-009 |
| `DATA_SUBJECTS_EMPTY` | High | Sí | `pa-no-data-subjects` | GAP-RULE-010 |
| `SYSTEMS_WITHOUT_OWNER` | High | Sí | `pa-systems-without-owner` | GAP-RULE-011 |

## 5. Exportaciones

El módulo Reporting existente debe mapearse al contrato público Exports.

| ExportType | Visibilidad | Regla |
|---|---|---|
| `ProcessingActivityPdfSummary` | UserVisible | Puede generarse con warnings. |
| `GlobalRatExcel` | UserVisible | Requiere permisos. |
| `ApprovalHistory` | UserVisible | Debe incluir auditoría relevante. |
| `InternalJson` | TechnicalOnly | No visible en UI MVP. |

### 5.1 Shape minimo de Export y ciclo de vida

Estados mínimos del recurso `Export`:

```text
Requested → Generating → Completed
Requested → Generating → Failed
```

En el contrato público de Exports (`GET/POST /api/v1/processing-activities/{processingActivityId}/exports`, `GET /api/v1/exports/{exportId}`, `GET /api/v1/exports/{exportId}/download`), el shape mínimo del recurso `Export` debe ser:

| Campo | Tipo | Regla |
|---|---|---|
| `id` | `uuid` | Identificador único del export. |
| `processingActivityId` | `uuid` | Actividad de tratamiento dueña del export. |
| `exportType` | `enum` | Uno de `ProcessingActivityPdfSummary`, `GlobalRatExcel`, `ApprovalHistory`, `InternalJson`. |
| `status` | `enum` | `Requested`, `Generating`, `Completed`, `Failed`. |
| `version` | `integer` | Incremental por combinación `processingActivityId + exportType`. |
| `warnings` | `array<string>` | Opcional; warnings funcionales no bloqueantes. |
| `contentType` | `string` | Ej. `application/pdf`, `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`, `application/json`. |
| `artifactDocumentId` | `uuid` | Referencia al blob/documento generado. |
| `requestedByUserId` | `uuid` | Usuario que solicitó la exportación. |
| `generatedAt` | `datetime utc` | Nullable mientras el export no haya completado. |
| `correlationId` | `string` | Correlation ID auditable de la solicitud/generación. |

Contra el código actual, `src/Modules/Reporting/Domain/ReportJob.cs` ya cubre parcialmente este contrato: tiene `Id`, `Status`, `ArtifactDocumentId`, `RequestedBy`, `CompletedAt` y timestamps de solicitud/inicio/cierre; además, `src/Modules/Reporting/Domain/ReportJobStatus.cs` ya modela `Requested`, `Running`, `Completed`, `Failed` y `Expired`, por lo que `Generating` puede mapearse a `Running`. Sin embargo, el recurso actual requiere extensión para cumplir el contrato público de Export: no existe `processingActivityId` explícito, no existe `version`, no existe `warnings`, no existe `contentType`, no existe `correlationId`, y `generatedAt` debería formalizarse como campo de contrato derivado de `CompletedAt`. Además, `src/Modules/Reporting/Domain/ReportType.cs` hoy define `RAT`, `Gaps`, `EvidencePack` y `ExecutiveSummary`, que no coinciden con el enum público `ExportType`, y `src/Modules/Reporting/Api/ReportsController.cs` hoy expone el baseline legacy `GET/POST /api/reports` y `GET /api/reports/{id}` sin el recurso contextual por `processingActivityId` ni el endpoint `GET /api/v1/exports/{exportId}/download`.

Todo export oficial debe generar evento auditable.
