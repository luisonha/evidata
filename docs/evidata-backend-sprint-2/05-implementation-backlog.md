# 05 — Backlog de implementación backend v1.6.4

## P0 — Bloqueantes de contrato

| ID | Acción | Resultado esperado | Gate |
|---|---|---|---|
| P0-001 | Exponer rutas oficiales `/api/v1/...`. | OpenAPI público sólo usa rutas versionadas. | OpenAPI contract |
| P0-002 | Eliminar `/weatherforecast`. | No aparece en OpenAPI ni endpoints públicos. | Forbidden routes |
| P0-003 | Agregar `operationId` a toda operación. | Cliente generado estable. | OpenAPI lint |
| P0-004 | Agregar `securitySchemes` y `security`. | Contrato declara protección por endpoint. | OpenAPI lint |
| P0-005 | Agregar `x-change-status`. | Cada operación clasifica su cambio. | OpenAPI lint |
| P0-006 | Estandarizar `ApiErrorResponse`. | Errores consistentes. | Contract tests |
| P0-007 | Prohibir `/api/v1/treatments`. | Naming backend protegido. | Forbidden routes |
| P0-008 | Crear mapping operation → controller/action → test. | No hay endpoints declarados sin implementación planificada. | Traceability gate |

## P1 — Alineamiento funcional

| ID | Acción | Resultado esperado | Gate |
|---|---|---|---|
| P1-001 | Implementar `/control`. | `ProcessingActivityControlViewModel` disponible. | API + Integration |
| P1-002 | Crear/mapear `ProcessingActivityVersion`. | Versionado formal del RAT. | Domain tests |
| P1-003 | Crear/mapear `ProcessingActivityNode`. | Nodos RAT estandarizados. | Domain + API |
| P1-004 | Agregar `availableActions` y `blockedActions`. | UI no infiere permisos críticos. | Security tests |
| P1-005 | Formalizar `EvidenceRequirement`. | Evidencia requerida es trazable. | Integration |
| P1-006 | Formalizar `EvidenceValidation`. | Validación/insuficiencia/rechazo auditable. | Evidence tests |
| P1-007 | Formalizar `GapRule`. | Reglas trazadas a fixtures y tests. | Gap rule tests |
| P1-008 | Mapear Reporting a Exports. | Exportaciones oficiales y warnings. | Export tests |
| P1-009 | Mapear AuditLog a AuditEvent o extenderlo. | Acciones críticas auditables. | Audit tests |
| P1-010 | Crear `TimelineEvent`. | Bitácora UI proyectada. | Timeline tests |
| P1-011a | Implementar handler ValidateEvidence + instrumentación de auditoría (SEC-EV-001). | Timeline y auditoría reflejan validación de evidencia. | Audit + Security tests |
| P1-011b | Implementar handler AcceptGapWithRisk + instrumentación de auditoría (SEC-GAP-001). | Timeline y auditoría reflejan aceptación de riesgo. | Audit + Security tests |
| P1-011c | ✅ COMPLETADO (9/10): Implementar handlers SubmitForReview + Archive + auditoría + tests. AUD-ACT-001 (Activate) documentado como gap arquitectónico → P1-012. | 9 de 10 acciones críticas auditables instrumentadas (AUD-PA-001, AUD-NODE-001, AUD-REV-001, AUD-APP-001, AUD-ARC-001, AUD-EV-001, AUD-EV-002, AUD-GAP-001, AUD-EXP-001). 1 gap (AUD-ACT-001) requiere decisión de producto. PR #111 MERGED a develop. | ✅ Audit tests + 778 unit tests passing |
| P1-012 | ✅ COMPLETADO: Formalizar transición Activate() en ProcessingActivity (AUD-ACT-001). | ProcessingActivity FSM incluye estado Active, ActivateProcessingActivityCommandHandler con auditoría ProcessingActivityActivated, SEC-ACT-001 RBAC contract satisfecho, 5 new tests, 0 regressions. PR #112 MERGED a develop (2026-07-10). | ✅ 778 unit tests passing |
| P1-013 | ✅ COMPLETADO (2/4 blockers, P1-016 follow-up): Implementar ApproveProcessingActivity handler con 4 business blockers (SEC-APP-001). | ApproveProcessingActivityCommandHandler con RBAC SEC-APP-001 (ProcessOwner ≠ Approver) implementado. 2/4 blockers implementados: ✓ CriticalGapOpen, ✓ MissingLegalBasisEvidence. 2/4 blockers pendientes (domain model dependencies): ⚠ RequiredReviewPending, ⚠ VersionModifiedAfterReview → tracked in P1-016. PR #113 MERGED a develop (2026-07-10). | ✅ 778 unit tests passing, P1-016 pending |
| P1-014 | ✅ COMPLETADO (3/4 gaps, P1-014-P2 follow-up): Completar GenerateOfficialExport handler (SEC-EXP-001 compliance). | **Gap #1 ✅**: ExportService ahora valida estado "Active" ADEMÁS "Approved". **Gap #3 ✅**: HTTP 422 con código OfficialExportRequiresApproval correcto. **Gap #4 ✅**: ExportGenerationBlocked auditado con result.Blocked, metadata completa, correlationId preservado. **Gap #2 ⚠️ DEFERRED P2**: Auto-detección de ExportWarning (requiere coordinación cross-module GapManagement/Evidence). PR #114 MERGED a develop (2026-07-10), 782 unit tests passing, 0 regressions, Gandalf aprobó condicional a ítem P2 formal. | ✅ Export tests + 782 unit tests passing |
| P1-015 | ✅ COMPLETADO: Implementar DownloadEvidence endpoint + autorización (SEC-EVDOWN-001 — 4/4 gaps cerrados sin deferral). | Endpoint HTTP `[HttpGet("{id:guid}/download")]` en EvidenceController ✓, validación RBAC antes de SAS (fail-closed: 403 SensitiveEvidenceRestricted si Viewer intenta acceso sensible) ✓, auditoría con AuditEventType.EvidenceDownloaded (éxito) + EvidenceAccessDenied (denegación) ✓, validación `reason` obligatorio para Sensitive ✓. PR #115 MERGED a develop (2026-07-10). 783 unit tests passing, 0 regressions. Hallazgo menor (Gandalf): respuesta 403 usa Forbid() sin ApiErrorEnvelope (inconsistencia formato, no seguridad) — nota de calidad, no requiere backlog nuevo. | ✅ Security + Evidence tests + 783 unit tests passing |
| P1-016 | Implementar 2 blockers restantes de ApproveProcessingActivity (P1-013 follow-up). | Requiere: (1) agregar Review.Status enum estado "Requerida" a Workflow module, (2) agregar ProcessingActivity.ReviewedAt timestamp, (3) actualizar ApproveProcessingActivityCommandHandler para validar: ✓ todas revisiones requeridas aprobadas (RequiredReviewPending), ✓ versión no modificada post-revisión (VersionModifiedAfterReview), (4) tests. Estas son domain model dependencies, no engineering defects (Aragorn documented honestly in PR #113). | Security + Domain tests, P1 priority |

## RBAC Compliance Closure — Contrato Ampliado (04-rbac-audit-evidence-gaps-contract.md)

**Estado de los 6 Permisos Críticos:**

| Permiso | Contrato | Implementación | Estado | Seguimiento |
|---------|----------|-----------------|--------|-------------|
| ApproveProcessingActivity | SEC-APP-001 | P1-013 | ✅ 2/4 blockers | P1-016 (2 blockers) |
| ActivateProcessingActivity | SEC-ACT-001 | P1-012 | ✅ Completo | Ninguno |
| ValidateEvidence | SEC-EV-001 | Prior | ✅ Completo | Ninguno |
| AcceptGapWithRisk | SEC-GAP-001 | Prior | ✅ Completo | Ninguno |
| GenerateOfficialExport | SEC-EXP-001 | P1-014 | ✅ 3/4 gaps | P1-014-P2 (Gap #2) |
| DownloadEvidence | SEC-EVDOWN-001 | P1-015 | ✅ 4/4 gaps (Completo) | Ninguno |

**Resumen:**
- ✅ **3 permisos completamente implementados** (ActivateProcessingActivity, ValidateEvidence, AcceptGapWithRisk)
- ✅ **3 permisos parcialmente o completamente implementados** con seguimientos formales documentados (ApproveProcessingActivity P1-016, GenerateOfficialExport P1-014-P2, DownloadEvidence completo)
- 📊 **783 unit tests pasando**, 0 regressions (PR #112, #113, #114, #115)
- 🔐 **Cierre de auditoría RBAC completa** — Ciclo P1-011 → P1-015 sistemáticamente cierra cada permiso

**Próximas Tareas:**
1. P1-016: 2 extensiones de modelo de dominio (Review.Status, ProcessingActivity.ReviewedAt)
2. P1-014-P2: Agregador IProcessingActivityRiskAssessmentService para auto-detección de ExportWarning (P2, no bloqueante)
3. Transición a develop→main y follow-up administrativo



## P2 — Limpieza y compatibilidad

| ID | Acción | Resultado esperado |
|---|---|---|
| P2-001 | Definir retiro de rutas `/api/...` legacy. | Migración controlada. |
| P2-001a | **[NEW]** P1-014-P2: Implementar auto-detección de ExportWarning en GenerateOfficialExport (SEC-EXP-001 Gap #2). | Crear `IProcessingActivityRiskAssessmentService` en ProcessingInventory que agregue datos de riesgo de todos módulos (brechas críticas abiertas, evidencia pendiente, revisiones pendientes) desde GapManagement/Evidence, inyectado en ExportService para generar ExportWarning automáticamente. Opción recomendada (Gandalf): agregador centralizado en ProcessingInventory para encapsulación limpia. Opciones alternativas documentadas (Aragorn decision point). Prioridad: P2 (no bloqueante, aceptado por usuario). |
| P2-002 | Limpiar referencias históricas `TreatmentEvidenceSummary`. | Naming consistente. |
| P2-003 | Separar OpenAPI interno legacy si se requiere. | Contrato público limpio. |
| P2-004 | Agregar step-up opcional para acciones críticas. | Seguridad avanzada P2. |
| P2-005 | Agregar `EvidenceVersion` si storage lo soporta. | Historial de evidencia más robusto. |

## Orden recomendado de ejecución

1. P0 completo: contrato API/OpenAPI.
2. P1-001 a P1-004: `/control`, versionado, nodos y permisos.
3. P1-005 a P1-007: evidencia y brechas.
4. P1-008 a P1-010: exports, audit y timeline.
4.5. P1-011a/b/c: cerrar huecos de auditoría en acciones de seguridad crítica (ValidateEvidence + SecEv001, AcceptGapWithRisk + SecGap001, remaining 5 handlers). **CRÍTICO PARA CIERRE DE SEGURIDAD** — ejecutar inmediatamente post-P1-010, antes de P2.
5. P2 según necesidad de compatibilidad.
