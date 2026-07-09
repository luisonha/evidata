# 02 — Contrato de dominio e implementación backend

## 1. Principio

El dominio backend debe organizarse alrededor de `ProcessingActivity`. La UI puede mostrar “Tratamiento”, pero el backend no debe modelar `Treatment` como agregado canónico.

## 2. Mapeo contra backend existente

| Concepto objetivo | Implementación actual esperada/detectada | Decisión | Acción |
|---|---|---|---|
| `ProcessingActivity` | `ProcessingInventory/Domain/ProcessingActivity.cs` | Mantener y alinear | ExistingModify |
| `ProcessingActivityVersion` | No confirmado | Crear | New |
| `ProcessingActivityNode` | RAT sections/flags/systems | Mapear como proyección o entidad formal | ExistingModify/New |
| `ProcessingActivityTemplate` | Parcial/no confirmado | Crear o alinear | New/ExistingModify |
| `Evidence` | Evidence module | Mantener y alinear | ExistingModify |
| `EvidenceRequirement` | Crear con `reviewDomain` técnico (`Legal`/`Security`) | Crear | New |
| `EvidenceValidation` | No confirmado/parcial | Crear o formalizar | New/ExistingModify |
| `Gap` | GapManagement | Mantener y alinear | ExistingModify |
| `GapRule` | RatGapDetection | Formalizar regla | ExistingModify |
| `Review` | Workflow/Review parcial | Alinear | ExistingModify |
| `ReviewDecision` | Crear como registro inmutable asociado a `Review` | Crear | New |
| `AuditEvent` | AuditLog/Audit module | Mapear conceptualmente a implementación real | ExistingModify |
| `TimelineEvent` | No confirmado | Crear proyección | New |
| `ExportRequest` | Reporting | Mapear contrato público a Reporting | ExistingModify |

### `ReviewDecision`

`ReviewDecision` no tiene workflow propio. El workflow vive en `Review`; `ReviewDecision` es append-only.

Campos mínimos:

```text
id
reviewId
processingActivityId
decisionCode
comment
decidedByUserId
decidedAt
supersedesDecisionId
metadata
```

Valores permitidos para `decisionCode`:

```text
Approved
ChangesRequested
Rejected
```

## 3. Entidades mínimas de ProcessingActivity

`ProcessingActivity` debe tener, al menos:

```text
id
tenantId
name
description
areaId
ownerUserId
category
status
activeVersionId
currentDraftVersionId
createdAt
updatedAt
archivedAt
```

## 4. Versionado

`ProcessingActivityVersion` debe resolver:

- edición segura;
- activación de versión aprobada;
- preservación de historial;
- bloqueo de edición directa de versión activa;
- relación con revisiones y evidencias.

Estados mínimos:

```text
Draft
InReview
ChangesRequested
Approved
Active
Deprecated
Archived
```

Reglas:

| Regla | Descripción |
|---|---|
| Active read-only | Una versión activa no se edita directamente. |
| Active modification | Editar una actividad activa crea nueva versión Draft. |
| Approved activation | Sólo una versión Approved puede activar. |
| Previous deprecation | Al activar una versión, la anterior queda Deprecated. |
| Archived read-only | Lo archivado es sólo lectura. |

## 5. Nodos operativos

`ProcessingActivityNode` debe representar o proyectar las áreas del RAT:

```text
Purpose
LegalBasis
DataCategories
DataSubjects
Systems
Providers
Transfers
Retention
Security
Evidence
Gaps
Review
Export
```

Cada nodo debe exponer:

```text
processingActivityId
versionId
nodeCode
status
completionPercentage
riskLevel
fields
suggestedFields
confirmedFields
updatedAt
```

Si el repo ya tiene `RatSections`, `RatFlags` o `RatSistemas`, no se deben duplicar sin análisis. Deben mapearse a `ProcessingActivityNode` o usarse como fuente de proyección.

### 5.1 Catalogo cerrado y tipado

Para `ProcessingActivityNode`, el catálogo de `nodeCode` debe ser cerrado a estos 13 valores y `fields` debe resolverse con tipo fuertemente tipado por nodo (por ejemplo, `PurposeNodeFields`, `LegalBasisNodeFields`, etc.), no con `Dictionary<string,object>`.

| nodeCode | fuente de datos actual | tipo de fields | formula completionPercentage | formula riskLevel |
|---|---|---|---|---|
| `Purpose` | `ProcessingActivity.Purpose.Purpose` (`PurposeSection`, columna `purpose_text`) | Tipo fuertemente tipado por nodo (`PurposeNodeFields`) | campos obligatorios completos del nodo / 1 (`purpose`) | `Critical` si el nodo tiene algún gap abierto severity `Critical`; `High` si no tiene `Critical` pero sí `High`; `Medium` si no tiene `Critical/High` pero sí `Medium`; `Low` en otro caso |
| `LegalBasis` | `ProcessingActivity.Purpose.LegalBasis` + `ProcessingActivity.Purpose.LegalBasisJustification` (columnas `legal_basis`, `legal_basis_justification`) | Tipo fuertemente tipado por nodo (`LegalBasisNodeFields`) | campos obligatorios completos del nodo / 2 (`legalBasis`, `legalBasisJustification`) | `Critical` si existe gap abierto asociado a `LEGAL_BASIS_MISSING`; si no, misma regla de severidad máxima abierta del nodo |
| `DataCategories` | `ProcessingActivity._dataCategories` / `ProcessingActivity.DataCategories` (`data_categories` JSONB) | Tipo fuertemente tipado por nodo (`DataCategoriesNodeFields`) | entradas válidas con `DataCategoryId` / mínimo obligatorio del nodo (1) | `High` si existe gap abierto asociado a `DATA_CATEGORIES_EMPTY`; en otro caso, severidad máxima abierta del nodo |
| `DataSubjects` | `ProcessingActivity._dataSubjects` / `ProcessingActivity.DataSubjects` (`data_subjects` JSONB) | Tipo fuertemente tipado por nodo (`DataSubjectsNodeFields`) | entradas válidas con `SubjectType` / mínimo obligatorio del nodo (1) | severidad máxima de gaps abiertos asociados al nodo; si no hay gaps asociados, `Low` |
| `Systems` | `ProcessingActivity._systems` / `ProcessingActivity.Systems` (`systems` JSONB, `SystemEntry`) | Tipo fuertemente tipado por nodo (`SystemsNodeFields`) | campos obligatorios completos de cada entrada / campos obligatorios totales cargados (`systemName`) | severidad máxima de gaps abiertos asociados al nodo; si no hay gaps asociados, `Low` |
| `Providers` | `ProcessingActivity._suppliers` / `ProcessingActivity.Suppliers` (`suppliers` JSONB, `SupplierEntry`) | Tipo fuertemente tipado por nodo (`ProvidersNodeFields`) | campos obligatorios completos de cada entrada / campos obligatorios totales cargados (`supplierName`) | severidad máxima de gaps abiertos asociados al nodo; si no hay gaps asociados, `Low` |
| `Transfers` | `ProcessingActivity.HasInternationalTransfer` + `ProcessingActivity.Flags.InternationalTransfer`; detalle de país receptor/garantía/destinatario: `Nuevo` | Tipo fuertemente tipado por nodo (`TransfersNodeFields`) | campos obligatorios completos del nodo / total obligatorio del nodo cuando `hasInternationalTransfer=true`; si `false`, 100% | `Critical` si existe gap abierto `TRANSFER_WITHOUT_DESTINATION_COUNTRY`, `TRANSFER_WITHOUT_SAFEGUARD` o `TRANSFER_WITHOUT_BLOCKING_EVIDENCE`; `High` si existe `TRANSFER_WITHOUT_RECEIVER`; en otro caso, severidad máxima abierta del nodo |
| `Retention` | `ProcessingActivity.Retention` (`RetentionSection`, columnas `retention_period_description`, `retention_months`, `retention_legal_justification`) | Tipo fuertemente tipado por nodo (`RetentionNodeFields`) | campos obligatorios completos del nodo / 1 (`periodDescription`) | `High` si existe gap abierto asociado a `RETENTION_UNDEFINED`; en otro caso, severidad máxima abierta del nodo |
| `Security` | `ProcessingActivity._securityMeasures` / `ProcessingActivity.SecurityMeasures` (`security_measures` JSONB, `SecurityMeasureEntry`) | Tipo fuertemente tipado por nodo (`SecurityNodeFields`) | entradas válidas con `measureType` y `description` / entradas obligatorias totales del nodo | `Critical` si existe gap abierto asociado a `SENSITIVE_DATA_WITHOUT_SECURITY_REVIEW`; en otro caso, severidad máxima abierta del nodo |
| `Evidence` | `EvidenceLink` (`LinkedEntityType.ProcessingActivity`, `LinkedEntityId`) + `Evidence` (`Type`, `Status`, `Sensitivity`, `Title`) vía `RatEvidenceService` | Tipo fuertemente tipado por nodo (`EvidenceNodeFields`) | evidencias obligatorias vinculadas y activas / evidencias obligatorias totales del nodo | `Critical` si existe gap abierto `TRANSFER_WITHOUT_BLOCKING_EVIDENCE`; `High` si `ProcessingActivity.Flags.MissingLegalBasisEvidence=true`; en otro caso, severidad máxima abierta del nodo |
| `Gaps` | `ComplianceGap` (`SourceModule="ProcessingInventory"`, `SourceEntityId=ProcessingActivity.Id`) | Tipo fuertemente tipado por nodo (`GapsNodeFields`) | gaps cerrados, resueltos o `AcceptedRisk` / gaps totales del nodo | severidad máxima de gaps abiertos del nodo (`Critical` > `High` > `Medium` > `Low`) |
| `Review` | `Review` (`TargetModule="ProcessingInventory"`, `TargetEntityType="ProcessingActivity"`, `TargetEntityId=ProcessingActivity.Id`) | Tipo fuertemente tipado por nodo (`ReviewNodeFields`) | hitos obligatorios completos / 3 (`created`, `started`, `completed`) | `High` si hay review `Open` o `InProgress` sobre un RAT con flags `RequiresEnhancedReview`; `Medium` si hay review abierta sin esos flags; `Low` si la última review está `Approved` |
| `Export` | `Reporting.ReportJob` (`ReportType`, `Parameters`, `Status`, `ArtifactDocumentId`); vínculo explícito por `ProcessingActivityId`: `Nuevo` | Tipo fuertemente tipado por nodo (`ExportNodeFields`) | campos obligatorios completos del nodo / 2 (`reportType`, `parameters`); sumar artefacto emitido cuando `Status=Completed` | severidad máxima de gaps abiertos asociados al nodo; si no hay gaps asociados, `Low` |

## 6. Reglas de naming

Permitido backend:

```text
ProcessingActivity
ProcessingActivityVersion
ProcessingActivityNode
ProcessingActivityTemplate
```

Prohibido backend:

```text
Treatment
TreatmentVersion
TreatmentNode
TreatmentEvidence
TreatmentGap
/api/v1/treatments
```

Permitido frontend/UI:

```text
Tratamiento
Tratamientos
TreatmentControlViewModel como alias TypeScript de ProcessingActivityControlViewModel
```

## 7. Guardas de regresión

Deben existir validaciones de CI para detectar:

- `class Treatment` en backend;
- `record Treatment` en backend;
- `TreatmentVersion`;
- `TreatmentEvidence`;
- `/api/v1/treatments`;
- `/api/treatments`;
- `/weatherforecast` en contrato público.
