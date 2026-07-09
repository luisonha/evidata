# 03 — Contrato `/control` y ViewModel para frontend

## 1. Propósito

`GET /api/v1/processing-activities/{processingActivityId}/control` es el read model principal para la futura pantalla UI “Control del tratamiento”.

Aunque el frontend aún no exista, el backend debe entregar un contrato estable y consumible.

## 2. Endpoint

```http
GET /api/v1/processing-activities/{processingActivityId}/control
```

Debe devolver `ProcessingActivityControlViewModel`.

## 3. Root JSON obligatorio

```json
{
  "processingActivity": {},
  "version": {},
  "controlTower": {},
  "priorityActions": [],
  "operationalMap": {},
  "evidenceSummary": {},
  "gapSummary": {},
  "reviewSummary": {},
  "timeline": [],
  "exports": [],
  "permissions": {},
  "blockedActions": [],
  "localization": {}
}
```

No se debe usar `treatment` como propiedad root backend.

## 4. ViewModels obligatorios

| ViewModel | Uso |
|---|---|
| `ProcessingActivityControlViewModel` | Contrato principal `/control`. |
| `ProcessingActivityListViewModel` | Listado. |
| `ProcessingActivityDetailViewModel` | Detalle técnico. |
| `ProcessingActivityNodeViewModel` | Nodos del mapa operativo. |
| `EvidenceSummaryViewModel` | Resumen de evidencia. |
| `GapSummaryViewModel` | Resumen de brechas. |
| `ReviewSummaryViewModel` | Estado de revisión. |
| `TimelineEventViewModel` | Bitácora visible. |
| `ExportOptionViewModel` | Opciones de exportación. |
| `ResourcePermissionsViewModel` | Permisos calculados por recurso. |
| `AvailableActionViewModel` | Acciones permitidas. |
| `BlockedActionViewModel` | Acciones bloqueadas y motivo. |

## 5. Acciones y bloqueos

El backend debe calcular:

```text
availableActions
blockedActions
blockingReasons
completionPercentage
nodeStatus
riskLevel
```

El frontend no debe inferir permisos críticos por sí mismo.

Ejemplo de bloqueo:

```json
{
  "action": "ApproveProcessingActivity",
  "reasonCode": "BlockingEvidenceMissing",
  "labelKey": "blockedReason.blockingEvidenceMissing",
  "severity": "Critical",
  "relatedNode": "Evidence"
}
```

## 6. Contrato frontend futuro

| Ruta UI futura | Endpoint backend | ViewModel | Estados obligatorios |
|---|---|---|---|
| `/treatments` | `GET /api/v1/processing-activities` | `ProcessingActivityListViewModel` | loading, empty, error, no permission |
| `/treatments/:id/control` | `GET /api/v1/processing-activities/{id}/control` | `ProcessingActivityControlViewModel` | loading, partial error, total error, no permission, blocked, offline |
| `/treatments/:id/control?node=Transfers` | mismo `/control` + PATCH node | `ProcessingActivityNodeViewModel` | unsaved, validation, conflict |

## 7. i18n

El backend debe devolver códigos y `labelKey`, no textos finales UI.

Permitido:

```json
{
  "status": "InReview",
  "labelKey": "processingActivity.status.inReview"
}
```

No permitido:

```json
{
  "status": "En revisión"
}
```

## 8. Pruebas mínimas

| Test | Tipo | Gate |
|---|---|---|
| `/control` devuelve root `processingActivity`, no `treatment` | Contract/API | PR |
| `/control` incluye `permissions` y `blockedActions` | Integration | Main |
| Botón aprobar bloqueado por evidencia faltante | API + future frontend | Release |
| `labelKey` presente en estados visibles | Contract | PR |

## 9. Schemas tipados

Todo estado/enum visible en UI debe viajar con su `labelKey` asociado (`statusLabelKey`, `severityLabelKey`, etc.), sin devolver texto final.

### `ProcessingActivityControlViewModel`

```text
processingActivity: ProcessingActivityDetailViewModel
version: object ({ id?: string (uuid), version: number (>=1), status: string (enum ProcessingActivityVersionStatus), statusLabelKey: string, activeVersionId?: string (uuid), currentDraftVersionId?: string (uuid) })
controlTower: object ({ completionPercentage: number (0-100), riskLevel?: string (enum RiskLevel), riskLevelLabelKey?: string, availableActions: AvailableActionViewModel[], blockedActions: BlockedActionViewModel[] })
priorityActions: AvailableActionViewModel[]
operationalMap: object ({ nodes: ProcessingActivityNodeViewModel[] })
evidenceSummary: EvidenceSummaryViewModel
gapSummary: GapSummaryViewModel
reviewSummary: ReviewSummaryViewModel
timeline: TimelineEventViewModel[]
exports: ExportOptionViewModel[]
permissions: ResourcePermissionsViewModel
blockedActions: BlockedActionViewModel[]
localization: object (solo metadatos i18n/label keys; no textos finales UI)
```

### `ProcessingActivityListViewModel`

```text
id: string (uuid)
name: string
description?: string
status: string (enum ProcessingActivityStatus)
statusLabelKey: string
version: number (>=1)
updatedAt?: string (date-time)
archivedAt?: string (date-time)
```

### `ProcessingActivityDetailViewModel`

```text
id: string (uuid)
tenantId: string (uuid)
name: string
description?: string
areaId?: string (uuid)
ownerUserId?: string (uuid)
category?: string
status: string (enum ProcessingActivityStatus)
statusLabelKey: string
version: number (>=1)
activeVersionId?: string (uuid)
currentDraftVersionId?: string (uuid)
createdAt: string (date-time)
updatedAt?: string (date-time)
archivedAt?: string (date-time)
```

### `ProcessingActivityNodeViewModel`

```text
processingActivityId: string (uuid)
versionId: string (uuid)
nodeCode: string (enum ProcessingActivityNodeCode)
nodeCodeLabelKey: string
status: string (enum ProcessingActivityNodeStatus)
statusLabelKey: string
completionPercentage: number (0-100)
riskLevel?: string (enum RiskLevel)
riskLevelLabelKey?: string
fields: object
suggestedFields: object
confirmedFields: object
updatedAt: string (date-time)
```

### `EvidenceSummaryViewModel`

```text
processingActivityId: string (uuid)
versionId: string (uuid)
totalRequirements: number (>=0)
pendingCount: number (>=0)
attachedCount: number (>=0)
validatedCount: number (>=0)
insufficientCount: number (>=0)
rejectedCount: number (>=0)
blockingRequirementsCount: number (>=0)
completionPercentage: number (0-100)
```

### `GapSummaryViewModel`

```text
processingActivityId: string (uuid)
versionId: string (uuid)
totalCount: number (>=0)
openCount: number (>=0)
inCorrectionCount: number (>=0)
resolvedCount: number (>=0)
acceptedWithRiskCount: number (>=0)
dismissedCount: number (>=0)
highestSeverity?: string (enum GapSeverity)
highestSeverityLabelKey?: string
approvalBlocked: boolean
```

### `ReviewSummaryViewModel`

```text
processingActivityId: string (uuid)
versionId: string (uuid)
status: string (enum ProcessingActivityVersionStatus)
statusLabelKey: string
lastDecisionCode?: string (enum ReviewDecisionCode)
lastDecisionLabelKey?: string
requiredDomains: Array<object ({ code: string (enum ReviewDomain), labelKey: string })>
pendingDomains: Array<object ({ code: string (enum ReviewDomain), labelKey: string })>
decidedAt?: string (date-time)
```

### `TimelineEventViewModel`

```text
id: string (uuid)
eventType: string (enum AuditEventType)
eventTypeLabelKey: string
resourceType: string
resourceId: string (uuid)
actorUserId: string (uuid)
occurredAt: string (date-time)
result: string (enum AuditEventResult)
resultLabelKey: string
correlationId?: string
metadata?: object
```

### `ExportOptionViewModel`

```text
exportType: string (enum ExportType)
exportTypeLabelKey: string
visibility: string (enum ExportVisibility)
visibilityLabelKey: string
isAvailable: boolean
blockedReasonCode?: string
blockedReasonLabelKey?: string
```

### `ResourcePermissionsViewModel`

```text
roleCodes: Array<object ({ code: string (enum RbacRoleCode), labelKey: string })>
availableActions: AvailableActionViewModel[]
readOnly: boolean
blockedActions: BlockedActionViewModel[]
```

### `AvailableActionViewModel`

```text
action: string (enum PermissionCode)
labelKey: string
```

### `BlockedActionViewModel`

```text
action: string (enum PermissionCode)
actionLabelKey: string
reasonCode: string
labelKey: string
severity: string (enum GapSeverity)
severityLabelKey: string
relatedNode?: string (enum ProcessingActivityNodeCode)
relatedNodeLabelKey?: string
```
