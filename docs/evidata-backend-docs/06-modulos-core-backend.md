# 06 — Módulos core backend

## 1. Introducción

Este documento define los módulos que viven inicialmente dentro del core modular .NET 10. Cada módulo tiene límites, responsabilidades, roles, arquitectura y seguridad. El objetivo es permitir implementación modular ahora y extracción futura si se justifica.

---

# 2. Tenant Management

## 2.1 Definición

Administra las organizaciones clientes de ATLAS. Cada tenant representa una entidad aislada funcionalmente dentro del SaaS.

## 2.2 Alcance

Incluye:

- creación y administración de tenant;
- estado del tenant;
- configuración funcional;
- plan contratado;
- límites de uso;
- módulos habilitados;
- configuración de retención;
- configuración de seguridad por tenant.

No incluye:

- autenticación;
- billing detallado;
- RAT;
- evidencias;
- documentos;
- brechas.

## 2.3 Roles

| Rol | Responsabilidad |
|---|---|
| PlatformAdmin | Crear, suspender y administrar tenants. |
| TenantOwner | Administrar configuración de su organización. |
| TenantAdmin | Operar configuración delegada. |
| Auditor | Consultar configuración según permiso. |

## 2.4 Responsabilidades

- Resolver tenant actual.
- Validar tenant activo.
- Exponer configuración.
- Validar límites.
- Registrar cambios.
- Emitir eventos de configuración.

## 2.5 Límites

No conoce detalles internos de tratamientos, evidencias o brechas. Sólo provee contexto y configuración.

## 2.6 Arquitectura

Módulo core con persistencia propia. Otros módulos consumen contexto tenant mediante contrato interno, no por acceso directo a tablas.

## 2.7 Seguridad

- Validación de tenant en cada request.
- Prohibición cross-tenant.
- Auditoría de configuración.
- Separación PlatformAdmin/TenantAdmin.
- Alertas por tenant suspendido usado.

## 2.8 Infraestructura Azure

- API en App Service/Container Apps.
- PostgreSQL para configuración.
- App Insights para trazabilidad.
- Key Vault para configuración sensible.

## 2.9 Eventos

- TenantCreated.
- TenantActivated.
- TenantSuspended.
- TenantConfigurationChanged.
- TenantQuotaExceeded.

---

# 3. Identity Bridge / User Management

## 3.1 Definición

Conecta la identidad administrada externa con usuarios, roles y permisos internos de ATLAS.

## 3.2 Alcance

Incluye:

- usuarios internos;
- invitaciones;
- asociación usuario-tenant;
- roles funcionales;
- estado funcional del usuario;
- revocación lógica.

No incluye:

- contraseñas;
- MFA propio;
- recuperación de contraseña;
- emisión primaria de tokens.

## 3.3 Roles

- PlatformAdmin.
- TenantOwner.
- TenantAdmin.
- ComplianceAdmin.
- LegalReviewer.
- ProcessOwner.
- SecurityReviewer.
- ExternalConsultant.
- ReadOnlyAuditor.

## 3.4 Responsabilidades

- Validar identidad externa.
- Mapear usuario externo a usuario ATLAS.
- Gestionar invitaciones.
- Asignar roles por tenant.
- Revocar acceso funcional.
- Emitir eventos de usuario.

## 3.5 Límites

No decide permisos contextuales sobre entidades. Esa decisión corresponde a Security & Authorization.

## 3.6 Arquitectura

Módulo core integrado con Entra External ID o proveedor equivalente. Mantiene usuarios funcionales y roles internos.

## 3.7 Seguridad

- Validación de issuer y audience.
- Auditoría de invitaciones.
- Auditoría de roles.
- Bloqueo de usuarios deshabilitados.
- Mínimo privilegio.
- Expiración de invitaciones.

## 3.8 Infraestructura Azure

- Entra External ID.
- PostgreSQL para usuarios funcionales.
- Key Vault para configuración.
- App Insights para errores de identidad.

## 3.9 Eventos

- UserInvited.
- UserActivated.
- UserDisabled.
- UserRoleAssigned.
- UserRoleRemoved.
- UserAccessRevoked.

---

# 4. Security & Authorization

## 4.1 Definición

Módulo transversal que evalúa permisos, políticas y acceso contextual.

## 4.2 Alcance

Incluye:

- RBAC interno;
- permisos funcionales;
- políticas por dominio;
- tenant guard;
- auditoría de acciones sensibles;
- eventos de seguridad.

No incluye:

- autenticación primaria;
- secretos;
- SIEM avanzado;
- decisiones legales.

## 4.3 Roles

Todos los roles pasan por este módulo para evaluación de permisos.

## 4.4 Responsabilidades

- Deny by default.
- Evaluar usuario, rol, permiso, tenant, entidad y estado.
- Registrar accesos denegados.
- Proteger acciones sensibles.
- Emitir eventos de seguridad.

## 4.5 Límites

No decide cumplimiento legal. No reemplaza reglas de negocio del módulo, pero las protege.

## 4.6 Arquitectura

Core transversal. Todos los módulos lo consultan mediante contratos internos.

## 4.7 Seguridad

- Tenant guard obligatorio.
- Políticas por entidad.
- Auditoría de denegaciones.
- Logging de acciones sensibles.
- Protección contra escalamiento de privilegios.

## 4.8 Infraestructura Azure

- PostgreSQL para roles/permisos.
- App Insights para trazas.
- Azure Monitor para alertas.
- Storage Queues para security jobs.

## 4.9 Eventos

- AccessDenied.
- SensitiveActionPerformed.
- CrossTenantAccessAttempted.
- AdminRoleAssigned.

---

# 5. Legal Knowledge

## 5.1 Definición

Administra fuentes legales, obligaciones, derechos, bases, artículos y taxonomías.

## 5.2 Alcance

Incluye:

- fuentes legales;
- versiones;
- artículos;
- obligaciones;
- derechos del titular;
- bases de licitud;
- categorías de datos;
- categorías sensibles;
- gatillantes;
- taxonomías globales y tenant.

No incluye:

- asesoría legal autónoma;
- certificación de cumplimiento;
- modificación de tratamientos.

## 5.3 Roles

| Rol | Acción |
|---|---|
| PlatformAdmin | Mantener fuentes globales. |
| ComplianceAdmin | Consultar y usar taxonomías. |
| LegalReviewer | Consultar fuentes y obligaciones. |
| TenantAdmin | Extender taxonomías propias si se habilita. |

## 5.4 Responsabilidades

- Versionar fuentes.
- Mantener obligaciones estructuradas.
- Alimentar RAT, brechas, búsqueda y MCP.
- Evitar eliminación de fuentes usadas.

## 5.5 Límites

No modifica entidades de negocio; sólo entrega referencias y taxonomías.

## 5.6 Arquitectura

Módulo core, candidato a servicio futuro si crece multi-jurisdicción.

## 5.7 Seguridad

- Fuentes globales sólo administradas por plataforma.
- Versionado obligatorio.
- Auditoría de cambios.
- Separación entre fuente oficial y configuración tenant.

## 5.8 Infraestructura Azure

- PostgreSQL para fuentes estructuradas.
- Blob Storage para documentos fuente.
- Storage Queue para reindexación.
- Search Indexing Function para indexar corpus.

## 5.9 Eventos

- LegalSourceCreated.
- LegalSourceVersioned.
- LegalObligationCreated.
- LegalTaxonomyChanged.
- LegalSourcePublished.

---

# 6. Document Metadata

## 6.1 Definición

Administra metadata y versiones de documentos. El archivo físico vive en Blob Storage.

## 6.2 Alcance

Incluye:

- documento;
- versión;
- tipo;
- estado de procesamiento;
- hash;
- tamaño;
- ubicación Blob;
- propietario;
- clasificación;
- relación con evidencias.

No incluye:

- extracción de texto;
- OCR;
- embeddings;
- indexación.

## 6.3 Roles

- ComplianceAdmin.
- LegalReviewer.
- ProcessOwner.
- SecurityReviewer.
- ExternalConsultant.
- Auditor.

## 6.4 Responsabilidades

- Registrar documentos.
- Versionar.
- Controlar metadata.
- Emitir DocumentUploaded.
- Administrar estado de procesamiento.

## 6.5 Límites

No interpreta contenido ni decide suficiencia jurídica.

## 6.6 Arquitectura

Módulo core. Publica a Storage Queue vía Outbox para procesamiento documental.

## 6.7 Seguridad

- Blob privado.
- Tenant en metadata/ruta.
- Descarga auditada.
- Acceso con permisos.
- Eliminación lógica preferente.
- Hash por versión.

## 6.8 Infraestructura Azure

- PostgreSQL para metadata.
- Blob Storage para archivos.
- Storage Queue para procesamiento.
- App Insights para trazabilidad.

## 6.9 Eventos

- DocumentUploaded.
- DocumentVersionCreated.
- DocumentDeleted.
- DocumentClassified.
- DocumentDownloadRequested.

---

# 7. Evidence

## 7.1 Definición

Administra evidencias de cumplimiento asociadas a entidades del sistema.

## 7.2 Alcance

Incluye:

- creación de evidencia;
- clasificación;
- vínculo a entidades;
- estado;
- adjuntos;
- relación con documentos;
- evidencia de revisión/aprobación/cierre;
- solicitud de evidence pack.

No incluye:

- generación pesada de paquetes;
- procesamiento de archivos;
- decisión legal definitiva.

## 7.3 Roles

- ComplianceAdmin.
- LegalReviewer.
- ProcessOwner.
- SecurityReviewer.
- ExternalConsultant.
- Auditor.

## 7.4 Responsabilidades

- Registrar soporte probatorio.
- Asociar evidencia a tratamientos y brechas.
- Controlar acceso.
- Orquestar exportación.
- Mantener historial.

## 7.5 Límites

No declara suficiencia jurídica final. Puede medir completitud formal.

## 7.6 Arquitectura

Módulo core. Se integra con Documents, RAT, Gaps, Reporting y Audit.

## 7.7 Seguridad

- Permisos por entidad.
- Descarga auditada.
- Clasificación de sensibilidad.
- Eliminación lógica.
- Razón obligatoria para eliminar/reemplazar.
- Protección de evidence packs.

## 7.8 Infraestructura Azure

- PostgreSQL para metadata.
- Blob Storage para adjuntos.
- Storage Queue para generation pack.
- App Insights para accesos.

## 7.9 Eventos

- EvidenceCreated.
- EvidenceLinked.
- EvidenceUnlinked.
- EvidenceDownloaded.
- EvidencePackRequested.
- EvidenceDeleted.

---

# 8. Processing Inventory / RAT

## 8.1 Definición

Módulo central de Opción B. Administra actividades de tratamiento y su trazabilidad.

## 8.2 Alcance

Incluye:

- tratamiento;
- finalidad;
- base de licitud;
- categorías de datos;
- titulares;
- datos sensibles;
- NNA;
- biometría;
- sistemas;
- proveedores básicos;
- transferencias declaradas;
- retención;
- medidas de seguridad;
- responsables;
- evidencias;
- flags;
- revisión;
- versionado;
- exportación.

No incluye inicialmente:

- DSAR completo;
- consentimiento multicanal;
- incidentes;
- EIPD avanzada;
- gestión contractual completa;
- transferencias internacionales avanzadas.

## 8.3 Roles

| Rol | Responsabilidad |
|---|---|
| ProcessOwner | Levantar y mantener tratamientos. |
| ComplianceAdmin | Administrar inventario. |
| LegalReviewer | Revisar base legal y completitud. |
| SecurityReviewer | Revisar medidas de seguridad. |
| TenantOwner | Supervisar. |
| ExternalConsultant | Apoyar levantamiento. |
| Auditor | Consultar/exportar según permiso. |

## 8.4 Responsabilidades

- Inventariar tratamientos.
- Clasificar datos.
- Validar completitud.
- Versionar.
- Conectar evidencias.
- Derivar brechas.
- Alimentar búsqueda y MCP.

## 8.5 Reglas

- No aprobar sin finalidad.
- No aprobar sin base de licitud.
- Datos sensibles activan revisión.
- NNA activa revisión.
- Biometría activa revisión.
- Transferencia internacional activa warning.
- Decisión automatizada activa warning.
- Cambio post-aprobación crea nueva versión.
- Aprobación genera auditoría.
- Todo tratamiento requiere owner funcional.

## 8.6 Estados

- Draft.
- InReview.
- ChangesRequested.
- Approved.
- Active.
- Deprecated.
- Archived.

## 8.7 Límites

No responde solicitudes de titulares. No captura consentimientos reales. No certifica cumplimiento.

## 8.8 Arquitectura

Core modular. No se separa como microservicio en MVP. Es el principal dueño del dominio de inventario.

## 8.9 Seguridad

- Permisos por estado.
- Edición restringida de aprobados.
- Snapshot aprobado.
- Auditoría de cambios.
- Protección de tratamientos sensibles.
- Exportación auditada.

## 8.10 Infraestructura Azure

- PostgreSQL para datos transaccionales.
- Blob Storage vía evidencias.
- Storage Queue para reportes e indexación.
- App Insights para trazabilidad.

## 8.11 Eventos

- ProcessingActivityCreated.
- ProcessingActivitySubmitted.
- ProcessingActivityApproved.
- ProcessingActivityChangesRequested.
- ProcessingActivityArchived.
- ProcessingActivityRiskFlagged.
- ProcessingActivityVersionCreated.

---

# 9. Workflow

## 9.1 Definición

Módulo transversal de tareas, comentarios, revisiones, aprobaciones y transiciones.

## 9.2 Alcance

Incluye:

- tareas;
- comentarios;
- asignaciones;
- revisiones;
- aprobaciones;
- devoluciones;
- deadlines;
- historial.

No incluye:

- reglas específicas de RAT o Gaps;
- envío físico de correo;
- autorización primaria.

## 9.3 Roles

- ProcessOwner.
- LegalReviewer.
- SecurityReviewer.
- ComplianceAdmin.
- TenantOwner.

## 9.4 Responsabilidades

- Coordinar revisiones.
- Registrar decisiones.
- Controlar transiciones.
- Emitir notificaciones.

## 9.5 Límites

No decide reglas de negocio específicas; las recibe de cada módulo.

## 9.6 Arquitectura

Core transversal para RAT, Gaps y futuros DSAR/EIPD.

## 9.7 Seguridad

- Sólo usuarios asignados actúan.
- Permisos por estado.
- Comentarios auditados.
- Control de visibilidad para externos.

## 9.8 Eventos

- TaskAssigned.
- TaskCompleted.
- ReviewRequested.
- ReviewCompleted.
- ApprovalGranted.
- ChangesRequested.

---

# 10. Gap Management

## 10.1 Definición

Administra brechas, remediaciones y aceptación de riesgo.

## 10.2 Alcance

Incluye:

- brechas;
- severidad;
- responsable;
- fecha objetivo;
- estado;
- relación con tratamiento;
- evidencia de cierre;
- aceptación de riesgo;
- reporte de avance.

No incluye:

- gestión de proyectos avanzada;
- integración Jira/Azure DevOps;
- remediación automática.

## 10.3 Roles

- ComplianceAdmin.
- ProcessOwner.
- LegalReviewer.
- SecurityReviewer.
- TenantOwner.
- Auditor.

## 10.4 Responsabilidades

- Registrar hallazgos.
- Priorizar.
- Asignar.
- Controlar avance.
- Cerrar con evidencia.

## 10.5 Estados

- Open.
- Assigned.
- InProgress.
- Blocked.
- Resolved.
- AcceptedRisk.
- Closed.

## 10.6 Límites

No modifica directamente el tratamiento. Las correcciones ocurren en RAT.

## 10.7 Arquitectura

Core modular, consume eventos del RAT y Legal Knowledge.

## 10.8 Seguridad

- Cierre exige evidencia.
- Aceptación de riesgo requiere rol elevado.
- Historial de cambios.
- Alertas por brechas críticas vencidas.

## 10.9 Eventos

- GapCreated.
- GapAssigned.
- GapSeverityChanged.
- GapResolved.
- GapClosed.
- GapAcceptedRisk.
- GapOverdue.

---

# 11. Reporting Orchestration

## 11.1 Definición

Orquesta solicitudes de reportes y evidence packs.

## 11.2 Alcance

Incluye:

- solicitud de reporte;
- job;
- estado;
- parámetros;
- permisos;
- descarga;
- historial.

No incluye:

- render pesado;
- generación de PDF/Excel;
- transformación intensiva.

## 11.3 Roles

- ComplianceAdmin.
- LegalReviewer.
- TenantOwner.
- Auditor.

## 11.4 Responsabilidades

- Crear jobs.
- Validar permisos.
- Publicar a cola.
- Registrar resultado.
- Controlar descarga.

## 11.5 Seguridad

- Reportes sensibles requieren permiso elevado.
- Descarga auditada.
- Enlaces temporales.
- Tenant isolation.

## 11.6 Eventos

- ReportRequested.
- ReportCompleted.
- ReportFailed.
- ReportDownloaded.

---

# 12. Search Query

## 12.1 Definición

Expone búsquedas legales e internas al frontend y MCP.

## 12.2 Alcance

Incluye:

- búsqueda legal;
- búsqueda en documentos procesados;
- búsqueda en tratamientos;
- búsqueda en evidencias autorizadas;
- filtros;
- referencias;
- historial.

No incluye:

- indexación;
- embeddings batch;
- OCR;
- generación IA.

## 12.3 Roles

Roles con permisos de consulta según recurso.

## 12.4 Arquitectura

Core para consultas interactivas. Search Indexing Function mantiene índices.

## 12.5 Seguridad

- Filtro tenant obligatorio.
- No mostrar documentos sin permiso.
- Auditoría de búsquedas sensibles.
- Control sobre evidencias sensibles.

---

# 13. MCP Interactive

## 13.1 Definición

Asistente contextual de ATLAS. Se implementa al final de Opción B.

## 13.2 Alcance

Incluye:

- preguntas legales generales;
- preguntas sobre tratamientos;
- preguntas sobre brechas;
- preguntas sobre evidencias faltantes;
- respuestas con citas;
- clasificación de riesgo;
- abstención;
- revisión humana;
- feedback.

No incluye:

- certificación de cumplimiento;
- asesoría legal autónoma;
- aprobación automática;
- modificación automática del RAT.

## 13.3 Roles

- ComplianceAdmin.
- LegalReviewer.
- TenantOwner.
- ExternalConsultant con permisos limitados.

## 13.4 Responsabilidades

- Responder con fuentes.
- Minimizar contexto.
- Clasificar riesgo.
- Registrar interacción.
- Derivar a revisión humana.

## 13.5 Límites

El core debe funcionar sin MCP. MCP no debe ser dependencia obligatoria de RAT.

## 13.6 Arquitectura

Core modular inicial. Jobs batch en Function. Candidato futuro a microservicio si escala.

## 13.7 Seguridad

- Permiso explícito para contexto tenant.
- No responder sin fuente en consultas normativas.
- Registrar fuentes.
- Proteger datos sensibles.
- No entrenar con datos tenant sin autorización.
- Alertar alto riesgo.

## 13.8 Eventos

- McpInteractionCreated.
- McpHighRiskQueryCreated.
- McpHumanReviewRequested.
- McpFeedbackSubmitted.
- McpCitationVerificationFailed.

---

# 14. Audit

## 14.1 Definición

Módulo transversal de auditoría funcional y técnica.

## 14.2 Alcance

Incluye:

- acciones de usuario;
- cambios de estado;
- accesos sensibles;
- descargas;
- exportaciones;
- aprobaciones;
- consultas MCP;
- eventos de seguridad.

No incluye:

- SIEM enterprise completo;
- análisis avanzado de amenazas.

## 14.3 Responsabilidades

- Registrar eventos auditables.
- Permitir consulta autorizada.
- Exportar auditoría si se habilita.
- Mantener correlación.

## 14.4 Seguridad

- Append-only o equivalente.
- Acceso restringido.
- Exportación auditada.
- Retención configurable.
- No registrar datos innecesarios.

## 14.5 Infraestructura

- PostgreSQL para audit logs.
- App Insights para telemetría técnica.
- Blob Storage para exportaciones.
- Storage Queues para jobs de exportación.

---

## Trazabilidad de fuentes

Este documento debe leerse con trazabilidad explícita a las dos fuentes entregadas:

- `ley-datos-personales.pdf`: aporta el fundamento normativo-operativo de la Ley 21.719, los elementos computables, los patrones de workflow, evidencia, DSAR, consentimiento, incidentes, EIPD, RAT funcional, transferencias y decisiones automatizadas.
- `ATLAS-Informe-Factibilidad.pdf`: aporta la recomendación de producto, la priorización de Opción B, los riesgos comerciales/técnicos, la arquitectura recomendada, el enfoque de implementación, los costos y la decisión de no construir una suite completa desde el inicio.

Las decisiones técnicas de este documento no deben interpretarse como exigencias legales directas. Cuando una decisión sea una recomendación de arquitectura, se declara como decisión del proyecto y no como mandato normativo.
