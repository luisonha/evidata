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

# Permisos críticos backend — Evidata

## Propósito de esta sección

Esta sección define los permisos críticos que protegen acciones de alto impacto sobre un `ProcessingActivity`, evidencias, brechas, revisiones y exportaciones oficiales.

No se trata solamente de definir “qué rol puede hacer qué”, sino de establecer un **contrato de autorización verificable** para acciones críticas, considerando:

- usuario autenticado;
- tenant;
- roles y permisos;
- recurso específico;
- estado del tratamiento;
- versión activa o borrador;
- tipo y sensibilidad de evidencia;
- severidad de la brecha;
- políticas configuradas del tenant;
- auditoría obligatoria;
- pruebas de seguridad obligatorias.

La autorización de estas acciones **siempre debe ejecutarse en backend**. El frontend puede mostrar u ocultar acciones, pero nunca debe ser considerado como mecanismo de seguridad.

---

## Principios obligatorios

Toda acción crítica debe cumplir estas condiciones mínimas:

```text
1. Validarse siempre en backend.
2. Requerir un PermissionCode explícito.
3. Evaluar contexto del recurso, no sólo rol global.
4. Generar AuditEvent con resultado Succeeded, Denied o Blocked.
5. Retornar errores consistentes: 403 para falta de permiso, 422 para bloqueo de negocio.
6. Tener test de seguridad obligatorio.
7. No confiar en botones ocultos/deshabilitados en frontend como medida de seguridad.
```

---

## Tabla principal de permisos críticos

| PermissionCode | Qué protege | Regla mínima de autorización | Regla de negocio adicional | Error esperado | Auditoría obligatoria | Test obligatorio |
|---|---|---|---|---|---|---|
| `ApproveProcessingActivity` | Aprobación formal de una versión del tratamiento. | `ProcessOwner` no puede aprobar su propio tratamiento. Sólo roles con capacidad revisora o administrativa pueden aprobar según política. | No se puede aprobar si existen evidencias bloqueantes pendientes, brechas críticas abiertas, revisiones requeridas pendientes o versión modificada después de revisión. | `403 InsufficientPermissions` o `422 BlockingEvidenceMissing / CriticalGapOpen / RequiredReviewPending / VersionModifiedAfterReview` | `ProcessingActivityApproved` o `ApprovalBlocked` | `SEC-APP-001` |
| `ActivateProcessingActivity` | Activación de una versión aprobada como versión vigente. | Sólo `TenantOwner` o `ComplianceAdmin`, según política del tenant. | Sólo una versión `Approved` puede activarse. Al activar, la versión anterior debe quedar `Deprecated`. | `403 InsufficientPermissions` o `422 InvalidStatusTransition` | `ProcessingActivityActivated` | `SEC-ACT-001` |
| `ValidateEvidence` | Validación de evidencia como suficiente para un requerimiento. | Debe validar el reviewer correcto según tipo de evidencia: legal, seguridad, cumplimiento u otro tipo definido. | El dueño del tratamiento no debe validar su propia evidencia cuando la política requiera independencia. Evidencia rechazada no puede validarse sin reemplazo o reapertura. | `403 InsufficientPermissions` o `422 InvalidEvidenceStatus` | `EvidenceValidated` o `EvidenceValidationDenied` | `SEC-EV-001` |
| `AcceptGapWithRisk` | Aceptación formal de una brecha sin corrección completa. | Sólo `TenantOwner` o `ComplianceAdmin`. | Requiere justificación obligatoria. Si la brecha es crítica, puede requerir doble aprobación o política explícita del tenant. | `403 InsufficientPermissions` o `422 ReasonRequired` | `GapAcceptedWithRisk` | `SEC-GAP-001` |
| `GenerateOfficialExport` | Generación de una salida oficial auditable. | Sólo roles autorizados, típicamente `TenantOwner` o `ComplianceAdmin`. | El tratamiento debe estar `Approved` o `Active`, salvo exportaciones preliminares marcadas como no oficiales. Debe registrar advertencias si hay riesgos, brechas o evidencias pendientes. | `403 InsufficientPermissions` o `422 OfficialExportRequiresApproval` | `OfficialExportGenerated` o `ExportGenerationBlocked` | `SEC-EXP-001` |
| `DownloadEvidence` | Descarga de archivos o evidencias asociadas al tratamiento. | El usuario debe tener permiso de lectura sobre la evidencia y el tratamiento. | `Viewer` no puede descargar evidencia sensible o confidencial. Toda descarga debe auditarse. | `403 SensitiveEvidenceRestricted / InsufficientPermissions` | `EvidenceDownloaded` o `EvidenceAccessDenied` | `SEC-EVDOWN-001` |

---

## Detalle por permiso

## `ApproveProcessingActivity`

Este permiso controla la aprobación formal de una versión del tratamiento. No debe confundirse con guardar cambios, enviar a revisión o comentar una revisión.

La aprobación significa:

```text
La organización considera que la versión revisada del tratamiento está lista para quedar aprobada, sujeta a posterior activación si corresponde.
```

### Reglas mínimas

```text
- El usuario debe pertenecer al mismo tenant.
- El usuario debe tener `ApproveProcessingActivity`.
- El usuario no puede aprobar su propio tratamiento si es `ProcessOwner`.
- La versión debe estar en estado aprobable.
- No deben existir blockers activos.
- Deben existir las revisiones requeridas aprobadas.
- La versión no debe haber cambiado después de la revisión.
```

### Ejemplos de bloqueo

```text
- Falta evidencia bloqueante.
- Existe brecha crítica abierta.
- Falta revisión legal.
- Falta revisión de seguridad.
- La versión fue modificada después de la revisión.
```

### Resultado esperado

```text
Success:
ProcessingActivityVersion.status = Approved
ProcessingActivity.status = Approved
AuditEvent = ProcessingActivityApproved

Blocked:
HTTP 422
code = BlockingEvidenceMissing | CriticalGapOpen | RequiredReviewPending | VersionModifiedAfterReview
AuditEvent = ApprovalBlocked

Denied:
HTTP 403
code = InsufficientPermissions
AuditEvent = AccessDenied o ApprovalDenied
```

### Test mínimo

```text
SEC-APP-001:
Dado un tratamiento cuyo owner es ProcessOwner,
cuando ese mismo ProcessOwner intenta aprobarlo,
entonces el backend responde 403 InsufficientPermissions
y registra AuditEvent con result = Denied.
```

---

## `ActivateProcessingActivity`

Este permiso controla la activación de una versión aprobada. Activar no es lo mismo que aprobar.

La activación significa:

```text
La versión aprobada pasa a ser la versión vigente del tratamiento.
```

### Reglas mínimas

```text
- Sólo `TenantOwner` o `ComplianceAdmin`, según política.
- La versión debe estar `Approved`.
- No se debe activar una versión `Draft`, `InReview`, `ChangesRequested`, `Archived` o `Deprecated`.
- Al activar una nueva versión, la versión activa anterior debe quedar `Deprecated`.
- Debe actualizarse `activeVersionId`.
```

### Resultado esperado

```text
Success:
ProcessingActivity.status = Active
selectedVersion.status = Active
previousActiveVersion.status = Deprecated
AuditEvent = ProcessingActivityActivated

Blocked:
HTTP 422
code = InvalidStatusTransition

Denied:
HTTP 403
code = InsufficientPermissions
```

### Test mínimo

```text
SEC-ACT-001:
Dado un usuario ProcessOwner con tratamiento aprobado,
cuando intenta activar la versión,
entonces el backend responde 403
y no modifica activeVersionId.
```

---

## `ValidateEvidence`

Este permiso controla la validación de evidencia. No basta con que el usuario pueda ver o adjuntar evidencia.

Validar evidencia significa:

```text
Un rol autorizado declara que la evidencia adjunta es suficiente para el requerimiento correspondiente.
```

### Reglas mínimas

```text
- El usuario debe tener `ValidateEvidence`.
- El tipo de evidencia debe corresponder al scope del reviewer.
- LegalReviewer valida evidencias legales.
- SecurityReviewer valida evidencias de seguridad.
- ComplianceAdmin puede validar evidencias de cumplimiento según política.
- ProcessOwner puede adjuntar evidencia, pero no necesariamente validarla.
- Viewer nunca valida evidencia.
```

### Ejemplos de scope

```text
Contrato de encargado        → LegalReviewer
Política de seguridad        → SecurityReviewer
Registro RAT                 → ComplianceAdmin / LegalReviewer según política
Evidencia técnica de cifrado  → SecurityReviewer
```

### Resultado esperado

```text
Success:
Evidence.status = Validated
EvidenceValidation.created
AuditEvent = EvidenceValidated

Blocked:
HTTP 422
code = InvalidEvidenceStatus

Denied:
HTTP 403
code = InsufficientPermissions
```

### Test mínimo

```text
SEC-EV-001:
Dada una evidencia de tipo Security,
cuando un LegalReviewer intenta validarla,
entonces el backend responde 403
y la evidencia conserva su estado anterior.
```

---

## `AcceptGapWithRisk`

Este permiso es especialmente sensible porque permite cerrar o mantener una brecha aceptando el riesgo asociado.

Aceptar una brecha con riesgo significa:

```text
La organización decide no corregir completamente la brecha en este momento y deja constancia formal de la aceptación del riesgo.
```

### Reglas mínimas

```text
- Sólo `TenantOwner` o `ComplianceAdmin`.
- Debe existir justificación obligatoria.
- Debe registrar quién aceptó el riesgo.
- Debe registrar fecha, motivo, severidad y estado previo.
- No debe eliminar la brecha ni borrar su historial.
- Si la brecha es crítica, puede requerir política especial.
```

### Resultado esperado

```text
Success:
Gap.status = AcceptedWithRisk
GapResolution.created
AuditEvent = GapAcceptedWithRisk

Blocked:
HTTP 422
code = ReasonRequired

Denied:
HTTP 403
code = InsufficientPermissions
```

### Test mínimo

```text
SEC-GAP-001:
Dado un gap crítico abierto,
cuando un ComplianceAdmin intenta aceptarlo sin justificación,
entonces el backend responde 422 ReasonRequired
y el gap permanece Open.
```

---

## `GenerateOfficialExport`

Este permiso controla exportaciones oficiales, no simples vistas preliminares.

Una exportación oficial significa:

```text
Un documento o salida generada por Evidata que puede usarse como evidencia formal ante auditoría, fiscalización o revisión interna.
```

### Reglas mínimas

```text
- Sólo roles autorizados.
- El tratamiento debe estar `Approved` o `Active`.
- Si existen advertencias, deben incluirse como ExportWarning.
- Debe quedar AuditEvent.
- Debe quedar trazabilidad del usuario, versión, fecha, tipo de exportación y parámetros.
```

### Tipos de exportación oficial

```text
TreatmentPdfSummary
ApprovalHistory
GlobalRatExcel
```

### Exportaciones técnicas no visibles

```text
InternalJson
```

### Resultado esperado

```text
Success:
ExportRequest.created
ExportFile.generated
AuditEvent = OfficialExportGenerated

Blocked:
HTTP 422
code = OfficialExportRequiresApproval

Denied:
HTTP 403
code = InsufficientPermissions
```

### Test mínimo

```text
SEC-EXP-001:
Dado un tratamiento en Draft,
cuando un ComplianceAdmin intenta generar una exportación oficial,
entonces el backend responde 422 OfficialExportRequiresApproval.
```

---

## `DownloadEvidence`

Este permiso protege la descarga de evidencias, especialmente evidencias sensibles.

Descargar evidencia significa:

```text
Acceder al archivo o contenido probatorio fuera de la vista resumida del sistema.
```

### Reglas mínimas

```text
- El usuario debe tener `DownloadEvidence`.
- El usuario debe tener acceso al tratamiento.
- La sensibilidad de la evidencia debe evaluarse.
- Viewer no puede descargar evidencia sensible.
- Toda descarga debe auditarse.
- Un acceso denegado también debe auditarse.
```

### Clasificación mínima sugerida

```text
Public
Internal
Confidential
Sensitive
```

### Resultado esperado

```text
Success:
Archivo entregado mediante URL segura o stream controlado.
AuditEvent = EvidenceDownloaded

Denied:
HTTP 403
code = SensitiveEvidenceRestricted
AuditEvent = EvidenceAccessDenied
```

### Test mínimo

```text
SEC-EVDOWN-001:
Dada una evidencia Sensitive,
cuando un Viewer intenta descargarla,
entonces el backend responde 403 SensitiveEvidenceRestricted
y registra EvidenceAccessDenied.
```

---

# Contrato esperado para autorización

Cada acción crítica debería pasar por un servicio de autorización, no por validaciones dispersas en controllers/functions.

## Contexto de autorización

```csharp
public sealed record AuthorizationContext(
    Guid TenantId,
    Guid UserId,
    IReadOnlyCollection<string> RoleCodes,
    string PermissionCode,
    string ResourceType,
    Guid ResourceId,
    Guid? ProcessingActivityId,
    Guid? VersionId,
    string? ResourceStatus,
    string? EvidenceType,
    string? EvidenceSensitivity,
    string? GapSeverity,
    string? ReviewType,
    string? ActionReason
);
```

## Decisión de autorización

```csharp
public sealed record AuthorizationDecision(
    bool IsAllowed,
    string? DenyReasonCode,
    string? BlockedReasonCode,
    IReadOnlyCollection<BlockedAction> BlockedActions,
    bool RequiresReason,
    bool RequiresComment,
    bool RequiresAudit
);
```

## Regla de resultado HTTP

```text
IsAllowed = false por permiso insuficiente → HTTP 403
IsAllowed = true pero bloqueado por regla de negocio → HTTP 422
```

---

# Relación con frontend

El frontend puede mostrar botones habilitados, deshabilitados o razones de bloqueo, pero eso no reemplaza la autorización backend.

El backend debe devolver permisos y bloqueos en los ViewModels relevantes.

Ejemplo:

```json
{
  "permissions": {
    "availableActions": [
      "ViewProcessingActivity",
      "AttachEvidence"
    ],
    "blockedActions": [
      {
        "action": "ApproveProcessingActivity",
        "reasonCode": "BlockingEvidenceMissing",
        "labelKey": "blockedReason.blockingEvidenceMissing",
        "severity": "Critical",
        "relatedNode": "Evidence"
      }
    ]
  }
}
```

La UI sólo interpreta:

```text
- qué acciones mostrar;
- cuáles deshabilitar;
- qué explicación presentar;
- qué CTA ofrecer.
```

Pero la seguridad real vive en backend.

---

# Tabla resumida para documentación ejecutiva

| PermissionCode | Objetivo | Regla crítica | Backend debe validar | Test obligatorio |
|---|---|---|---|---|
| `ApproveProcessingActivity` | Aprobar versión revisada del tratamiento. | El owner no aprueba su propio tratamiento. | Rol, owner, estado, blockers, revisiones, versión no modificada. | `SEC-APP-001` |
| `ActivateProcessingActivity` | Activar versión aprobada. | Sólo roles autorizados. | Estado `Approved`, política del tenant, deprecación de versión anterior. | `SEC-ACT-001` |
| `ValidateEvidence` | Validar evidencia como suficiente. | Reviewer correcto según tipo. | Tipo, sensibilidad, estado, independencia del validador. | `SEC-EV-001` |
| `AcceptGapWithRisk` | Aceptar brecha con riesgo. | Sólo TenantOwner/ComplianceAdmin y con justificación. | Severidad, estado, razón obligatoria, política para críticos. | `SEC-GAP-001` |
| `GenerateOfficialExport` | Emitir salida oficial. | Sólo roles autorizados y tratamiento aprobado/activo. | Tipo de exportación, estado, warnings, trazabilidad. | `SEC-EXP-001` |
| `DownloadEvidence` | Descargar evidencia. | Viewer no descarga evidencia sensible. | Sensibilidad, permiso, acceso al tratamiento, auditoría. | `SEC-EVDOWN-001` |

---

# Resultado esperado de esta sección

Esta sección busca asegurar que Evidata proteja acciones críticas mediante reglas backend verificables, trazables y testeables.

Los objetivos son:

```text
- impedir conflictos de interés;
- evitar aprobaciones indebidas;
- asegurar separación de funciones;
- proteger evidencia sensible;
- evitar aceptación informal de riesgos;
- controlar exportaciones oficiales;
- garantizar auditoría de acciones exitosas, denegadas o bloqueadas;
- convertir cada permiso crítico en pruebas obligatorias de seguridad.
```

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
