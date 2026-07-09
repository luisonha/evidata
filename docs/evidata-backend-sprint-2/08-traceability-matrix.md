# 08 — Traceability matrix backend v1

Fuentes revisadas: `01-api-contract-v1.md` (§3), `05-implementation-backlog.md`, `06-quality-gates-and-dod.md`, más los controllers/handlers actuales en `src/Evidata.Api` y módulos `ProcessingInventory`, `Evidence`, `GapManagement`, `Workflow`, `Reporting` y `Audit`.

Criterio usado:

- `ExistingModify`: existe base legacy o capacidad cercana y debe versionarse/alinearse.
- `New`: el endpoint/caso de uso no está expuesto hoy y requiere handler/API nueva.
- `DecisionRequired`: el contrato no calza todavía de forma cerrada con el modelo/ciclo de vida actual.
- `security`: contrato objetivo uniforme `bearer JWT`.

| operationId | metodo+ruta /api/v1 | handler real o planificado (namespace.Clase.Metodo) | security (bearer JWT) | x-change-status | test id sugerido | gate (PR/Main/Release) |
|---|---|---|---|---|---|---|
| listProcessingActivities | `GET /processing-activities` | `Evidata.Modules.ProcessingInventory.Application.Queries.ListProcessingActivitiesQueryHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-PA-001` | Main |
| createProcessingActivity | `POST /processing-activities` | `Evidata.Modules.ProcessingInventory.Application.Commands.CreateProcessingActivityCommandHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-PA-002` | Main |
| getProcessingActivityById | `GET /processing-activities/{processingActivityId}` | `Evidata.Modules.ProcessingInventory.Application.Queries.GetProcessingActivityQueryHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-PA-003` | Main |
| updateProcessingActivity | `PATCH /processing-activities/{processingActivityId}` | `Planificado: Evidata.Modules.ProcessingInventory.Domain.ProcessingActivity.Update` | bearer JWT | New | `TC-API-PA-004` | Main |
| getProcessingActivityControl | `GET /processing-activities/{processingActivityId}/control` | `Pendiente creación (T-P1-03)` | bearer JWT | New | `TC-API-PA-005` | Release |
| patchProcessingActivityNode | `PATCH /processing-activities/{processingActivityId}/nodes/{nodeCode}` | `Pendiente creación (T-P1-08)` | bearer JWT | New | `TC-API-PA-006` | Main |
| sendProcessingActivityToReview | `POST /processing-activities/{processingActivityId}/send-to-review` | `Planificado: Evidata.Modules.ProcessingInventory.Domain.ProcessingActivity.SubmitForReview` | bearer JWT | New | `TC-API-PA-007` | Main |
| approveProcessingActivity | `POST /processing-activities/{processingActivityId}/approve` | `Planificado: Evidata.Modules.ProcessingInventory.Infrastructure.Versioning.ProcessingActivityVersionService.ApproveAndSnapshotAsync` | bearer JWT | New | `TC-API-PA-008` | Main |
| activateProcessingActivity | `POST /processing-activities/{processingActivityId}/activate` | `Pendiente creación` | bearer JWT | DecisionRequired | `TC-API-PA-009` | Main |
| archiveProcessingActivity | `POST /processing-activities/{processingActivityId}/archive` | `Planificado: Evidata.Modules.ProcessingInventory.Domain.ProcessingActivity.Archive` | bearer JWT | New | `TC-API-PA-010` | Main |
| listProcessingActivityTemplates | `GET /processing-activity-templates` | `Pendiente creación (T-P1-06)` | bearer JWT | New | `TC-API-TPL-001` | Main |
| getProcessingActivityTemplateById | `GET /processing-activity-templates/{templateId}` | `Pendiente creación (T-P1-06)` | bearer JWT | New | `TC-API-TPL-002` | Main |
| listProcessingActivityEvidence | `GET /processing-activities/{processingActivityId}/evidence` | `Evidata.Modules.Evidence.Application.Queries.ListEvidenceQueryHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-EVD-001` | Main |
| createProcessingActivityEvidence | `POST /processing-activities/{processingActivityId}/evidence` | `Evidata.Modules.Evidence.Application.Commands.CreateEvidenceCommandHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-EVD-002` | Main |
| validateProcessingActivityEvidence | `POST /processing-activities/{processingActivityId}/evidence/{evidenceId}/validate` | `Pendiente creación (T-P1-13)` | bearer JWT | New | `TC-API-EVD-003` | Main |
| markProcessingActivityEvidenceInsufficient | `POST /processing-activities/{processingActivityId}/evidence/{evidenceId}/mark-insufficient` | `Pendiente creación (T-P1-13)` | bearer JWT | New | `TC-API-EVD-004` | Main |
| rejectProcessingActivityEvidence | `POST /processing-activities/{processingActivityId}/evidence/{evidenceId}/reject` | `Pendiente creación (T-P1-13)` | bearer JWT | New | `TC-API-EVD-005` | Main |
| replaceProcessingActivityEvidence | `POST /processing-activities/{processingActivityId}/evidence/{evidenceId}/replace` | `Pendiente creación (T-P1-12/T-P1-13)` | bearer JWT | New | `TC-API-EVD-006` | Main |
| listProcessingActivityGaps | `GET /processing-activities/{processingActivityId}/gaps` | `Evidata.Modules.GapManagement.Application.Queries.ListGapsQueryHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-GAP-001` | Main |
| createProcessingActivityGap | `POST /processing-activities/{processingActivityId}/gaps` | `Evidata.Modules.GapManagement.Application.Commands.CreateGapCommandHandler.HandleAsync` | bearer JWT | ExistingModify | `TC-API-GAP-002` | Main |
| markGapInCorrection | `POST /processing-activities/{processingActivityId}/gaps/{gapId}/mark-in-correction` | `Pendiente creación (T-P1-15)` | bearer JWT | New | `TC-API-GAP-003` | Main |
| resolveProcessingActivityGap | `POST /processing-activities/{processingActivityId}/gaps/{gapId}/resolve` | `Planificado: Evidata.Modules.GapManagement.Domain.ComplianceGap.Resolve` | bearer JWT | New | `TC-API-GAP-004` | Main |
| acceptProcessingActivityGapWithRisk | `POST /processing-activities/{processingActivityId}/gaps/{gapId}/accept-with-risk` | `Planificado: Evidata.Modules.GapManagement.Domain.ComplianceGap.AcceptRisk` | bearer JWT | New | `TC-API-GAP-005` | Main |
| dismissProcessingActivityGap | `POST /processing-activities/{processingActivityId}/gaps/{gapId}/dismiss` | `Pendiente creación (T-P1-15)` | bearer JWT | DecisionRequired | `TC-API-GAP-006` | Main |
| listProcessingActivityReviews | `GET /processing-activities/{processingActivityId}/reviews` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.GetOpenReviewsForEntityAsync` | bearer JWT | New | `TC-API-REV-001` | Main |
| createProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.CreateAsync` | bearer JWT | New | `TC-API-REV-002` | Main |
| takeProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/take` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.StartAsync` | bearer JWT | New | `TC-API-REV-003` | Main |
| requestChangesProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/request-changes` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.RequestChangesAsync` | bearer JWT | New | `TC-API-REV-004` | Main |
| approveProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/approve` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.ApproveAsync` | bearer JWT | New | `TC-API-REV-005` | Main |
| rejectProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/reject` | `Pendiente creación (T-P1-10)` | bearer JWT | DecisionRequired | `TC-API-REV-006` | Main |
| cancelProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/cancel` | `Planificado: Evidata.Modules.Workflow.Infrastructure.Reviews.ReviewService.CancelAsync` | bearer JWT | New | `TC-API-REV-007` | Main |
| reopenProcessingActivityReview | `POST /processing-activities/{processingActivityId}/reviews/{reviewId}/reopen` | `Pendiente creación (T-P1-10)` | bearer JWT | DecisionRequired | `TC-API-REV-008` | Main |
| listProcessingActivityExports | `GET /processing-activities/{processingActivityId}/exports` | `Evidata.Modules.Reporting.Infrastructure.Jobs.ReportJobService.GetByTenantAsync` | bearer JWT | ExistingModify | `TC-API-EXP-001` | Main |
| createProcessingActivityExport | `POST /processing-activities/{processingActivityId}/exports` | `Evidata.Modules.Reporting.Infrastructure.Jobs.ReportJobService.RequestAsync` | bearer JWT | ExistingModify | `TC-API-EXP-002` | Main |
| getExportById | `GET /exports/{exportId}` | `Evidata.Modules.Reporting.Infrastructure.Jobs.ReportJobService.GetByIdAsync` | bearer JWT | ExistingModify | `TC-API-EXP-003` | Main |
| downloadExport | `GET /exports/{exportId}/download` | `Pendiente creación (T-P1-16)` | bearer JWT | New | `TC-API-EXP-004` | Release |
| getProcessingActivityTimeline | `GET /processing-activities/{processingActivityId}/timeline` | `Pendiente creación (T-P1-18)` | bearer JWT | New | `TC-API-TML-001` | Main |
| listProcessingActivityAuditEvents | `GET /processing-activities/{processingActivityId}/audit-events` | `Evidata.Modules.Audit.Api.AuditController.GetByResource` | bearer JWT | ExistingModify | `TC-API-AUD-001` | Main |

## Operaciones sin mapeo cerrado

- `activateProcessingActivity`: el contrato pide `activate`, pero el agregado `ProcessingActivity` hoy modela `Draft → UnderReview → Approved → Archived`; no existe un estado `Active` ni método equivalente.
- `validate/mark-insufficient/reject/replace` de evidencia: el módulo `Evidence` actual expone alta/listado y estados genéricos (`Activate`, `Archive`, `Supersede`), pero no una capa cerrada de `EvidenceValidation`; por eso sólo queda cerrado el backlog `T-P1-12/T-P1-13`.
- `dismissProcessingActivityGap`: `ComplianceGap` tiene `Resolve`, `AcceptRisk`, `Close`, `Block`, `StartProgress`, pero no un `Dismiss` explícito; falta decisión semántica.
- `rejectProcessingActivityReview` y `reopenProcessingActivityReview`: el agregado `Review` sólo soporta `Start`, `Approve`, `RequestChanges` y `Cancel`; rechazo y reapertura no existen hoy.
- `listProcessingActivityReviews`: el servicio actual devuelve revisiones abiertas por entidad (`GetOpenReviewsForEntityAsync`), no un listado contractual claramente cerrado para todos los estados.
- `downloadExport`: Reporting hoy entrega `artifactDocumentId`, pero no existe en los módulos inspeccionados un handler HTTP específico de descarga para `/exports/{exportId}/download`.
- Templates y timeline: no se encontraron controllers/handlers/clases runtime equivalentes en los módulos inspeccionados; quedan únicamente planificados por `T-P1-06` y `T-P1-18`.
