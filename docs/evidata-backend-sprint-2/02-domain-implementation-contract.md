# 02 — Contrato de dominio e implementación backend

## 1. Principio

El dominio backend debe organizarse alrededor de `ProcessingActivity`. La UI puede mostrar “Tratamiento”, pero el backend no debe modelar `Treatment` como agregado canónico.

## 2. Mapeo contra backend existente

| Concepto objetivo | Implementación actual esperada/detectada | Decisión | Acción |
|---|---|---|---|
| `ProcessingActivity` | `ProcessingInventory/Domain/ProcessingActivity.cs` | Mantener y alinear | ExistingModify |
| `ProcessingActivityVersion` | `ProcessingActivity.Version` + `ProcessingActivity.SupersedesId` + `ProcessingActivitySnapshot` + `IProcessingActivityVersionService`/`ProcessingActivityVersionService` | Adoptar como estrategia oficial sin duplicar infraestructura | ExistingModify |
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

## ADR: Estrategia de versionado de ProcessingActivity

### Estado

Aceptado para esta iteración.

### Contexto

El contrato original de este documento asumía que `ProcessingActivityVersion` aún no estaba confirmado como implementación real. La revisión del código actual muestra que el versionado ya está resuelto con cuatro piezas concretas:

- `ProcessingActivity` modela cada versión como una fila completa del agregado, con `Version`, `SupersedesId`, `ApprovedBy` y `ApprovedAt`.
- `ProcessingActivity.CreateNewVersion(Guid createdBy)` crea una nueva versión `Draft`, incrementa `Version` y enlaza la versión previa vía `SupersedesId`.
- `ProcessingActivitySnapshot` captura una copia inmutable al aprobar, incluyendo `Version`, `ApprovedBy`, `ApprovedAt` y `Payload` JSON.
- `IProcessingActivityVersionService` / `ProcessingActivityVersionService` coordinan `ApproveAndSnapshotAsync`, `CreateNewVersionAsync` y `GetSnapshotsAsync`.

### Decisión

Se adopta la infraestructura de versionado ya existente como estrategia oficial del sprint, sin crear una entidad paralela adicional llamada `ProcessingActivityVersion` ni duplicar infraestructura.

El término canónico `ProcessingActivityVersion` de los documentos del sprint se debe interpretar, para esta base de código, como un modelo compuesto por:

1. la instancia actual de `ProcessingActivity` (que representa una versión editable o aprobada),
2. la cadena de reemplazo entre versiones vía `SupersedesId`,
3. los metadatos de aprobación (`ApprovedBy`, `ApprovedAt`),
4. y el historial inmutable persistido en `ProcessingActivitySnapshot`.

No hace falta duplicar ni reemplazar nada porque el código ya cubre los objetivos esenciales del contrato: bloqueo de edición sobre aprobados, creación de nueva versión a partir de un aprobado, preservación de historial y servicio de orquestación transaccional para aprobación + snapshot.

### Mapeo al modelo canónico y a `/control`

#### Interpretación canónica

| Concepto canónico esperado | Implementación real adoptada |
|---|---|
| `ProcessingActivityVersion.id` | `ProcessingActivity.Id` de la fila que representa esa versión |
| `ProcessingActivityVersion.version` | `ProcessingActivity.Version` |
| relación con versión anterior | `ProcessingActivity.SupersedesId` |
| aprobación de versión | `ProcessingActivity.ApprovedBy` + `ProcessingActivity.ApprovedAt` |
| historial inmutable | `ProcessingActivitySnapshot` |
| estado congelado aprobado | `ProcessingActivitySnapshot.Payload` (JSON) |

#### Mapeo de campos al contrato de la sección 9 de `03-control-viewmodel-and-ui-contract.md`

| Campo real | Estado en doc 03 | Decisión ADR |
|---|---|---|
| `Version` (`int`) | Ya existe en `ProcessingActivityDetailViewModel.version` y `ProcessingActivityControlViewModel.version.version` | Se mantiene tal cual. |
| `SupersedesId` | No estaba declarado en sección 9 | Se agrega como campo opcional de metadata de versión. |
| `ApprovedAt` | No estaba declarado en sección 9 | Se agrega como campo opcional de metadata de versión. |
| `ApprovedBy` | No estaba declarado en sección 9 | Se agrega como campo opcional de metadata de versión. |
| `ProcessingActivitySnapshot.Payload` | No estaba declarado en `/control` | No se expone en `/control`; queda como artefacto de auditoría/historial. |

Regla de exposición adoptada:

- `/control.processingActivity` representa la versión actual como detalle del agregado.
- `/control.version` representa el metadata de versionado de esa misma fila.
- `Payload` del snapshot no debe inyectarse en `/control`, porque es un freeze completo para auditoría y comparación, no un read model operativo principal.

### Alcance de la operación "nueva versión" en P1

`CreateNewVersionAsync` existe en la capa de aplicación/infraestructura, pero no aparece hoy como operación HTTP cerrada en la traceability matrix ni en el backlog P1 con `operationId` propio.

Por evidencia documental de esta iteración:

- sí está trazada la aprobación (`approveProcessingActivity` → `ApproveAndSnapshotAsync`);
- no está trazada una operación pública específica para "crear nueva versión";
- no está trazado un endpoint para historial de snapshots.

Por tanto, en este sprint P1 la capacidad de "nueva versión" queda formalizada como capacidad interna existente del dominio/servicio, pero no como operación API pública comprometida por contrato. Si se quiere exponer, debe entrar después como endpoint explícito y agregarse a la traceability matrix.

### Brechas y ambigüedades conocidas

1. **Sin endpoint HTTP para historial**: `GetSnapshotsAsync` existe, pero la traceability matrix no declara un endpoint para listar snapshots/version history.
2. **Sin endpoint HTTP explícito para crear nueva versión**: `CreateNewVersionAsync` existe, pero no hay `operationId` ni ruta `/api/v1/...` trazada para esa acción.
3. **Desacople con `activeVersionId/currentDraftVersionId`**: la sección 9 del doc 03 usa esos campos, pero el código actual no mantiene esos punteros como propiedades persistidas del agregado; hoy sólo son proyectables/derivables, no fuente primaria.
4. **Desacople con estado `Active`**: la traceability matrix ya marca `activateProcessingActivity` como `DecisionRequired`, y el agregado real sólo modela `Draft → UnderReview → Approved → Archived`.
5. **Parámetro no aplicado en servicio**: `ApproveAndSnapshotAsync` recibe `retentionRequired`, pero la implementación actual no lo usa; las precondiciones de aprobación quedan delegadas al validador del agregado. Esto debe tratarse como ambigüedad abierta, no como regla ya resuelta.
